using HealthcareAppointmentSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAppointmentSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<ConsultationNote> ConsultationNotes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser 1 - 0..1 Patient
        builder.Entity<Patient>()
            .HasOne(p => p.User).WithOne().HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Patient>().HasIndex(p => p.UserId).IsUnique();

        // ApplicationUser 1 - 0..1 Doctor
        builder.Entity<Doctor>()
            .HasOne(d => d.User).WithOne().HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Doctor>().HasIndex(d => d.UserId).IsUnique();
        builder.Entity<Doctor>().Property(d => d.ConsultationFee).HasColumnType("decimal(10,2)");

        // Specialization 1 - * Doctor (cannot delete a specialization that is in use)
        builder.Entity<Doctor>()
            .HasOne(d => d.Specialization).WithMany(s => s.Doctors).HasForeignKey(d => d.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Specialization>().HasIndex(s => s.Name).IsUnique();

        // Doctor 1 - * DoctorSchedule
        builder.Entity<DoctorSchedule>()
            .HasOne(s => s.Doctor).WithMany(d => d.Schedules).HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Patient 1 - * Appointment, Doctor 1 - * Appointment (Restrict avoids multiple cascade paths)
        builder.Entity<Appointment>()
            .HasOne(a => a.Patient).WithMany(p => p.Appointments).HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor).WithMany(d => d.Appointments).HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>().Property(a => a.AppointmentDate).HasColumnType("date");

        // Database-level guarantee against double booking: one active appointment per doctor/date/start time.
        // Cancelled (Status = 3) appointments free the slot again.
        builder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.StartTime })
            .IsUnique()
            .HasFilter("[Status] <> 3");

        // Appointment 1 - 0..1 ConsultationNote
        builder.Entity<ConsultationNote>()
            .HasOne(n => n.Appointment).WithOne(a => a.ConsultationNote).HasForeignKey<ConsultationNote>(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
