using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sofra.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestrictAllergenDietaryTagDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItemAllergens_Allergens_AllergenId",
                table: "MenuItemAllergens");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItemDietaryTags_DietaryTags_DietaryTagId",
                table: "MenuItemDietaryTags");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItemAllergens_Allergens_AllergenId",
                table: "MenuItemAllergens",
                column: "AllergenId",
                principalTable: "Allergens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItemDietaryTags_DietaryTags_DietaryTagId",
                table: "MenuItemDietaryTags",
                column: "DietaryTagId",
                principalTable: "DietaryTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItemAllergens_Allergens_AllergenId",
                table: "MenuItemAllergens");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItemDietaryTags_DietaryTags_DietaryTagId",
                table: "MenuItemDietaryTags");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItemAllergens_Allergens_AllergenId",
                table: "MenuItemAllergens",
                column: "AllergenId",
                principalTable: "Allergens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItemDietaryTags_DietaryTags_DietaryTagId",
                table: "MenuItemDietaryTags",
                column: "DietaryTagId",
                principalTable: "DietaryTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
