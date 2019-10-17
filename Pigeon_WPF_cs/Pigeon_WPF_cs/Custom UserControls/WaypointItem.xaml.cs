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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace Pigeon_WPF_cs.Custom_UserControls
{
    /// <summary>
    /// Interaction logic for WaypointItem.xaml
    /// </summary>
    public partial class WaypointItem : UserControl
    {
        public WaypointItem(GMapMarker marker)
        {
            InitializeComponent();
            TheMarkerHolder = marker;
            Image img;
            try
            {
                img = ((CustomMarker)(marker.Shape)).GetImage;
            }catch(Exception exc)
            {
                Console.WriteLine("Not using custommarker, use normal image shape instead");
                img = (Image)marker.Shape;
            }
            wp_ikon.Source = img.Source;
            wp_name.Text = '#' + marker.Tag.ToString();
            wp_lat.Text = marker.Position.Lat.ToString("0.#########");
            wp_longt.Text = marker.Position.Lng.ToString("0.#########");
        }
        
        public void SetProperties(PointLatLng latlng)
        {
            wp_lat.Text = latlng.Lat.ToString("0.#########");
            wp_longt.Text = latlng.Lng.ToString("0.#########");
        }

        /// <summary>
        /// Gets the marker that is in current WaypointItem
        /// </summary>
        public GMapMarker TheMarkerHolder { get; }
    }
}
