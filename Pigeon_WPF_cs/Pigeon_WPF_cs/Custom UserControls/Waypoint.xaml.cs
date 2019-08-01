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

using GMap;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace Pigeon_WPF_cs.Custom_UserControls
{
    /// <summary>
    /// Interaction logic for Waypoint.xaml
    /// </summary>
    public partial class Waypoint : UserControl
    {
        private double lat = -7.275869, longt = 112.794307;
        public Waypoint()
        {
            InitializeComponent();
            setHomePos(lat, longt);

            double markerWidth = 40, markerHeight = 40;
            thePoints = new List<PointLatLng>();
            currpos = new GMapMarker(new PointLatLng(0, 0))
            {
                Shape = new Image
                {
                    Width = markerWidth,
                    Height = markerHeight,
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/marker-waypoint.png"))
                },
                Offset = new Point(-markerWidth / 2, -markerHeight)
            }; mapView.Markers.Add(currpos);
            
        }

        #region GMap.NET Implementation

        private void mapView_Loaded(object sender, RoutedEventArgs e)
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            mapView.MultiTouchEnabled = true;
            mapView.MapProvider = ArcGIS_StreetMap_World_2D_MapProvider.Instance;
            mapView.MinZoom = 2;
            mapView.MaxZoom = 20;
            mapView.Zoom = 5;
            mapView.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            mapView.CanDragMap = true;
            mapView.DragButton = MouseButton.Left;
            mapView.IgnoreMarkerOnMouseWheel = true;
            mapView.ShowCenter = false;
            mapView.Position = new PointLatLng(lat, longt);
       
        }
        #endregion

        #region Main Functions

        private void setHomePos(double homelat, double homelongt)
        {
            double markerWidth = 40, markerHeight = 40;
            GMapMarker homeIco = new GMapMarker(new PointLatLng(homelat, homelongt))
            {
                Shape = new Image
                {
                    Width = markerWidth,
                    Height = markerHeight,
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/marker-waypoint.png"))
                },
                Offset = new Point(-markerWidth / 2, -markerHeight)
            };

            GMapMarker homeLbl = new GMapMarker(new PointLatLng(homelat, homelongt))
            {
                Shape = new Label
                {
                    Content = "HOME",
                    Width = markerWidth,
                    Height = markerHeight,
                    Padding = new Thickness(0),
                    FontSize = 16,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Top
                },
                //Offset = new System.Windows.Point(-markerWidth / 2, 0)
            };
            mapView.Markers.Add(homeIco);
            mapView.Markers.Add(homeLbl);
        }

        GMapMarker currpos;
        public void setCurrentPos(double currlat, double currlongt, float heading)
        {
            currpos.Position = new PointLatLng(currlat, currlongt);
            currpos.Shape.RenderTransform = new RotateTransform(heading);
        }

        //place waypoint at mouse double click point
        short routenum = 1;
        short waynum = 1;
        private void MapView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //waypointSelMenu.Items.Add("Testing");
            //waypointSelMenu.Items.Add("Testing");
            //waypointSelMenu.Items.Add("Testing");
            Point addMark = e.GetPosition(mapView);
            PointLatLng addMarkP = mapView.FromLocalToLatLng(Convert.ToInt32(addMark.X), Convert.ToInt32(addMark.Y));
            AddMarkerAt(addMarkP,waynum++,routenum++);
            Console.WriteLine(addMarkP.Lat + " " + addMarkP.Lng);
        }

        //waypoint placing
        List<PointLatLng> thePoints;
        private void AddMarkerAt(PointLatLng theLatLng, short waynum, short routenum)
        {
            short markerWidth = 40, markerHeight = 40;
            GMapMarker theMarkerimg = new GMapMarker(theLatLng)
            {
                Shape = new System.Windows.Controls.Image
                {
                    Width = markerWidth,
                    Height = markerHeight,
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/marker-waypoint.png"))
                },
                Offset = new System.Windows.Point(-markerWidth / 2, -markerHeight),
                Tag = "way" + waynum.ToString()
            };
            GMapMarker theMarkerlbl = new GMapMarker(theLatLng)
            {
                Shape = new Label
                {
                    Content = "Waypoint " + waynum.ToString(),
                    Width = markerWidth,
                    Height = markerHeight,
                    Padding = new Thickness(0),
                    FontSize = 14,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Top
                },
                Offset = new Point(-markerWidth / 2, 0),
                Tag = "way" + waynum.ToString()
            };
            mapView.Markers.Add(theMarkerimg);
            mapView.Markers.Add(theMarkerlbl);

            thePoints.Add(theLatLng);
            GMapRoute theRoute = new GMapRoute(thePoints);
            //mapView.Markers.RemoveAt(theRoute);
            mapView.Markers.Remove(theRoute);
            mapView.Markers.Add(theRoute);
        }

        private void chooseMap(object sender, SelectionChangedEventArgs e)
        {
            ComboBox mapChoose = (ComboBox)sender;
            switch (mapChoose.SelectedIndex)
            {
                case 0:
                    mapView.MapProvider = ArcGIS_World_Topo_MapProvider.Instance;
                    break;
                case 1:
                    mapView.MapProvider = ArcGIS_Imagery_World_2D_MapProvider.Instance;
                    break;
                case 2:
                    mapView.MapProvider = ArcGIS_StreetMap_World_2D_MapProvider.Instance;
                    break;
                case 3:
                    mapView.MapProvider = GoogleMapProvider.Instance;
                    break;
                case 4:
                    mapView.MapProvider = GoogleSatelliteMapProvider.Instance;
                    break;
                case 5:
                    mapView.MapProvider = GoogleTerrainMapProvider.Instance;
                    break;
                case 6:
                    mapView.MapProvider = GoogleHybridMapProvider.Instance;
                    break;
            }
        }

        #endregion
    }
}
