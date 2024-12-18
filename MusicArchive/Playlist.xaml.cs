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
using System.Collections.ObjectModel;
using static MusicArchive.Downloads;

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

            var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Playlists"));
            foreach (var file in files)
            {
                string fullFileName = Path.GetFileName(file);
                bool anImage = fullFileName.EndsWith(".png") ? true : false;
                string fileName = anImage? fullFileName.Replace(".png", "") : fullFileName.Replace(".txt", "");

                PlaylistList.Add(new PlaylistMetadata
                {
                    Name = fileName,
                    ImagePath = anImage? file : null
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
