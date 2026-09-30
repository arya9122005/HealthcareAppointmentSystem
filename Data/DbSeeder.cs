using HealthcareAppointmentSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAppointmentSystem.Data;

public static class DbSeeder
{
    public const string AdminEmail = "admin@healthcare.com";
    public const string AdminPassword = "Admin@123";
    public const string DoctorPassword = "Doctor@123";
    public const string PatientPassword = "Patient@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { AppRoles.Admin, AppRoles.Doctor, AppRoles.Patient })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await userManager.FindByEmailAsync(AdminEmail) == null)
            await CreateUserAsync(userManager, "System Administrator", AdminEmail, "9830000000", AdminPassword, AppRoles.Admin);

        if (await db.Specializations.AnyAsync()) return; // demo data already loaded

        // ---- Specializations ----
        var specs = new Dictionary<string, Specialization>
        {
            ["Cardiology"] = new() { Name = "Cardiology", Description = "Heart and blood vessel care." },
            ["Dermatology"] = new() { Name = "Dermatology", Description = "Skin, hair and nail conditions." },
            ["Pediatrics"] = new() { Name = "Pediatrics", Description = "Health care for infants, children and teenagers." },
            ["General Medicine"] = new() { Name = "General Medicine", Description = "Everyday illnesses and preventive care." },
            ["Orthopedics"] = new() { Name = "Orthopedics", Description = "Bones, joints and muscles." }
        };
        db.Specializations.AddRange(specs.Values);

        // ---- Doctors (Mon-Fri schedules; the last one also works Saturday mornings) ----
        var doctorData = new[]
        {
            (Name: "Dr. Ananya Sharma", Email: "ananya.sharma@healthcare.com", Phone: "9830011111", Spec: "Cardiology",
             Qual: "MD, DM (Cardiology)", Exp: 14, Fee: 900m, Start: 9, End: 13,
             Bio: "Interventional cardiologist focused on preventive heart care and long-term management of hypertension."),
            (Name: "Dr. Rohan Mukherjee", Email: "rohan.mukherjee@healthcare.com", Phone: "9830022222", Spec: "Dermatology",
             Qual: "MD (Dermatology)", Exp: 9, Fee: 700m, Start: 10, End: 16,
             Bio: "Treats acne, eczema and pigmentation concerns with a gentle, evidence-based approach."),
            (Name: "Dr. Meera Iyer", Email: "meera.iyer@healthcare.com", Phone: "9830033333", Spec: "Pediatrics",
             Qual: "MD (Pediatrics)", Exp: 11, Fee: 600m, Start: 9, End: 14,
             Bio: "Paediatrician who enjoys helping parents with growth, nutrition and vaccination questions."),
            (Name: "Dr. Kabir Sen", Email: "kabir.sen@healthcare.com", Phone: "9830044444", Spec: "General Medicine",
             Qual: "MBBS, MD (Medicine)", Exp: 7, Fee: 500m, Start: 10, End: 15,
             Bio: "Family physician covering fevers, infections, diabetes follow-ups and routine check-ups."),
            (Name: "Dr. Farah Khan", Email: "farah.khan@healthcare.com", Phone: "9830055555", Spec: "Orthopedics",
             Qual: "MS (Orthopedics)", Exp: 12, Fee: 800m, Start: 9, End: 13,
             Bio: "Orthopaedic surgeon specialising in sports injuries, joint pain and rehabilitation.")
        };

        var doctors = new List<Doctor>();
        for (var i = 0; i < doctorData.Length; i++)
        {
            var d = doctorData[i];
            var user = await CreateUserAsync(userManager, d.Name, d.Email, d.Phone, DoctorPassword, AppRoles.Doctor);
            var doctor = new Doctor
            {
                UserId = user.Id,
                Specialization = specs[d.Spec],
                Qualification = d.Qual,
                ExperienceYears = d.Exp,
                ConsultationFee = d.Fee,
                Bio = d.Bio
            };
            foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            {
                doctor.Schedules.Add(new DoctorSchedule
                {
                    DayOfWeek = day,
                    StartTime = TimeSpan.FromHours(d.Start),
                    EndTime = TimeSpan.FromHours(d.End),
                    SlotMinutes = 30
                });
            }
            if (i == doctorData.Length - 1)
            {
                doctor.Schedules.Add(new DoctorSchedule
                {
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = TimeSpan.FromHours(10),
                    EndTime = TimeSpan.FromHours(12),
                    SlotMinutes = 30
                });
            }
            doctors.Add(doctor);
        }
        db.Doctors.AddRange(doctors);

