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

        public Playlist()
        {
            this.InitializeComponent();
            PlaylistList = new ObservableCollection<PlaylistMetadata>();
            PlaylistView.ItemsSource = PlaylistList;

            LoadPlaylists();
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
                //DebugTextBlock.Text = "For changes to take effect sometimes it may require you restart the app.";
            }
        }

        public void AddPlaylist(object sender, RoutedEventArgs e)
        {
            var mainWindow = PageWindow;

            double ScreenWidth = mainWindow.ActualWidth;
            double ScreenHeight = mainWindow.ActualWidth;

            double centerX = ScreenWidth / 2;
            double centerY = ScreenHeight / 2;

            Windows.Foundation.Point centerPoint = new Windows.Foundation.Point(centerX, centerY);

            AddNewFlyout.ShowAt((FrameworkElement)sender, new FlyoutShowOptions { Position = centerPoint });
        }

        public async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {

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
