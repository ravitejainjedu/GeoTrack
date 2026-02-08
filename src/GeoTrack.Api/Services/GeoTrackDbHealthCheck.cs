using System.Threading;
using System.Threading.Tasks;
using GeoTrack.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GeoTrack.Api.Services;

public class GeoTrackDbHealthCheck : IHealthCheck
{
    private readonly GeoTrackDbContext _context;

    public GeoTrackDbHealthCheck(GeoTrackDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple check: Can we connect?
            if (await _context.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Healthy();
            }
            return HealthCheckResult.Unhealthy("Cannot connect to database.");
        }
        catch (System.Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed.", ex);
        }
    }
}
