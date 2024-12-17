using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicArchive
{
    public class TimeFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double seconds) // The Slider's value is passed as a double
            {
                var timeSpan = TimeSpan.FromSeconds(seconds);
                return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"; // mm:ss format
            }

            return "00:00"; // Default fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // Not required for one-way binding
        }
    }
}
