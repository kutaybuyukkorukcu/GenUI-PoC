using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FogData.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Temperature = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Humidity = table.Column<int>(type: "integer", nullable: false),
                    WindSpeed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SaleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SalesPersonId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesData_People_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_People_Email",
                table: "People",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_Region",
                table: "People",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_Region",
                table: "SalesData",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_SaleDate",
                table: "SalesData",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesData_SalesPersonId",
                table: "SalesData",
                column: "SalesPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherData_Date",
                table: "WeatherData",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherData_Location",
                table: "WeatherData",
                column: "Location");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesData");

            migrationBuilder.DropTable(
                name: "WeatherData");

            migrationBuilder.DropTable(
                name: "People");
        }
    }
}
