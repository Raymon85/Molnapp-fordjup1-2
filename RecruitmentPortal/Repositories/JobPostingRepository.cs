using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;

namespace RecruitmentPortal.Repositories;

public class JobPostingRepository : IJobPostingRepository
{
    private readonly ApplicationDbContext _db;

    public JobPostingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<JobPosting>> GetAllAsync() =>
        await _db.JobPostings
            .Include(j => j.Applications)
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync();

    public async Task<IEnumerable<JobPosting>> GetActiveAsync() =>
        await _db.JobPostings.Where(j => j.IsActive).OrderByDescending(j => j.PostedAt).ToListAsync();

    public async Task<JobPosting?> GetByIdAsync(int id) =>
        await _db.JobPostings.FindAsync(id);

    public async Task<JobPosting?> GetByIdWithApplicationsAsync(int id) =>
        await _db.JobPostings
            .Include(j => j.Applications)
            .ThenInclude(a => a.Applicant)
            .FirstOrDefaultAsync(j => j.Id == id);

    public async Task AddAsync(JobPosting job)
    {
        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(JobPosting job)
    {
        _db.JobPostings.Update(job);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var job = await _db.JobPostings.FindAsync(id);
        if (job != null)
        {
            _db.JobPostings.Remove(job);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _db.JobPostings.AnyAsync(j => j.Id == id);
}
