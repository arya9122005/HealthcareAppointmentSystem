using System.ComponentModel.DataAnnotations;

namespace HealthcareAppointmentSystem.Models;

public class Patient
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    public string Gender { get; set; }

    [StringLength(5)]
    public string BloodGroup { get; set; }

    [StringLength(250)]
    public string Address { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
