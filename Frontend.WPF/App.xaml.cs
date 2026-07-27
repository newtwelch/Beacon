using Backend.Core.Logging;
using Backend.Core.Services;
using Frontend.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SQLite;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Velopack;

namespace Frontend.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? Host { get; private set; }
        public static IServiceProvider Services => Host!.Services;

        protected override void OnStartup(StartupEventArgs e)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string songDbPath = Path.Combine(appData, "Beacon", "songs.db");
            string beaconPath = Path.Combine(appData, "Beacon");
            string bibleDbPath = Path.Combine(appData, "Beacon", "bibles.db");
            SQLiteOpenFlags bibleDbFlags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

        Directory.CreateDirectory(Path.GetDirectoryName(beaconPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(songDbPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(bibleDbPath)!);

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddWpfBlazorWebView();

                    services.AddBlazorWebViewDeveloperTools();


                    services.AddLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddConsole();
                        builder.AddProvider(new FileLoggerProvider(beaconPath));
                        builder.SetMinimumLevel(LogLevel.Information);
                    });

                    // Backend services
                    services.AddSingleton<ICustomHttpFactory, HttpFactory>();
                    services.AddSingleton<SongService>(serviceProvider => 
                                                        new SongService(new SQLiteAsyncConnection(songDbPath),
                                                        serviceProvider.GetRequiredService<ILogger<SongService>>()));
                    services.AddSingleton<BookAliasService>();
                    services.AddSingleton<BibleService>(serviceProvider => 
                                                        new BibleService(serviceProvider.GetRequiredService<ICustomHttpFactory>(),
                                                                          bibleDbPath,
                                                                          bibleDbFlags));
                    services.AddSingleton<ProjectionService>();
                    
                    services.AddSingleton<IUpdateService>(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<VelopackUpdateService>>();

                        // Create the UpdateManager pointing to your REST API
                        var releasesUrl = "http://beaconapi.runasp.net/updates";
                        var manager = new UpdateManager(releasesUrl);

                        return new VelopackUpdateService(manager, logger);
                    });

                })
                .Build();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (Host != null)
            {
                await Host.StopAsync();
                Host.Dispose();
            }
            base.OnExit(e);
        }
    }

}
