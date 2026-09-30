using System.ComponentModel.DataAnnotations;

namespace HealthcareAppointmentSystem.Models;

public class ConsultationNote
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; }

    [Required, StringLength(300)]
    public string Diagnosis { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; }

    [StringLength(1000)]
    public string Prescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
