using CloudinaryDotNet;
using DotNetEnv;
using Hangfire;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Principal;
using Vegetarian.Application;
using Vegetarian.Application.Implements.Auth;
using Vegetarian.Application.Implements.Caching;
using Vegetarian.Application.Implements.External_Service;
using Vegetarian.Application.Implements.Hangfire;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Application.Repositories;
using Vegetarian.Application.Services.External_Service;
using Vegetarian.Infrastructure;
using Vegetarian.Infrastructure.Options;
using Vegetarian.Infrastructure.Repositories;
using Vegetarian.Infrastructure.Services.Caching;
using Vegetarian.Infrastructure.Services.Email;
using Vegetarian.Infrastructure.Services.Storage;
using Vegetarian.Infrastructure.Services.Token;

namespace Vegetarian.API.Extensions
{
    public static class DIExtension
    {
        public static IServiceCollection AddDI(this IServiceCollection services)
        {
            Env.Load();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepo<>), typeof(GenericRepo<>));
            services.AddScoped<IUserRepo, UserRepo>();
            services.AddScoped<IEmailOtpRepo, EmailOtpRepo>();
            services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IHangfireService, HangfireService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICategoryRepo, CategoryRepo>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMenuRepo, MenuRepo>();
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<ICartRepo, CartRepo>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IVoucherRepo, VoucherRepo>();
            services.AddScoped<IVoucherRedemptionRepo, VoucherRedemptionRepo>();
            services.AddScoped<IVoucherService, VoucherService>();

            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<ICloudinaryService, CloudinaryService>();

            services.AddSingleton<ICachingService, CachingService>();
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = $"{Env.GetString("REDIS")},abortConnect=false";
                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddSingleton<Cloudinary>(cd =>
            {
                var options = cd.GetRequiredService<IOptions<CloudinaryOptions>>().Value;

                var account = new Account(
                     options.CloudName,
                     options.ApiKey,
                     options.ApiSecret
                    );

                return new Cloudinary(account);
            });

            //services.AddSingleton<PayOS>(p =>
            //{
            //    var options = p.GetRequiredService<IOptions<PayOsOptions>>().Value;

            //    var account = new PayOS(
            //         options.ClientId,
            //         options.ApiKey,
            //         options.ChecksumKey
            //        );

            //    return account;
            //});

            //services.AddSingleton<IDistributedLockFactory>(sp =>
            //{
            //    var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            //    return RedLockFactory.Create(new List<RedLockMultiplexer>
            //    {
            //        new RedLockMultiplexer(multiplexer)
            //    });
            //});

            return services;
        }
    }
}
