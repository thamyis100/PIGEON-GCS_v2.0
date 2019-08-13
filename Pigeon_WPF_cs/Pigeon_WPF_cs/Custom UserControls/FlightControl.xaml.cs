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
using Pigeon_WPF_cs.JPGScreenshot;
using System.Windows.Threading;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using GMap.NET.MapProviders;
using System.Globalization;

namespace Pigeon_WPF_cs.Custom_UserControls
{
    #region Custom Bitmap to BitmapImage

    static class BitmapHelper
    {
        /// <summary>
        /// Convert Bitmap to BitmapImage
        /// </summary>
        /// <returns>BitmapImage</returns>
        //Bitmap to BitmapImage
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

        private float heading_val, pitch_val, roll_val, alti_val;//, bearing_val;
        private short airspeed_val;
        private double lat = -7.275869, longt = 112.794307;

        // The Data Goes :
        // [0] = Yaw (-000.00)
        // [1] = pitch (-00.00)
        // [2] = roll (-00.00)
        // [3] = airspeed (00)
        // [4] = altitude (-000.00)
        // [5] = latitude (-00.000000)
        // [6] = longtitude (-000.000000)
        // [7] = Bearing (000.00) //not used

        public FlightControl()
        {
            InitializeComponent();

            DataContext = this;
            PrepareWebcam(); //Cari webcam
            PrepareUSBConn(); //Cari usb
        }

        #region USBSerial

