using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Domain.Models;

namespace Vegetarian.Infrastructure.Data
{
    public static class Configuration
    {
        public static ModelBuilder RelationshipConfiguration(this ModelBuilder builder)
        {
            builder.Entity<VoucherRedemption>()
                .HasKey(vr => new
                {
                    vr.UserID,
                    vr.VoucherID
                });

            builder.Entity<OrderMenus>()
                .HasKey(om => new
                {
                    om.OrderId,
                    om.MenuId
                });

            builder.Entity<CartItem>()
                .HasKey(ct => new { ct.MenuId, ct.CartId });

            builder.Entity<CartItem>()
                .HasOne(ct => ct.Menu)
                .WithMany(ct => ct.CartItems)
                .HasForeignKey(ct => ct.MenuId);

            builder.Entity<CartItem>()
                .HasOne(ct => ct.Cart)
                .WithMany(ct => ct.CartItems)
                .HasForeignKey(ct => ct.CartId);

            builder.Entity<VoucherRedemption>()
                .HasOne(vr => vr.User)
                .WithMany(vr => vr.VoucherRedemptions)
                .HasForeignKey(vr => vr.UserID);

            builder.Entity<VoucherRedemption>()
                .HasOne(vr => vr.Voucher)
                .WithMany(vr => vr.VoucherRedemptions)
                .HasForeignKey(vr => vr.VoucherID);

            builder.Entity<VoucherRedemption>()
                .Property(x => x.VoucherRedemptionStatus)
                .HasConversion<string>();

            builder.Entity<Order>()
                .Property(x => x.Status)
                .HasConversion<string>();

            builder.Entity<Advertisement>()
                .Property(a => a.AdTargetType)
                .HasConversion<string>();

            builder.Entity<OrderMenus>()
                .HasOne(om => om.Menus)
                .WithMany(om => om.OrderMenus)
                .HasForeignKey(om => om.MenuId);

            builder.Entity<OrderMenus>()
                .HasOne(om => om.Orders)
                .WithMany(om => om.OrderMenus)
                .HasForeignKey(om => om.OrderId);

            builder.Entity<Category>()
                .HasMany(c => c.Menus)
                .WithOne(c => c.Category)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasIndex(r => new { r.Stars });

            builder.HasPostgresExtension("unaccent");

            builder.Entity<Menu>()
                .HasIndex(m => new { m.Name })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("vi_unaccent");

            builder.Entity<Voucher>()
                .HasIndex(v => new { v.Code })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("vi_unaccent");

            builder.Entity<Voucher>()
                .HasIndex(v => new { v.StartDate, v.EndDate });

            builder.Entity<User>()
                .HasIndex(u => new { u.UserName, u.PhoneNumber, u.Email })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("vi_unaccent");

            return builder;
        }
    }
}
