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
using System.Text.Json.Serialization.Metadata;
using static MusicArchive.Downloads;

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
        private SongMetaData? rightClickedContext;

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
                SongMetaData? metaData = null;
                try
                {
                    metaData = JsonSerializer.Deserialize<SongMetaData>(File.ReadAllText(metaDataPath));
                }
                catch (Exception ex)
                {
                    Directory.Delete(folder, true);

                    var channelFiles = Directory.GetFiles(AppContext.BaseDirectory, "Channels");
                    foreach (var file in channelFiles)
                    {
                        if (Path.GetFileName(folder).Replace(".png", "").EndsWith(Path.GetFileName(file)))
                        {
                            File.Delete(file);
                            break;
                        }
                    }

                    continue;
                }

                SongList.Add(new SongMetaData
                {
                    ThumbnailPath = thumbnailPath,
                    Title = metaData?.Title ?? "Error",
                    Author = metaData?.Author ?? "Error",
                    Duration = metaData?.Duration ?? "Error",
                    Playlists = metaData?.Playlists ?? [],
                    FileSize = metaData?.FileSize ?? "Error",
                    SongPath = songPath ?? Path.Combine(AppContext.BaseDirectory, "Assets", "error.mp3"),
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
                if (searchTerm.StartsWith("artistname:"))
                {
                    SongList.Where(song => !song.Author.ToLower().Contains(searchTerm.Replace("artistname:", "").ToLower())).ToList().ForEach(song => SongList.Remove(song));
                }
                else if (searchTerm.StartsWith("playlistname:"))
                {
                    SongList.Where(song => !song.Playlists.Any(playlist => playlist.ToLower().Contains(searchTerm.Replace("playlistname:", "").ToLower()))).ToList().ForEach(song => SongList.Remove(song));
                }
                else
                {
                    SongList.Where(song => !song.Title.ToLower().Contains(searchTerm.ToLower()) && !song.Author.ToLower().Contains(searchTerm.ToLower())).ToList().ForEach(song => SongList.Remove(song));
                }

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

        public void PlayAllSongs(object sender, RoutedEventArgs e)
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

                App.MainWindow.LoadSongs(playbackList, startingItem, customItems, false);
            }
        }
        public void PlayAllSongsShuffled(object sender, RoutedEventArgs e)
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

        public void OnMenuFlyoutOpen(object sender, object e)
        {
            MenuFlyout menuFlyout = sender as MenuFlyout;
            MenuFlyoutSubItem menuFlyoutSubItem = menuFlyout.Items[3] as MenuFlyoutSubItem;
            SongMetaData songMetaData = menuFlyoutSubItem.DataContext as SongMetaData;

            menuFlyoutSubItem.Items.Clear();

            var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Playlists"));
            foreach (var file in files)
            {
                string fullName = Path.GetFileName(file);
                string playlistName = fullName.EndsWith(".png") ? fullName.Replace(".png", "") : fullName.Replace(".txt", "");

                MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
                {
                    Icon = new SymbolIcon(songMetaData.Playlists.Any((playlist) => playlist == playlistName) ? Symbol.Remove : Symbol.Add),
                    Text = playlistName,
                };
                menuFlyoutItem.Click += AddToPlaylist;
                menuFlyoutSubItem.Items.Add(menuFlyoutItem);
            }

            MenuFlyoutItem addButton = new MenuFlyoutItem
            {
                Text = "Add"
            };
            addButton.Click += OpenAddFlyout;
            menuFlyoutSubItem.Items.Add(addButton);
        }

        public void OnMenuFlyoutClose(object sender, object e)
        {
            MenuFlyout menuFlyout = sender as MenuFlyout;
            MenuFlyoutSubItem menuFlyoutSubItem = menuFlyout.Items[3] as MenuFlyoutSubItem;
        }

        public void AddToPlaylist(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menu)
            {
                SongMetaData songMetaData = menu.DataContext as SongMetaData;
                List<string> playlist = songMetaData.Playlists.ToList();

                if (menu.Icon is SymbolIcon symbolIcon)
                {
                    Symbol iconSymbol = symbolIcon.Symbol;

                    if (iconSymbol == Symbol.Add)
                    {
                        playlist.Add(menu.Text);
                        menu.Icon = new SymbolIcon(Symbol.Remove);
                    }
                    else if (iconSymbol == Symbol.Remove)
                    {
                        playlist.Remove(menu.Text);
                        menu.Icon = new SymbolIcon(Symbol.Add);
                    }
                }

                MetaData metaData = new MetaData
                {
                    Title = songMetaData.Title,
                    Author = songMetaData.Author,
                    Duration = songMetaData.Duration,
                    FileSize = songMetaData.FileSize,
                    Playlists = playlist.ToArray()
                };

                JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
                JsonSerializerOptions options = jsonSerializerOptions;
                string songMetaPath = Path.Combine(AppContext.BaseDirectory, "Songs", SantizeTitle($"{songMetaData.Title} by {songMetaData.Author}"));
                string json = JsonSerializer.Serialize(metaData);
                File.WriteAllText(Path.Combine(songMetaPath, "metaData.json"), json);
                LoadSongs();
            }

        }

        public void OpenAddFlyout(object sender, RoutedEventArgs e)
        {
            AddNewFlyout.ShowAt((FrameworkElement)sender);
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                rightClickedContext = menuFlyoutItem.DataContext as SongMetaData;
            }
        }

        public void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "Playlists", NewItemTextBox.Text + ".txt");
                File.WriteAllText(filePath, "");
                AddNewFlyout.Hide();

                var playlistList = rightClickedContext.Playlists.Append(NewItemTextBox.Text).ToArray(); 
                JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
                JsonSerializerOptions options = jsonSerializerOptions;
                string songMetaPath = Path.Combine(AppContext.BaseDirectory, "Songs", SantizeTitle($"{rightClickedContext.Title} by {rightClickedContext.Author}"));
                MetaData metaData = new() { Title = rightClickedContext.Title, Author = rightClickedContext.Author, Duration = rightClickedContext.Duration, FileSize = rightClickedContext.FileSize, Playlists = playlistList };
                string json = JsonSerializer.Serialize(metaData);
                File.WriteAllText(Path.Combine(songMetaPath, "metaData.json"), json);
                rightClickedContext = null;
                LoadSongs();
            }

            catch (Exception ex)
            {
                DebugTextBox.Text = ex.Message;
            }
        }
    }
}

