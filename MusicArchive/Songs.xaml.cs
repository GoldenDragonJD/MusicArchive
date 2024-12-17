using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Text.Json;
using YoutubeExplode.Common;
using System.Collections.ObjectModel;
using MusicArchive;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Media.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicArchive
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public partial class SongTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? SongTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            return SongTemplate;
        }
    }

    public sealed partial class Songs : Page
    {
        public ObservableCollection<SongMetaData> SongList { get; set; }
        private int SearchCharCount { get; set; }

        public Songs()
        {
            this.InitializeComponent();
            SongList = new ObservableCollection<SongMetaData>();
            SongListItems.ItemsSource = SongList;
            SearchCharCount = 0;
            LoadSongs();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string channelName)
            {
                SearchSongs.Text = channelName;
                FilterSongs(SearchSongs, null);
            }
        }

        public void LoadSongs()
        {
            var folders = Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Songs")).ToArray();

            SongList = new ObservableCollection<SongMetaData>();
            SongListItems.ItemsSource = SongList;
            //DebugTextBox.Text = folders.Length.ToString();

            foreach (string folder in folders)
            {
                string folderName = Path.GetFileName(folder);
                string thumbnailPath = Path.Combine(AppContext.BaseDirectory, "Songs", folderName, "thumbnail.png");
                string metaDataPath = Path.Combine("Songs", folderName, "metaData.json");
                string songPath = Path.Combine(AppContext.BaseDirectory, "Songs", folderName, "songFile.webm");

                SongMetaData? metaData = JsonSerializer.Deserialize<SongMetaData>(File.ReadAllText(metaDataPath));

                SongList.Add(new SongMetaData
                {
                    ThumbnailPath = thumbnailPath,
                    Title = metaData?.Title,
                    Author = metaData?.Author,
                    Duration = metaData?.Duration,
                    Playlists = metaData?.Playlists,
                    FileSize = metaData?.FileSize,
                    SongPath = songPath,
                });

            }
        }

        public void FilterSongs(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string searchTerm = textBox.Text.ToLower();
                int currentCharCount = searchTerm.Length;

                //DebugTextBox.Text = $"{currentCharCount} / {SearchCharCount}";

                if (currentCharCount < SearchCharCount) LoadSongs();

                SearchCharCount = currentCharCount;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                SongList.Where(song => !song.Title.ToLower().Contains(searchTerm) && !song.Author.ToLower().Contains(searchTerm)).ToList().ForEach(song => SongList.Remove(song));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        public async void PlaySong_DoublePress(object sender, DoubleTappedRoutedEventArgs e)
        {
            var window = App.MainWindow;
            //MainWindow window = App.MainWindow;
            if (SongListItems.SelectedItem is SongMetaData songMetaData)
            {
                //DebugTextBox.Text = $"Double Clicked! {songMetaData.Title}";
                
                await window.PlaySong(songMetaData);
            }
        }

        public async void PlayOneSong(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                SongMetaData? songMeta = menuFlyoutItem.DataContext as SongMetaData;
                await App.MainWindow.PlaySong(songMeta);
            }
        }

        public async void PlayAllSongs(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                SongMetaData? songMeta = menuFlyoutItem.DataContext as SongMetaData;
                MediaPlaybackList playbackList = new();
                CustomPlayBackItem startingItem = null;
                var customItems = new List<CustomPlayBackItem>();

                foreach(SongMetaData song in SongList)
                {
                    MediaSource mediaSource = MediaSource.CreateFromUri(new Uri(song.SongPath));
                    MediaPlaybackItem playbackItem = new(mediaSource);
                    CustomPlayBackItem customPlayBackItem = new(playbackItem, song);
                    
                    playbackList.Items.Add(playbackItem);
                    customItems.Add(customPlayBackItem);

                    if (song == songMeta)
                    {
                        playbackList.StartingItem = playbackItem;
                        startingItem = customPlayBackItem;
                    }
                }

                App.MainWindow.LoadSongs(playbackList, startingItem, customItems, false);
            }
        }
        public async void PlayAllSongsShuffled(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                SongMetaData? songMeta = menuFlyoutItem.DataContext as SongMetaData;
                MediaPlaybackList playbackList = new();
                CustomPlayBackItem startingItem = null;
                var customItems = new List<CustomPlayBackItem>();

                foreach (SongMetaData song in SongList)
                {
                    MediaSource mediaSource = MediaSource.CreateFromUri(new Uri(song.SongPath));
                    MediaPlaybackItem playbackItem = new(mediaSource);
                    CustomPlayBackItem customPlayBackItem = new(playbackItem, song);

                    playbackList.Items.Add(playbackItem);
                    customItems.Add(customPlayBackItem);

                    if (song == songMeta)
                    {
                        playbackList.StartingItem = playbackItem;
                        startingItem = customPlayBackItem;
                    }
                }

                App.MainWindow.LoadSongs(playbackList, startingItem, customItems, true);
            }
        }

        public static string SantizeTitle(string title)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitizedTitle = new(title.Where(c => !invalidChars.Contains(c)).ToArray());
            return sanitizedTitle;
        }

        public void RemoveSong(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                try
                {
                    SongMetaData? songMeta = menuFlyoutItem.DataContext as SongMetaData;
                    if (songMeta == null) return;
                    string folderName = SantizeTitle($"{songMeta.Title} by {songMeta.Author}");
                    Directory.Delete(Path.Combine(AppContext.BaseDirectory, "Songs", folderName), true);
                    SongList.Remove(songMeta);
                    bool deleteChannel = true;
                    foreach (SongMetaData song in SongList)
                    {
                        if (song.Author == songMeta.Author) deleteChannel = false;
                    }
                    if (deleteChannel)
                    {
                        File.Delete(Path.Combine(AppContext.BaseDirectory, "Channels", songMeta.Author + ".png"));
                    }
                }
                catch (Exception ex)
                {
                    DebugTextBox.Text = ex.Message;
                }
            }
        }
    }
}

