using System.Globalization;

namespace RaceDay.Mvc.Infrastructure;

public static class Fmt
{
    public static string Date(DateTime d) => d.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture);
    public static string DateTimeLong(DateTime d) => d.ToString("ddd d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
    public static string Distance(double km) => km % 1 == 0
        ? $"{km:0} km"
        : $"{km.ToString("0.##", CultureInfo.InvariantCulture)} km";

    public static string Ordinal(int n)
    {
        var rem100 = n % 100;
        if (rem100 is >= 11 and <= 13) return $"{n}th";
        return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
    }

    /// <summary>Maps an enrolment status to a badge class (green Confirmed, amber Pending).</summary>
    public static string StatusClass(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "confirmed" => "rd-status rd-status-confirmed",
        "pending" => "rd-status rd-status-pending",
        "cancelled" or "canceled" or "rejected" or "declined" => "rd-status rd-status-cancelled",
        _ => "rd-status rd-status-other"
    };
}
