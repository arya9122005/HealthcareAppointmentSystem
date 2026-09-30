using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HealthcareAppointmentSystem.Models;

// Extends the built-in Identity user. Email, PhoneNumber and password hash live on IdentityUser.
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string FullName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
