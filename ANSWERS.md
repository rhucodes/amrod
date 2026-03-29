# Amrod E-Commerce Developer Assessment — Answers

## 1. Architecture & Integration Reasoning

### Stack Choices

- **ASP.NET Core MVC (.NET 10)** — chosen to satisfy the assessment requirement for MVC + Razor views while keeping the project in a single, cohesive solution
- **PostgreSQL + Entity Framework Core** — relational database with EF Core as the ORM. PostgreSQL was chosen for its robustness, JSON support, and production-readiness
- **Vanilla JS** — no frontend framework overhead. The assessment required demonstrating API integration so JavaScript fetch calls to our own WebAPI endpoints satisfy both the MVC and API integration requirements cleanly
- **PayFast** — South Africa's leading payment gateway, integrated via their standard checkout flow with ITN webhook for payment confirmation

### Project Structure

A deliberate single project approach was chosen over a multi project solution:

- One `dotnet run` starts everything
- MVC controllers serve Razor views
- API controllers (`/api/*`) serve JSON to the frontend JS
- Both live in the same project, share the same DI container, models and services
- Tests folder lives inside the same project

### Data Flow

```
Browser > MVC Controller > Razor View (initial HTML)
Browser JS > API Controller > Service > EF Core > PostgreSQL
Browser > PayFast (payment form POST)
PayFast > /api/payfast/notify (ITN webhook) > Order status update
```

### API Design

RESTful endpoints following standard conventions:

- `GET /api/products` — supports query string filtering (`search`, `minPrice`, `maxPrice`, `inStock`, `sortBy`, `sortOrder`)
- `POST /api/orders` — atomically creates order, decrements stock, returns order confirmation
- `POST /api/customers` — idempotent: returns existing customer if email already exists
- `PUT /api/orders/{id}/status` — used by ITN webhook to update order status
- `POST /api/payfast/notify` — ITN webhook endpoint

---

## 2. Data Accuracy & System Stability

### Stock Management

- Stock is validated **before** the order is created, if any item has insufficient stock the entire order is rejected with a descriptive error message
- Stock is decremented **atomically** in the same `SaveChangesAsync` call that creates the order and order items, no partial updates are possible
- Stock can never go below 0 — the validation check `product.Stock < item.Quantity` prevents this
- Products with `Stock = 0` are displayed as "Out of stock" in the UI and cannot be added to cart

### Price Accuracy

- Prices are stored as `decimal` with `HasPrecision(18, 2)` in EF Core, no floating point rounding errors
- `unit_price` is snapshotted on `OrderItem` at the time of purchase, if a product price changes later, historical orders remain accurate
- Amount formatting for PayFast uses `CultureInfo.InvariantCulture` to guarantee `.` as the decimal separator regardless of server locale, this was a real bug discovered during development (see Challenges section)
- Products with `Price = 0` are displayed as "POA" (Price on Application) and cannot be added to cart

### Validation

- All API inputs are validated with Data Annotations (`[Required]`, `[Range]`, `[EmailAddress]`, `[MaxLength]`)
- `ModelState.IsValid` is checked on every POST/PUT endpoint before processing
- Price filter inputs in the UI are validated as numeric before the API call is made
- Email format is validated both client-side (regex) and server-side (`[EmailAddress]`)

### DTOs

- API responses use DTOs (`OrderDto`, `CustomerDto`, `ProductDto`, `OrderItemDto`) rather than returning EF entities directly
- This prevents circular reference serialization errors (e.g. `Order > Customer > Orders > Customer...`) and controls exactly what data is exposed

---

## 3. Challenges Faced & Solutions Applied

### Challenge 1: PayFast Signature Mismatch

**Problem**: After implementing PayFast checkout, the sandbox returned `Generated signature does not match submitted signature`.

**Debugging process**:

