using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration; // Optional if we use Env Vars directly
using Microsoft.Extensions.DependencyInjection;

namespace GeoTrack.IntegrationTests;

public class GeoTrackApiFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureTestServices;

    public GeoTrackApiFactory(Action<IServiceCollection>? configureTestServices = null)
    {
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Ensure API Key is set configuration
        builder.UseSetting("GeoTrack:ApiKey", TestConstants.ApiKeyValue);

        // 2. Apply custom test services (e.g. TestContainer DbContext)
        if (_configureTestServices != null)
        {
            builder.ConfigureServices(_configureTestServices);
        }
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        
        // 2. Ensure every client created has the API Key header
        if (!client.DefaultRequestHeaders.Contains(TestConstants.ApiKeyHeader))
        {
            client.DefaultRequestHeaders.Add(TestConstants.ApiKeyHeader, TestConstants.ApiKeyValue);
        }
    }
}
