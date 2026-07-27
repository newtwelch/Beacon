using Microsoft.Extensions.Logging;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Services
{
    public class VersionService
    {
        private readonly ILogger<VersionService> logger;

        public VersionService(ILogger<VersionService> _logger)
        {
            logger = _logger;
        }

        public event Action? RequestVersion;
        public event Action<string>? SendVersion;
    }
}
