using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSys.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    LicenseNo = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Doctors", x => x.Id); });


            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId",
                table: "Visits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_DoctorId",
                table: "Visits",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Doctors_DoctorId",
                table: "Visits",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Visits_Doctors_DoctorId", "Visits");
            migrationBuilder.DropIndex("IX_Visits_DoctorId", "Visits");
            migrationBuilder.DropColumn("DoctorId", "Visits");


            migrationBuilder.DropTable("Doctors");
        }

    }
}
