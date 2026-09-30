using System.ComponentModel.DataAnnotations;

namespace HealthcareAppointmentSystem.Models;

public class Doctor
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public int SpecializationId { get; set; }
    public Specialization Specialization { get; set; }

    [StringLength(150)]
    public string Qualification { get; set; }

    [Range(0, 60)]
    public int ExperienceYears { get; set; }

    [Range(0, 100000)]
    public decimal ConsultationFee { get; set; }

    [StringLength(1000)]
    public string Bio { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
