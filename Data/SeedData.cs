using AmrodAssessment.Models;
using Microsoft.Extensions.Configuration;

namespace AmrodAssessment.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        if (db.Products.Any()) return;

        var baseUrl = config["AppBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5212";

        var products = new List<Product>
        {
            new() {
                Name        = "Swiss Cougar Toledo Anti-Theft Laptop Backpack",
                Description = "Keep your valuables safe and secure with this minimalist design, modern anti-theft laptop backpack.",
                Price       = 699.99m,
                Stock       = 500,
                ImageUrl    = $"{baseUrl}/images/products/backpack-laptop.jpg"
            },
            new() {
                Name        = "Okiyo Miyag Mini Jute Gift Bag",
                Description = "A reusable alternative to disposable gift bags, made from natural and sustainable jute material, with cotton rope handles.",
                Price       = 279.99m,
                Stock       = 350,
                ImageUrl    = $"{baseUrl}/images/products/jute-gift-bag.jpg"
            },
            new() {
                Name        = "Mens Lando Bodywarmer",
                Description = "Introducing our Lando Bodywarmer – the perfect balance between warmth and mobility.",
                Price       = 499.99m,
                Stock       = 200,
                ImageUrl    = $"{baseUrl}/images/products/bodywarmer-lando.jpg"
            },
            new() {
                Name        = "Altitude Events Sublimation Satin Wristband",
                Description = "One-time-use polyester wristband that can be branded in full colour...",
                Price       = 749.99m,
                Stock       = 120,
                ImageUrl    = $"{baseUrl}/images/products/wristband-satin.jpg"
            },
            new() {
                Name        = "Altitude Panama Pencil",
                Description = "Aluminium mechanical pencil with a shiny anodised, coloured barrel and shiny chrome accents.",
                Price       = 189.99m,
                Stock       = 400,
                ImageUrl    = $"{baseUrl}/images/products/pencil-panama.jpg"
            },
            new() {
                Name        = "Altitude Boxter XOXO Game Coaster",
                Description = "This clever tic-tac-toe or noughts & crosses game functions as a drinks coaster too.",
                Price       = 159.99m,
                Stock       = 300,
                ImageUrl    = $"{baseUrl}/images/products/game-coaster.jpg"
            },
        };

        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();
    }
}