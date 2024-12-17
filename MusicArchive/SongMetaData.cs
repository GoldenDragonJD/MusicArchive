using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicArchive
{
    public partial class SongMetaData
    {
        public string? ThumbnailPath { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Duration { get; set; }
        public string[]? Playlists { get; set; }
        public string? FileSize { get; set; }
        public string? SongPath { get; set; }
    }
}
