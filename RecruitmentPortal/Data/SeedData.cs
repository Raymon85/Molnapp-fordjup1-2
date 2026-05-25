using Microsoft.AspNetCore.Identity;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Data;

public static class SeedData
{
    public const string AdminRole = "Admin";
    public const string CandidateRole = "Candidate";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        await db.Database.EnsureCreatedAsync();

        foreach (var role in new[] { AdminRole, CandidateRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role {Role}", role);
            }
        }

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@cloudsoft.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin@12345!";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "CloudSoft Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AdminRole);
                logger.LogInformation("Seeded admin user {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to seed admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!db.JobPostings.Any())
        {
            db.JobPostings.AddRange(
                new JobPosting
                {
                    Title = "Senior .NET Developer",
                    Description = "We are looking for an experienced .NET developer to join our cloud team. You will design and build scalable services on Azure.",
                    Location = "Stockholm",
                    Department = "Engineering",
                    IsActive = true
                },
                new JobPosting
                {
                    Title = "DevOps Engineer",
                    Description = "Help us build and maintain our CI/CD pipelines and Azure infrastructure. Experience with Docker and Kubernetes required.",
                    Location = "Remote",
                    Department = "Infrastructure",
                    IsActive = true
                },
                new JobPosting
                {
                    Title = "Product Manager",
                    Description = "Lead product development for our recruitment platform. Work closely with engineering and design.",
                    Location = "Gothenburg",
                    Department = "Product",
                    IsActive = true
                }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded sample job postings");
        }
    }
}
