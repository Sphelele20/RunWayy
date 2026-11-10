using RunWay.Web.Models.Api;

namespace RunWay.Web.Models;

/// <summary>Small formatting helpers used by the Razor views.</summary>
public static class UiHelpers
{
    /// <summary>URL of an image served through this app (which proxies the API / Azure Blob Storage).</summary>
    public static string? ImageUrl(string? blobName) =>
        string.IsNullOrWhiteSpace(blobName) ? null : "/img/" + blobName;

    public static string FormatTime(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes:00}:{t.Seconds:00}";

    public static string Ordinal(int n)
    {
        var mod100 = n % 100;
        if (mod100 is >= 11 and <= 13) return n + "th";
        return (n % 10) switch { 1 => n + "st", 2 => n + "nd", 3 => n + "rd", _ => n + "th" };
    }

    public static EventType[] EventTypes => new[] { EventType.Run, EventType.Walk, EventType.Cycle };
    public static string TypeLabel(EventType t) => t switch
    {
        EventType.FunRun => "Fun Run",
        _ => t.ToString()
    };

    public static string StatusClass(EnrolmentStatus s) => s switch
    {
        EnrolmentStatus.Confirmed => "status-confirmed",
        EnrolmentStatus.Pending => "status-pending",
        _ => "status-cancelled"
    };

    public static string GenderLabel(Gender g) => g == Gender.PreferNotToSay ? "Prefer not to say" : g.ToString();
}
