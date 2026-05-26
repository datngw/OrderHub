using OrderHub.Domain.Products;
using OrderHub.Domain.Users;

namespace OrderHub.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    private static readonly string[] Categories =
        ["Electronics", "Clothing", "Books", "Home & Garden", "Sports", "Toys", "Food", "Automotive", "Health", "Music"];

    public static void Seed(OrderHubDbContext context)
    {
        if (context.Users.Any()) return;

        var admin = new User
        {
            Email = "admin@orderhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName = "System Admin",
            Role = UserRole.Admin
        };

        var customer = new User
        {
            Email = "customer@orderhub.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
            FullName = "John Doe",
            Role = UserRole.Customer
        };

        context.Users.AddRange(admin, customer);
        context.SaveChanges();

        var products = new List<Product>();
        var random = new Random(42);

        for (int i = 1; i <= 100; i++)
        {
            products.Add(new Product
            {
                SKU = $"SKU-{i:D5}",
                Name = $"Product {i}",
                Description = $"Description for product {i}. A quality item in the {Categories[i % Categories.Length]} category.",
                Price = Math.Round((decimal)(random.NextDouble() * 990 + 10), 2),
                Stock = random.Next(0, 500),
                Category = Categories[i % Categories.Length]
            });
        }

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
