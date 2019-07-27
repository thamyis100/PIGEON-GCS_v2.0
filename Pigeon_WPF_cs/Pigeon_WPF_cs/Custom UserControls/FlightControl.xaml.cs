using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
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
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.IO.Ports;

namespace Pigeon_WPF_cs.Custom_UserControls
{
    #region Custom ToBitmapImage()
    static class BitmapHelper
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }
    }
    #endregion

    public partial class FlightControl : UserControl, INotifyPropertyChanged
    {

        public FlightControl()
        {
            InitializeComponent();
            DataContext = this;
            GetVideoDevices(); //Cari webcam
            GetConnectedSerial(); //Cari usb
        }

        #region USBSerial

        public ObservableCollection<ComboBoxItem> sPorts { get; set; }
        public ComboBoxItem selectedPort { get; set; }
        public ComboBoxItem selectedBaud { get; set; }
        private SerialPort thePort;
        private void GetConnectedSerial()
        {
            sPorts = new ObservableCollection<ComboBoxItem>();
            sPorts.Add(new ComboBoxItem { Content = "COM PORTS" });
            sPorts.Add(new ComboBoxItem { Content = "..REFRESH.." });
            getPortList();
            selectedBaud = (ComboBoxItem)cb_bauds.Items[2];
        }
        private void getPortList()
        {
            string[] ports = SerialPort.GetPortNames();
            for (int indeks = 0; indeks < ports.Length; indeks++) sPorts.Add(new ComboBoxItem { Content = ports[indeks] });
            selectedPort= sPorts[0];
        }

        private void Cb_ports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            string now = selectedPort.Content.ToString();
            if (now == "..REFRESH..") { refreshPortList(); cb.SelectedIndex = 0; }
        }

        private void refreshPortList()
        {
            while (sPorts.Count > 2)
            {
                sPorts.RemoveAt(2);
            }
            getPortList();
            refreshed();
        }

        private async void refreshed()
        {
            sPorts[0].Content = "[refreshed]";
            cb_ports.IsEnabled = false;
            await Task.Delay(2000);
            cb_ports.IsEnabled = true;
            sPorts[0].Content = "COM PORTS";
        }

        private void img_conn_0(object sender, MouseEventArgs e) => img_conn.Source = new BitmapImage( new Uri("pack://application:,,,/Resources/icons/icons8-connected-80.png"));
        private void img_conn_1(object sender, MouseEventArgs e) => img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-disconnected-80.png"));

        #endregion

        #region LiveStreamCamera

        public ObservableCollection<FilterInfo> Cameras { get; set; }
        public ObservableCollection<String> Cams { get; set; }

        public FilterInfo CurrentCamera
        {
            get { return currCam; }
            set { currCam = value; OnPropertyChanged("CurrentCamera"); }
        }
        private FilterInfo currCam;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string theProp)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(theProp);
                handler(this, e);
            }
        }

        private void GetVideoDevices()
        {
            Cameras = new ObservableCollection<FilterInfo>();
            Cams = new ObservableCollection<String>();
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                Cameras.Add(filterInfo);
                Cams.Add(filterInfo.Name.ToString());
            }

            if (Cameras.Any())
            {
                CurrentCamera = Cameras[0];
            }
            else
            {
                MessageBox.Show("No video sources found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void stopControl() => StopCam(); //Exit Trigger
        private void StopCam()
        {
            if(liveStream != null && liveStream.IsRunning)
            {
                liveStream.SignalToStop();
                liveStream.NewFrame -= new NewFrameEventHandler(cam_AvailFrame);
            }
        }

        private IVideoSource liveStream;

        private void StartCam()
        {
            if(CurrentCamera != null)
            {
                liveStream = new VideoCaptureDevice(CurrentCamera.MonikerString);
                liveStream.NewFrame += cam_AvailFrame;
                liveStream.Start();
            }
        }
        private void startWebcam(object sender, RoutedEventArgs e) => StartCam();//Button trigger

        private void cam_AvailFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi;
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    bi = bitmap.ToBitmapImage();
                }
                bi.Freeze(); // avoid cross thread operations and prevents leaks
                Dispatcher.BeginInvoke(new ThreadStart(delegate { liveCam.Source = bi; }));
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error upon receiving new frame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopCam();
            }
        }

        #endregion

        #region GMap Usage

        private void theMapLoad(object sender, RoutedEventArgs e)
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            theMap.DragButton = MouseButton.Left;
            theMap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            theMap.CenterPosition = new GMap.NET.PointLatLng(-7.275869, 112.794307);
            theMap.ShowCenter = false;
            theMap.Zoom = 18;
        }

        #endregion

    }
}
