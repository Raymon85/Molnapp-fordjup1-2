using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models;

public class JobPosting
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [Required, MaxLength(100)]
    public string Location { get; set; } = "";

    [Required, MaxLength(100)]
    public string Department { get; set; } = "";

    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
