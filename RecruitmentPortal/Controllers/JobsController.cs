using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Repositories;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.Controllers;

public class JobsController : Controller
{
    private readonly IJobPostingRepository _jobRepo;
    private readonly IJobApplicationRepository _appRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IJobPostingRepository jobRepo,
        IJobApplicationRepository appRepo,
        UserManager<ApplicationUser> userManager,
        IBlobStorageService blobService,
        ILogger<JobsController> logger)
    {
        _jobRepo = jobRepo;
        _appRepo = appRepo;
        _userManager = userManager;
        _blobService = blobService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var jobs = await _jobRepo.GetActiveAsync();
        if (!string.IsNullOrWhiteSpace(search))
            jobs = jobs.Where(j => j.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || j.Department.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || j.Location.Contains(search, StringComparison.OrdinalIgnoreCase));

        ViewBag.Search = search;
        return View(jobs);
    }

    public async Task<IActionResult> Details(int id)
    {
        var job = await _jobRepo.GetByIdAsync(id);
        if (job == null) return NotFound();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User)!;
            ViewBag.HasApplied = await _appRepo.HasAppliedAsync(id, userId);
        }

        return View(job);
    }

    [Authorize(Roles = SeedData.CandidateRole)]
    public async Task<IActionResult> Apply(int id)
    {
        var job = await _jobRepo.GetByIdAsync(id);
        if (job == null || !job.IsActive) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        if (await _appRepo.HasAppliedAsync(id, userId))
        {
            TempData["Error"] = "You have already applied for this position.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.Job = job;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.CandidateRole)]
    public async Task<IActionResult> Apply(int id, string coverLetter, IFormFile? cvFile)
    {
        var job = await _jobRepo.GetByIdAsync(id);
        if (job == null || !job.IsActive) return NotFound();

        var userId = _userManager.GetUserId(User)!;

        if (string.IsNullOrWhiteSpace(coverLetter))
        {
            ModelState.AddModelError("coverLetter", "Cover letter is required.");
            ViewBag.Job = job;
            return View();
        }

        string? blobUrl = null;
        string? fileName = null;

        if (cvFile != null && cvFile.Length > 0)
        {
            if (cvFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("cvFile", "CV file must be under 5 MB.");
                ViewBag.Job = job;
                return View();
            }

            try
            {
                await using var stream = cvFile.OpenReadStream();
                blobUrl = await _blobService.UploadAsync(stream, cvFile.FileName, cvFile.ContentType);
                fileName = cvFile.FileName;
                _logger.LogInformation("User {UserId} uploaded CV for job {JobId}", userId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload CV for user {UserId}, job {JobId}", userId, id);
                ModelState.AddModelError("", "CV upload failed. Please try again.");
                ViewBag.Job = job;
                return View();
            }
        }

        var application = new JobApplication
        {
            JobPostingId = id,
            ApplicantId = userId,
            CoverLetter = coverLetter,
            CvBlobUrl = blobUrl,
            CvFileName = fileName
        };

        await _appRepo.AddAsync(application);
        _logger.LogInformation("User {UserId} applied for job {JobId}", userId, id);

        TempData["Success"] = "Your application has been submitted!";
        return RedirectToAction(nameof(MyApplications));
    }

    [Authorize(Roles = SeedData.CandidateRole)]
    public async Task<IActionResult> MyApplications()
    {
        var userId = _userManager.GetUserId(User)!;
        var applications = await _appRepo.GetByApplicantIdAsync(userId);
        return View(applications);
    }
}
