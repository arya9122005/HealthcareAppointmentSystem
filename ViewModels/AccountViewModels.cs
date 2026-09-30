using System.ComponentModel.DataAnnotations;

namespace HealthcareAppointmentSystem.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Please enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Please enter your phone number.")]
    [RegularExpression(@"^[0-9+\-\s()]{7,15}$", ErrorMessage = "Enter a valid phone number (7-15 digits).")]
    public string Phone { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; }

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; }
}
