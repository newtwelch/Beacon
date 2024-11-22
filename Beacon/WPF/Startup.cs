using Beacon.Model;
using Beacon.Model.Bibles;
using Beacon.Model.Songs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beacon.WPF;

public static class Startup
{
    public static IServiceProvider Services { get; private set; }

    public static void Init()
    {
        var host = Host.CreateDefaultBuilder()
                       .ConfigureServices(WireupServices)
                       .Build();
        Services = host.Services;
    }

    private static void WireupServices(IServiceCollection services)
    {
        services.AddWpfBlazorWebView();
        services.AddSingleton<ISongService, SongService>();
        services.AddSingleton<IBibleService, BibleService>();
        services.AddSingleton<ICustomHttpFactory, HttpFactory>();
        services.AddSingleton<SettingsService>();

        

#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
    }
}
