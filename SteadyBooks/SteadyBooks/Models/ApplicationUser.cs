using Microsoft.AspNetCore.Identity;

namespace SteadyBooks.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirmName { get; set; }
    public string? PrimaryContactEmail { get; set; }
}
