# Peony Health - Healthcare Appointment & Patient Management System

ASP.NET Core MVC (.NET 8) · Entity Framework Core · SQL Server · ASP.NET Core Identity · Razor · Bootstrap 5

## Run it (Visual Studio 2022, 17.8+)

1. Unzip, then **File > Open > Project/Solution** and choose `HealthcareAppointmentSystem.csproj`.
2. Check the connection string in `appsettings.json`.
   - LocalDB (default): `Server=(localdb)\\MSSQLLocalDB;...`
   - SQL Server Express: change the server to `.\\SQLEXPRESS`
3. Press **F5**. On first start the app creates the `HealthcareAppointmentDb` database and loads the demo data.

Bootstrap, Bootstrap Icons, jQuery and Google Fonts load from CDNs, so the first run needs internet access.

## Use real EF Core migrations (recommended for the course)

Tools > NuGet Package Manager > Package Manager Console:

```powershell
Add-Migration InitialCreate
Update-Database
```

If the app already ran once without migrations, drop the database first: `Drop-Database`, then run the two commands above.

## Demo logins

| Role | Email | Password |
|---|---|---|
| Admin | admin@healthcare.com | Admin@123 |
| Doctor | ananya.sharma@healthcare.com (Cardiology) | Doctor@123 |
| Doctor | rohan.mukherjee@healthcare.com (Dermatology) | Doctor@123 |
| Doctor | meera.iyer@healthcare.com (Pediatrics) | Doctor@123 |
| Doctor | kabir.sen@healthcare.com (General Medicine) | Doctor@123 |
| Doctor | farah.khan@healthcare.com (Orthopedics) | Doctor@123 |
| Patient | priya.das@example.com | Patient@123 |
| Patient | rahul.mehta@example.com | Patient@123 |
| Patient | sneha.banerjee@example.com | Patient@123 |

## Entity relationships

- `ApplicationUser` (Identity) 1 to 0..1 `Patient`, and 1 to 0..1 `Doctor`
- `Specialization` 1 to many `Doctor`
- `Doctor` 1 to many `DoctorSchedule` (one row per working weekday)
- `Patient` 1 to many `Appointment`, `Doctor` 1 to many `Appointment`
- `Appointment` 1 to 0..1 `ConsultationNote`

## How double booking is prevented

1. `AppointmentService.GetAvailableSlotsAsync` only offers slots inside the doctor's schedule that are not already booked.
2. `AppointmentService.BookAsync` re-checks the slot right before saving.
3. A filtered unique index on (DoctorId, AppointmentDate, StartTime) where Status is not Cancelled makes the database itself reject a duplicate.

## Try these flows

- Patient: Find doctors > Book > My appointments > Cancel; Consultation history shows notes from completed visits.
- Doctor: Confirm a pending request; complete Farah Khan's or Ananya Sharma's confirmed visit and add notes; edit My schedule.
- Admin: add a doctor, deactivate them, manage specializations, cancel an appointment, grant admin access.
