using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using Vegetarian.API.Extensions;
using Vegetarian.Infrastructure.Data;
using Vegetarian.Infrastructure.Options;
using Vegetarian.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddExtensions();

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection("EmailOptions"));

builder.Services.Configure<GoogleAuthOptions>(
    builder.Configuration.GetSection("GoogleAuthOptions"));

builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection("CloudinaryOptions"));

builder.Services.Configure<PayOsOptions>(
    builder.Configuration.GetSection("PayOsOptions"));

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
    .AddCookie()
    .AddGoogle((opts) =>
    {
        var googleOptions = builder.Configuration
          .GetSection("GoogleAuthOptions")
          .Get<GoogleAuthOptions>();

        opts.ClientId = googleOptions.ClientId;
        opts.ClientSecret = googleOptions.ClientSecret;
        opts.CallbackPath = "/signin-google";
        opts.SaveTokens = true;

        opts.Scope.Add("profile");
        opts.Scope.Add("email");

        opts.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        opts.ClaimActions.MapJsonKey("picture", "picture");
    });


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
await app.ApplyMigrationsAsync();
await app.SeedAsync();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/hubs/notification");
app.MapControllers();

app.Run();
