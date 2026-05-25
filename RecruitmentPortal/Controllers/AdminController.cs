using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Repositories;

namespace RecruitmentPortal.Controllers;

[Authorize(Roles = SeedData.AdminRole)]
public class AdminController : Controller
{
    private readonly IJobPostingRepository _jobRepo;
    private readonly IJobApplicationRepository _appRepo;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IJobPostingRepository jobRepo,
        IJobApplicationRepository appRepo,
        ILogger<AdminController> logger)
    {
        _jobRepo = jobRepo;
        _appRepo = appRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var jobs = await _jobRepo.GetAllAsync();
        var applications = await _appRepo.GetAllAsync();
        ViewBag.TotalJobs = jobs.Count();
        ViewBag.ActiveJobs = jobs.Count(j => j.IsActive);
        ViewBag.TotalApplications = applications.Count();
        ViewBag.PendingApplications = applications.Count(a => a.Status == ApplicationStatus.Pending);
        return View();
    }

    public async Task<IActionResult> Jobs()
    {
        var jobs = await _jobRepo.GetAllAsync();
        return View(jobs);
    }

    public IActionResult CreateJob() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJob(JobPosting model)
    {
        if (!ModelState.IsValid) return View(model);

        model.PostedAt = DateTime.UtcNow;
        await _jobRepo.AddAsync(model);
        _logger.LogInformation("Admin created job posting '{Title}' (Id={Id})", model.Title, model.Id);

        TempData["Success"] = $"Job '{model.Title}' created.";
        return RedirectToAction(nameof(Jobs));
    }

    public async Task<IActionResult> EditJob(int id)
    {
        var job = await _jobRepo.GetByIdAsync(id);
        if (job == null) return NotFound();
        return View(job);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditJob(int id, JobPosting model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        await _jobRepo.UpdateAsync(model);
        _logger.LogInformation("Admin updated job posting '{Title}' (Id={Id})", model.Title, id);

        TempData["Success"] = "Job updated.";
        return RedirectToAction(nameof(Jobs));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await _jobRepo.GetByIdAsync(id);
        if (job != null)
        {
            await _jobRepo.DeleteAsync(id);
            _logger.LogInformation("Admin deleted job posting Id={Id}", id);
        }
        return RedirectToAction(nameof(Jobs));
    }

    public async Task<IActionResult> Applications(int? jobId)
    {
        IEnumerable<JobApplication> applications;
        if (jobId.HasValue)
        {
            applications = await _appRepo.GetByJobIdAsync(jobId.Value);
            ViewBag.FilterJobId = jobId;
        }
        else
        {
            applications = await _appRepo.GetAllAsync();
        }
        return View(applications);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApplicationStatus(int id, ApplicationStatus status)
    {
        var app = await _appRepo.GetByIdAsync(id);
        if (app == null) return NotFound();

        app.Status = status;
        await _appRepo.UpdateAsync(app);
        _logger.LogInformation("Admin updated application {AppId} status to {Status}", id, status);

        TempData["Success"] = $"Application status updated to {status}.";
        return RedirectToAction(nameof(Applications));
    }
}
