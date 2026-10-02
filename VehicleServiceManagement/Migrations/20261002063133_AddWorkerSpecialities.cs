using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleServiceManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerSpecialities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Profession",
                table: "Workers");

            migrationBuilder.CreateTable(
                name: "WorkerSpecialities",
                columns: table => new
                {
                    WorkerSpecialityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    Speciality = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerSpecialities", x => x.WorkerSpecialityId);
                    table.ForeignKey(
                        name: "FK_WorkerSpecialities_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSpecialities_WorkerId",
                table: "WorkerSpecialities",
                column: "WorkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerSpecialities");

            migrationBuilder.AddColumn<string>(
                name: "Profession",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
