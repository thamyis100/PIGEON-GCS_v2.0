using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
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
        private const double idn_lat = -1.5437881d, idn_lon = 119.095757d;
        const double MarkerWidth = 50, MarkerHeight = 50;

        public Waypoint()
        {
            DataContext = this;

            InitializeComponent();
        }

        #region Predefined markers functions

        private GMapMarker HomeMarker, WahanaMarker, TrackerMarker;
        private GMapRoute Tracker_Wahana, FlightTrail;

        private List<PointLatLng> TrailPoints;
        private List<PointLatLng> TrackingPoints;

        /// <summary>
        /// Set koordinat awal titik Home sejak terbang pertama
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        private void SetHomePos(double lat, double lon)
        {
            HomeMarker = new GMapMarker(new PointLatLng(lat, lon))
            {
                Tag = "Home Pos",
                Shape = new Image
                {
                    Width = MarkerWidth,
                    Height = MarkerHeight,
                    Source = Properties.Resources.home_ico.ToBitmapSource(),
                    ToolTip = "HOME"
                },
                Offset = new Point(-MarkerWidth / 2.0, -MarkerHeight / 2.0)
            };

            mapView.Markers.Add(HomeMarker);
        }

        /*
        public void doRotateHomepos()
        {

            var rotateAnim = new DoubleAnimation()
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1)
            };
            var pindahAnimX = new DoubleAnimation()
            {
                From = lastpos.X,
                To = lastpos.X + 0.001,
                Duration = TimeSpan.FromSeconds(1)
            };
            var pindahAnimY = new DoubleAnimation()
            {
                From = lastpos.Y,
                To = lastpos.Y + 0.001,
                Duration = TimeSpan.FromSeconds(1)
            };

            //Debug.WriteLine(pindahAnimX.To.ToString() + " " + pindahAnimY.To.ToString());

            var rotateTrans = new RotateTransform();
            HomePos.Shape.RenderTransformOrigin = new Point(0.5, 0.5);
            HomePos.Shape.RenderTransform = rotateTrans;
            rotateTrans.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);

            var positionWahana = new TranslateTransform();
            positionWahana.Changed += new EventHandler(PindahMap);
            positionWahana.BeginAnimation(TranslateTransform.XProperty, pindahAnimX);
            positionWahana.BeginAnimation(TranslateTransform.YProperty, pindahAnimY);
            lastpos.X += 0.001; lastpos.Y += 0.001;
        }
        */

        /// <summary>
        /// Set koordinat & posisi awal wahana
        /// </summary>
        public void StartPosWahana()
        {
            TrailPoints = new List<PointLatLng>();

            if (WahanaMarker != null)
                mapView.Markers.Remove(WahanaMarker);

            WahanaMarker = new GMapMarker(new PointLatLng(App.Wahana.GPS.Latitude / 10000000.0, App.Wahana.GPS.Longitude / 10000000.0))
            {
                Tag = "Posisi Wahana",
                Shape = new Image
                {
                    Width = MarkerWidth,
                    Height = MarkerHeight,
                    Source = Properties.Resources.ikon_quadcopter.ToBitmapSource(),
                    ToolTip = "Quadcopter",
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(App.Wahana.IMU.Yaw)
                },
                Offset = new Point(-MarkerWidth / 2.0, -MarkerHeight / 2.0)
            };

            RenderOptions.SetBitmapScalingMode(WahanaMarker.Shape, BitmapScalingMode.HighQuality);

            mapView.Markers.Add(WahanaMarker);

            FlightTrail = new GMapRoute(TrailPoints)
            {
                Tag = "Trail"
            };

            mapView.Markers.Add(FlightTrail);
        }

        bool IsIkutiWahana = false;

        /// <summary>
        /// Set koordinat & posisi wahana
        /// </summary>
        public void UpdatePosWahana()
        {
            if (WahanaMarker == null)
            {
                SetHomePos(App.Wahana.GPS.Latitude / 10000000.0, App.Wahana.GPS.Longitude / 10000000.0);
                StartPosWahana();
            }

            WahanaMarker.Position = new PointLatLng(App.Wahana.GPS.Latitude / 10000000.0, App.Wahana.GPS.Longitude / 10000000.0);

            WahanaMarker.Shape.RenderTransform = new RotateTransform(App.Wahana.IMU.Yaw);

            if (IsIkutiWahana)
                mapView.CenterPosition = mapView.Position = WahanaMarker.Position;

            FlightTrail.Points.Add(new PointLatLng(App.Wahana.GPS.Latitude / 10000000.0, App.Wahana.GPS.Longitude / 10000000.0));
        }

        public void ResetMarkers()
        {

        }

        /// <summary>
        /// Set koordinat awal Tracker
        /// </summary>
        public void StartPosTracker()
        {
            if (TrackerMarker != null)
                mapView.Markers.Remove(TrackerMarker);

            TrackerMarker = new GMapMarker(new PointLatLng(App.Tracker.GPS.Latitude / 10000000.0, App.Tracker.GPS.Longitude / 10000000.0))
            {
                Tag = "Tracker",
                Shape = new Image
                {
                    Width = MarkerWidth,
                    Height = MarkerHeight,
                    Source = Properties.Resources.tracker_ico.ToBitmapSource(),
                    ToolTip = "Antenna Tracker",
                    RenderTransformOrigin = new Point(0.5, 1)
                },
                Offset = new Point(-MarkerWidth / 2.0, -MarkerHeight)
            };

            RenderOptions.SetBitmapScalingMode(TrackerMarker.Shape, BitmapScalingMode.HighQuality);

            mapView.Markers.Add(TrackerMarker);
        }

        /// <summary>
        /// Set koordinat & posisi tracker
        /// </summary>
        internal void UpdatePosTracker()
        {
            if (TrackerMarker == null)
            {
                StartPosTracker();
            }

            TrackerMarker.Position = new PointLatLng(App.Tracker.GPS.Latitude / 10000000.0, App.Tracker.GPS.Longitude / 10000000.0);

            TrackerMarker.Shape.RenderTransform = new RotateTransform(App.Tracker.IMU.Yaw);

            if (WahanaMarker != null && TrackingPoints == null)
            {
                TrackingPoints = new List<PointLatLng>();
                TrackingPoints.Add(TrackerMarker.Position);
                TrackingPoints.Add(WahanaMarker.Position);

                Tracker_Wahana = new GMapRoute(TrackingPoints);
                mapView.Markers.Add(Tracker_Wahana);
            }
        }

        public void SetArahTracker(float bearing)
        {
            //if (!isAdaWahana && WahanaMarker != null)
            //{
            //    isAdaWahana = true;
            //    List<PointLatLng> pos = new List<PointLatLng> { TrackerMarker.Position, WahanaMarker.Position };
            //    Tracker_Wahana = new GMapRoute(pos);
            //    mapView.Markers.Add(Tracker_Wahana);
            //    return;
            //}
            //else if (WahanaMarker == null) { return; }

            //Tracker_Wahana.Points.RemoveAt(1);

            TrackerMarker.Shape.RenderTransform = new RotateTransform(bearing);
        }
        
        private void IkutiWahana_Clicked(object sender, RoutedEventArgs e)
        {
            if (WahanaMarker == null)
            {
                MessageBox.Show("Silakan memulai terbang terlebih dahulu.", "Wahana tidak tersedia");
                return;
            }

            IsIkutiWahana = !IsIkutiWahana;

            /*var move = new DoubleAnimation()
            {
                From = 0,
                To = 30,
                Duration = TimeSpan.FromSeconds(3)
            };

            var dump = new RotateTransform();
            
            dump.Changed += new EventHandler(GeserMap);
            dump.BeginAnimation(RotateTransform.AngleProperty, move);
            */

            mapView.CenterPosition = mapView.Position = WahanaMarker.Position;
        }

        private void GeserMapAnim(object sender, EventArgs e)
        {
            //Debug.WriteLine("PINDAH");
            //mapView.CenterPosition = mapView.Position = WahanaMarker.Position;
        }

        #endregion


        #region GMap.NET Implementation

        private void MapView_Loaded(object sender, RoutedEventArgs e)
        {
            mapView.ShowCenter = false;
            mapView.CacheLocation = System.IO.Path.GetDirectoryName(App.DocsPath + "/MapCache/");
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            mapView.MultiTouchEnabled = true;
            ChooseMap(null, null);
            mapView.MinZoom = 3;
            mapView.MaxZoom = 25;
            mapView.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
            mapView.CanDragMap = true;
            mapView.DragButton = MouseButton.Left;
            mapView.IgnoreMarkerOnMouseWheel = true;
            mapView.Markers.CollectionChanged += UpdateWaypointList;

            mapView.Position = new PointLatLng(idn_lat, idn_lon);
            mapView.Zoom = 5.75f;
        }

        private void ChooseMap(object sender, SelectionChangedEventArgs e)
        {
            switch (cb_map_type.SelectedIndex)
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


        #region Waypoint functions

        GMapRoute WP_Trail;

        //waypoint placing
        private List<PointLatLng> WP_Points = new List<PointLatLng>();

        int WaypointNum = 0;

        // Place waypoint at mouse double click point
        private void AddWaypoint_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point marker_point = e.GetPosition(mapView);
            
            PointLatLng marker_koor = mapView.FromLocalToLatLng(Convert.ToInt32(marker_point.X), Convert.ToInt32(marker_point.Y));

            WaypointNum++;

            var addedMarker = new GMapMarker(marker_koor)
            {
                Offset = new System.Windows.Point(-MarkerWidth / 2.0, -MarkerHeight),
                Tag = "WP" + WaypointNum.ToString()
            };
            addedMarker.Shape = new CustomMarker(mapView, addedMarker, (BitmapImage)GetWaypointIkon(Properties.Resources.marker_waypoint.ToBitmapSource(), WaypointNum));
            
            mapView.Markers.Add(addedMarker);

            if (WP_Trail == null)
            {
                WP_Trail = new GMapRoute(WP_Points)
                {
                    Shape = new System.Windows.Shapes.Path()
                    {
                        Stroke = Brushes.Orange,
                        StrokeThickness = 3,
                        //Effect = new DropShadowEffect() { Color = Color.FromRgb(0, 0, 0) }
                    },
                    Tag = "WP_Trail"
                };
                mapView.Markers.Add(WP_Trail);
            }

            WP_Points.Add(marker_koor);
        }

        private bool IsWPDockHidden = true;

        private void ToggleWPDock(object sender, RoutedEventArgs e)
        {
            var nextHeight = 25;

            if (IsWPDockHidden)
            {
                nextHeight = 300;
                wp_dock_btn.Content = "Markers \u25BC";
            }
            else
            {
                wp_dock_btn.Content = "Markers \u25B2";
            }

            IsWPDockHidden = !IsWPDockHidden;

            wp_dock.BeginAnimation(HeightProperty, new DoubleAnimation()
            {
                From = wp_dock.Height,
                To = nextHeight,
                Duration = TimeSpan.FromMilliseconds(100)
            });

            wp_dock.Height = nextHeight;
        }

        short CurrRouteIndex = 0;
        short CurrWayIndex = 0;

        private void UpdateWaypointList(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var themarker in e.NewItems)
                        {
                            switch (themarker.GetType().Name.ToString())
                            {
                                case "GMapMarker":
                                    var str = ((GMapMarker)themarker).Tag.ToString().ToLower().Replace(" ", "");
                                    Debug.WriteLine("\nAdded new marker: " + str);

                                    var wpitem = new WaypointItem((GMapMarker)themarker) { Name = str };
                                    wp_dock_stack.Children.Add(wpitem);

                                    try { ((CustomMarker)((GMapMarker)themarker).Shape).wpItem = wpitem; }
                                    catch { }
                                    break;
                                case "GMapRoute":
                                    //Debug.WriteLine("Added route: " + ((GMapRoute)themarker).Tag.ToString());
                                    break;
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var item in e.OldItems)
                        {
                            switch (item.GetType().Name.ToString())
                            {
                                case "GMapMarker":
                                    var str = ((GMapMarker)item).Tag.ToString().ToLower().Replace(" ", "");
                                    Debug.WriteLine("Removed: " + str);
                                    foreach (WaypointItem stack in wp_dock_stack.Children)
                                    {
                                        if (stack.Name.ToString() == str) wp_dock_stack.Children.Remove(stack);
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.StackTrace);
                return;
            }
        }

        private void SendWaypoint(GMapMarker marker)
        {
            var win = (MainWindow)Window.GetWindow(this);
            win.flight_Ctrl.SendToConnection(marker);
        }
        
        private object GetWaypointIkon(BitmapSource img, int wp_num)
        {
            var target = new RenderTargetBitmap(img.PixelWidth, img.PixelHeight, img.DpiX, img.DpiY, PixelFormats.Pbgra32);

            var visual = new DrawingVisual();

            using (var drawer = visual.RenderOpen())
            {
                drawer.DrawImage(img, new Rect(0, 0, img.Width, img.Height));
                drawer.DrawEllipse(Brushes.White, null, new Point(67,67), 45, 45);

                var format = new FormattedText(
                    wp_num.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.ExtraBold, FontStretches.Medium),
                    75,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                format.TextAlignment = TextAlignment.Center;

                drawer.DrawText(format, new Point(target.Width / 2.0, 15));
            }
            
            target.Render(visual);

            var theBMP = new BitmapImage();

            var bitmapEncoder = new PngBitmapEncoder();

            bitmapEncoder.Frames.Add(BitmapFrame.Create(target));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                theBMP.BeginInit();
                theBMP.CacheOption = BitmapCacheOption.OnLoad;
                theBMP.StreamSource = stream;
                theBMP.EndInit();
            }

            return theBMP;
        }

        #endregion

    }
}
