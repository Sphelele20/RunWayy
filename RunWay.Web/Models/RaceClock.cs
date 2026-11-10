namespace RunWay.Web.Models;
// Event form values are South African wall time, independent of the host timezone.
public static class RaceClock {
 public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(2), DateTimeKind.Unspecified);
 public static DateTime Today => Now.Date;
}
