using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AlbionWebApp.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelasItemPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Qualities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemLabelId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<byte>(type: "smallint", nullable: false),
                    Enchantment = table.Column<byte>(type: "smallint", nullable: false),
                    QualityId = table.Column<int>(type: "integer", nullable: false),
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    SellPriceMin = table.Column<int>(type: "integer", nullable: true),
                    SellPriceMinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SellPriceMax = table.Column<int>(type: "integer", nullable: true),
                    BuyPriceMin = table.Column<int>(type: "integer", nullable: true),
                    BuyPriceMax = table.Column<int>(type: "integer", nullable: true),
                    BuyPriceMaxDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemPrices_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemPrices_ItemLabels_ItemLabelId",
                        column: x => x.ItemLabelId,
                        principalTable: "ItemLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemPrices_Qualities_QualityId",
                        column: x => x.QualityId,
                        principalTable: "Qualities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrices_CityId",
                table: "ItemPrices",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrices_ItemLabelId",
                table: "ItemPrices",
                column: "ItemLabelId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPrices_QualityId",
                table: "ItemPrices",
                column: "QualityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemPrices");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Qualities");
        }
    }
}
