using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.Json;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("updates")]
    public class UpdatesController : ControllerBase
    {
        private readonly string updatesFolder =
        Path.Combine(Directory.GetCurrentDirectory(), "Updates");

        [HttpGet("releases.win.json")]
        public IActionResult Feed()
        {
            var path = Path.Combine(updatesFolder, "releases.win.json");

            if (!System.IO.File.Exists(path))
                return NotFound();

            return PhysicalFile(path, "application/json");
        }

        [HttpGet("{fileName}")]
        public IActionResult Download(string fileName)
        {
            var path = Path.Combine(updatesFolder, fileName);

            if (!System.IO.File.Exists(path))
                return NotFound();

            return PhysicalFile(path, "application/octet-stream");
        }
    }
}