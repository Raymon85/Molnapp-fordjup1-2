using Microsoft.Extensions.Diagnostics.HealthChecks;
using RecruitmentPortal.Services;

namespace RecruitmentPortal.HealthChecks;

public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IBlobStorageService _blobService;

    public BlobStorageHealthCheck(IBlobStorageService blobService)
    {
        _blobService = blobService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var available = await _blobService.IsAvailableAsync();
        return available
            ? HealthCheckResult.Healthy("Blob storage is reachable")
            : HealthCheckResult.Unhealthy("Blob storage is not reachable");
    }
}
