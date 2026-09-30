using System.ComponentModel.DataAnnotations;
using HealthcareAppointmentSystem.Models;
using HealthcareAppointmentSystem.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HealthcareAppointmentSystem.ViewModels;

public class StatCard
{
    public StatCard(string label, object value, string icon, string tone = "rose")
    {
        Label = label; Value = value; Icon = icon; Tone = tone;
    }
    public string Label { get; }
    public object Value { get; }
    public string Icon { get; }
    public string Tone { get; }
}

public class PatientDashboardViewModel
{
    public string FullName { get; set; }
    public Appointment Next { get; set; }
    public int UpcomingCount { get; set; }
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public List<Appointment> Recent { get; set; } = new();
}

public class PatientProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; }

    public string Email { get; set; }

    [Required(ErrorMessage = "Please enter your phone number.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,15}$", ErrorMessage = "Enter a valid phone number (7-15 digits).")]
    public string Phone { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; }

    [StringLength(5)]
    [Display(Name = "Blood group")]
    public string BloodGroup { get; set; }

    [StringLength(250)]
    public string Address { get; set; }
}

public class DoctorListViewModel
{
    public string Search { get; set; }
    public int? SpecializationId { get; set; }
    public List<Doctor> Doctors { get; set; } = new();
    public List<Specialization> Specializations { get; set; } = new();
}

public class BookAppointmentViewModel
{
    public int DoctorId { get; set; }

    [ValidateNever]
    public Doctor Doctor { get; set; }

    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Please choose a date.")]
    public DateTime Date { get; set; }

    [ValidateNever]
    public List<TimeSlot> Slots { get; set; } = new();

    // "HH:mm" of the selected slot
    public string SelectedStart { get; set; }

    [Required(ErrorMessage = "Please tell the doctor why you are visiting.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 300 characters.")]
    [Display(Name = "Reason for visit")]
    public string ReasonForVisit { get; set; }
}

public class PatientAppointmentsViewModel
{
    public List<Appointment> Upcoming { get; set; } = new();
    public List<Appointment> Past { get; set; } = new();
}
