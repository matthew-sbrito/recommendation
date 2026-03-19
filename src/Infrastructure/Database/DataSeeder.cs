using Application.Abstractions.Authentication;
using Application.Abstractions.Services;
using Domain.Categories;
using Domain.Orders;
using Domain.Products;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class DataSeeder(
    ApplicationDbContext context,
    IProductEmbeddingService embeddingService,
    IPasswordHasher passwordHasher,
    ILogger<DataSeeder> logger)
{
    private const int EmbeddingDimensions = 1024;
    private const string DefaultPassword = "Password123!";

    // ── Seed IDs ────────────────────────────────────────────────────────────

    private static class CategoryIds
    {
        public static readonly Guid Electronics = new("00000001-0000-0000-0000-000000000001");
        public static readonly Guid Clothing = new("00000001-0000-0000-0000-000000000002");
        public static readonly Guid Books = new("00000001-0000-0000-0000-000000000003");
        public static readonly Guid Sports = new("00000001-0000-0000-0000-000000000004");
        public static readonly Guid HomeKitchen = new("00000001-0000-0000-0000-000000000005");
        public static readonly Guid Beauty = new("00000001-0000-0000-0000-000000000006");
        public static readonly Guid ToysGames = new("00000001-0000-0000-0000-000000000007");
        public static readonly Guid Food = new("00000001-0000-0000-0000-000000000008");
    }

    // ── Entry point ─────────────────────────────────────────────────────────

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Categories.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded. Skipping.");
            return;
        }

        logger.LogInformation("Starting database seed...");

        List<Category> categories = SeedCategories();
        await context.Categories.AddRangeAsync(categories, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        List<Product> products = await SeedProductsAsync(categories, cancellationToken);
        await context.Products.AddRangeAsync(products, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        List<User> users = SeedUsers();
        await context.Users.AddRangeAsync(users, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        List<Order> orders = SeedOrders(products);
        await context.Orders.AddRangeAsync(orders, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seed complete: {Categories} categories, {Products} products, {Users} users, {Orders} orders.",
            categories.Count, products.Count, users.Count, orders.Count);
    }

    // ── Categories ───────────────────────────────────────────────────────────

    private static List<Category> SeedCategories() =>
    [
        new() { Id = CategoryIds.Electronics, Name = "Electronics",         Description = "Consumer electronics, gadgets, and tech accessories." },
        new() { Id = CategoryIds.Clothing,    Name = "Clothing & Apparel",  Description = "Fashion, clothing, and accessories for all occasions." },
        new() { Id = CategoryIds.Books,       Name = "Books",               Description = "Fiction, non-fiction, textbooks, and educational materials." },
        new() { Id = CategoryIds.Sports,      Name = "Sports & Outdoors",   Description = "Equipment and gear for sports, fitness, and outdoor activities." },
        new() { Id = CategoryIds.HomeKitchen, Name = "Home & Kitchen",      Description = "Appliances, cookware, and home essentials." },
        new() { Id = CategoryIds.Beauty,      Name = "Beauty & Personal Care", Description = "Skincare, makeup, haircare, and grooming products." },
        new() { Id = CategoryIds.ToysGames,   Name = "Toys & Games",        Description = "Board games, toys, puzzles, and entertainment for all ages." },
        new() { Id = CategoryIds.Food,        Name = "Food & Beverages",    Description = "Specialty foods, snacks, beverages, and pantry essentials." },
    ];

    // ── Products ─────────────────────────────────────────────────────────────

    private sealed record ProductSeed(string Name, string Description, decimal Price);

    private static readonly Dictionary<Guid, ProductSeed[]> ProductData = new()
    {
        [CategoryIds.Electronics] =
        [
            new("Wireless Noise-Cancelling Headphones", "Premium over-ear headphones with active noise cancellation, 30h battery, and Hi-Res audio support.", 149.99m),
            new("Smart 4K TV 55\"", "55-inch 4K UHD Smart TV with HDR10+, built-in streaming apps, and voice control.", 499.99m),
            new("Mechanical Gaming Keyboard", "Tenkeyless RGB mechanical keyboard with Cherry MX switches and N-key rollover.", 89.99m),
            new("USB-C Hub 7-in-1", "Multi-port hub with 4K HDMI, 100W PD, 3x USB-A, SD/MicroSD card reader.", 49.99m),
            new("Portable Bluetooth Speaker", "Waterproof IPX7 speaker with 360° sound, 20h battery, and built-in microphone.", 79.99m),
            new("4K Webcam with Autofocus", "Ultra HD webcam with AI-powered autofocus, built-in stereo microphone, and low-light correction.", 119.99m),
            new("Wireless Gaming Mouse", "Ergonomic wireless gaming mouse with 25600 DPI sensor, 70h battery, and RGB lighting.", 69.99m),
            new("Smart Watch Pro", "Fitness smartwatch with GPS, heart rate monitoring, SpO2 sensor, and 7-day battery life.", 199.99m),
            new("Adjustable Laptop Stand", "Aluminium ergonomic laptop stand with 6 height levels, compatible with 10\"–17\" laptops.", 39.99m),
            new("65W GaN USB-C Charger", "Compact 65W GaN charger with 3 ports (2x USB-C, 1x USB-A) and foldable prongs.", 44.99m),
        ],
        [CategoryIds.Clothing] =
        [
            new("Classic Slim-Fit Denim Jeans", "Premium stretch denim jeans with a modern slim fit, available in multiple washes.", 59.99m),
            new("Merino Wool Crew Sweater", "Soft 100% merino wool sweater, machine-washable and odor-resistant.", 89.99m),
            new("Men's Running Shoes Pro", "Lightweight responsive running shoes with breathable mesh upper and cushioned midsole.", 129.99m),
            new("Women's Leather Biker Jacket", "Genuine leather jacket with quilted shoulders, zip pockets, and slim silhouette.", 189.99m),
            new("Cotton Crew-Neck T-Shirt Pack (3)", "Set of 3 premium 100% cotton t-shirts in classic colors.", 34.99m),
            new("Women's High-Waist Yoga Leggings", "4-way stretch leggings with moisture-wicking fabric and hidden waistband pocket.", 49.99m),
            new("Waterproof Rain Jacket", "Packable rain jacket with sealed seams, adjustable hood, and pit-zip ventilation.", 99.99m),
            new("Casual Canvas Sneakers", "Minimalist low-top canvas sneakers with vulcanized rubber sole and padded collar.", 44.99m),
            new("Men's Oxford Dress Shirt", "Wrinkle-resistant Oxford cotton dress shirt with button-down collar.", 54.99m),
            new("Fleece Zip-Up Hoodie", "Warm anti-pill fleece hoodie with kangaroo pocket and YKK zipper.", 64.99m),
        ],
        [CategoryIds.Books] =
        [
            new("Clean Code", "A Handbook of Agile Software Craftsmanship by Robert C. Martin. Learn to write readable, maintainable code.", 34.99m),
            new("Atomic Habits", "An Easy & Proven Way to Build Good Habits & Break Bad Ones by James Clear.", 17.99m),
            new("The Great Gatsby", "F. Scott Fitzgerald's classic novel of the Jazz Age, wealth, and the American dream.", 12.99m),
            new("Sapiens: A Brief History of Humankind", "Yuval Noah Harari's exploration of the history and impact of Homo Sapiens on the world.", 18.99m),
            new("Design Patterns: GoF", "Elements of Reusable Object-Oriented Software — the foundational software design patterns book.", 44.99m),
            new("The Pragmatic Programmer", "20th Anniversary Edition by David Thomas & Andrew Hunt — timeless software engineering advice.", 49.99m),
            new("Zero to One", "Peter Thiel's notes on startups and how to build companies that create new things.", 15.99m),
            new("The Psychology of Money", "Morgan Housel on timeless lessons about wealth, greed, and happiness.", 16.99m),
            new("Dune", "Frank Herbert's epic science fiction masterpiece set in a distant future of politics, religion, and ecology.", 14.99m),
            new("Educated: A Memoir", "Tara Westover's extraordinary account of growing up in a survivalist family in rural Idaho.", 16.99m),
        ],
        [CategoryIds.Sports] =
        [
            new("Premium Yoga Mat 6mm", "Extra-thick non-slip yoga mat with alignment lines, carrying strap, and sweat-resistant surface.", 39.99m),
            new("Adjustable Dumbbell Set (5–52.5 lbs)", "Space-saving adjustable dumbbells that replace 15 sets with a quick-adjust dial mechanism.", 299.99m),
            new("Resistance Bands Kit (5 levels)", "Set of 5 fabric resistance bands for strength training, stretching, and physical therapy.", 24.99m),
            new("Road Cycling Helmet", "MIPS-certified aerodynamic road helmet with 18 vents, adjustable fit system, and visor.", 89.99m),
            new("Tennis Racket Pro Series", "Professional 300g tennis racket with graphite frame, 16x19 string pattern, and grip size 4 3/8.", 109.99m),
            new("GPS Running Watch", "Advanced running watch with GPS, VO2 max, cadence, and training load metrics.", 249.99m),
            new("Official Size Basketball", "Indoor/outdoor composite leather basketball with deep channel design for superior grip.", 39.99m),
            new("Pull-Up Bar Doorway", "No-screw doorframe pull-up bar with multiple grip positions, supports up to 300 lbs.", 29.99m),
            new("High-Density Foam Roller", "36\" extra-firm foam roller for deep tissue massage and muscle recovery.", 19.99m),
            new("Hiking Backpack 45L", "Internal frame backpack with hydration sleeve, hip-belt pockets, and rain cover.", 119.99m),
        ],
        [CategoryIds.HomeKitchen] =
        [
            new("Air Fryer 5.8QT", "Large-capacity digital air fryer with 8 presets, touch panel, and dishwasher-safe basket.", 89.99m),
            new("Pre-Seasoned Cast Iron Skillet 12\"", "Professional-grade cast iron skillet for stovetop, oven, and campfire use.", 34.99m),
            new("Programmable Coffee Maker 12-Cup", "Auto-brew coffee maker with built-in grinder, thermal carafe, and brewing strength control.", 129.99m),
            new("Instant Pot Duo 8QT", "7-in-1 multi-cooker: pressure cooker, slow cooker, rice cooker, steamer, sauté, yogurt maker.", 99.99m),
            new("Professional Knife Set 15-Piece", "German high-carbon stainless steel knives with full-tang blades and wooden block.", 149.99m),
            new("Stand Mixer 5QT", "550W tilt-head stand mixer with 10 speeds, flat beater, dough hook, and wire whisk.", 199.99m),
            new("Robot Vacuum Cleaner", "Smart robot vacuum with LiDAR navigation, app control, HEPA filter, and auto-empty base.", 349.99m),
            new("Sous Vide Precision Cooker", "1100W immersion circulator with Wi-Fi control, temperature accuracy ±0.1°C, up to 20L.", 79.99m),
            new("Non-Stick Cookware Set 10-Piece", "PFOA-free hard-anodized cookware set with glass lids, oven-safe to 400°F.", 119.99m),
            new("Digital Kitchen Scale", "High-precision kitchen scale with 0.1g accuracy, tare function, and 11lb capacity.", 14.99m),
        ],
        [CategoryIds.Beauty] =
        [
            new("Vitamin C Brightening Serum", "20% stabilized vitamin C serum with hyaluronic acid and ferulic acid for radiant skin.", 34.99m),
            new("Hyaluronic Acid Moisturizer", "Oil-free daily moisturizer with 3 types of hyaluronic acid for 72h hydration.", 29.99m),
            new("Electric Toothbrush Pro", "Sonic electric toothbrush with 5 modes, UV sanitizing case, and 2-min smart timer.", 69.99m),
            new("Professional Ionic Hair Dryer", "2200W ionic hair dryer with diffuser, concentrator, and cold-shot button.", 79.99m),
            new("Retinol Eye Cream", "Advanced retinol + peptide eye cream for dark circles, puffiness, and fine lines.", 39.99m),
            new("Beard Grooming Kit", "Complete beard kit with trimmer, boar bristle brush, balm, oil, and stainless steel scissors.", 44.99m),
            new("SPF 50 Mineral Sunscreen", "Reef-safe, non-comedogenic broad-spectrum SPF 50 sunscreen with zinc oxide.", 24.99m),
            new("Gentle Hydrating Facial Cleanser", "pH-balanced creamy cleanser for all skin types, removes makeup and impurities without stripping.", 19.99m),
            new("Argan Oil Hair Treatment", "Moroccan argan oil serum for frizz control, shine, and heat protection up to 450°F.", 22.99m),
            new("Badger Shaving Brush & Stand Set", "Pure badger hair shaving brush with chrome stand for classic wet-shaving routine.", 34.99m),
        ],
        [CategoryIds.ToysGames] =
        [
            new("Catan Board Game", "Classic strategy board game for 3–4 players: trade, build, and settle the island of Catan.", 44.99m),
            new("LEGO Architecture Skyline Set", "710-piece LEGO set building iconic city skylines — perfect for teens and adults.", 59.99m),
            new("Wooden Chess Set Premium", "Staunton-style weighted wooden chess pieces with folding board and storage.", 49.99m),
            new("1000-Piece Landscape Puzzle", "High-quality jigsaw puzzle with a scenic mountain lake panoramic design.", 19.99m),
            new("Magnetic Drawing Board", "Erasable doodle board with stampers and magnetic drawing pen — mess-free creativity.", 14.99m),
            new("RC Off-Road Racing Car", "1:16 scale 4WD remote-control car with 25 MPH speed, suspension, and rechargeable battery.", 49.99m),
            new("Uno Card Game", "Classic Uno card game with 112 cards — fast-paced fun for 2–10 players.", 8.99m),
            new("Giant Jenga Outdoor Set", "Large-format Jenga set with 54 solid pine blocks and carry bag — indoor/outdoor.", 39.99m),
            new("Speed Cube 3x3", "Competition-grade 3x3 magnetic speed cube with corner-cutting and smooth rotation.", 12.99m),
            new("Science Experiment Kit for Kids", "STEM kit with 30+ experiments in chemistry, physics, and biology for ages 8–14.", 34.99m),
        ],
        [CategoryIds.Food] =
        [
            new("Organic Green Tea (50 bags)", "Premium Japanese organic green tea bags, lightly grassy with a fresh finish.", 14.99m),
            new("Dark Chocolate Variety Box", "Assortment of 24 premium single-origin dark chocolates (70%–90% cacao).", 29.99m),
            new("Himalayan Pink Salt Grinder", "Natural coarse-grain Himalayan pink salt in a refillable adjustable ceramic grinder.", 9.99m),
            new("Natural Almond Butter 16oz", "Stone-ground creamy almond butter with no added sugar, salt, or oil.", 12.99m),
            new("Extra Virgin Olive Oil 750ml", "Cold-first-pressed Greek EVOO with robust flavor, PDO certified.", 18.99m),
            new("Specialty Coffee Beans 1kg", "Single-origin Ethiopian Yirgacheffe whole-bean coffee with fruity, floral notes.", 24.99m),
            new("Mixed Nuts Premium Snack Pack", "Variety pack of roasted almonds, cashews, pecans, walnuts, and macadamias — 2lb.", 22.99m),
            new("Artisan Hot Sauce Collection", "Set of 6 small-batch hot sauces ranging from mild chipotle to extreme ghost pepper.", 34.99m),
            new("Whey Protein Powder Vanilla 2lb", "25g protein per serving, cold-processed whey concentrate with no artificial sweeteners.", 39.99m),
            new("Ceremonial Grade Matcha 50g", "Japanese ceremonial matcha powder with a bright green color and umami-rich taste.", 24.99m),
        ],
    };

    private async Task<List<Product>> SeedProductsAsync(
        List<Category> categories,
        CancellationToken cancellationToken)
    {
        var categoryMap = categories.ToDictionary(c => c.Id);
        var products = new List<Product>();
        int productIndex = 1;

        foreach ((Guid categoryId, ProductSeed[] seeds) in ProductData)
        {
            Category category = categoryMap[categoryId];

            foreach (ProductSeed seed in seeds)
            {
                var product = new Product
                {
                    Id = new Guid($"00000002-0000-0000-0000-{productIndex:D12}"),
                    Name = seed.Name,
                    Description = seed.Description,
                    Price = Money.Create(seed.Price).Value,
                    CategoryId = categoryId,
                    Category = category,
                    CreatedAt = DateTime.UtcNow,
                    Embedding = new Vector(new float[EmbeddingDimensions])
                };

                Result<float[]> embeddingResult = await embeddingService.GenerateEmbeddingAsync(product, cancellationToken);

                if (embeddingResult.IsSuccess)
                {
                    product.Embedding = new Vector(embeddingResult.Value);
                    logger.LogInformation("Generated embedding for: {Product}", product.Name);
                }
                else
                {
                    product.Embedding = GenerateRandomEmbedding();
                    logger.LogWarning("Embedding failed for '{Product}', using random vector. Error: {Error}",
                        product.Name, embeddingResult.Error.Description);
                }

                product.Category = null!; // clear nav prop before insert to avoid tracking issues
                products.Add(product);
                productIndex++;
            }
        }

        return products;
    }

    private static Vector GenerateRandomEmbedding()
    {
        float[] values = new float[EmbeddingDimensions];
        float magnitude = 0f;

        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            byte[] bytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            values[i] = BitConverter.ToInt32(bytes) / (float)int.MaxValue;
            magnitude += values[i] * values[i];
        }

        magnitude = MathF.Sqrt(magnitude);
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            values[i] /= magnitude;
        }

        return new Vector(values);
    }

    // ── Users ────────────────────────────────────────────────────────────────

    private sealed record UserSeed(Guid Id, string Email, string FirstName, string LastName, Gender Gender, DateOnly BirthDate);

    private static readonly UserSeed[] Users =
    [
        new(new Guid("00000003-0000-0000-0000-000000000001"), "carlos.silva@test.com",   "Carlos",   "Silva",     Gender.Male,   new DateOnly(1997, 3, 15)),
        new(new Guid("00000003-0000-0000-0000-000000000002"), "ana.costa@test.com",      "Ana",      "Costa",     Gender.Female, new DateOnly(1992, 7, 22)),
        new(new Guid("00000003-0000-0000-0000-000000000003"), "pedro.santos@test.com",   "Pedro",    "Santos",    Gender.Male,   new DateOnly(1990, 11, 5)),
        new(new Guid("00000003-0000-0000-0000-000000000004"), "julia.lima@test.com",     "Julia",    "Lima",      Gender.Female, new DateOnly(1998, 1, 30)),
        new(new Guid("00000003-0000-0000-0000-000000000005"), "lucas.oliveira@test.com", "Lucas",    "Oliveira",  Gender.Male,   new DateOnly(1995, 6, 10)),
        new(new Guid("00000003-0000-0000-0000-000000000006"), "mariana.rocha@test.com",  "Mariana",  "Rocha",     Gender.Female, new DateOnly(1988, 9, 14)),
        new(new Guid("00000003-0000-0000-0000-000000000007"), "gabriel.alves@test.com",  "Gabriel",  "Alves",     Gender.Male,   new DateOnly(1996, 4, 20)),
        new(new Guid("00000003-0000-0000-0000-000000000008"), "beatriz.nunes@test.com",  "Beatriz",  "Nunes",     Gender.Female, new DateOnly(1993, 12, 3)),
        new(new Guid("00000003-0000-0000-0000-000000000009"), "alex.ferreira@test.com",  "Alex",     "Ferreira",  Gender.Other,  new DateOnly(1991, 8, 18)),
        new(new Guid("00000003-0000-0000-0000-000000000010"), "rafael.souza@test.com",   "Rafael",   "Souza",     Gender.Male,   new DateOnly(2001, 5, 25)),
    ];

    private List<User> SeedUsers() =>
        Users.Select(u => new User
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Gender = u.Gender,
            BirthDate = u.BirthDate,
            PasswordHash = passwordHasher.Hash(DefaultPassword)
        }).ToList();

    // ── Orders ───────────────────────────────────────────────────────────────

    private static List<Order> SeedOrders(List<Product> products)
    {
        var byName = products.ToDictionary(p => p.Name);
        var orders = new List<Order>();
        int orderIndex = 1;

        Order MakeOrder(Guid userId, (string Name, int Qty)[] items)
        {
            var orderItems = items.Select(i =>
            {
                Product product = byName[i.Name];
                return new OrderItem
                {
                    Id = new Guid($"00000005-0000-0000-0000-{orderIndex:D12}"),
                    ProductId = product.Id,
                    Quantity = i.Qty,
                    UnitPrice = product.Price.Amount
                };
            }).ToList();

            var order = new Order
            {
                Id = new Guid($"00000004-0000-0000-0000-{orderIndex:D12}"),
                UserId = userId,
                CreatedAt = DateTime.UtcNow.AddDays(-orderIndex * 3),
                Items = orderItems,
                TotalAmount = orderItems.Sum(i => i.Quantity * i.UnitPrice)
            };

            orderIndex++;
            return order;
        }

        // Carlos — tech buyer (Electronics)
        orders.Add(MakeOrder(Users[0].Id,
        [
            ("Wireless Noise-Cancelling Headphones", 1),
            ("Mechanical Gaming Keyboard", 1),
            ("Wireless Gaming Mouse", 1),
        ]));
        orders.Add(MakeOrder(Users[0].Id,
        [
            ("USB-C Hub 7-in-1", 1),
            ("65W GaN USB-C Charger", 2),
        ]));

        // Ana — beauty + home buyer
        orders.Add(MakeOrder(Users[1].Id,
        [
            ("Vitamin C Brightening Serum", 1),
            ("Hyaluronic Acid Moisturizer", 1),
            ("Retinol Eye Cream", 1),
        ]));
        orders.Add(MakeOrder(Users[1].Id,
        [
            ("Programmable Coffee Maker 12-Cup", 1),
            ("Digital Kitchen Scale", 1),
        ]));

        // Pedro — books + toys
        orders.Add(MakeOrder(Users[2].Id,
        [
            ("Clean Code", 1),
            ("The Pragmatic Programmer", 1),
            ("Design Patterns: GoF", 1),
        ]));
        orders.Add(MakeOrder(Users[2].Id,
        [
            ("Wooden Chess Set Premium", 1),
            ("Speed Cube 3x3", 2),
        ]));

        // Julia — fitness + clothing
        orders.Add(MakeOrder(Users[3].Id,
        [
            ("Women's High-Waist Yoga Leggings", 2),
            ("Men's Running Shoes Pro", 1),
            ("Premium Yoga Mat 6mm", 1),
        ]));
        orders.Add(MakeOrder(Users[3].Id,
        [
            ("Adjustable Dumbbell Set (5–52.5 lbs)", 1),
            ("Resistance Bands Kit (5 levels)", 1),
        ]));

        // Lucas — food + mixed
        orders.Add(MakeOrder(Users[4].Id,
        [
            ("Specialty Coffee Beans 1kg", 2),
            ("Organic Green Tea (50 bags)", 1),
            ("Ceremonial Grade Matcha 50g", 1),
        ]));
        orders.Add(MakeOrder(Users[4].Id,
        [
            ("Whey Protein Powder Vanilla 2lb", 1),
            ("Mixed Nuts Premium Snack Pack", 2),
            ("Artisan Hot Sauce Collection", 1),
        ]));

        return orders;
    }
}
