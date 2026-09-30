using System.ComponentModel.DataAnnotations;
using HealthcareAppointmentSystem.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HealthcareAppointmentSystem.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalAppointments { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public List<Appointment> Recent { get; set; } = new();
}

public class DoctorFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the doctor's name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Please enter an email."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Please enter a phone number.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,15}$", ErrorMessage = "Enter a valid phone number (7-15 digits).")]
    public string Phone { get; set; }

    // Required only when creating a doctor (checked in the controller).
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Initial password")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Please choose a specialization.")]
    [Display(Name = "Specialization")]
    public int? SpecializationId { get; set; }

    [StringLength(150)]
    public string Qualification { get; set; }

    [Range(0, 60, ErrorMessage = "Experience must be between 0 and 60 years.")]
    [Display(Name = "Years of experience")]
    public int ExperienceYears { get; set; }

    [Range(0, 100000, ErrorMessage = "Enter a valid fee.")]
    [Display(Name = "Consultation fee")]
    public decimal ConsultationFee { get; set; }

    [StringLength(1000)]
    public string Bio { get; set; }

    [ValidateNever]
    public List<Specialization> Specializations { get; set; } = new();
}

public class AdminAppointmentsViewModel
{
    public string Status { get; set; }
    public List<Appointment> Items { get; set; } = new();
}

public class UserRowViewModel
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}
