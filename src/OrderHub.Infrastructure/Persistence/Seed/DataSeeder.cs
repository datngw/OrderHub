using OrderHub.Application.Common.Security;
using OrderHub.Domain.Products;
using OrderHub.Domain.Users;

namespace OrderHub.Infrastructure.Persistence.Seed;

public sealed class DataSeeder(IPasswordHasher passwordHasher)
{
    private static readonly string[] Categories =
        ["Electronics", "Clothing", "Books", "Home & Garden", "Sports", "Toys", "Food", "Automotive", "Health", "Music"];

    private static readonly string[] ProductTemplates =
    [
        "iPhone {0} Pro Max", "Samsung Galaxy S{0}", "MacBook Air M{0}", "Dell XPS {0}",
        "Sony WH-1000XM{0}", "iPad Pro {0} inch", "AirPods Pro {0}", "LG OLED C{0} Series",
        "Nintendo Switch {0}", "PlayStation {0} Slim", "Xbox Series X {0}", "Bose QuietComfort {0}",
        "Canon EOS R{0}", "DJI Mavic {0} Pro", "Apple Watch Ultra {0}", "Surface Pro {0}",
        "Google Pixel {0} Pro", "OnePlus {0}", "ThinkPad X1 Carbon Gen {0}", "Kindle Paperwhite {0}",
        "Sony Alpha {0}", "GoPro Hero {0}", "Bose SoundLink {0}", "JBL Charge {0}",
        "Logitech MX Master {0}", "Razer DeathAdder {0}", "Samsung QLED {0}", "LG gram {0}",
        "ASUS ROG {0}", "MSI Stealth {0}", "HP Spectre x{0}", "Lenovo Yoga {0}",
        "Dell UltraSharp {0}", "BenQ PD {0}", "Samsung Galaxy Buds {0}", "Marshall Stanmore {0}",
        "Garmin Forerunner {0}", "Fitbit Versa {0}", "Dyson V{0}", "Roborock S{0}",
        "Nespresso Vertuo {0}", "KitchenAid Artisan {0}", "Le Creuset Dutch Oven {0}",
        "Yeti Rambler {0}", "Stanley Quencher {0}", "Nike Air Max {0}", "Adidas Ultraboost {0}",
        "North Face Puffer {0}", "Patagonia Better Sweater {0}", "Ray-Ban Aviator {0}"
    ];

    public void Seed(OrderHubDbContext context)
    {
        SeedUsers(context);
        SeedProducts(context);
    }

    private void SeedUsers(OrderHubDbContext context)
    {
        if (context.Users.Any()) return;

        var admin = new User
        {
            Email = "admin@orderhub.com",
            PasswordHash = passwordHasher.HashPassword("Admin@123"),
            FullName = "System Admin",
            Phone = "0900000001",
            Role = UserRoleEnum.Admin
        };

        var customer = new User
        {
            Email = "customer@orderhub.com",
            PasswordHash = passwordHasher.HashPassword("Customer@123"),
            FullName = "John Doe",
            Phone = "0900000002",
            Role = UserRoleEnum.Customer
        };

        context.Users.AddRange(admin, customer);
        context.SaveChanges();
    }

    private void SeedProducts(OrderHubDbContext context)
    {
        if (context.Products.Any()) return;

        const int totalProducts = 10_000;
        var products = new List<Product>(totalProducts);
        var random = new Random(42);

        for (int i = 1; i <= totalProducts; i++)
        {
            var template = ProductTemplates[i % ProductTemplates.Length];
            var variant = (i / ProductTemplates.Length) + 1;
            var category = Categories[i % Categories.Length];

            products.Add(new Product
            {
                SKU = $"SKU-{i:D5}",
                Name = string.Format(template, variant),
                Description = $"High-quality {category.ToLowerInvariant()} product. {string.Format(template, variant)} — reliable performance with modern design.",
                Price = Math.Round((decimal)(random.NextDouble() * 49_891_000) + 99_000m) / 1000m * 1000m,
                Stock = random.Next(0, 500),
                Category = category
            });

            // Save in batches to avoid excessive memory usage
            if (i % 2000 == 0)
            {
                context.Products.AddRange(products);
                context.SaveChanges();
                products.Clear();
            }
        }

        // Save remaining products
        if (products.Count > 0)
        {
            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }
}
