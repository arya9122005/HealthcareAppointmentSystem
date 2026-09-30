using System.ComponentModel.DataAnnotations;
using HealthcareAppointmentSystem.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HealthcareAppointmentSystem.ViewModels;

public class DoctorDashboardViewModel
{
    public Doctor Doctor { get; set; }
    public List<Appointment> Today { get; set; } = new();
    public List<Appointment> Upcoming { get; set; } = new();
    public int TotalPatients { get; set; }
    public int PendingCount { get; set; }
    public int CompletedCount { get; set; }
}

public class DoctorProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; }

    public string Email { get; set; }

    [Required(ErrorMessage = "Please enter your phone number.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,15}$", ErrorMessage = "Enter a valid phone number (7-15 digits).")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "Please choose a specialization.")]
    [Display(Name = "Specialization")]
    public int SpecializationId { get; set; }

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

public class ScheduleDayViewModel
{
    public DayOfWeek Day { get; set; }
    public bool IsAvailable { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotMinutes { get; set; } = 30;
}

public class ScheduleViewModel
{
    public List<ScheduleDayViewModel> Days { get; set; } = new();
}

public class DoctorAppointmentsViewModel
{
    public string View { get; set; } = "upcoming";
    public List<Appointment> Items { get; set; } = new();
}

public class ConsultationNoteViewModel
{
    public int AppointmentId { get; set; }

    [ValidateNever]
    public Appointment Appointment { get; set; }

    [Required(ErrorMessage = "Please enter a diagnosis."), StringLength(300)]
    public string Diagnosis { get; set; }

    [StringLength(2000)]
    [Display(Name = "Consultation notes")]
    public string Notes { get; set; }

    [StringLength(1000)]
    public string Prescription { get; set; }
}

public class PatientHistoryViewModel
{
    public Patient Patient { get; set; }
    public List<Appointment> Appointments { get; set; } = new();
}
