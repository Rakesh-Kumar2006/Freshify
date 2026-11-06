using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freshify.Migrations
{
    public partial class AddUserImageUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ImageUrl column already exists in Users.
            // No need to add it again.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the existing ImageUrl column.
            // Do not delete it during rollback.
        }
    }
}