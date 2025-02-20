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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicArchive
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class Artists : Page
    {
        public ObservableCollection<ChannelMetaData> channelList { get; set; }

        public Artists()
        {
            this.InitializeComponent();
            channelList = new ObservableCollection<ChannelMetaData>();

            var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Channels"));
            foreach (string file in files)
            {
                channelList.Add(new ChannelMetaData
                {
                    Name = Path.GetFileName(file).Replace(".png", ""),
                    Path = Path.Combine(AppContext.BaseDirectory, "Channels", file)
                });
            }
        }

        public void GrindButtonDoubbleClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (ChannelListGrid.SelectedItem is ChannelMetaData metaData)
            {
                App.MainWindow.ChangeFrame(typeof(Songs), $"ArtistName:{metaData.Name}");
            }
        }
    }
}
