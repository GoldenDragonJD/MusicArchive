using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using YTSearch.NET;
using System.Collections.ObjectModel;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.UI.Xaml.Media.Animation;
using System.Net;
using System.Text.Json;
using System.Net.Http;
using AngleSharp.Dom;
using System.Threading.Tasks;
using YoutubeExplode.Common;
using System.Text.Json.Serialization.Metadata;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicArchive
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public partial class YouTubeVideoTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? NotDownloadedTemplate { get; set; }
        public DataTemplate? DownloadedTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is YouTubeVideo video)
            {
                return video.Downloaded ? DownloadedTemplate : NotDownloadedTemplate;
            }
            return base.SelectTemplateCore(item, container);
        }
    }
    public sealed partial class Downloads : Page
    {
        public ObservableCollection<YouTubeVideo> VideoList { get; set; }
        public Downloads()
        {
            this.InitializeComponent();
            VideoList = new ObservableCollection<YouTubeVideo>();
            DownloadList.ItemsSource = VideoList;
        }

        public void SearchBar_KeyDown(object sender, KeyRoutedEventArgs e) { if (e.Key == Windows.System.VirtualKey.Enter) { SearchYoutubeClick(sender, new RoutedEventArgs()); } }

        public static string SantizeTitle(string title)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitizedTitle = new(title.Where(c => !invalidChars.Contains(c)).ToArray());
            return sanitizedTitle;
        }


        public async void SearchYoutubeClick(object sender, RoutedEventArgs e)
        {
            string searchQuery = SearchBar.Text;
            if (string.IsNullOrWhiteSpace(searchQuery)) return;

            //YouTubeSearchClient client = new YouTubeSearchClient();
            //YouTubeVideoSearchResult result = await client.SearchYoutubeVideoAsync(searchQuery);

            //var results = result.Results.ToArray();

            var youtube = new YoutubeClient();
            var searchResults = await youtube.Search.GetVideosAsync(searchQuery).CollectAsync(20);
            var results = searchResults.ToArray();

            // Clear existing items in the ObservableCollection
            VideoList.Clear();

            // get files from directory
            try
            {
                string[] folders = Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Songs"));

                foreach (var item in results)
                {
                    if (item?.Duration == null) continue;

                    VideoList.Add(new YouTubeVideo
                    {
                        ThumbnailUrl = item.Thumbnails.ToArray()[0].Url ?? "None",
                        Title = item.Title ?? "None",
                        Author = item.Author.ChannelTitle,
                        Duration = "Length: " + item.Duration.Value.ToString("hh\\:mm\\:ss"),
                        Downloaded = folders.Contains(Path.Combine(AppContext.BaseDirectory, "Songs", SantizeTitle($"{item.Title ?? "None"} by {item.Author.ChannelTitle}"))),
                        Id = item.Id.Value ?? "None",
                        GridWidth = DownloadList.ActualWidth,
                        ChannelId = item.Author.ChannelId,
                    });
                }
            }
            catch (Exception ex)
            {
                DebugTextBox.Text = ex.Message;
            }
        }

        public void DownloadList_DoubleClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (DownloadList.SelectedItem is YouTubeVideo selectedVideo)
            {
                DebugTextBox.Text = $"{selectedVideo.Title} by {selectedVideo.Author}";

                string folderName = SantizeTitle($"{selectedVideo.Title ?? "None"} by {selectedVideo.Author ?? "None"}");

                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Songs", folderName));
                string filePath = Path.Combine(AppContext.BaseDirectory, "Songs", folderName);
                if (selectedVideo.Id == null) return;
                DownloadMusic(selectedVideo.Id, filePath, selectedVideo.Title ?? "None", selectedVideo);
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is YouTubeVideo video)
            {
                video.GridWidth = grid.ActualWidth;
            }
        }
        private string formatByte(long bytes)
        {
            float i = (float)bytes;
            if (i < 1024) return $"{i:0.00} B";
            else if (i < 1024 * 1024) return $"{(i / 1024):0.00} KB";
            else if (i < 1024 * 1024 * 1024) return $"{(i / 1024 / 1024):0.00} MB";
            else return $"{(i / 1024 / 1024 / 1024):0.00} GB";
        }

        public class MetaData
        {
            public required string Title { get; set; }
            public required string Author { get; set; }
            public required string Duration { get; set; }
            public required string[] Playlists { get; set; }
            public required string FileSize { get; set; }
        }
        public async Task DownloadMusic(string Id, string directoryPath, string title, YouTubeVideo video)
        {
            try
            {
                YoutubeClient client = new();
                var streamManifest = await client.Videos.Streams.GetManifestAsync(Id);
                var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                string filePath = Path.Combine(directoryPath, "songFile.webm");

                var progress = new Progress<double>(percent =>
                {
                    if (percent == 1.0)
                    {
                        video.Downloaded = true;
                        //DebugTextBox.Text = $"Downloaded: {title}";
                        return;
                    }
                    //DebugTextBox.Text = $"Downloading: {percent:P2}";
                    video.DownloadProgress = percent * video.GridWidth;
                });

                await client.Videos.Streams.DownloadAsync(audioStream, filePath, progress);

                SaveImage(video.ThumbnailUrl ?? "None", directoryPath);
                UpdateChannel(video.ChannelId);

                JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
                JsonSerializerOptions options = jsonSerializerOptions;
                string fileSize = formatByte(new FileInfo(filePath).Length);
                MetaData metaData = new() { Title = video.Title ?? "None", Author = video.Author ?? "None", Duration = video.Duration ?? "None", Playlists = [], FileSize = fileSize };
                string json = JsonSerializer.Serialize(metaData);
                File.WriteAllText(Path.Combine(directoryPath, "metaData.json"), json);

                //SearchYoutubeClick(null, null);
            }
            catch (Exception ex)
            {
                DebugTextBox.Text = "Error: " + ex.Message;
            }
        }

        public static async void SaveImage(string imageUrl, string directoryPath)
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                string imagePath = Path.Combine(directoryPath, "thumbnail.png");
                File.WriteAllBytes(imagePath, imageBytes);
            }
        }

        public static async void UpdateChannel(string channeId)
        {
            var client = new YoutubeClient();
            var channel = await client.Channels.GetAsync(channeId);
            if (channel == null) return;

            var thumbnailUrl = channel.Thumbnails[0].Url;

            HttpClient httpClient = new();
            HttpResponseMessage response = await httpClient.GetAsync(thumbnailUrl);
            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                string imagePath = Path.Combine(AppContext.BaseDirectory, "Channels", channel.Title + ".png");
                File.WriteAllBytes(imagePath, imageBytes);
            }
        }

    }

}
