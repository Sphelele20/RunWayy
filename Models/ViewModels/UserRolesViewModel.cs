namespace RunWay.Models.ViewModels
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
