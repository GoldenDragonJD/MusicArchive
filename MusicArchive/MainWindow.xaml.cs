using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.UI.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Microsoft.UI.Input;
using System.Globalization;
using Windows.ApplicationModel;
using ABI.Windows.Foundation;
using Windows.Networking.XboxLive;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicArchive
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private List<SongMetaData> SongsQueued = new(); //List of songs>
        private bool isSliderBeingDragged = false;
        private int SongIndex = 0;
        private MediaPlaybackList PlaybackList;
        //private bool isSwitching = false;

        public MainWindow()
        {
            this.InitializeComponent();

            string SongFolderName = "Songs";
            string ChannelFolderName = "Channels";
            string playlistFolderName = "Playlists";

            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, SongFolderName)))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, SongFolderName));
            }

            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, ChannelFolderName)))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, ChannelFolderName));
            }

            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, playlistFolderName)))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, playlistFolderName));
            }

            NavigationView.SelectedItem = NavigationView.MenuItems[0];
            SongSlider.AddHandler(Button.PointerPressedEvent, new PointerEventHandler(SliderPointerPressed), true);
            SongSlider.AddHandler(Button.PointerReleasedEvent, new PointerEventHandler(SliderPointerReleased), true);
        }

        public void NavigationChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            switch (args.SelectedItemContainer.Name)
            {
                case "Playlist_Tab":
                    ContentFrame.Navigate(typeof(Playlist));
                    break;
                case "Songs_Tab":
                    ContentFrame.Navigate(typeof(Songs), "");
                    break;
                case "Artists_Tab":
                    ContentFrame.Navigate(typeof(Artists));
                    break;
                case "Downloads_Tab":
                    ContentFrame.Navigate(typeof(Downloads));
                    break;
                default:
                    break;
            }
        }

        public void ChangeFrame(Type pageType, string e)
        {
            NavigationView.SelectedItem = NavigationView.MenuItems[0];
            ContentFrame.Navigate(pageType, e);
        }
        public void MediaPlayerOpened(MediaPlayer sender, object args)
        {
            SongTime.Text = "Triggered at all";
            var duration = sender.PlaybackSession.NaturalDuration;
            SongTime.Text = $"00:00 / {duration:mm\\:ss}";
            //DebugTextBlock.Text += " | Media has been opened. Duration: " + duration.ToString(@"mm\:ss");
        }

        public void PlayBackPossitionChanged(MediaPlaybackSession sender, object args)
        {
            var position = sender.Position;
            var duration = sender.NaturalDuration;

            // Update current position and total duration text
            SongTime.Text = $"{position:mm\\:ss} / {duration:mm\\:ss}";
            //DebugTextBlock.Text = " | Position changed: " + position.ToString(@"mm\\:ss");
        }

        public async void LoadSongs(MediaPlaybackList playbackList, CustomPlayBackItem customPlayBackItem, List<CustomPlayBackItem> customPlayBackItems, bool shuffled)
        {
            SongPlayer.MediaPlayer.Dispose();
            SongPlayer.SetMediaPlayer(new MediaPlayer());
            PlaybackList = playbackList;
            PlaybackList.ShuffleEnabled = shuffled;
            SongPlayer.MediaPlayer.Source = PlaybackList;
            PlaybackList.CurrentItemChanged += async (s, args) =>
            {
                if (args.NewItem != null)
                {
                    var customItem = customPlayBackItems.FirstOrDefault(ci => ci.PlaybackItem == args.NewItem);
                    if (customItem != null)
                    {
                        await UpdateNowPlayingInfo(customItem.SongMetaData);
                    }
                }
            };
            SongPlayer.MediaPlayer.Volume = VolumeSlider.Value / 100;
            SongPlayer.MediaPlayer.Play();
            positionTimer.Interval = TimeSpan.FromMilliseconds(200); // Poll every 200ms
            positionTimer.Tick += (sender, e) =>
            {
                if (!isSliderBeingDragged) // Only update slider if not dragged by the user
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        SongSlider.Maximum = SongPlayer.MediaPlayer.NaturalDuration.TotalSeconds;
                        SongSlider.Value = SongPlayer.MediaPlayer.Position.TotalSeconds;
                        SongPlayer.MediaPlayer.Volume = VolumeSlider.Value / 100;
                        SongTime.Text = $"{SongPlayer.MediaPlayer.Position:mm\\:ss} / {SongPlayer.MediaPlayer.NaturalDuration:mm\\:ss}";
                    });
                }
            };
            positionTimer.Start();


            PlaySymbol.Symbol = Symbol.Pause;

            PlayButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            SkipBackButton.IsEnabled = true;
            SkipForwardButton.IsEnabled = true;
            SongSlider.IsEnabled = true;
        }

        private async Task UpdateNowPlayingInfo(SongMetaData songMetaData)
        {
            if (songMetaData == null) return;

            DispatcherQueue.TryEnqueue(async () =>
            {
                SongName.Text = songMetaData.Title;
                ArtistName.Text = songMetaData.Author;
                Uri imageUri = new Uri(songMetaData.ThumbnailPath ?? "");
                var file = await StorageFile.GetFileFromPathAsync(songMetaData.ThumbnailPath);
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new();
                    await bitmapImage.SetSourceAsync(stream);
                    CoverArt.Source = bitmapImage;
                }
            });
        }

        private DispatcherTimer positionTimer = new DispatcherTimer();

        //public void PlayNextSong()
        //{
        //    if (isSwitching) return;
        //    if (SongPlayer.MediaPlayer == null) return;
        //    if (SongPlayer.MediaPlayer.NaturalDuration.TotalSeconds != SongPlayer.MediaPlayer.Position.TotalSeconds) return;

        //    SongIndex += 1;
        //    if (SongIndex == SongsQueued.Count) SongIndex = 0;

        //    //SongPlayer?.MediaPlayer?.Dispose();
        //    //CoverArt.Source = new BitmapImage(new Uri("ms-appx:///Assets/placeHolder.png"));
        //    //SongName.Text = "None";
        //    //ArtistName.Text = "None";
        //    //SongTime.Text = "00:00 / 00:00";
        //    ////positionTimer.Stop();
        //    //SongSlider.Value = 0;
        //    //PlaySymbol.Symbol = Symbol.Play;
        //    //SongSlider.Maximum = 1;

        //    //PlayButton.IsEnabled = false;
        //    //StopButton.IsEnabled = false;
        //    //SkipBackButton.IsEnabled = false;
        //    //SkipForwardButton.IsEnabled = false;
        //    //SongSlider.IsEnabled = false;
        //    PlaySong(SongsQueued[SongIndex]);
        //}

        public async Task PlayPreview(YouTubeVideo youTube)
        {
            try
            {
                SongName.Text = youTube.Title;
                ArtistName.Text = youTube.Author;
                YoutubeClient youtubeClient = new YoutubeClient();
                var results = await youtubeClient.Videos.Streams.GetManifestAsync(youTube.Id);
                var audioStreamUrl = results.GetAudioOnlyStreams().GetWithHighestBitrate().Url;
                //DebugTextBlock.Text = audioStreamUrl;

                // Register event handlers

                // Set the source and start playback
                CoverArt.Source = new BitmapImage(new Uri(youTube.ThumbnailUrl));
                SongPlayer.MediaPlayer.Dispose();
                SongPlayer.SetMediaPlayer(new MediaPlayer());
                SongPlayer.Source = MediaSource.CreateFromUri(new Uri(audioStreamUrl));
                SongPlayer.MediaPlayer.Volume = VolumeSlider.Value / 100;
                SongPlayer.MediaPlayer.Play();
                positionTimer.Interval = TimeSpan.FromMilliseconds(200); // Poll every 200ms
                positionTimer.Tick += (sender, e) =>
                {
                    if (!isSliderBeingDragged) // Only update slider if not dragged by the user
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            SongSlider.Maximum = SongPlayer.MediaPlayer.NaturalDuration.TotalSeconds;
                            SongSlider.Value = SongPlayer.MediaPlayer.Position.TotalSeconds;
                            SongPlayer.MediaPlayer.Volume = VolumeSlider.Value / 100;

                            if (SongPlayer.MediaPlayer.CurrentState == MediaPlayerState.Paused) PlaySymbol.Symbol = Symbol.Play;

                            SongTime.Text = $"{SongPlayer.MediaPlayer.Position:mm\\:ss} / {SongPlayer.MediaPlayer.NaturalDuration:mm\\:ss}";
                        });
                    }
                };
                positionTimer.Start();


                PlaySymbol.Symbol = Symbol.Pause;

                PlayButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                SkipBackButton.IsEnabled = true;
                SkipForwardButton.IsEnabled = true;
                SongSlider.IsEnabled = true;
            }
            catch (Exception ex)
            {
                DebugTextBlock.Text = ex.Message;
            }
        }

        public async Task PlaySong(SongMetaData songMetaData)
        {
            try
            {
                PlaybackList = null;
                Uri imageUri = new Uri(songMetaData.ThumbnailPath ?? "");
                var file = await StorageFile.GetFileFromPathAsync(songMetaData.ThumbnailPath);
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new();
                    await bitmapImage.SetSourceAsync(stream);
                    CoverArt.Source = bitmapImage;
                }

                SongName.Text = songMetaData.Title;
                ArtistName.Text = songMetaData.Author;

                // Register event handlers

                // Set the source and start playback
                SongPlayer.MediaPlayer.Dispose();
                SongPlayer.SetMediaPlayer(new MediaPlayer());
                SongPlayer.Source = MediaSource.CreateFromUri(new Uri(songMetaData.SongPath));
                SongPlayer.MediaPlayer.Volume = VolumeSlider.Value / 100;
                SongPlayer.MediaPlayer.Play();
                positionTimer.Interval = TimeSpan.FromMilliseconds(200); // Poll every 200ms
                positionTimer.Tick += (sender, e) =>
                {
                    if (!isSliderBeingDragged) // Only update slider if not dragged by the user
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            SongSlider.Maximum = SongPlayer.MediaPlayer.NaturalDuration.TotalSeconds;
                            SongSlider.Value = SongPlayer.MediaPlayer.Position.TotalSeconds;
                            SongPlayer.MediaPlayer.Volume = VolumeSlider.Value / 100;

                            if (SongPlayer.MediaPlayer.CurrentState == MediaPlayerState.Paused) PlaySymbol.Symbol = Symbol.Play;

                            SongTime.Text = $"{SongPlayer.MediaPlayer.Position:mm\\:ss} / {SongPlayer.MediaPlayer.NaturalDuration:mm\\:ss}";
                        });
                    }
                };
                positionTimer.Start();


                PlaySymbol.Symbol = Symbol.Pause;

                PlayButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                SkipBackButton.IsEnabled = true;
                SkipForwardButton.IsEnabled = true;
                SongSlider.IsEnabled = true;

                //VolumeSlider.IsEnabled = true;
            }
            catch (Exception ex)
            {
                DebugTextBlock.Text = ex.Message;
            }
        }

        public void SliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (isSliderBeingDragged)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(e.NewValue);
                SongPlayer.MediaPlayer.PlaybackSession.Position = newPosition;
                var seconds = (int)e.NewValue;

                SongTime.Text = $"{newPosition:mm\\:ss} / {SongPlayer.MediaPlayer.PlaybackSession.NaturalDuration:mm\\:ss}";
            }
        }

        public void SliderPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            positionTimer.Stop();
            isSliderBeingDragged = true;
        }

        public void SliderPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                positionTimer.Start();
                isSliderBeingDragged = false;
                if (SongPlayer.MediaPlayer == null) return;
                SongPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SongSlider.Value);
            }
            catch (Exception ex)
            {
                DebugTextBlock.Text = ex.Message;
            }
        }

        public void PlayButtonClicked(object sender, RoutedEventArgs e)
        {
            if (SongPlayer.MediaPlayer.CurrentState.ToString() == "Paused")
            {
                SongPlayer.MediaPlayer.Play();
                PlaySymbol.Symbol = Symbol.Pause;
            }
            else
            {
                SongPlayer.MediaPlayer.Pause();
                PlaySymbol.Symbol = Symbol.Play;
            }
        }

        public void StopButtonClicked(object sender, RoutedEventArgs e)
        {
            SongPlayer.MediaPlayer.Source = null;
            CoverArt.Source = new BitmapImage(new Uri("ms-appx:///Assets/placeHolder.png"));
            SongName.Text = "None";
            ArtistName.Text = "None";
            SongTime.Text = "00:00 / 00:00";
            positionTimer.Stop();
            SongSlider.Value = 0;
            PlaySymbol.Symbol = Symbol.Play;
            SongSlider.Maximum = 1;

            PlayButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            SkipBackButton.IsEnabled = false;
            SkipForwardButton.IsEnabled = false;
            SongSlider.IsEnabled = false;
            PlaybackList = null;
            //VolumeSlider.IsEnabled = false;
        }

        public void NextButtonClicked(object sender, RoutedEventArgs e)
        {
            if (PlaybackList == null)
            {
                SongPlayer.MediaPlayer.Position = SongPlayer.MediaPlayer.NaturalDuration;
                return;
            }
            PlaybackList.MoveNext();
        }

        public void BackButtonClicked(object sender, RoutedEventArgs e)
        {
            if (PlaybackList == null)
            {
                SongPlayer.MediaPlayer.Position = TimeSpan.Zero;
                return;
            }
            PlaybackList.MovePrevious();
        }

        public void VolumeControlSliderChange(object sender, RangeBaseValueChangedEventArgs e)
        {
            //if (sender is Slider volumeSlider)
            //{
            //    if (SongPlayer?.MediaPlayer?.PlaybackSession == null) return;
            //    SongPlayer.MediaPlayer.Volume = volumeSlider.Value / 100;
            //}
        }
    }
}