        // ---- Patients ----
        var patientData = new[]
        {
            (Name: "Priya Das", Email: "priya.das@example.com", Phone: "9831012345", Dob: new DateTime(1994, 3, 12), Gender: "Female", Blood: "B+", Address: "12 Lake Road, Kolkata"),
            (Name: "Rahul Mehta", Email: "rahul.mehta@example.com", Phone: "9831023456", Dob: new DateTime(1988, 9, 2), Gender: "Male", Blood: "O+", Address: "45 Park Street, Kolkata"),
            (Name: "Sneha Banerjee", Email: "sneha.banerjee@example.com", Phone: "9831034567", Dob: new DateTime(2001, 11, 25), Gender: "Female", Blood: "A+", Address: "8 Garden Avenue, Howrah")
        };
        var patients = new List<Patient>();
        foreach (var p in patientData)
        {
            var user = await CreateUserAsync(userManager, p.Name, p.Email, p.Phone, PatientPassword, AppRoles.Patient);
            patients.Add(new Patient { UserId = user.Id, DateOfBirth = p.Dob, Gender = p.Gender, BloodGroup = p.Blood, Address = p.Address });
        }
        db.Patients.AddRange(patients);

        // ---- Appointments (dates are relative to today so the demo always looks current) ----
        Appointment Make(int patient, int doctor, int dayOffset, int hour, int minute, AppointmentStatus status, string reason)
        {
            var start = new TimeSpan(hour, minute, 0);
            return new Appointment
            {
                Patient = patients[patient],
                Doctor = doctors[doctor],
                AppointmentDate = Weekday(dayOffset),
                StartTime = start,
                EndTime = start + TimeSpan.FromMinutes(30),
                Status = status,
                ReasonForVisit = reason,
                CreatedAt = DateTime.Now.AddDays(dayOffset - 2)
            };
        }

        var completed1 = Make(0, 0, -14, 10, 0, AppointmentStatus.Completed, "Occasional chest tightness while climbing stairs");
        completed1.ConsultationNote = new ConsultationNote
        {
            Diagnosis = "Mild hypertension, no cardiac abnormality on ECG",
            Notes = "Blood pressure 142/92. ECG normal. Advised reduced salt intake, daily 30-minute walk and a home BP log. Review in 4 weeks.",
            Prescription = "Amlodipine 5 mg once daily for 4 weeks"
        };
        var completed2 = Make(0, 2, -7, 10, 30, AppointmentStatus.Completed, "Recurring cough in my 6-year-old (bringing my son)");
        completed2.ConsultationNote = new ConsultationNote
        {
            Diagnosis = "Allergic cough",
            Notes = "Clear chest on examination, no fever. Likely dust allergy. Keep bedding clean and avoid cold drinks.",
            Prescription = "Cetirizine syrup 5 ml at night for 5 days"
        };
        var completed3 = Make(1, 1, -10, 11, 0, AppointmentStatus.Completed, "Persistent acne on the jawline");
        completed3.ConsultationNote = new ConsultationNote
        {
            Diagnosis = "Mild inflammatory acne",
            Notes = "Start a gentle cleanser and topical retinoid at night. Use sunscreen daily. Follow up in 6 weeks.",
            Prescription = "Adapalene 0.1% gel at night; SPF 30 sunscreen"
        };

        db.Appointments.AddRange(
            completed1, completed2, completed3,
            Make(2, 3, -1, 11, 0, AppointmentStatus.Confirmed, "Knee pain after a fall"),
            Make(2, 0, 0, 12, 0, AppointmentStatus.Confirmed, "Palpitations at night"),
            Make(1, 0, 1, 11, 0, AppointmentStatus.Pending, "Routine heart check-up"),
            Make(0, 1, 2, 10, 0, AppointmentStatus.Confirmed, "Skin allergy follow-up"),
            Make(2, 4, 3, 10, 0, AppointmentStatus.Confirmed, "Lower back pain"),
            Make(0, 3, 4, 10, 30, AppointmentStatus.Pending, "Seasonal fever and body ache"),
            Make(1, 2, -3, 10, 0, AppointmentStatus.Cancelled, "Vaccination query"));

        await db.SaveChangesAsync();
    }

    // Moves a date off the weekend (forward for future offsets, backward for past ones).
    private static DateTime Weekday(int dayOffset)
    {
        var date = DateTime.Today.AddDays(dayOffset);
        var step = dayOffset < 0 ? -1 : 1;
        while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            date = date.AddDays(step);
        return date;
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager, string fullName, string email, string phone, string password, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            PhoneNumber = phone
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Seeding failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
