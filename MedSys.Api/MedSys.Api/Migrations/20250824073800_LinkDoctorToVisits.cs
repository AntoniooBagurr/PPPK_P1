using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSys.Api.Migrations
{
    /// <inheritdoc />
    public partial class LinkDoctorToVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // index – kreiraj samo ako ne postoji
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class c
        JOIN pg_namespace n ON n.oid=c.relnamespace
        WHERE c.relkind='i'
          AND c.relname='IX_Visits_DoctorId'
          AND n.nspname='public'
    ) THEN
        CREATE INDEX ""IX_Visits_DoctorId""
        ON ""public"".""Visits"" (""DoctorId"");
    END IF;
END $$;");

            // FK – dodaj samo ako ne postoji
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Visits_Doctors_DoctorId'
    ) THEN
        ALTER TABLE ""public"".""Visits""
        ADD CONSTRAINT ""FK_Visits_Doctors_DoctorId""
        FOREIGN KEY (""DoctorId"") REFERENCES ""public"".""Doctors""(""Id"")
        ON DELETE SET NULL;
    END IF;
END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""public"".""Visits""
                           DROP CONSTRAINT IF EXISTS ""FK_Visits_Doctors_DoctorId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Visits_DoctorId"";");
        }


    }
}
