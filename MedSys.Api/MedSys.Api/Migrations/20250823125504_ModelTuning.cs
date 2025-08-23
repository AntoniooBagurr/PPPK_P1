using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSys.Api.Migrations
{
    /// <inheritdoc />
    public partial class ModelTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ekstenzija za gen_random_uuid (sigurno ako već postoji)
            migrationBuilder.Sql(@"create extension if not exists ""pgcrypto"";");

            // 3NF: tablica tipova pregleda
            migrationBuilder.CreateTable(
                name: "VisitTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitTypes", x => x.Code);
                });

            // Seed tipova pregleda
            migrationBuilder.Sql(@"
        insert into ""VisitTypes""(""Code"",""Name"") values
        ('GP','Opći tjelesni pregled'),
        ('KRV','Test krvi'),
        ('X-RAY','Rendgensko skeniranje'),
        ('CT','CT sken'),
        ('MR','MRI sken'),
        ('ULTRA','Ultrazvuk'),
        ('EKG','Elektrokardiogram'),
        ('ECHO','Ehokardiogram'),
        ('EYE','Pregled očiju'),
        ('DERM','Dermatološki pregled'),
        ('DENTA','Pregled zuba'),
        ('MAMMO','Mamografija'),
        ('NEURO','Neurološki pregled')
        on conflict do nothing;
    ");

            // FK: Visits.VisitType -> VisitTypes.Code
            migrationBuilder.Sql(@"
        alter table ""Visits""
        add constraint ""FK_Visits_VisitTypes_VisitType""
        foreign key (""VisitType"") references ""VisitTypes""(""Code"");
    ");

            // CHECK ograničenja
            migrationBuilder.Sql(@"
        alter table ""Patients""
        add constraint ck_patients_oib_format check (""OIB"" ~ '^[0-9]{11}$');
    ");
            migrationBuilder.Sql(@"
        alter table ""Patients""
        add constraint ck_patients_birthdate_past check (""BirthDate"" < now());
    ");
            migrationBuilder.Sql(@"
        alter table ""MedicalHistory""
        add constraint ck_history_dates check (""EndDate"" is null or ""EndDate"" >= ""StartDate"");
    ");

            // Indeksi
            migrationBuilder.Sql(@"create index if not exists idx_patients_oib on ""Patients""(""OIB"");");
            migrationBuilder.Sql(@"create index if not exists idx_documents_visit on ""Documents""(""VisitId"");");
            migrationBuilder.Sql(@"create index if not exists idx_prescriptions_visit on ""Prescriptions""(""VisitId"");");
            migrationBuilder.Sql(@"create index if not exists idx_items_prescription on ""PrescriptionItems""(""PrescriptionId"");");
            migrationBuilder.Sql(@"create index if not exists idx_items_medication on ""PrescriptionItems""(""MedicationId"");");

            // Jedinstvenost lijeka bez obzira na velika/mala slova
            migrationBuilder.Sql(@"create unique index if not exists uq_medications_name_ci on ""Medications"" (lower(""Name""));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"drop index if exists uq_medications_name_ci;");
            migrationBuilder.Sql(@"drop index if exists idx_items_medication;");
            migrationBuilder.Sql(@"drop index if exists idx_items_prescription;");
            migrationBuilder.Sql(@"drop index if exists idx_prescriptions_visit;");
            migrationBuilder.Sql(@"drop index if exists idx_documents_visit;");
            migrationBuilder.Sql(@"drop index if exists idx_patients_oib;");

            migrationBuilder.Sql(@"alter table ""MedicalHistory"" drop constraint if exists ck_history_dates;");
            migrationBuilder.Sql(@"alter table ""Patients"" drop constraint if exists ck_patients_birthdate_past;");
            migrationBuilder.Sql(@"alter table ""Patients"" drop constraint if exists ck_patients_oib_format;");

            migrationBuilder.Sql(@"alter table ""Visits"" drop constraint if exists ""FK_Visits_VisitTypes_VisitType"";");
            migrationBuilder.DropTable(name: "VisitTypes");
        }
    }
}
