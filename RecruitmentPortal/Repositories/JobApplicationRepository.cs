using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Repositories;

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly ApplicationDbContext _db;

    public JobApplicationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<JobApplication>> GetAllAsync() =>
        await _db.JobApplications
            .Include(a => a.JobPosting)
            .Include(a => a.Applicant)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

    public async Task<IEnumerable<JobApplication>> GetByJobIdAsync(int jobId) =>
        await _db.JobApplications
            .Include(a => a.Applicant)
            .Where(a => a.JobPostingId == jobId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

    public async Task<IEnumerable<JobApplication>> GetByApplicantIdAsync(string applicantId) =>
        await _db.JobApplications
            .Include(a => a.JobPosting)
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

    public async Task<JobApplication?> GetByIdAsync(int id) =>
        await _db.JobApplications
            .Include(a => a.JobPosting)
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<bool> HasAppliedAsync(int jobId, string applicantId) =>
        await _db.JobApplications.AnyAsync(a => a.JobPostingId == jobId && a.ApplicantId == applicantId);

    public async Task AddAsync(JobApplication application)
    {
        _db.JobApplications.Add(application);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(JobApplication application)
    {
        _db.JobApplications.Update(application);
        await _db.SaveChangesAsync();
    }
}
