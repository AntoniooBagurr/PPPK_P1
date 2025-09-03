using MedSys.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalHistory> MedicalHistory => Set<MedicalHistory>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<VisitType> VisitTypes => Set<VisitType>();
    public DbSet<Doctor> Doctors => Set<Doctor>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ===================== Patient =====================
        b.Entity<Patient>(e =>
        {
            e.Property(x => x.OIB).IsRequired().HasMaxLength(11);
            e.HasIndex(x => x.LastName);
        });

        // ===================== VisitType =====================
        b.Entity<VisitType>(e =>
        {
            e.HasKey(vt => vt.Code);
            e.Property(vt => vt.Code).HasMaxLength(10);
        });

        b.Entity<Visit>()
            .HasOne<VisitType>()
            .WithMany()
            .HasForeignKey(v => v.VisitType)
            .HasPrincipalKey(vt => vt.Code);

        // ===================== Doctor (AUTH) =====================
        b.Entity<Doctor>(e =>
        {
            e.Property(d => d.FullName).IsRequired().HasMaxLength(150);

            e.HasIndex(d => d.LicenseNo).IsUnique().HasFilter("\"LicenseNo\" IS NOT NULL");

            e.Property(d => d.Email).HasMaxLength(256);
            e.HasIndex(d => d.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");

            e.Property(d => d.PwdHash).HasMaxLength(256).IsRequired();
            e.Property(d => d.PwdSalt).HasMaxLength(256).IsRequired();
        });

        // Visit → Doctor 
        b.Entity<Visit>()
            .HasOne(v => v.Doctor)
            .WithMany(d => d.Visits)
            .HasForeignKey(v => v.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        // ===================== Document =====================
        b.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);

            e.HasOne(d => d.Visit)
             .WithMany(v => v.Documents)
             .HasForeignKey(d => d.VisitId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.Patient)
             .WithMany(p => p.Documents)
             .HasForeignKey(d => d.PatientId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(d => d.PatientId);
            e.HasIndex(d => d.VisitId);

            e.Property(d => d.UploadedAt).HasDefaultValueSql("now()");
        });
    }
}
