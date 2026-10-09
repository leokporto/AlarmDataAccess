using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alarms.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Alarms",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<byte>(type: "tinyint", nullable: false),
                    Condition = table.Column<byte>(type: "tinyint", nullable: false),
                    Limit = table.Column<double>(type: "float", nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    Timestamp_UTCTicks = table.Column<long>(type: "bigint", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.CheckConstraint("CK_Alarms_Priority", "[Priority] BETWEEN 1 AND 10");
                });

            migrationBuilder.CreateIndex(
                name: "UX_Alarms_Name",
                schema: "dbo",
                table: "Alarms",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms",
                schema: "dbo");
        }
    }
}
