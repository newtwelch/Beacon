using SQLite;

namespace Backend.Core.Models.Beacon
{
    public class BeaconVersion
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
    }
}
