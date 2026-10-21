using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AlbionWebApp.Migrations
{
    /// <inheritdoc />
    public partial class SeedCitiesAndQualities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemPrices_Cities_CityId",
                table: "ItemPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemPrices_ItemLabels_ItemLabelId",
                table: "ItemPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemPrices_Qualities_QualityId",
                table: "ItemPrices");

            migrationBuilder.DropIndex(
                name: "IX_ItemPrices_CityId",
                table: "ItemPrices");

            migrationBuilder.DropIndex(
                name: "IX_ItemPrices_ItemLabelId",
                table: "ItemPrices");

            migrationBuilder.DropIndex(
                name: "IX_ItemPrices_QualityId",
                table: "ItemPrices");

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Caerleon" },
                    { 2, "Bridgewatch" },
                    { 3, "Lymhurst" },
                    { 4, "Martlock" },
                    { 5, "Thetford" },
                    { 6, "FortSterling" },
                    { 7, "Brecilien" }
                });

            migrationBuilder.InsertData(
                table: "Qualities",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Normal" },
                    { 2, "Good" },
                    { 3, "Outstanding" },
                    { 4, "Excellent" },
                    { 5, "Masterpiece" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Qualities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Qualities",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Qualities",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Qualities",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Qualities",
                keyColumn: "Id",
                keyValue: 5);

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

            migrationBuilder.AddForeignKey(
                name: "FK_ItemPrices_Cities_CityId",
                table: "ItemPrices",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemPrices_ItemLabels_ItemLabelId",
                table: "ItemPrices",
                column: "ItemLabelId",
                principalTable: "ItemLabels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemPrices_Qualities_QualityId",
                table: "ItemPrices",
                column: "QualityId",
                principalTable: "Qualities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
