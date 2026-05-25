using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.DTOs;
using RecruitmentPortal.Models;
using RecruitmentPortal.Repositories;

namespace RecruitmentPortal.Controllers.Api;

[ApiController]
[Route("api/jobs")]
[Produces("application/json")]
public class JobsApiController : ControllerBase
{
    private readonly IJobPostingRepository _jobRepo;
    private readonly ILogger<JobsApiController> _logger;

    public JobsApiController(IJobPostingRepository jobRepo, ILogger<JobsApiController> logger)
    {
        _jobRepo = jobRepo;
        _logger = logger;
    }

    /// <summary>List all active job postings</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobPostingDto>>> GetAll()
    {
        var jobs = await _jobRepo.GetActiveAsync();
        _logger.LogInformation("API: returning {Count} active job postings", jobs.Count());
        return Ok(jobs.Select(ToDto));
    }

    /// <summary>Get a single job posting by id</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobPostingDto>> GetById(int id)
    {
        var job = await _jobRepo.GetByIdWithApplicationsAsync(id);
        if (job == null) return NotFound(new { error = $"Job {id} not found" });
        return Ok(ToDto(job));
    }

    /// <summary>Create a new job posting</summary>
    [HttpPost]
    public async Task<ActionResult<JobPostingDto>> Create([FromBody] CreateJobPostingDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var job = new JobPosting
        {
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            Department = dto.Department,
            PostedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _jobRepo.AddAsync(job);
        _logger.LogInformation("API: created job posting '{Title}' Id={Id}", job.Title, job.Id);

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToDto(job));
    }

    /// <summary>Update a job posting</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<JobPostingDto>> Update(int id, [FromBody] UpdateJobPostingDto dto)
    {
        var job = await _jobRepo.GetByIdAsync(id);
        if (job == null) return NotFound(new { error = $"Job {id} not found" });

        job.Title = dto.Title;
        job.Description = dto.Description;
        job.Location = dto.Location;
        job.Department = dto.Department;
        job.IsActive = dto.IsActive;

        await _jobRepo.UpdateAsync(job);
        _logger.LogInformation("API: updated job posting Id={Id}", id);

        return Ok(ToDto(job));
    }

    /// <summary>Delete a job posting</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _jobRepo.ExistsAsync(id)) return NotFound(new { error = $"Job {id} not found" });
        await _jobRepo.DeleteAsync(id);
        _logger.LogInformation("API: deleted job posting Id={Id}", id);
        return NoContent();
    }

    private static JobPostingDto ToDto(JobPosting j) => new()
    {
        Id = j.Id,
        Title = j.Title,
        Description = j.Description,
        Location = j.Location,
        Department = j.Department,
        PostedAt = j.PostedAt,
        IsActive = j.IsActive,
        ApplicationCount = j.Applications?.Count ?? 0
    };
}
