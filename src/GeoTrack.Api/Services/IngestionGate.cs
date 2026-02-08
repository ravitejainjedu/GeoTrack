using System.Threading;
using System.Threading.Tasks;
using GeoTrack.Application.Common.Interfaces;

namespace GeoTrack.Api.Services;

public class IngestionGate : IIngestionGate
{
    // Limit to 4 concurrent requests as requested
    private readonly SemaphoreSlim _semaphore = new(4);

    public async Task<bool> TryEnterAsync(int timeoutMs)
    {
        return await _semaphore.WaitAsync(timeoutMs);
    }

    public void Exit()
    {
        try 
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Should not happen if logic is correct, but safe to ignore or log
        }
    }
}
