using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Services
{
    public record Result<T>(T? Data, bool Success, string? ErrorMessage = null)
    {
        public static Result<T> Ok(T data) => new(data, true);
        public static Result<T> Fail(string error) => new(default, false, error);
    }

}

