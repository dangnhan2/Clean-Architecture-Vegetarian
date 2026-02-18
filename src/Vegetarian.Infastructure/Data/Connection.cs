using DotNetEnv;
using Hangfire;
using Hangfire.Logging;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Infrastructure.Data
{
    public static class Connection
    {
        public static void AddConnection(this IServiceCollection services)
        {
            Env.Load();
            // Connect to Db
            var cacheString = Env.GetString("REDIS");

            services.AddDbContext<VegetarianDbContext>(otps =>
            {
                otps.UseNpgsql(Env.GetString("DefaultConnection"));
            });

            //Log.Information(Env.GetString("DefaultConnection"));
            //Log.Information(Env.GetString("REDIS"));

            //Connect to HangfireDb
            services.AddHangfire(otps => otps
             .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
             .UseSimpleAssemblyNameTypeSerializer()
             .UseRecommendedSerializerSettings()
             .UsePostgreSqlStorage(Env.GetString("DefaultConnection"))
            );

            //Connect to Redis
            services.AddStackExchangeRedisCache(opt =>
            {
                opt.Configuration = cacheString;
            });
        }
    }
}
