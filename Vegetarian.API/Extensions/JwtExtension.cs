using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.API.Extensions
{
    public static class JwtExtension
    {
        public static IServiceCollection AddJwtConfig(this IServiceCollection services)
        {
            Env.Load();
            // Cấu hình mật khẩu
            services.Configure<IdentityOptions>(opts =>
            {
                opts.Password.RequireDigit = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequiredLength = 6;
                opts.Password.RequireNonAlphanumeric = false;

                opts.User.RequireUniqueEmail = true;
                opts.User.AllowedUserNameCharacters = null;
                //"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -._@+ ";
            });

            // Cấu hình authentication
            services
            .AddAuthentication(config =>
            {
                config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                config.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(config =>
            {
                config.SaveToken = true;
                config.RequireHttpsMetadata = false;
                config.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,  // Kiểm tra người phát hành Token
                    ValidateAudience = true, // Kiểm tra người nhận Token
                    ValidateIssuerSigningKey = true, // Kiểm tra chữ ký Token
                    ValidIssuer = Env.GetString("ISSUER"), // Chỉ chấp nhận token được phát hành bởi ISSUER
                    ValidAudience = Env.GetString("AUDIENCE"), // Chỉ chấp nhận token phát cho AUDIENCE
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Env.GetString("SECRET_KEY")))
                };

                config.Events = new JwtBearerEvents
                {
                    OnMessageReceived = conntext =>
                    {
                        var accessToken = conntext.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken) && conntext.HttpContext.Request.Path.StartsWithSegments("/hubs/notification"))
                        {
                            conntext.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
            return services;
        }
    }
}
