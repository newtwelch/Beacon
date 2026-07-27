using AsyncAwaitBestPractices;

namespace Backend.Core.Services
{
    public interface IUpdateService
    {
        Task<Result<bool>> CheckForUpdateAsync();   // Returns true if update is available
        Task<Result<bool>> DownloadUpdateAsync();   // Downloads the stored latest update
        Task<Result<bool>> ApplyAndRestartIfPendingAsync(string[] restartArgs = null);
        /// <summary>
        /// Optional: expose info for UI.
        /// </summary>
        string LatestVersion { get; }
        string LatestNotes { get; }
        bool IsUpdateAvailable { get; }
        event Action<int> OnDownloadProgress;
    }
}
