using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.Json;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("updates")]
    public class UpdatesController : ControllerBase
    {
        private readonly string updatesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Updates");

        // Matches Velopack asset structure
        public record ReleaseAsset(
            string PackageId,
            string Version,
            string Type,
            string FileName,
            string SHA1,
            long Size
        );

        public IActionResult GetReleaseFeed()
        {
            if (!Directory.Exists(updatesFolder))
                return NotFound(new { Error = "Releases folder not found" });

            var exeFiles = Directory.GetFiles(updatesFolder, "*.exe");

            var assets = exeFiles
                .Select(f =>
                {
                    var fileName = Path.GetFileName(f);
                    // Extract version from filename (assuming format: Name_1.0.0.exe)
                    var versionPart = Path.GetFileNameWithoutExtension(f).Split('_').Last();

                    // Compute SHA1
                    string sha1Hash;
                    using (var sha1 = SHA1.Create())
                    using (var stream = System.IO.File.OpenRead(f))
                    {
                        sha1Hash = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    }

                    var fileInfo = new FileInfo(f);

                    return new ReleaseAsset(
                        PackageId: "Beacon",
                        Version: versionPart,
                        Type: "Full",
                        FileName: fileName,
                        SHA1: sha1Hash,
                        Size: fileInfo.Length
                    );
                })
                // Sort by version descending (latest first)
                .OrderByDescending(r =>
                {
                    return Version.TryParse(r.Version, out var v) ? v : new Version(0, 0, 0);
                })
                .ToList();

            var feed = new { Assets = assets };

            return new JsonResult(feed, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}