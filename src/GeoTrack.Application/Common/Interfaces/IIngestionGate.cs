using System.Threading.Tasks;

namespace GeoTrack.Application.Common.Interfaces;

public interface IIngestionGate
{
    Task<bool> TryEnterAsync(int timeoutMs);
    void Exit();
}
