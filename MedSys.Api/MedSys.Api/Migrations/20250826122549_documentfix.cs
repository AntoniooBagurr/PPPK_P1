using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSys.Api.Migrations
{
    /// <inheritdoc />
    public partial class documentfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "VisitId",
                table: "Documents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PatientId",
                table: "Documents",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Patients_PatientId",
                table: "Documents",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Documents_Patients_PatientId", "Documents");
            migrationBuilder.DropIndex("IX_Documents_PatientId", "Documents");
            migrationBuilder.DropColumn("PatientId", "Documents");

            migrationBuilder.AlterColumn<Guid>(
                name: "VisitId",
                table: "Documents",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
