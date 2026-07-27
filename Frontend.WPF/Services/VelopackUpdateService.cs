using Backend.Core.Services;
using Microsoft.Extensions.Logging;
using Velopack;

namespace Frontend.WPF.Services
{
    public class VelopackUpdateService : IUpdateService
    {
        public event Action<int>? OnDownloadProgress;
        private readonly UpdateManager manager;
        private readonly ILogger<VelopackUpdateService> logger;

        // Store latest Velopack update info
        private Velopack.UpdateInfo? latestVelopackInfo;

        public VelopackUpdateService(UpdateManager _manager, ILogger<VelopackUpdateService> _logger)
        {
            manager = _manager;
            logger = _logger;
        }

        // Expose info for UI
        public string LatestVersion { get; private set; } = "";
        public string LatestNotes { get; private set; } = "";
        public bool IsUpdateAvailable { get; private set; } = false;

        public async Task<Result<bool>> CheckForUpdateAsync()
        {
            return await ExecuteAsync(async () =>
            {
                latestVelopackInfo = await manager.CheckForUpdatesAsync();
                if (latestVelopackInfo == null)
                {
                    IsUpdateAvailable = false;
                    LatestVersion = "";
                    LatestNotes = "";
                    logger.LogInformation("No updates available.");
                    return false;
                }
                
                // Store minimal info for UI
                LatestVersion = latestVelopackInfo.TargetFullRelease.Version.ToString();
                LatestNotes = latestVelopackInfo.TargetFullRelease.NotesHTML ?? "";
                IsUpdateAvailable = true;

                logger.LogInformation("Update available: {Version}", LatestVersion);
                return true;
            });
        }

        public async Task<Result<bool>> DownloadUpdateAsync()
        {
            return await ExecuteAsync(async () =>
            {
                if (latestVelopackInfo == null)
                    throw new InvalidOperationException("No update info stored from previous check.");

                await manager.DownloadUpdatesAsync(latestVelopackInfo, progress =>
                {
                    OnDownloadProgress?.Invoke(progress); // <- notify UI
                });

                return true;
            });
        }

        public Task<Result<bool>> ApplyAndRestartIfPendingAsync(string[] restartArgs = null)
        {
            return ExecuteAsync(() =>
            {
                var pending = manager.UpdatePendingRestart;
                if (pending == null)
                {
                    logger.LogInformation("No pending update to apply.");
                    return Task.FromResult(false);
                }

                manager.ApplyUpdatesAndRestart(pending, restartArgs);
                return Task.FromResult(true);
            });
        }

        // Generic executor with logging
        private async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                var result = await operation();
                return Result<T>.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Velopack operation failed.");
                return Result<T>.Fail(ex.Message);
            }
        }
    }
}