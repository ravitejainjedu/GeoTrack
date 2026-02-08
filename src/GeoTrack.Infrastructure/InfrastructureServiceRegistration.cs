using GeoTrack.Application.Common.Interfaces;
using GeoTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeoTrack.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<GeoTrackDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IGeoTrackDbContext>(provider => provider.GetRequiredService<GeoTrackDbContext>());

        return services;
    }
}