1. Added a `/api/payfast/debug/{orderId}` endpoint to expose the raw string being hashed before it was sent to PayFast
2. Added `_logger.LogInformation` calls throughout the ITN notify endpoint to log every field PayFast sent back
3. Discovered `amount` was formatting as `119,99` on the development machine instead of the required `119.99`
4. Fixed by using `order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)`
5. ITN signature was still failing — discovered PayFast sends back many more fields than we sent (e.g. `pf_payment_id`, `amount_gross`, `amount_fee`, `custom_str1-5`, `custom_int1-5`) and all must be included in the signature
6. Referenced PayFast's open source C# implementation (`PayFastNotify.GetCalculatedSignature`) to align our `UrlEncode` character map and hash logic exactly
7. Key findings: use `Encoding.ASCII` not `Encoding.UTF8`, use PayFast's character-by-character `UrlEncode` not `Uri.EscapeDataString`, skip `billing_date` and `token` when empty, URL encode ITN values before hashing

**Resolution**: Rebuilt `ValidateSignature` as a direct port of PayFast's reference implementation.

### Challenge 2: EF Core Circular Reference Serialization

**Problem**: `POST /api/orders` was returning HTTP 500 with `A possible object cycle was detected` because `Order` > `Customer` > `Orders` > `Customer` created an infinite loop during JSON serialization.

**Solution**: Introduced DTOs for all API responses. Rather than serializing EF entities directly, we map to flat DTO objects that contain only the data the client needs, with no navigation property cycles.

### Challenge 3: xUnit in MVC Project

**Problem**: Adding xUnit test packages to an MVC project caused a build warning about conflicting entry points (`AutoGeneratedProgram.Main` vs the MVC startup).

**Solution**: Added `<GenerateProgramFile>false</GenerateProgramFile>` to the test-related `ItemGroup` in the `.csproj` to suppress the conflict while keeping tests in the same project.

### Challenge 4: PostgreSQL Volume Conflict

**Problem**: On first Docker Compose run, PostgreSQL failed to start due to an existing incompatible data volume from a previous installation.

**Solution**: Ran `docker compose down -v` to remove the named volume entirely, then `docker compose up -d` to recreate it cleanly.

---

## 4. CI/CD & Deployment

### Local Quality Gates — Husky

Rather than a remote CI/CD pipeline, this project enforces quality checks locally using Husky git hooks before any code reaches the repository:

- `pre-commit` — runs `dotnet build` and `dotnet test` on every commit. If either fails the commit is blocked entirely
- `commit-msg` — enforces conventional commit message format (`feat`, `fix`, `docs`, `test`, `chore`) keeping the repository history clean and readable for reviewers
- `pre-push` - ensures that the app builds successfully before push the commits to remote repo

This guarantees no broken builds or failing tests can be pushed to the repository.

### Deployment — Coolify

The application is deployed to a self-hosted Coolify instance and accessible at [amrod.darhu.co.za](https://amrod.darhu.co.za). Coolify watches the GitHub repository and triggers a redeploy on every push to `main`.

The deployment flow:
```
Push to GitHub (main)
        v
Coolify detects push via webhook
        v
Coolify builds the Dockerfile (multi-stage — SDK build + ASP.NET runtime)
        v
Container starts with environment variables injected by Coolify
        v
Program.cs runs MigrateAsync() on startup — database migrated automatically
        v
SeedData runs if Products table is empty
        v
Docker HEALTHCHECK polls /health endpoint every 30s
        v
Traefik reverse proxy routes public traffic to the container
```

### Environment Configuration

All secrets are stored as environment variables in Coolify — nothing sensitive is committed to source control. ASP.NET Core reads them automatically via `WebApplication.CreateBuilder` which calls `AddEnvironmentVariables()` internally. Coolify injects them as OS-level environment variables using Docker's `-e` flag at container startup — the same mechanism as any platform (Railway, Heroku, AWS ECS).

The double underscore maps nested JSON structure:
```bash
# Set in Coolify UI
PayFast__MerchantId=your-merchant-id
ConnectionStrings__DefaultConnection=Host=postgres-service;...

# ASP.NET Core reads as
{ "PayFast": { "MerchantId": "your-merchant-id" } }
{ "ConnectionStrings": { "DefaultConnection": "Host=postgres-service;..." } }
```

No `appsettings.Production.json` file is needed or committed, `appsettings.json` provides the base structure and Coolify environment variables override the values at runtime.

### Infrastructure

| Component | Technology |
|-----------|------------|
| Server | VPS running Coolify |
| Reverse proxy | Traefik (managed by Coolify) |
| Database | PostgreSQL resource on same Coolify server |
| Container port | 8082 |
