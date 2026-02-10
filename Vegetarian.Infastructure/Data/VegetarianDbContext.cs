using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vegetarian.Domain.Models;

namespace Vegetarian.Infrastructure.Data
{
    public class VegetarianDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public VegetarianDbContext(DbContextOptions<VegetarianDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Order { get; set; }
        public DbSet<Menu> Menu { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Voucher> Voucher { get; set; }
        public DbSet<VoucherRedemption> VoucherRedemption { get; set; }
        public DbSet<RefreshToken> RefreshToken { get; set; }
        public DbSet<OrderMenus> OrderMenu { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<CartItem> CartItem { get; set; }
        public DbSet<EmailOtp> EmailOtp { get; set; }
        public DbSet<Address> Address { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<Rating> Rating { get; set; }
        public DbSet<Advertisement> Advertisement { get; set; }
        public DbSet<ResponseRating> ResponseRating { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.RelationshipConfiguration();
        }
    }
}
