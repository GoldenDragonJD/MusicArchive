using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Collections.ObjectModel;
using static MusicArchive.Downloads;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using WinRT.Interop;
using System.Threading.Tasks;
using ABI.Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicArchive
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Playlist : Page
    {
        public ObservableCollection<PlaylistMetadata> PlaylistList { get; set; }
        private PlaylistMetadata playlistRightClicked;

        public Playlist()
        {
            this.InitializeComponent();
            PlaylistList = new ObservableCollection<PlaylistMetadata>();
            PlaylistView.ItemsSource = PlaylistList;

            LoadPlaylists();
        }

        public static string SantizeTitle(string title)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitizedTitle = new(title.Where(c => !invalidChars.Contains(c)).ToArray());
            return sanitizedTitle;
        }

        public void LoadPlaylists()
        {
            PlaylistList.Clear();
            var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Playlists"));
            foreach (var file in files)
            {
                string fullFileName = Path.GetFileName(file);
                bool anImage = fullFileName.EndsWith(".png") ? true : false;
                string fileName = anImage ? fullFileName.Replace(".png", "") : fullFileName.Replace(".txt", "");

                PlaylistList.Add(new PlaylistMetadata
                {
                    Name = fileName,
                    ImagePath = anImage ? file : null
                });
            }
        }


        public void GoToSong(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (PlaylistView.SelectedItem is PlaylistMetadata metadata)
            {
                App.MainWindow.ChangeFrame(typeof(Songs), $"PlaylistName:{metadata.Name}");
            }
        }

        public async void ChangeImage(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = sender as MenuFlyoutItem;
            PlaylistMetadata metadata = menuFlyoutItem.DataContext as PlaylistMetadata;

            FileOpenPicker fileOpenPicker = new()
            {
                ViewMode = PickerViewMode.Thumbnail,
                FileTypeFilter = { ".jpg", ".jpeg", ".png", ".gif" },
            };

            nint windowHandle = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);

            StorageFile file = await fileOpenPicker.PickSingleFileAsync();

            if (file == null)
            {
                DebugTextBlock.Text = "None";
                return;
            }

            PlaylistMetadata playlist = sender as PlaylistMetadata;

            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(AppContext.BaseDirectory, "Playlists"));
                if (metadata.ImagePath == null) File.Delete(Path.Combine(AppContext.BaseDirectory, "Playlists", metadata.Name + ".txt"));
                //if (metadata.ImagePath != null) File.Delete(Path.Combine(AppContext.BaseDirectory, "Playlists", metadata.Name + ".png"));
                StorageFile pngFile = await storageFolder.CreateFileAsync($"{metadata.Name}.png", CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream pngStream = await pngFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, pngStream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                }

                //var container = (ContentPresenter)PlaylistView.ContainerFromItem(metadata);
                //var image = (Image)container.FindName("PlaylistImage");
                //image.Source = new BitmapImage(new Uri(metadata.ImagePath));
                LoadPlaylists();
                DebugTextBlock.Text = "For changes to take effect sometimes it may require you restart the app.";
            }
        }

        public void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewItemTextBox.Text == "")
            {
                DebugTextBlock.Text = "Invalid Name";
                return;
            }

            string filePath = Path.Combine(AppContext.BaseDirectory, "Playlists", SantizeTitle(NewItemTextBox.Text) + ".txt");
            File.WriteAllText(filePath, "");
            PlaylistList.Add(new PlaylistMetadata { ImagePath = null, Name = SantizeTitle(NewItemTextBox.Text) });
            AddNewFlyout2.Hide();
        }

        public void RenameMenuOpen(object sender, RoutedEventArgs e)
        {
            double xCenter = PageWindow.ActualWidth / 2;
            double yCenter = PageWindow.ActualHeight / 2;

            FlyoutRenameMenu.ShowAt((FrameworkElement)PlaylistView, new FlyoutShowOptions
            {
                Position = new Windows.Foundation.Point
                {
                    X = xCenter,
                    Y = yCenter
                }
            });

            if (sender is MenuFlyoutItem menu)
            {
                playlistRightClicked = menu.DataContext as PlaylistMetadata;
            }
        }

        public async void RenamePlaylist(object sender, RoutedEventArgs e)
        {
            if (RenameText.Text == "" && playlistRightClicked == null)
            {
                DebugTextBlock.Text = "Invalid Name or playlistRightClicked null";
                return;
            }
            string playlistName = playlistRightClicked.ImagePath == null ? playlistRightClicked.Name + ".txt" : playlistRightClicked.Name + ".png";
            string ext = Path.GetExtension(playlistName);
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path.Combine(AppContext.BaseDirectory, "Playlists", playlistName));
            await file.RenameAsync(SantizeTitle($"{RenameText.Text}{ext}"));
            LoadPlaylists();
            FlyoutRenameMenu.Hide();
        }
    }


    public partial class SelectTemplate : DataTemplateSelector
    {
        public DataTemplate? PlaceHolderTemplate { get; set; }
        public DataTemplate? DataTemplate { get; set; }
        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is PlaylistMetadata metadata)
            {
                return metadata.ImagePath == null ? PlaceHolderTemplate : DataTemplate;
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}
