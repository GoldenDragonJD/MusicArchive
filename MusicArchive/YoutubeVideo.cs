using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MusicArchive
{
    public partial class YouTubeVideo : INotifyPropertyChanged
    {
        public string? ThumbnailUrl { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Duration { get; set; }
        public string? ChannelId { get; set; }
        private bool downloaded;
        public bool Downloaded
        {
            get => downloaded;
            set
            {
                if (downloaded != value)
                {
                    downloaded = value;
                    OnPropertyChanged(nameof(Downloaded));
                }
            }
        }
        private double gridWidth;
        public double GridWidth { get => gridWidth; set { gridWidth = value; OnPropertyChanged(nameof(GridWidth)); } }
        public string? Id { get; set; }

        private double downloadProgress;
        public double DownloadProgress
        {
            get { return downloadProgress; }
            set
            {
                if (downloadProgress != value)
                {
                    downloadProgress = value;
                    OnPropertyChanged(nameof(DownloadProgress));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged; protected void OnPropertyChanged(string propertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
    }

}
