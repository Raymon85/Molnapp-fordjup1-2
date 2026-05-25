using RecruitmentPortal.Models;

namespace RecruitmentPortal.Repositories;

public interface IJobPostingRepository
{
    Task<IEnumerable<JobPosting>> GetAllAsync();
    Task<IEnumerable<JobPosting>> GetActiveAsync();
    Task<JobPosting?> GetByIdAsync(int id);
    Task<JobPosting?> GetByIdWithApplicationsAsync(int id);
    Task AddAsync(JobPosting job);
    Task UpdateAsync(JobPosting job);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
