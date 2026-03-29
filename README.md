# Amrod E-Commerce Assessment

A full-stack e-commerce web application built with ASP.NET Core MVC (.NET 10.0), PostgreSQL, and PayFast payment integration.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [ngrok](https://ngrok.com/) — required for PayFast ITN webhook in development

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd AmrodAssessment
```

### 2. Start PostgreSQL with Docker

```bash
docker compose up -d
```

### 3. Expose local server for PayFast ITN

PayFast needs a public URL to send payment notifications. In a new terminal:

```bash
ngrok http 5212
```

Copy the ngrok HTTPS URL (e.g. `https://abc123.ngrok.io`)

### 4. Configure appsettings

Update `appsettings.json` with your ngrok URL:

```json
"PayFast": {
  "NotifyUrl": "https://abc123.ngrok.io/api/payfast/notify"
}
```

### 5. Run the application

```bash
dotnet run
```

The application will:

- Apply any pending EF Core migrations automatically on startup
- Seed the database with sample products if empty
- Start listening on `http://localhost:5212`

### 6. Open in browser

```
http://localhost:5212
```

## Running Tests

```bash
dotnet test
```

## API Endpoints

| Method | Endpoint                          | Description                                                                         |
| ------ | --------------------------------- | ----------------------------------------------------------------------------------- |
| GET    | `/api/products`                   | List products (supports `?search=&minPrice=&maxPrice=&inStock=&sortBy=&sortOrder=`) |
| GET    | `/api/products/{id}`              | Single product                                                                      |
| POST   | `/api/products`                   | Create product                                                                      |
| PUT    | `/api/products/{id}`              | Update product                                                                      |
| DELETE | `/api/products/{id}`              | Delete product                                                                      |
| GET    | `/api/customers/{id}`             | Get customer                                                                        |
| POST   | `/api/customers`                  | Create customer (idempotent by email)                                               |
| GET    | `/api/orders`                     | List all orders                                                                     |
| GET    | `/api/orders/{id}`                | Single order with items                                                             |
| POST   | `/api/orders`                     | Place order (decrements stock)                                                      |
| PUT    | `/api/orders/{id}/status`         | Update order status                                                                 |
| GET    | `/api/payfast/initiate/{orderId}` | Get PayFast payment data                                                            |
| POST   | `/api/payfast/notify`             | PayFast ITN webhook                                                                 |

## PayFast Test Credentials (Sandbox)

| Field        | Value            |
| ------------ | ---------------- |
| Merchant ID  | 10000100         |
| Merchant Key | 46f0cd694581a    |
| Card Number  | 4000000000000002 |

## Environment Variables (Production)

In production, override these via environment variables or a secrets manager:

```bash
ConnectionStrings__DefaultConnection=Host=...
PayFast__MerchantId=...
PayFast__MerchantKey=...
PayFast__PassPhrase=...
PayFast__NotifyUrl=https://yourdomain.com/api/payfast/notify
```
