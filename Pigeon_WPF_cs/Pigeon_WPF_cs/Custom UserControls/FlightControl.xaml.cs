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
using Pigeon_WPF_cs.CsvReadWrite;
using System.Windows.Threading;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using GMap.NET.MapProviders;
using System.Globalization;
using System.Windows.Media.Animation;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using MessagePack;
using System.Diagnostics;

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

    [MessagePackObject]
    public class FlightData
    {
        public byte FlightMode;
        public float Bearing;
        public float NosePitch;
        public float Roll;
        public short Airspeed;
        public float Altitude;
        public double Latitude;
        public double Longitude;
        public float BatteryVoltage;
    }

    [MessagePackObject]
    public class TrackerData
    {
        public float Bearing;
        public float Pitch;
        public float BatteryVolt;
    }

    public class GPSData
    {
        public sbyte lat_int8;
        public float lat_float;
        public short lon_int16;
        public float lon_float;

        public bool SetLatitude(byte[] thebytes)
        {
            try
            {
                lat_int8 = (sbyte)thebytes[0];
                lat_float = BitConverter.ToSingle(thebytes, 1);
                return true;
            }
            catch { return false; }
        }
        public bool SetLongitude(byte[] thebytes)
        {
            try
            {
                lon_int16 = BitConverter.ToInt16(thebytes, 0);
                lon_float = BitConverter.ToSingle(thebytes, 2);
                return true;
            }
            catch { return false; }
        }

        public string LatDDMString => string.Format("{0} {1}", lat_int8, lat_float);
        public string LonDDMString => string.Format("{0} {1}", lon_int16, lon_float);
        public double LatDecimal => lat_int8 + (lat_float / 60.0f);
        public double LonDecimal => lon_int16 + (lon_float / 60.0f);
    }

    public partial class FlightControl : UserControl, INotifyPropertyChanged
    {
        FlightData Wahana;
        private float heading_val, pitch_val, roll_val, alti_val, batt_volt, batt_cur, tracker_yaw, tracker_pitch;
        private byte fmode;
        private ushort airspeed_val = 0;
        private double lat = -7.275869000d, longt = 112.794307000d;
        GPSData efalcongps = new GPSData(), trackergps = new GPSData();
        //global path for saving data
        string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Pigeon GCS/";

        //The Data Goes :



        //speakoutloud timer
        DispatcherTimer dataTimer;
        public FlightControl()
        {
            InitializeComponent();

            DataContext = this;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            PrepareWebcam(); //Cari webcam
            PrepareUSBConn(); //Cari usb

            //dataTimer = new DispatcherTimer();
            //dataTimer.Interval = TimeSpan.FromSeconds(8);
            //dataTimer.Tick += SpeakOutLoud;
        }

        #region WIFI Connect

        private bool isUsingWifi = false;
        private UdpClient udpSocket;
        
        private bool isCurrentlyRecv = false;
        public bool GetCurrentRecv { get => isCurrentlyRecv; }
        private async void StartListening()
        {
            while (isCurrentlyRecv)
            {
                UdpReceiveResult it = await udpSocket.ReceiveAsync();
                ParseDataAsync(it.Buffer);
            }
            udpSocket.Close();
            return;
        }

        #endregion

        #region BUTTONS
        
        private void ToggleSerial(object sender, RoutedEventArgs e)
        {
            var win = (MainWindow)Window.GetWindow(this);
            
            if (isUsingWifi)
            {
                isUsingWifi = !isUsingWifi;
                isCurrentlyRecv = true;
                udpSocket = new UdpClient(0);
                Debug.WriteLine("ToggleConn: UDP PORT is " + ((IPEndPoint)udpSocket.Client.LocalEndPoint).Port.ToString());
                StartListening();
                connected = true;
                toggleConn(true);
            }
            else if (!connected) {
                if (startSerial(selectedPort, selectedBaud) == 1) Debug.WriteLine("Serial is fine");
                else return;
            }
            else
            {
                if (thePort != null) thePort.Close();
                isCurrentlyRecv = false;
                connected = false;
                toggleConn(false);
                isFirstData = true;
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
                mainWin.setConnStat(true, false);
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-connected-80.png"));
                ind_conn_status.Content = "Connected";
            } else
            {
                cb_ports.IsEnabled = true;
                cb_bauds.IsEnabled = true;
                stream_panel.IsEnabled = false;
                mainWin.setConnStat(false, false);
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-disconnected-80.png"));
                ind_conn_status.Content = "Disconnected";
                
                mainWin.map_Ctrl.FmodeEnable(false);
                isCurrentlyRecv = false;

                if (menulis != null) menulis.Close();
                if (udpSocket != null) udpSocket.Close();
            }
        }

        #endregion

        #region MAVLINK PARSING



        #endregion

        #region USBSerial

        public ObservableCollection<ComboBoxItem> sPorts { get; set; }
        public ComboBoxItem selectedPort { get; set; }
        public ComboBoxItem selectedBaud { get; set; }
        private SerialPort thePort;
        public bool connected = false;
        private async void PrepareUSBConn()
        {
            sPorts = new ObservableCollection<ComboBoxItem>();
            sPorts.Add(new ComboBoxItem { Content = "COM PORTS" });
            sPorts.Add(new ComboBoxItem { Content = "..REFRESH.." });
            sPorts.Add(new ComboBoxItem { Content = "WIFI AP/UDP" });
            getPortList();
            selectedBaud = (ComboBoxItem)cb_bauds.Items[0];
        }
        private void getPortList()
        {
            string[] ports = SerialPort.GetPortNames();
            for (int indeks = 0; indeks < ports.Length; indeks++) sPorts.Add(new ComboBoxItem { Content = ports[indeks] });
            selectedPort = sPorts[0];
        }

        private void Cb_ports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            string now = selectedPort.Content.ToString();
            if (now == "..REFRESH..") { refreshPortList(); cb.SelectedIndex = 0; }
            else if (now == "WIFI AP/UDP") { cb_bauds.IsEnabled = false; isUsingWifi = true; return; }
            else cb_bauds.IsEnabled = true;
            isUsingWifi = false;
        }

        private void refreshPortList()
        {
            while (sPorts.Count > 3)
            {
                sPorts.RemoveAt(3);
            }
            getPortList();
            //refreshing();
        }

        private async void refreshing()
        {
            sPorts[0].Content = "[refreshing]";
            cb_ports.IsEnabled = false;
            await Task.Delay(1000);
            cb_ports.IsEnabled = true;
            sPorts[0].Content = "COM PORTS";
        }

        private int startSerial(ComboBoxItem comPort, ComboBoxItem baud)
        {
            if (comPort.Content.ToString().Length > 5 || comPort.Content.ToString() == "")
            {
                MessageBox.Show("Tidak ada COM PORT yang dipilih!");
                return 0;
            }
            if (cb_bauds.SelectedIndex == 0)
            {
                MessageBox.Show("Tidak ada Baudrate yang dipilih!");
                return 0;
            }
            try
            {
                thePort = new SerialPort(comPort.Content.ToString(), int.Parse(baud.Content.ToString()), Parity.None, 8, StopBits.One);
                thePort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                thePort.NewLine = "\n";
                thePort.ReceivedBytesThreshold = 8;
                if (!(thePort.IsOpen)) thePort.Open();

                connected = true;
                toggleConn(true);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Port sudah digunakan,\nsilakan pilih port lain.\n" + ex.Message, "Port digunakan", MessageBoxButton.OK, MessageBoxImage.Information);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening serial port :: " + ex.Message, "Error!");
                Debug.WriteLine(ex.StackTrace);
                return 0;
            }
            return 1;
        }

        TimeSpan lastRecv = TimeSpan.Zero;
        private delegate void UpdateUiTextDelegate(char dataType);    
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort receive = (SerialPort)sender;

            try { ParseDataAsync(Encoding.ASCII.GetBytes(receive.ReadTo("#"))); Debug.WriteLine("Done reading to #"); }
            catch (Exception exc)
            {
                Debug.WriteLine("invalid");
                Debug.WriteLine(exc.StackTrace);
            }
        }

        #endregion

        #region Update Data

        //parse data
        private async void ParseDataAsync(byte[] dataIn)
        {
            //Debug.Write($"\ndataIn = [");
            //foreach (byte bite in dataIn)
            //{
            //    Debug.Write(bite.ToString("X2") + ' ');
            //}
            //Debug.Write("]\n");

            try
            {
                switch (dataIn[0])
                {
                    case (byte)'N': //should be 29 bytes data
                        heading_val = BitConverter.ToSingle(dataIn, 1); //use (heading_val + 360) % 360; to get positive degrees
                        pitch_val = BitConverter.ToSingle(dataIn, 5);
                        roll_val = BitConverter.ToSingle(dataIn, 9);
                        alti_val = BitConverter.ToSingle(dataIn, 13);
                        byte[] thebytes = new byte[5];
                        Array.Copy(dataIn, 17, thebytes, 0, 5);
                        Debug.Write("Lat : [");
                        foreach (byte item in thebytes)
                        {
                            Debug.Write(item.ToString("X2") + ' ');
                        }
                        Debug.WriteLine(']');
                        Debug.WriteLine(efalcongps.lat_int8.ToString() + ((sbyte)thebytes[0]).ToString());
                        Debug.WriteLine(efalcongps.lat_float.ToString() + BitConverter.ToSingle(thebytes, 1).ToString());

                        efalcongps.SetLatitude(thebytes);
                        byte[] thobytes = new byte[6];
                        Array.Copy(dataIn, 22, thebytes, 0, 6);
                        efalcongps.SetLatitude(thebytes);

                        //efalcongps.lat_int8 = (sbyte)dataIn[17];
                        //efalcongps.lat_float = BitConverter.ToSingle(dataIn, 18);
                        //efalcongps.lon_int16 = BitConverter.ToInt16(dataIn, 22);
                        //efalcongps.lon_float = BitConverter.ToSingle(dataIn, 24);

                        Debug.WriteLine("\nNavigation data updated :\n"
                            + "Yaw : " + heading_val.ToString() + '\n'
                            + "Pitch : " + pitch_val.ToString() + '\n'
                            + "Roll : " + roll_val.ToString() + '\n'
                            + "Altitude : " + alti_val.ToString() + '\n'
                            + "Lat DDM (DD) : " + efalcongps.LatDDMString + " (" + efalcongps.LatDecimal.ToString() + ")\n"
                            + "Lon DDM (DD) : " + efalcongps.LonDDMString + " (" + efalcongps.LonDecimal.ToString() + ")\n"
                            );
                        break;
                    case (byte)'M':
                        fmode = dataIn[1];
                        Debug.WriteLine("Flight Mode updated : " + fmode.ToString("X2"));
                        break;
                    case (byte)'B':
                        batt_volt = BitConverter.ToSingle(dataIn, 1);
                        batt_cur = BitConverter.ToSingle(dataIn, 5);
                        Debug.WriteLine("Battery updated :\n"
                            + "Volt : " + batt_volt.ToString() + '\n'
                            + "Arus : " + batt_cur.ToString() + '\n'
                            );
                        break;
                    case (byte)'T':
                        tracker_yaw = BitConverter.ToSingle(dataIn, 1);
                        tracker_pitch = BitConverter.ToSingle(dataIn, 5);
                        byte[] bytes = new byte[6];
                        Array.Copy(dataIn, 17, bytes, 0, 6);
                        efalcongps.SetLatitude(bytes);
                        Array.Copy(dataIn, 22, bytes, 0, 6);
                        efalcongps.SetLatitude(bytes);
                        Debug.WriteLine("Tracker data updated :\n"
                            + "Yaw : "+tracker_yaw.ToString()+'\n'
                            + "Pitch : " + tracker_pitch.ToString() + '\n'
                            + "Lat int float : " + trackergps.lat_int8.ToString() + ' ' + trackergps.lat_float.ToString()//" (" + trackergps.LatDecimal.ToString() + ")\n"
                            + "Lon int float : " + trackergps.lon_int16.ToString() + ' ' + trackergps.lon_float.ToString()//" (" + trackergps.LonDecimal.ToString() + ")\n"
                            );
                        break;
                    default:
                        Debug.WriteLine("There was data, but no identifier recognized, nothing updated");
                        return;
                }
            } catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            //Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(UpdateFlightData), dataIn[0]);
        }

        //Update Data on UI
        bool isFirstData = true, isBlackBoxRecord = true;
        double lastlat = 0, lastlng = 0;
        private void UpdateFlightData(char dataType)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);

            in_stream.Text = fmode + " | " + heading_val + " | " + pitch_val + " | " + roll_val + " | " + airspeed_val + " | " + alti_val + " | " + lat + " | " + longt + " | " + batt_volt;
            
            win.stats_Ctrl.addToStatistik(heading_val, pitch_val, roll_val, win.waktuTerbang);

            if (isFirstData)
            {
                isFirstData = false;
                win.map_Ctrl.StartPosWahana(lat, longt, heading_val);
                win.map_Ctrl.FmodeEnable(true);
                menulis = new CsvFileWriter(new FileStream(path + "BlackBox_" + DateTime.Now.ToString("(HH-mm-ss)_[dd-MM-yy]") + ".csv",
                          FileMode.Create, FileAccess.Write));
                dataTimer.Start();
                win.ToggleWaktuTerbang();
            }

            //if (!win.track_Ctrl.isTrackerReady) win.track_Ctrl.SetKoorTrack(lat, longt);

            win.map_Ctrl.SetMode(fmode);
            win.SetBaterai(batt_volt);

            if (isBlackBoxRecord) WriteBlackBox();

            tb_yaw.Text = heading_val.ToString("0.00", CultureInfo.CurrentUICulture) + "°";
            ind_heading.SetHeadingIndicatorParameters(Convert.ToInt32(heading_val));

            tb_pitch.Text = pitch_val.ToString("0.00", CultureInfo.CurrentUICulture) + "°";
            tb_roll.Text = roll_val.ToString("0.00", CultureInfo.CurrentUICulture) + "°";
            ind_attitude.SetAttitudeIndicatorParameters(pitch_val, -roll_val);

            tb_airspeed.Text = airspeed_val.ToString(CultureInfo.CurrentUICulture) + " km/j";
            ind_airspeed.SetAirSpeedIndicatorParameters(airspeed_val);

            tb_alti.Text = alti_val.ToString("0.00", CultureInfo.CurrentUICulture) + " m";

            if (lastlat == 0 || lastlng == 0) lastlat = lat; lastlng = longt;
            //if (lat - lastlat > 1.0f || lat - lastlat < -1.0f || longt - lastlng > 1.0f || longt - lastlng < -1.0f) return;
            win.track_Ctrl.SetKoorWahana(lat, longt, alti_val);
            win.map_Ctrl.setPosWahana(lat, longt, heading_val);
            if (win.track_Ctrl.GetTrackStatus()) win.track_Ctrl.ArahkanTracker();
            tb_lat.Text = lat.ToString("0.#########", CultureInfo.InvariantCulture);
            tb_longt.Text = longt.ToString("0.#########", CultureInfo.InvariantCulture);
        }
        
        //speak out our current heading, speed, altitude
        private void SpeakOutLoud(object sender, EventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            var str = "<s>Heading <emphasis><say-as interpret-as=\"number\">"+heading_val.ToString("0")+"</say-as></emphasis> derajat</s>" +
                "<s>Ketinggian <emphasis><say-as interpret-as=\"number\">" + alti_val.ToString("0") + "</say-as></emphasis> meter</s>" +
                "<s>Kecepatan <emphasis><say-as interpret-as=\"number\">" + airspeed_val.ToString("0") + "</say-as></emphasis> kilometer per jam</s>";
            win.SpeakOutloud(str);
        }

        // write data to local ground station record
        CsvFileWriter menulis;
        private void WriteBlackBox()
        {
            CsvRow baris = new CsvRow();
            /*  Urutan blackbox
             *  0. TimeSpan (ticks)
                1. Fmode
                2. Heading
                3. Pitch
                4. Roll
                5. Speed
                6. altitude
                7. latitude
                8. longitude
                9. Batt
            */
            MainWindow win = (MainWindow)Window.GetWindow(this);
            baris.Add(win.waktuTerbang.Ticks.ToString());
            baris.Add(fmode.ToString("X"));
            baris.Add(heading_val.ToString());
            baris.Add(pitch_val.ToString());
            baris.Add(roll_val.ToString());
            baris.Add(airspeed_val.ToString());
            baris.Add(alti_val.ToString());
            baris.Add(lat.ToString());
            baris.Add(longt.ToString());
            baris.Add(batt_volt.ToString());

            menulis.WriteRow(baris);
        }

        #endregion

        #region Sending Data

        private void sendCommand(object sender, RoutedEventArgs e)
        {
            switch (out_stream.Text)
            {
                case "TAKEOFF":
                    SendToConnection(command.TAKE_OFF, Efalcon.WAHANA);
                    break;
                case "LAND":
                    SendToConnection(command.LAND, Efalcon.WAHANA);
                    break;
                case "BATAL":
                    SendToConnection(command.BATALKAN, Efalcon.WAHANA);
                    break;
            }
            out_stream.Text = "";
        }

        public void SendToConnection(command cmd, Efalcon tujuan, string track = "")
        {
            Debug.WriteLine(cmd.ToString("X"));
            if (thePort != null || isCurrentlyRecv == true)
            { 
                switch (tujuan) {
                    case Efalcon.WAHANA:
                        thePort.BaseStream.WriteByte((Byte)cmd);
                        break;
                    case Efalcon.TRACKER:
                        thePort.Write(string.Format("i,{0}", track));
                        break;
                }
            }
            else { MessageBox.Show("Belum terkoneksi dengan efalcon", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        public void SendToConnection(GMapMarker marker)
        {
            string total = marker.Position.Lat.ToString() + ',' + marker.Position.Lng.ToString();
            var data = Encoding.ASCII.GetBytes(total);
            switch (isCurrentlyRecv)
            {
                case true: //using wifi
                    udpSocket.SendAsync(data, total.Length);
                    break;
                case false: //using usb serial
                    thePort.Write(total + '\n');
                    break;
            }
        }

        #endregion

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

        #region LiveStreamCamera

        public ObservableCollection<FilterInfo> Cameras { get; set; }

        public FilterInfo CurrentCamera
        {
            get { return currCam; }
            set { currCam = value; OnPropertyChanged("CurrentCamera"); }
        }

        private FilterInfo currCam;
        FileSystemWatcher watcher;
        private async void PrepareWebcam()
        {
            cb_cams.IsEnabled = false;
            Cameras = new ObservableCollection<FilterInfo>();
            getCameras();

            watcher = new FileSystemWatcher()
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = "*.jpeg",
                EnableRaisingEvents = true
            };
            watcher.Created += GambarBaru;
        }

        private void getCameras()
        {
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice)) Cameras.Add(filterInfo);
            if (Cameras.Any())
            {
                cb_cams.SelectedIndex = 0;
                CurrentCamera = Cameras[0];
                cb_cams.IsEnabled = true;
                cb_cams.IsEditable = false;
                cb_cams.HorizontalContentAlignment = HorizontalAlignment.Left;
            }
            else
            {
                cb_cams.HorizontalContentAlignment = HorizontalAlignment.Right;
                cb_cams.IsEnabled = false;
                cb_cams.IsEditable = true;
            }
        }

        private void refreshCameras(object sender, RoutedEventArgs e)
        {
            refreshcam();
            while (Cameras.Any()) Cameras.RemoveAt(0);
            getCameras();
        }
        private async void refreshcam()
        {
            btn_refreshcam.IsEnabled = false;
            for (int i = 0; i <= 360; i+=90)
            {
                img_refreshcam.RenderTransform = new RotateTransform(i);
                await Task.Delay(125);
            }
            btn_refreshcam.IsEnabled = true;
        }

        private delegate void UpdateScreenshotList(string path);
        private void GambarBaru(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine("New File at " + e.FullPath);
            if (e.FullPath.Contains("Capture_"))
            {
                Dispatcher.BeginInvoke(new UpdateScreenshotList(ShowSavedCaptures), e.FullPath);
            }
        }

        private void ShowSavedCaptures(string path)
        {
            screenshot_List.Children.Insert(0,new System.Windows.Controls.Image()
            {
                Source = new BitmapImage(new Uri(path)),
                Margin = new Thickness(0, 0, 2, 0),
            });
            RenderOptions.SetBitmapScalingMode(screenshot_List.Children[0], BitmapScalingMode.HighQuality);
            screenshot_List.Children[0].MouseLeftButtonDown += popupCapturedImage;
        }

        private void popupCapturedImage(object sender, MouseButtonEventArgs e)
        {
            //throw new NotImplementedException();
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
        private void startOnboardCam(object sender, RoutedEventArgs e)
        {
            if (liveStream != null) StopCam();
            if (CurrentCamera != null)
            {
                liveStream = new VideoCaptureDevice(CurrentCamera.MonikerString);
                liveStream.NewFrame += cam_AvailFrame;
                liveStream.Start();
            }
        }

        BitmapImage bi;
        private void cam_AvailFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
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

        byte i = 0;
        private void AmbilGambar(object sender, RoutedEventArgs e)
        {
            if (bi == null) return;
            FileStream file;
            
            try { file = new FileStream(path + "Capture_" + DateTime.Now.ToString("mm_ss[dd-M-yy]_") + ".jpeg", FileMode.CreateNew); }
            catch { file = new FileStream(path + "Capture_" + DateTime.Now.ToString("mm_ss[dd-M-yy]_") + (i++).ToString() + ".jpeg", FileMode.CreateNew); }

            var saveFile = new JpegBitmapEncoder();
            saveFile.Frames.Add(BitmapFrame.Create(bi));
            saveFile.Save(file);
            file.Close();
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
