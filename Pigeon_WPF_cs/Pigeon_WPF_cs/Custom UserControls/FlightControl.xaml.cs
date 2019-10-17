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

        private float heading_val, pitch_val, roll_val, alti_val, baterai;
        private byte fmode;
        private ushort airspeed_val;
        private double lat = -7.275869000, longt = 112.794307000;
        //global path for saving data
        string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Pigeon GCS/";

        // The Data Goes :
        // [0] = fmode (0x) 1 byte                  (0)
        // [1] = Yaw/Bearing (000.00) 4 byte        (1-4)
        // [2] = pitch (-00.00) 4 byte              (5-8)
        // [3] = roll (-00.00) 4 byte               (9-12)
        // [4] = airspeed (00) 2 byte               (13-14)
        // [5] = altitude (-000.00) 4 byte          (15-18)
        // [6] = latitude (-00.000000000) 8 byte    (19-26)
        // [7] = longtitude (-000.000000000) 8 byte (27-34)
        // [8] = baterai (00.00) 4 byte             (35-38)
        // Total 39 bytes
        //
        // if integrating tracker :
        // [9] = track yaw (00.00) 4 byte           (39-42)
        // [10]= track yaw (00.00) 4 byte           (43-46)
        // Total 47 bytes

        //speakoutloud timer
        DispatcherTimer dataTimer;
        public FlightControl()
        {
            InitializeComponent();

            DataContext = this;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            PrepareWebcam(); //Cari webcam
            PrepareUSBConn(); //Cari usb
            //WriteBlackBox();

            dataTimer = new DispatcherTimer();
            dataTimer.Interval = TimeSpan.FromSeconds(8);
            dataTimer.Tick += SpeakOutLoud;
        }

        #region WIFI Connect

        private bool isUsingWifi = false;
        private UdpClient udpSocket;
        
        private bool isCurrentlyRecv = false;
        public bool GetCurrentRecv { get => isCurrentlyRecv; }
        private void StartListening(IAsyncResult result)
        {
            if (!isCurrentlyRecv) return;
            isCurrentlyRecv = true;
            var theEndPoint = new IPEndPoint(IPAddress.Any, 60111);
            byte[] received = udpSocket.EndReceive(result, ref theEndPoint);

            if (received.Length == 39)
            {
                //Console.WriteLine("Received 34 bytes");
                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataMasukan), received);
            }
            else if (received.Length == 47) Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataIntegrasi), received);

            udpSocket.BeginReceive(new AsyncCallback(StartListening), null);
            return;
        }

        #endregion

        #region BUTTONS
        
        private void ToggleSerial(object sender, RoutedEventArgs e)
        {
            var win = (MainWindow)Window.GetWindow(this);
            try
            {
                if (isUsingWifi)
                {
                    isUsingWifi = !isUsingWifi;
                    isCurrentlyRecv = true;
                    udpSocket = new UdpClient(9601);
                    udpSocket.BeginReceive(new AsyncCallback(StartListening), null);
                    connected = true;
                    toggleConn(true);
                    //win.track_Ctrl.track_conn_bt.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else if (!connected) {
                    if (startSerial(selectedPort, selectedBaud) == 1) Console.WriteLine("Serial is fine");
                    else return;
                }
                else
                {
                    if (thePort != null) thePort.Close();
                    isCurrentlyRecv = false;
                    if (udpSocket != null) udpSocket.Close();
                    connected = false;
                    toggleConn(false);
                    isFirstData = true;
                }
                win.ToggleWaktuTerbang();
            }
            catch(Exception none) { Console.WriteLine("ToggleSerial: "+none.StackTrace); }
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
                try { menulis.Close(); isCurrentlyRecv = false; udpSocket.Close(); }
                catch { }
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
        private void PrepareUSBConn()
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
            refreshed();
        }

        private async void refreshed()
        {
            sPorts[0].Content = "[refreshed]";
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
                thePort.ReadTimeout = 5000;
                thePort.WriteTimeout = 500;
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
                Console.WriteLine(ex.StackTrace);
                return 0;
            }
            return 1;
        }

        TimeSpan lastRecv = TimeSpan.Zero;
        private delegate void UpdateUiTextDelegate(byte[] text);
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] dataIn = new byte[255];
            //string dataIN = "";
            SerialPort receive = (SerialPort)sender;
            try
            {
                byte index = 0;
                while (receive.BytesToRead > 0)
                {
                    byte chartoread = (byte)receive.ReadByte();
                    if (chartoread == 0x0A) break;
                    dataIn[index++] = chartoread;
                    //char chartoread = Convert.ToChar(receive.ReadChar());
                    //if (chartoread == '\n') break;
                    //dataIN += chartoread;
                }
                //string[] dataCount = dataIN.Split(',');

                //Console.WriteLine(dataIn);
                //Console.WriteLine("got {0} bytes:", index);
                //Console.WriteLine("Fmode: " + dataIn[0].ToString());
                //Console.WriteLine("Heading: " + BitConverter.ToSingle(SplitBytes(dataIn, 1, 4), 0).ToString());
                //Console.WriteLine("Pitch: " + BitConverter.ToSingle(SplitBytes(dataIn, 5, 4), 0).ToString());
                //Console.WriteLine("Roll: " + BitConverter.ToSingle(SplitBytes(dataIn, 9, 4), 0).ToString());
                //Console.WriteLine("Speed: " + BitConverter.ToInt16(SplitBytes(dataIn, 13, 2, true), 0).ToString());
                //Console.WriteLine("Alti: " + BitConverter.ToSingle(SplitBytes(dataIn, 15, 4), 0).ToString());
                //Console.WriteLine("Lat: " + BitConverter.ToDouble(SplitBytes(dataIn, 19, 8), 0).ToString());
                //Console.WriteLine("Longt: " + BitConverter.ToDouble(SplitBytes(dataIn, 27, 8), 0).ToString());
                //Console.WriteLine("Batt: " + BitConverter.ToSingle(SplitBytes(dataIn, 35, 4), 0).ToString());
                //Console.WriteLine(String.Format("Batt: {0:x2} {1:x2} {2:x2} {3:x2}", dataIn[35], dataIn[36], dataIn[37], dataIn[38]));

                //Console.WriteLine("Fmode: " + dataIn[0].ToString());
                //Console.WriteLine("Heading: " + BitConverter.ToSingle(dataIn, 1).ToString());
                //Console.WriteLine("Pitch: " + BitConverter.ToSingle(dataIn, 5).ToString());
                //Console.WriteLine("Roll: " + BitConverter.ToSingle(dataIn, 9).ToString());
                //Console.WriteLine("Speed: " + BitConverter.ToInt16(dataIn, 13).ToString());
                //Console.WriteLine("Alti: " + BitConverter.ToSingle(dataIn, 15).ToString());
                //Console.WriteLine("Lat: " + BitConverter.ToDouble(dataIn, 19).ToString());
                //Console.WriteLine("Longt: " + BitConverter.ToDouble(dataIn, 27).ToString());
                //Console.WriteLine("Batt: " + BitConverter.ToSingle(dataIn, 35).ToString());
                
                if (index == 39) Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataMasukan), dataIn);
                //else if (index == 9) Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataIntegrasi), dataIn);

                thePort.DiscardInBuffer();
            }
            catch (Exception exc)
            {
                Console.WriteLine("invalid");
                Console.WriteLine(exc.StackTrace);
            }
        }

        #endregion

        #region Update Data

        private byte[] SplitBytes(byte[] theBytes, byte startindex, byte length, bool reverse = false)
        {
            byte[] returned = new byte[length];
            switch (reverse)
            {
                case false:
                    for (byte x = 0; x < length; x++)
                    {
                        returned[x] = theBytes[x + startindex];
                    }
                    break;
                case true:
                    for (byte x = 0; x < length; x++)
                    {
                        returned[x] = theBytes[(startindex + length - 1) - x];
                    }
                    break;
            }
            return returned;
        }

        //Update Data on UI
        bool isFirstData = true, isBlackBoxRecord = true;
        private void dataIntegrasi(byte[] dataIntg)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            if (isFirstData)
            {
                win.track_Ctrl.StartTracking();
            }
            win.track_Ctrl.dataMasukan(SplitBytes(dataIntg, 40, 8));
            dataMasukan(SplitBytes(dataIntg, 0, 39));
        }

        double lastlat = 0; double lastlng = 0;
        private void dataMasukan(byte[] theData)
        {
            try
            {
                fmode = theData[0];
                heading_val = (BitConverter.ToSingle(SplitBytes(theData, 1, 4), 0) + 360) % 360;
                pitch_val = BitConverter.ToSingle(SplitBytes(theData, 5, 4), 0);
                roll_val = BitConverter.ToSingle(SplitBytes(theData, 9, 4), 0);
                airspeed_val = BitConverter.ToUInt16(SplitBytes(theData, 13, 2,true), 0); 
                alti_val = BitConverter.ToSingle(SplitBytes(theData, 15, 4), 0);
                lat = BitConverter.ToDouble(SplitBytes(theData, 19, 8), 0);
                longt = BitConverter.ToDouble(SplitBytes(theData, 27, 8), 0);
                baterai = BitConverter.ToSingle(SplitBytes(theData, 35, 4), 0);


                //float.TryParse(theData[0], NumberStyles.Float, CultureInfo.InvariantCulture, out heading_val);
                //pitch_val = float.Parse(theData[1], CultureInfo.InvariantCulture);
                //roll_val = float.Parse(theData[2], CultureInfo.InvariantCulture);
                //airspeed_val = short.Parse(theData[3], CultureInfo.InvariantCulture); //Convert.ToInt16(float.Parse(theData[3])); 
                //alti_val = float.Parse(theData[4], CultureInfo.InvariantCulture);
                //lat = double.Parse(theData[5], CultureInfo.InvariantCulture);
                //longt = double.Parse(theData[6], CultureInfo.InvariantCulture);
                if (!isCurrentlyRecv)
                {
                    if (udpSocket == null) udpSocket = new UdpClient(9601);
                    udpSocket.SendAsync(theData, 39, "192.168.4.5", 9601);
                    //Console.WriteLine("Sent to Android");
                }

                in_stream.Text = fmode + " | " + heading_val + " | " + pitch_val + " | " + roll_val + " | " + airspeed_val + " | " + alti_val + " | " + lat + " | " + longt + " | " + baterai;
            }
            catch (Exception exc) { Console.WriteLine("updatedata: "+exc.StackTrace); return; }

            MainWindow win = (MainWindow)Window.GetWindow(this);
            win.stats_Ctrl.addToStatistik(heading_val, pitch_val, roll_val, win.waktuTerbang);

            if (isFirstData)
            {
                isFirstData = false;
                win.map_Ctrl.StartPosWahana(lat, longt, heading_val);
                win.map_Ctrl.FmodeEnable(true);
                menulis = new CsvFileWriter(new FileStream(path + "BlackBox_" + DateTime.Now.ToString("(HH-mm-ss)_[dd-MM-yy]") + ".csv",
                          FileMode.Create, FileAccess.Write));
                dataTimer.Start();
            }

            //if (!win.track_Ctrl.isTrackerReady) win.track_Ctrl.SetKoorTrack(lat, longt);

            win.map_Ctrl.SetMode(fmode);
            win.SetBaterai(baterai);

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
            baris.Add(baterai.ToString());

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
            Console.WriteLine(cmd.ToString("X"));
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
        private void PrepareWebcam()
        {
            Cameras = new ObservableCollection<FilterInfo>();
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                Cameras.Add(filterInfo);
            }

            if (Cameras.Any())
            {
                //Cameras.RemoveAt(0);
                CurrentCamera = Cameras[1];
                cb_cams.SelectedIndex = 1;
            }
            else
            {
                //MessageBox.Show("Tidak ada kamera yang ditemukan", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                cb_cams.Items.Add("..REFRESH..");
            }
            

            watcher = new FileSystemWatcher()
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = "*.jpeg",
                EnableRaisingEvents = true
            };
            watcher.Created += GambarBaru;
        }

        private delegate void UpdateScreenshotList(string path);
        private void GambarBaru(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("New File at " + e.FullPath);
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
