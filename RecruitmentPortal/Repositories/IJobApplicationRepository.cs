using RecruitmentPortal.Models;

namespace RecruitmentPortal.Repositories;

public interface IJobApplicationRepository
{
    Task<IEnumerable<JobApplication>> GetAllAsync();
    Task<IEnumerable<JobApplication>> GetByJobIdAsync(int jobId);
    Task<IEnumerable<JobApplication>> GetByApplicantIdAsync(string applicantId);
    Task<JobApplication?> GetByIdAsync(int id);
    Task<bool> HasAppliedAsync(int jobId, string applicantId);
    Task AddAsync(JobApplication application);
    Task UpdateAsync(JobApplication application);
}
