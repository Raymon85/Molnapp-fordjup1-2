using System.ComponentModel.DataAnnotations;

namespace RecruitmentPortal.Models;

public enum ApplicationStatus
{
    Pending,
    Reviewed,
    Accepted,
    Rejected
}

public class JobApplication
{
    public int Id { get; set; }

    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public string ApplicantId { get; set; } = "";
    public ApplicationUser Applicant { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string CoverLetter { get; set; } = "";

    public string? CvBlobUrl { get; set; }

    public string? CvFileName { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
