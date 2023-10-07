using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OpenTelemetry.Service3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Orders",
                columns: new[] { "Id", "CustomerId", "Number" },
                values: new object[,]
                {
                    { 1, 1, "ORD-1" },
                    { 2, 1, "ORD-2" },
                    { 3, 2, "ORD-3" },
                    { 4, 2, "ORD-4" },
                    { 5, 5, "ORD-5" },
                    { 6, 5, "ORD-6" }
                });

            migrationBuilder.InsertData(
                table: "OrderItems",
                columns: new[] { "Id", "Name", "OrderId", "Price", "Quantity" },
                values: new object[,]
                {
                    { 1, "Item 1", 1, 10m, 2 },
                    { 2, "Item 2", 1, 20m, 1 },
                    { 3, "Item 3", 2, 30m, 1 },
                    { 4, "Item 1", 2, 10m, 21 },
                    { 5, "Item 3", 3, 30m, 1 },
                    { 6, "Item 34", 4, 0.5m, 15 },
                    { 7, "Item 21", 4, 23.5m, 143 },
                    { 8, "Item 1", 5, 10m, 2 },
                    { 9, "Item 1", 6, 10m, 21 },
                    { 10, "Item 3", 6, 30m, 1 },
                    { 11, "Item 34", 6, 0.5m, 15 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_Id",
                table: "OrderItems",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Id",
                table: "Orders",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
