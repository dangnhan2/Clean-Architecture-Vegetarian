using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vegetarian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangePaymentMethodToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderDate",
                table: "Order",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Order",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Order",
                newName: "OrderDate");
        }
    }
}
