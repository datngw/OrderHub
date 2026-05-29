using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeProductQueryPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Name_Category_Price",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_Category_CreatedAt",
                table: "Products",
                columns: new[] { "IsActive", "Category", "CreatedAt" },
                descending: new[] { false, false, true })
                .Annotation("Npgsql:IndexInclude", new[] { "Id", "SKU", "Name", "Description", "Price", "Stock" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_CreatedAt",
                table: "Products",
                columns: new[] { "IsActive", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("Npgsql:IndexInclude", new[] { "Id", "SKU", "Name", "Description", "Price", "Stock", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_Price",
                table: "Products",
                columns: new[] { "IsActive", "Price" })
                .Annotation("Npgsql:IndexInclude", new[] { "Id", "SKU", "Name", "Description", "Stock", "Category", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_Category_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_Price",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name_Category_Price",
                table: "Products",
                columns: new[] { "Name", "Category", "Price" });
        }
    }
}
