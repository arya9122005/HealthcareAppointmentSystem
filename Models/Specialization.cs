using System.ComponentModel.DataAnnotations;

namespace HealthcareAppointmentSystem.Models;

public class Specialization
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; }

    [StringLength(300)]
    public string Description { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
