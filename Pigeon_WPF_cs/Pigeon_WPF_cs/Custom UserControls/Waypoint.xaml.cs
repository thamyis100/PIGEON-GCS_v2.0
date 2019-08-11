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
using System.Windows.Threading;
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
        double markerWidth = 50, markerHeight = 50;
        public Waypoint()
        {
            InitializeComponent();
            setHomePos(lat, longt);
            thePoints = new List<PointLatLng>();
        }

        #region GMap.NET Implementation

        private void mapView_Loaded(object sender, RoutedEventArgs e)
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            //mapView.CacheLocation = "@pack://application:,,,/Resources/MapCache/";
            mapView.MultiTouchEnabled = true;
            mapView.MapProvider = ArcGIS_World_Topo_MapProvider.Instance;
            mapView.MinZoom = 2;
            mapView.MaxZoom = 19;
            mapView.Zoom = 15;
            mapView.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            mapView.CanDragMap = true;
            mapView.DragButton = MouseButton.Left;
            mapView.IgnoreMarkerOnMouseWheel = true;
            mapView.ShowCenter = false;
            mapView.Position = new PointLatLng(lat, longt);
       
        }
        #endregion

        #region List of default Markers

        private GMapMarker homeIco, poswahana;

        #endregion

        #region Main Functions

        private void setHomePos(double homelat, double homelongt)
        {
            double markerWidth = 40, markerHeight = 40;
            homeIco = new GMapMarker(new PointLatLng(homelat, homelongt))
            {
                Tag = "Home Pos",
                Shape = new Image
                {
                    Width = markerWidth,
                    Height = markerHeight,
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/home-ico.png")),
                    ToolTip = "HOME"
                },
                Offset = new Point(-markerWidth / 2, -markerHeight / 2)
            };
            mapView.Markers.Add(homeIco);

            GMapMarker homeLbl = new GMapMarker(new PointLatLng(homelat, homelongt))
            {
                Shape = new Label
                {
                    Content = "HOME",
                    Width = markerWidth,
                    Height = markerHeight,
                    Padding = new Thickness(0),
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Top,
                },
                Offset = new System.Windows.Point(-markerWidth / 2, (markerHeight / 2) - 5)
            };

            mapView.Markers.Add(homeLbl);
        }

        public void StartCurrentPos(double currlat, double currlongt, float heading)
        {
            poswahana = new GMapMarker(new PointLatLng(currlat, currlongt))
            {
                Tag = "Current Position",
                Shape = new Image
                {
                    Width = markerWidth,
                    Height = markerHeight,
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/ikon-wahana-pesawat-1.png"))
                },
                Offset = new Point(-markerWidth / 2, -markerHeight / 2)
            };
            RenderOptions.SetBitmapScalingMode(poswahana.Shape, BitmapScalingMode.HighQuality);

            mapView.Markers.Add(poswahana);
        }

        public void setPosWahana(double currlat, double currlongt, float bearing)
        {
            poswahana.Position = new PointLatLng(currlat, currlongt);
            poswahana.Shape.RenderTransformOrigin = new Point(0.5, 0.5);
            poswahana.Shape.RenderTransform = new RotateTransform(bearing);
        }

        //place waypoint at mouse double click point
        short routeIndex = 0;
        short wayIndex = 0;
        private void MapView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point addMark = e.GetPosition(mapView);
            PointLatLng addMarkP = mapView.FromLocalToLatLng(Convert.ToInt32(addMark.X), Convert.ToInt32(addMark.Y));
            //AddMarkerAt(addMarkP,wayIndex++,routeIndex++);
            Console.WriteLine("Double clicked at : " + addMarkP.Lat + " " + addMarkP.Lng);
        }

        //waypoint placing
        private List<PointLatLng> thePoints;
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
