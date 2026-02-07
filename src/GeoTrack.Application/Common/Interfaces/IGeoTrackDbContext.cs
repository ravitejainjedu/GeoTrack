using System.Threading;
using System.Threading.Tasks;
using GeoTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeoTrack.Application.Common.Interfaces;

public interface IGeoTrackDbContext
{
    DbSet<Device> Devices { get; }
    DbSet<Location> Locations { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
