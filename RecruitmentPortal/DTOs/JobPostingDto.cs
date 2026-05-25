namespace RecruitmentPortal.DTOs;

public class JobPostingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Department { get; set; } = "";
    public DateTime PostedAt { get; set; }
    public bool IsActive { get; set; }
    public int ApplicationCount { get; set; }
}

public class CreateJobPostingDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Department { get; set; } = "";
}

public class UpdateJobPostingDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Department { get; set; } = "";
    public bool IsActive { get; set; }
}
