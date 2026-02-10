using Microsoft.EntityFrameworkCore;
using Serilog;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.API.Extensions
{
    public static class MigrationExtension
    {
        public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
        {
            try
            {
                using IServiceScope scope = app.ApplicationServices.CreateScope();

                using VegetarianDbContext dbContext = scope.ServiceProvider.GetRequiredService<VegetarianDbContext>();

                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw;
            }

        }
    }
}
