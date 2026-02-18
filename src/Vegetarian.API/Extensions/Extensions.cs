using Hangfire;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.API.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddExtensions(this IServiceCollection services)
        {
            services.AddIdentity();
            services.AddJwtConfig();
            services.AddDI();
            services.AddSwaggerConfigure();
            services.AddConnection();
            services.AddSignalR();
            return services;
        }
    }
}
