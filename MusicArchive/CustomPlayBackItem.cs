using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace MusicArchive
{
    public class CustomPlayBackItem
    {
        public MediaPlaybackItem? PlaybackItem {get;}
        public SongMetaData? SongMetaData {get;}

        public CustomPlayBackItem(MediaPlaybackItem? playbackItem, SongMetaData? songMetaData)
        {
            PlaybackItem = playbackItem;
            SongMetaData = songMetaData;
        }
    }
}
