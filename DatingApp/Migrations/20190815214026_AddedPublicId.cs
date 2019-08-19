using Microsoft.EntityFrameworkCore.Migrations;

namespace DatingApp.Migrations
{
    public partial class AddedPublicId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_photos_Users_UserId",
            //     table: "photos");

            // migrationBuilder.AlterColumn<int>(
            //     name: "UserId",
            //     table: "photos",
            //     nullable: false,
            //     oldClrType: typeof(int),
            //     oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "photos",
                nullable: true);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_photos_Users_UserId",
            //     table: "photos",
            //     column: "UserId",
            //     principalTable: "Users",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //     name: "FK_photos_Users_UserId",
            //     table: "photos");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "photos");

            // migrationBuilder.AlterColumn<int>(
            //     name: "UserId",
            //     table: "photos",
            //     nullable: true,
            //     oldClrType: typeof(int));

            // migrationBuilder.AddForeignKey(
            //     name: "FK_photos_Users_UserId",
            //     table: "photos",
            //     column: "UserId",
            //     principalTable: "Users",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);
        }
    }
}
