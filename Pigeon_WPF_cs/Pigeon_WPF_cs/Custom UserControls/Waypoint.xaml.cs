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
        //private double lat = -7.275869, longt = 112.794307;
        private double idn_lat = -1.5437881d, idn_lon = 119.095757d;
        double markerWidth = 50, markerHeight = 50;

        public Waypoint()
        {
            InitializeComponent();

            DataContext = this;
            thePoints = new List<PointLatLng>();
        }

        #region Waypoint List Window

        private bool isWPDockHidden = true;
        private void toggleWPDock(object sender, RoutedEventArgs e)
        {
            var nextHeight = 25;
            if (isWPDockHidden)
            {
                nextHeight = 300;
                wp_dock_btn.Content = "Markers \u25BC";
            }
            else
            {
                wp_dock_btn.Content = "Markers \u25B2";
            }
            isWPDockHidden = !isWPDockHidden;

            wp_dock.BeginAnimation(HeightProperty, new DoubleAnimation() {
                From = wp_dock.Height,
                To = nextHeight,
                Duration = TimeSpan.FromMilliseconds(200)
            });
            wp_dock.Height = nextHeight;
        }

        #endregion

        #region GMap.NET Implementation

        private void mapView_Loaded(object sender, RoutedEventArgs e)
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

            //setHomePos(lat, longt);
            //mapView.Markers.Remove(homeIco);
        }

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

        #region List of default Markers, Routes, Path, dll

        private GMapMarker homeIco, poswahana, posgcs;
        private GMapRoute trackerWahana, trail;
        private List<PointLatLng> trailPoints;


        #endregion

        #region Main Functions

        //Home waypoint
        private void setHomePos(double homelat, double homelongt)
        {
            if (homeIco != null) mapView.Markers.Remove(homeIco);
            lastpos.X = homelat; lastpos.Y = homelongt;
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
        }

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
            homeIco.Shape.RenderTransformOrigin = new Point(0.5, 0.5);
            homeIco.Shape.RenderTransform = rotateTrans;
            rotateTrans.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);

            var positionWahana = new TranslateTransform();
            positionWahana.Changed += new EventHandler(pindahMap);
            positionWahana.BeginAnimation(TranslateTransform.XProperty, pindahAnimX);
            positionWahana.BeginAnimation(TranslateTransform.YProperty, pindahAnimY);
            lastpos.X += 0.001; lastpos.Y += 0.001;
        }

        //GCS waypoint
        bool isAdaWahana = false;
        public void StartPosGCS(double thelat, double thelongt)
        {
            if (posgcs != null) mapView.Markers.Remove(posgcs);
            if (trackerWahana != null) mapView.Markers.Remove(trackerWahana);
            posgcs = new GMapMarker(new PointLatLng(thelat, thelongt))
            {
                Tag = "Tracker",
                Shape = new Image
                {
                    Width = markerWidth,
                    Height = markerHeight,
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/ikon-gcs-1.png")),
                    ToolTip = "Antenna Tracker"
                },
                Offset = new Point(-markerWidth / 2.0d, -markerHeight)
            };
            RenderOptions.SetBitmapScalingMode(posgcs.Shape, BitmapScalingMode.HighQuality);
            mapView.Markers.Add(posgcs);


            if (poswahana != null)
            {
                isAdaWahana = true;
                List<PointLatLng> pos = new List<PointLatLng>{ posgcs.Position, poswahana.Position };
                trackerWahana = new GMapRoute(pos);
                mapView.Markers.Add(trackerWahana);
            }

            Debug.WriteLine(string.Format("Created posgcs at {0} , {1}", thelat, thelongt));
        }
        public void SetHeadingGCS(float bearing)
        {
            if (!isAdaWahana && poswahana != null)
            {
                isAdaWahana = true;
                List<PointLatLng> pos = new List<PointLatLng> { posgcs.Position, poswahana.Position };
                trackerWahana = new GMapRoute(pos);
                mapView.Markers.Add(trackerWahana);
                return;
            }
            else if(poswahana == null) { return; }
            trackerWahana.Points.RemoveAt(1);
            posgcs.Shape.RenderTransformOrigin = new Point(0.5, 1);
            posgcs.Shape.RenderTransform = new RotateTransform(bearing);

            trackerWahana.Points.Add(poswahana.Position);
        }

        //Wahana Waypoint
        public void StartPosWahana(double currlat, double currlongt, float heading)
        {
            trailPoints = new List<PointLatLng>();
            lastpos.X = currlat;
            lastpos.Y = currlongt;
            lastbearing = heading;
            poswahana = new GMapMarker(new PointLatLng(currlat, currlongt))
            {
                Tag = "Posisi Wahana",
                Shape = new Image
                {
                    Width = markerWidth,
                    Height = markerHeight,
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/ikon-quadcopter.png")),
                    ToolTip = "poswahana"
                },
                Offset = new Point(-markerWidth / 2, -markerHeight / 2)
            };
            RenderOptions.SetBitmapScalingMode(poswahana.Shape, BitmapScalingMode.HighQuality);

            mapView.Markers.Add(poswahana);
        }

        //default durasi, TODO: ganti berdasarkan history waktu terbang
        TimeSpan durasiData = TimeSpan.FromMilliseconds(80);

        float lastbearing; Point lastpos;
        bool isIkutiWahana = false;
        public async void SetPosWahana(double currlat, double currlongt, float bearing)
        {
            //if (currlat - lastpos.X > 1.0f || currlat - lastpos.X < -1.0f || currlongt - lastpos.Y > 1.0f || currlongt - lastpos.Y < -1.0f) return;

            var usedbearing = bearing;
            if (lastbearing > 270 && bearing < 90 ) usedbearing += 360;
            else if (lastbearing < 90 && bearing > 270) lastbearing += 360;

            var rotasiWahana = new DoubleAnimation()
            {
                From = lastbearing,
                To = usedbearing,
                Duration = durasiData
            };
            //var pindahAnimX = new DoubleAnimation()
            //{
            //    From = lastpos.X,
            //    To = currlat,
            //    Duration = durasiData
            //};
            //var pindahAnimY = new DoubleAnimation()
            //{
            //    From = lastpos.Y,
            //    To = currlongt,
            //    Duration = durasiData
            //};

            var rotateWahana = new RotateTransform();
            rotateWahana.Changed += new EventHandler(rotasiMap);
            poswahana.Shape.RenderTransformOrigin = new Point(0.5, 0.5);
            poswahana.Shape.RenderTransform = rotateWahana;
            rotateWahana.BeginAnimation(RotateTransform.AngleProperty, rotasiWahana);

            poswahana.Position = new PointLatLng(currlat, currlongt);
            if (isIkutiWahana) mapView.CenterPosition = mapView.Position = poswahana.Position;

            //var positionWahana = new TranslateTransform();
            //positionWahana.Changed += new EventHandler(pindahMap);
            //positionWahana.BeginAnimation(TranslateTransform.XProperty, pindahAnimX);
            //positionWahana.BeginAnimation(TranslateTransform.YProperty, pindahAnimY);

            lastpos.X = currlat; lastpos.Y = currlongt;
            lastbearing = bearing;

            trailPoints.Add(new PointLatLng(currlat, currlongt));
            if (trail != null) mapView.Markers.Remove(trail);
            trail = new GMapRoute(trailPoints)
            {
                Tag = "Trail"
            };
            mapView.Markers.Add(trail);
            if (trailPoints.Count > 10) trailPoints.RemoveAt(0);
        }

        private void rotasiMap(object sender, EventArgs e)
        {
            var it = (RotateTransform)sender;
            if(isIkutiWahana) mapView.Bearing = Convert.ToSingle(it.Angle);
        }

        private void pindahMap(object sender, EventArgs e)
        {
            var it = (TranslateTransform)sender;
            poswahana.Position = new PointLatLng(it.X, it.Y);
            if (isIkutiWahana) mapView.CenterPosition = mapView.Position = poswahana.Position;
        }

        //Buttons
        private void posOnWahana(object sender, RoutedEventArgs e)
        {
            if (poswahana == null) return;

            isIkutiWahana = !isIkutiWahana;
            var move = new DoubleAnimation()
            {
                From = 0,
                To = 30,
                Duration = TimeSpan.FromSeconds(3)
            };
            var dump = new RotateTransform();
            dump.Changed += new EventHandler(relokasiMap);
            dump.BeginAnimation(RotateTransform.AngleProperty, move);
        }

        private void relokasiMap(object sender, EventArgs e)
        {
            //Debug.WriteLine("PINDAH");
            mapView.CenterPosition = mapView.Position = poswahana.Position;
        }

        //Flight Mode
        //internal void SetMode(byte fmode)
        //{
        //    switch (fmode)
        //    {
        //        case 0x00:
        //            fmode_view.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 80));
        //            fmode_lbl.Content = "MANUAL MODE";
        //            break;
        //        case 0x08:
        //            fmode_view.Background = new SolidColorBrush(Color.FromArgb(50, 12, 161, 166));
        //            fmode_lbl.Content = "STABILIZER MODE";
        //            break;
        //        case 0x80:
        //            fmode_view.Background = new SolidColorBrush(Color.FromArgb(50, 200, 140, 0));
        //            fmode_lbl.Content = "AUTO MODE";
        //            break;
        //    }
        //}

        //Disable things that require connection
        //internal void FmodeEnable(bool stat)
        //{
        //    switch (stat)
        //    {
        //        case false:
        //            fmode_view.Visibility = Visibility.Hidden;
        //            break;
        //        case true:
        //            fmode_view.Visibility = Visibility.Visible;
        //            break;
        //    }
        //}

        #endregion

        #region Waypoint Commands

        //place waypoint at mouse double click point
        short routeIndex = 0;
        short wayIndex = 0;

        private void MapView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point addMark = e.GetPosition(mapView);
            PointLatLng addMarkP = mapView.FromLocalToLatLng(Convert.ToInt32(addMark.X), Convert.ToInt32(addMark.Y));
            AddMarkerAt(addMarkP, wayIndex++, routeIndex++);
            //Debug.WriteLine("Double clicked at : " + addMarkP.Lat + " " + addMarkP.Lng);
        }

        //waypoint placing
        private List<PointLatLng> thePoints;
        private void AddMarkerAt(PointLatLng theLatLng, short waynum, short routenum)
        {
            BitmapImage theIkon = (BitmapImage)GetWaypointIkon(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/marker-waypoint.png")),waynum);
            short markerWidth = 40, markerHeight = 40;
            GMapMarker addedMarker = new GMapMarker(theLatLng)
            {
                Offset = new System.Windows.Point(-markerWidth / 2, -markerHeight),
                Tag = "WP" + waynum.ToString()
            };
            addedMarker.Shape = new CustomMarker(mapView, addedMarker, theIkon);
            mapView.Markers.Add(addedMarker);

            thePoints.Add(theLatLng);
            if (thePoints.Count < 2) return;
            GMapRoute theRoute = new GMapRoute(thePoints)
            {
                Shape = new System.Windows.Shapes.Path()
                {
                    Stroke = Brushes.Orange,//new SolidColorBrush(Color.FromRgb(12,161,166)),
                    StrokeThickness = 3,
                    Effect = new DropShadowEffect() { Color = Color.FromRgb(0,0,0) }
                },
                Tag = "RT" + routenum.ToString()
            };

            mapView.Markers.Remove(theRoute);
            mapView.Markers.Add(theRoute);
        }

        private void SendWaypoint(GMapMarker marker)
        {
            var win = (MainWindow)Window.GetWindow(this);
            win.flight_Ctrl.SendToConnection(marker);
        }
        
        private double JarakHorizon(PointLatLng WPAsal, PointLatLng WPTujuan)
        {
            double R = 6372795;
            double deltaLat = (WPTujuan.Lat - WPAsal.Lat) * Math.PI / 180; //selisih Latitude dalam radian
            double deltaLongt = (WPTujuan.Lng - WPAsal.Lng) * Math.PI / 180; //selisih Longitude dalam radian

            //trigonometric
            double sisiA = Math.Pow(Math.Sin(deltaLat / 2), 2)
                            + Math.Cos(WPAsal.Lat * Math.PI / 180) * Math.Cos(WPTujuan.Lat * Math.PI / 180)
                            * Math.Pow(Math.Sin(deltaLongt / 2), 2);
            double sisiB = 2 * Math.Asin(Math.Min(1, Math.Sqrt(sisiA)));
            return R * sisiB;
        }

        private object GetWaypointIkon(BitmapImage bmp, short waynum)
        {

            var target = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, bmp.DpiX, bmp.DpiY, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();

            using (var r = visual.RenderOpen())
            {
                r.DrawImage(bmp, new Rect(0, 0, bmp.Width, bmp.Height));
                r.DrawEllipse(Brushes.White, null, new Point(67,67), 45, 45);

                FormattedText format = new FormattedText(
                    waynum.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.ExtraBold, FontStretches.Medium),
                    75, Brushes.Black);
                format.TextAlignment = TextAlignment.Center;

                r.DrawText(format, new Point(target.Width/2, 15));
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
