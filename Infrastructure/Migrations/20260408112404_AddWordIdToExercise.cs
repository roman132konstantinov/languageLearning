using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWordIdToExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WordId",
                table: "Exercises",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_WordId",
                table: "Exercises",
                column: "WordId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Words_WordId",
                table: "Exercises",
                column: "WordId",
                principalTable: "Words",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Words_WordId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_WordId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "WordId",
                table: "Exercises");
        }
    }
}
