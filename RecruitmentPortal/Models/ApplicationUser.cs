using Microsoft.AspNetCore.Identity;

namespace RecruitmentPortal.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
