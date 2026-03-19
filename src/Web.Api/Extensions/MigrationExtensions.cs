using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public static async Task SeedDataAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        DataSeeder seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

        await seeder.SeedAsync();
    }
}
