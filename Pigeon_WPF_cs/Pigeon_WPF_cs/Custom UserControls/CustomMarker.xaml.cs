using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GMap.NET.WindowsPresentation;

namespace Pigeon_WPF_cs.Custom_UserControls
{
    /// <summary>
    /// Interaction logic for CustomMarker.xaml
    /// </summary>
    public partial class CustomMarker : UserControl
    {
        GMapMarker Marker;
        GMapControl gmapctrl;
        bool isHolding = false;
        public WaypointItem wpItem;
        public Image GetImage { get; }

        public CustomMarker(GMapControl ctrl, GMapMarker marker, BitmapImage img)
        {
            InitializeComponent();

            gmapctrl = ctrl;
            Marker = marker;
            //
            //= new DropShadowEffect()
            //{
            //    Color = Color.FromRgb(255, 255, 200),
            //    Opacity = 20,
            //    BlurRadius = 15
            //};
            wp_img.Source = img;
            GetImage = wp_img;
            wp_img.Source.Freeze();

            MouseMove += new MouseEventHandler(CustomMarkerDemo_MouseMove);
            MouseLeftButtonDown += CustomMarker_MouseLeftButtonDown;
            MouseLeftButtonUp += CustomMarker_MouseLeftButtonUp;
        }

        private void CustomMarker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => isHolding = false;

        private void CustomMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => isHolding = true;

        private void CustomMarkerDemo_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed && isHolding)
            {
                Point p = new Point(e.GetPosition(gmapctrl).X,
                    e.GetPosition(gmapctrl).Y + e.GetPosition(wp_img).Y);
                Marker.Position = gmapctrl.FromLocalToLatLng((int)p.X, (int)p.Y);
            }
        }
    }
}
