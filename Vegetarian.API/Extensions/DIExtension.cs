using CloudinaryDotNet;
using DotNetEnv;
using Hangfire;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Net.payOS;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System.Security.Principal;
using Vegetarian.Application;
using Vegetarian.Application.Abstractions.BackgroundJobs;
using Vegetarian.Application.Abstractions.Caching;
using Vegetarian.Application.Abstractions.Email;
using Vegetarian.Application.Abstractions.Notifications;
using Vegetarian.Application.Abstractions.Payment;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Application.Abstractions.Storage;
using Vegetarian.Application.Abstractions.Token;
using Vegetarian.Application.Implements.Interface;
using Vegetarian.Application.Implements.Services;
using Vegetarian.Infrastructure;
using Vegetarian.Infrastructure.Options;
using Vegetarian.Infrastructure.Repositories;
using Vegetarian.Infrastructure.Services.BackgroundJobs;
using Vegetarian.Infrastructure.Services.Caching;
using Vegetarian.Infrastructure.Services.Email;
using Vegetarian.Infrastructure.Services.Notifications;
using Vegetarian.Infrastructure.Services.PayOs;
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
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IHangfireJobClient, HangfireJobClient>();
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
            services.AddScoped<IOrderRepo, OrderRepo>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IJobs, Jobs>();
            services.AddScoped<IPaymentGateway, PaymentGateway>();
            services.AddScoped<INotificationRepo, NotificationRepo>();
            services.AddScoped<INotificationSender, NotificationSender>();
            services.AddScoped<IHangfireJobClient, HangfireJobClient>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAddressRepo, AddressRepo>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IRatingRepo, RatingRepo>();
            services.AddScoped<IRatingService, RatingService>();
            services.AddScoped<IResponseRatingRepo, ResponseRatingRepo>();
            services.AddScoped<IOrderMenuRepo, OrderMenuRepo>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAdvertisementRepo, AdvertisementRepo>();
            services.AddScoped<IAdvertisementService, AdvertisementService>();

            services.AddTransient<IEmailSender, EmailSender>();
            services.AddTransient<ICloudinaryStorage, CloudinaryStorage>();

            services.AddSingleton<ICachingProvider, CachingProvider>();
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

            services.AddSingleton<PayOS>(p =>
            {
                var options = p.GetRequiredService<IOptions<PayOsOptions>>().Value;

                var account = new PayOS(
                     options.ClientId,
                     options.ApiKey,
                     options.ChecksumKey
                    );

                return account;
            });

            services.AddSingleton<IDistributedLockFactory>(sp =>
            {
                var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                return RedLockFactory.Create(new List<RedLockMultiplexer>
                {
                    new RedLockMultiplexer(multiplexer)
                });
            });

            return services;
        }
    }
}
