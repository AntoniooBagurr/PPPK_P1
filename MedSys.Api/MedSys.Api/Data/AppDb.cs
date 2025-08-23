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

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Patient
        b.Entity<Patient>(e =>
        {
            e.Property(x => x.OIB).IsRequired().HasMaxLength(11);
            e.HasIndex(x => x.LastName);
        });

        base.OnModelCreating(b);
    }
}
