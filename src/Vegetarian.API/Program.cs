using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Security.Claims;
using Vegetarian.API.Extensions;
using Vegetarian.API.Middleware;
using Vegetarian.Infrastructure.Data;
using Vegetarian.Infrastructure.Options;
using Vegetarian.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddExtensions();

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

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
builder.Services.AddAuthorization();

builder.Services.AddCors(o =>
{
    o.AddPolicy("Cors",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000", "https://fe.test.ac.vn/")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        });
});

builder.Services.AddHangfireServer();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
await app.ApplyMigrationsAsync();
await app.SeedAsync();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = 
    ForwardedHeaders.XForwardedFor
    | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseCors("Cors");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<GlobalException>();
app.UseRecurringJobs();
app.MapHub<NotificationHub>("/hubs/notification");
app.MapControllers();

app.Run();
