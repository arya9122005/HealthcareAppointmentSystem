namespace HealthcareAppointmentSystem.Services;

// Small formatting helpers used by the views.
public static class Fmt
{
    public static readonly DayOfWeek[] Week =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public static string Time(TimeSpan t) => DateTime.Today.Add(t).ToString("h:mm tt");
    public static string Date(DateTime d) => d.ToString("ddd, dd MMM yyyy");
    public static string ShortDate(DateTime d) => d.ToString("dd MMM yyyy");

    public static string Initials(string name)
    {
        var parts = (name ?? "").Replace("Dr.", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
        return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpper();
    }
}
