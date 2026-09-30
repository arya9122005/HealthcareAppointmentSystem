using HealthcareAppointmentSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAppointmentSystem.Data;

public static class AppointmentQueryExtensions
{
    // Loads everything the appointment views need in one query.
    public static IQueryable<Appointment> WithDetails(this IQueryable<Appointment> query) =>
        query.Include(a => a.Patient).ThenInclude(p => p.User)
             .Include(a => a.Doctor).ThenInclude(d => d.User)
             .Include(a => a.Doctor).ThenInclude(d => d.Specialization)
             .Include(a => a.ConsultationNote);
}
