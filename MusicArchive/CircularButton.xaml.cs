using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Content;

namespace MusicArchive
{
    public sealed partial class CircularButton : UserControl
    {
        public CircularButton()
        {
            this.InitializeComponent();

        }

        public SymbolIcon SymbolIcon
        {
            get { return (SymbolIcon)GetValue(SymbolIconProperty); }
            set { SetValue(SymbolIconProperty, value); }
        }

        public static readonly DependencyProperty SymbolIconProperty =
            DependencyProperty.Register("SymbolIcon", typeof(SymbolIcon), typeof(CircularButton), new PropertyMetadata(null));
    }
}
