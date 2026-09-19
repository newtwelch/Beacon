using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Models.Beacon
{
    public class Screen
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
