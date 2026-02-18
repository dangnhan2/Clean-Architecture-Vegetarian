using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.Tasks;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.API.Extensions
{
    public static class MigrationExtension
    {
        public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();
            using VegetarianDbContext dbContext = scope.ServiceProvider
                      .GetRequiredService<VegetarianDbContext>();

            var retries = 3;
            while (retries > 0)
            {
                try
                {
                    Console.WriteLine("Applying migrations...");
                    await dbContext.Database.MigrateAsync();
                    Console.WriteLine("✅ Migrations applied successfully.");
                    return; // ✅ thành công thì thoát
                }
                catch (Exception ex)
                {
                    retries--;
                    Console.WriteLine($"❌ Migration failed ({retries} retries left): {ex.Message}");
                    if (retries == 0) throw; // ✅ ném lại exception thay vì nuốt
                    await Task.Delay(3000);
                }
            }
        }
    }
}