        public ObservableCollection<ComboBoxItem> sPorts { get; set; }
        public ComboBoxItem selectedPort { get; set; }
        public ComboBoxItem selectedBaud { get; set; }
        private SerialPort thePort;
        public bool connected = false;
        private void PrepareUSBConn()
        {
            sPorts = new ObservableCollection<ComboBoxItem>();
            sPorts.Add(new ComboBoxItem { Content = "COM PORTS" });
            sPorts.Add(new ComboBoxItem { Content = "..REFRESH.." });
            getPortList();
            selectedBaud = (ComboBoxItem)cb_bauds.Items[0];
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

        private void startSerial(ComboBoxItem comPort, ComboBoxItem baud)
        {
            //Console.WriteLine("Connecting to " + comPort.Content.ToString() + " on " + baud.Content.ToString());
            if (comPort.Content.ToString().Length > 5 || comPort.Content.ToString() == "")
            {
                MessageBox.Show("Tidak ada COM PORT yang dipilih!");
                return;
            }
            if (cb_bauds.SelectedIndex==0)
            {
                MessageBox.Show("Tidak ada Baudrate yang dipilih!");
                return;
            }
            try
            {
                thePort = new SerialPort(comPort.Content.ToString(), int.Parse(baud.Content.ToString()), Parity.None, 8, StopBits.One);
                thePort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                thePort.ReadTimeout = 5000;
                thePort.WriteTimeout = 500;
                if (!(thePort.IsOpen)) thePort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening serial port :: " + ex.Message, "Error!");
                return;
            }
            connected = true;
            toggleConn(true);
        }

        private void ToggleSerial(object sender, RoutedEventArgs e)
        {
            if (!connected) {
                startSerial(selectedPort, selectedBaud);
            } else if (thePort.IsOpen)
            {
                thePort.Close();
                connected = false;
                toggleConn(false);
            }
        }

        private void toggleConn(bool s)
        {
            MainWindow mainWin = (MainWindow)Window.GetWindow(this);
            if (s)
            {
                cb_ports.IsEnabled = false;
                cb_bauds.IsEnabled = false;
                stream_panel.IsEnabled = true;
                mainWin.setConnStat(true);
            } else
            {
                cb_ports.IsEnabled = true;
                cb_bauds.IsEnabled = true;
                stream_panel.IsEnabled = false;
                mainWin.setConnStat(false);
            }
        }

        private delegate void UpdateUiTextDelegate(string[] text);
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string dataIn ="";
            string[] dataCount = null;
            SerialPort receive = (SerialPort)sender;
            try
            {
                while (receive.BytesToRead > 0)
                {
                    char chartoread = Convert.ToChar(receive.ReadChar());
                    if (chartoread == '\n') break;
                    dataIn += chartoread;
                    dataCount = dataIn.Split(',');
                }
            
                Console.WriteLine(string.Format("[Flight] Extracted ({0}) data from dataIn ='{1}'", dataCount.Length, dataIn));
                if (dataCount.Length == 7)
                {
                    if (!dataCount.Contains(""))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataMasukan), dataCount);
                        Console.WriteLine("valid");
                    }
                }
                //thePort.DiscardInBuffer();
            } catch(Exception exc)
            {
                Console.WriteLine("invalid");
            }
        }

        //Update Data on UI
        bool isFirstData = true;
        private void dataMasukan(string[] theData)
        {
            in_stream.Text = string.Join(" | ",theData);

            heading_val = float.Parse(theData[0], CultureInfo.InvariantCulture);
            pitch_val= float.Parse(theData[1], CultureInfo.InvariantCulture);
            roll_val = float.Parse(theData[2], CultureInfo.InvariantCulture);
            airspeed_val = short.Parse(theData[3]); //Convert.ToInt16(float.Parse(theData[3])); 
            alti_val = float.Parse(theData[4]);
            lat = double.Parse(theData[5], CultureInfo.InvariantCulture);
            longt = double.Parse(theData[6], CultureInfo.InvariantCulture);
            //bearing_val = float.Parse(theData[7], CultureInfo.InvariantCulture);

            Console.WriteLine("Values : " + heading_val + " " + pitch_val + " " + roll_val + " " + airspeed_val + " " + alti_val + " " + lat + " " + longt);

            
            MainWindow win = (MainWindow)Window.GetWindow(this);
            win.stats_Ctrl.addToStatistik(heading_val, pitch_val, roll_val);
            if (isFirstData)
            {
                isFirstData = false;
                win.map_Ctrl.StartPosWahana(lat, longt, heading_val);
            }
            win.track_Ctrl.SetKoorWahana(lat, longt, alti_val);

            tb_yaw.Text = heading_val.ToString();
            ind_heading.SetHeadingIndicatorParameters(Convert.ToInt32(heading_val));
            win.map_Ctrl.setPosWahana(lat, longt, heading_val);

            tb_pitch.Text = pitch_val.ToString();
            tb_roll.Text = roll_val.ToString();
            ind_attitude.SetAttitudeIndicatorParameters(Convert.ToDouble(pitch_val), -Convert.ToDouble(roll_val));

            tb_airspeed.Text = airspeed_val.ToString();
            ind_airspeed.SetAirSpeedIndicatorParameters(airspeed_val);

            tb_alti.Text = alti_val.ToString();
            tb_lat.Text = lat.ToString();
            tb_longt.Text = longt.ToString();
        }

        #region Animating back n forth
        private void img_conn_0(object sender, MouseEventArgs e)
        {
            if (connected)
            {
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-disconnected-80.png"));
                ind_conn_status.Content = "Disconnect";
            }
            else
            {
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-connected-80.png"));
                ind_conn_status.Content = "Connect";
            }
        }
        private void img_conn_1(object sender, MouseEventArgs e)
        {
            if (connected)
            {
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-connected-80.png"));
                ind_conn_status.Content = "Connected";
            }
            else
            {
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-disconnected-80.png"));
                ind_conn_status.Content = "Disconnected";
            }
        }
        #endregion

        #endregion

        #region LiveStreamCamera

        public ObservableCollection<FilterInfo> Cameras { get; set; }

        public FilterInfo CurrentCamera
        {
            get { return currCam; }
            set { currCam = value; OnPropertyChanged("CurrentCamera"); }
        }
        private FilterInfo currCam;

        private void PrepareWebcam()
        {
            Cameras = new ObservableCollection<FilterInfo>();
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                Cameras.Add(filterInfo);
            }

            if (Cameras.Any())
            {
                Cameras.RemoveAt(0);
                CurrentCamera = Cameras[0];
                cb_cams.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("Tidak ada kamera yang ditemukan", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                cb_cams.Items.Add("..REFRESH..");
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

        private void startOnboardCam(object sender, RoutedEventArgs e) => StartCam();

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

        private void Cb_cams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            string now = cb_cams.ToString();
            if (now == "..REFRESH..") { refreshCamList(); cb_cams.SelectedIndex = 0; }
        }

        private void refreshCamList()
        {
            while (Cameras.Count > 1)
            {
                Cameras.RemoveAt(0);
            }
            PrepareWebcam();
        }

        #endregion

        #region Avionics Instrument Controling

        public void hideAvionics() => avInst.Visibility = Visibility.Hidden;
        public void showAvionics() => avInst.Visibility = Visibility.Visible;

        #endregion

        #region Property Binding Handler

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

        #endregion
    }
}
