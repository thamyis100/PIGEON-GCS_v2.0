//#define DEBUGDATA

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
using System.Linq.Expressions;

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
        GPSData GPSLocation;
        public float BatteryVolt;
        public float BatteryCur;
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
        private sbyte lat_int8; //dd
        private short lon_int16; //ddd
        private float lat_float, lon_float; //mm.mmmm
        private double lat_dd, lon_dd; //decimals

        /// <summary>
        /// Masukkan nilai latitude dalam format
        /// <br/><paramref name="thebytes"/> = 1 bytes (signed int8 [dd]), 4 bytes (signed float32 [mm.mmmmm])
        /// </summary>
        /// <returns>true if conversion works, else false</returns>
        public bool SetLatitude(byte[] thebytes)
        {
            try
            {
                lat_int8 = (sbyte)thebytes[0];
                lat_float = BitConverter.ToSingle(thebytes, 1);
                lat_dd = lat_int8 + (lat_float / 60.0f);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Masukkan nilai longitude dalam format
        /// <br/><paramref name="thebytes"/> = 2 bytes (signed int16 [ddd])<br/>, 4 bytes (signed float32 [mm.mmmmm])
        /// </summary>
        /// <returns>true if conversion works, else false</returns>
        public bool SetLongitude(byte[] thebytes)
        {
            try
            {
                lon_int16 = BitConverter.ToInt16(thebytes, 0);
                lon_float = BitConverter.ToSingle(thebytes, 2);
                lon_dd = lon_int16 + (lon_float / 60.0f);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Decimal, Minutes, fraction of Minutes
        /// </summary>
        /// <returns>signed string : "ddmm.mmmmmm"</returns>
        public string GetLatDDMString() => $"{lat_int8}{lat_float}";
        /// <summary>
        /// Decimal, Minutes, fraction of Minutes
        /// </summary>
        /// <returns>signed string : "ddmm.mmmmmm"</returns>
        public string GetLonDDMString() => $"{lon_int16}{lon_float}";
        /// <summary>
        /// Ambil nilai Latitude GPS
        /// </summary>
        /// <returns>nilai decimal dalam tipe double(float64)</returns>
        public double GetLatDecimal() => lat_dd;
        public double GetLonDecimal() => lon_dd;
    }

    public partial class FlightControl : UserControl, INotifyPropertyChanged
    {
        FlightData Wahana;
        private float heading_val, pitch_val, roll_val, alti_val, batt_volt, batt_cur, tracker_yaw, tracker_pitch;
        
        private byte fmode;
        // 0b00000000 = 0x00 = Mati
        // 0b00001000 = 0x08 = Stabilizer
        // 0b10000000 = 0x80 = Hold Altitude
        
        private ushort airspeed_val = 0;
        //private double lat = -7.275869000d, longt = 112.794307000d;
        GPSData efalcongps = new GPSData(), trackergps = new GPSData();

        //global path for saving data
        private string docpath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Pigeon GCS/";
        public string Path => docpath;

        //The Data Goes :



        //speakoutloud timer
        DispatcherTimer dataTimer;
        public FlightControl()
        {
            InitializeComponent();

            DataContext = this;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(docpath));
            PrepareWebcam(); //Cari webcam
            PrepareUSBConn(); //Cari usb

            capturefolder = docpath + "/Camera Captures/" + DateTime.Now.ToString("MMM dd, yyyy") + '/';

            //dataTimer = new DispatcherTimer();
            //dataTimer.Interval = TimeSpan.FromSeconds(8);
            //dataTimer.Tick += SpeakOutLoud;
        }

        #region Internet Connect

        private bool isUsingInternet = false;
        private TcpClient tcpSock;
        
        private async void doInternetConn()
        {
            await tcpSock.ConnectAsync("tekat.co", 16767);
            NetworkStream it = tcpSock.GetStream();

            while (tcpSock.Connected)
            {

            }
        }

        void closeInetConn()
        {
            tcpSock.Close();
        }

        #endregion

        #region WIFI Connect

        private bool isUsingWifi = false;
        private UdpClient udpSocket;
        
        private bool isCurrentlyRecv = false;
        public bool GetCurrentRecv { get => isCurrentlyRecv; }
        private async void StartListening()
        {
            while (isCurrentlyRecv)
            {
                UdpReceiveResult it;
                try { 
                    it = await udpSocket.ReceiveAsync();
                }
                catch { break; }

                ParseDataAsync(it.Buffer);
            }
            return;
        }

        #endregion

        #region BUTTONS
        
        private void ToggleSerial(object sender, RoutedEventArgs e)
        {
            var win = (MainWindow)Window.GetWindow(this);

            if (isUsingInternet)
            {
                toggleConn(true);
                doInternetConn();
                Debug.WriteLine("ToggleConn: Internet TCP to tekat.co");
                connected = true;
                toggleConn(true);
            }
            else if (isUsingWifi)
            {
                isUsingWifi = !isUsingWifi;
                isCurrentlyRecv = true;
                udpSocket = new UdpClient(60111);
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
                if (thePort != null) thePort.Dispose();
                isCurrentlyRecv = false;
                connected = false;
                toggleConn(false);
                isFirstNav = isFirstMode = isFirstBatt = isFirstTracker = true;
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
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-connected-80.png"));
                ind_conn_status.Content = "Connected";
            } else
            {
                cb_ports.IsEnabled = true;
                cb_bauds.IsEnabled = true;
                stream_panel.IsEnabled = false;
                img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-disconnected-80.png"));
                ind_conn_status.Content = "Disconnected";
                
                mainWin.map_Ctrl.FmodeEnable(false);
                isCurrentlyRecv = false;

                if (menulis != null) menulis.Dispose();
                if (udpSocket != null) udpSocket.Dispose();
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
        private byte[] rxbuff;
        private async void PrepareUSBConn()
        {
            sPorts = new ObservableCollection<ComboBoxItem>();
            sPorts.Add(new ComboBoxItem { Content = "COM PORTS" });
            sPorts.Add(new ComboBoxItem { Content = "..REFRESH.." });
            sPorts.Add(new ComboBoxItem { Content = "WIFI AP/UDP" });
            sPorts.Add(new ComboBoxItem { Content = "INTERNET" });
            getPortList();
            selectedBaud = (ComboBoxItem)cb_bauds.Items[0];

            rxbuff = new byte[255];
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
            else if (now == "INTERNET") { cb_bauds.IsEnabled = false; isUsingInternet = true; return; }
            else cb_bauds.IsEnabled = true;
            isUsingWifi = isUsingInternet = false;
        }

        private void refreshPortList()
        {
            while (sPorts.Count > 4)
            {
                sPorts.RemoveAt(4);
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
        byte index = 0;
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort receive = (SerialPort)sender;

            try
            {
                while(receive.BytesToRead > 0)
                {
                    rxbuff[index++] = (byte)receive.ReadByte();
                }

                if (rxbuff[index - 1] == '#')
                {
                    ParseDataAsync(rxbuff);
                    index = 0;
                    Debug.WriteLine("Done read bytes to #");
                }
            }
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
            Debug.Write("ParseDataAsync: DataIN: [");
            foreach (byte item in dataIn)
            {
                Debug.Write(item.ToString("X2") + ' ');
            }
            Debug.WriteLine(']');

            try
            {
                switch (dataIn[0])
                {
                    case (byte)'N':
                        heading_val = BitConverter.ToSingle(dataIn, 1); //use (heading_val + 360) % 360; to get positive degrees
                        pitch_val = BitConverter.ToSingle(dataIn, 5);
                        roll_val = BitConverter.ToSingle(dataIn, 9);
                        alti_val = BitConverter.ToSingle(dataIn, 13);
                        
                        byte[] thebytes = new byte[5];
                        Array.Copy(dataIn, 17, thebytes, 0, 5);
                        efalcongps.SetLatitude(thebytes);
                        thebytes = new byte[6];
                        Array.Copy(dataIn, 22, thebytes, 0, 6);
                        efalcongps.SetLongitude(thebytes);

                        #region Navigation data debugging
                        #if DEBUGDATA
                        Debug.WriteLine("\nNavigation data updated :\n"
                            + "Yaw : " + heading_val.ToString() + '\n'
                            + "Pitch : " + pitch_val.ToString() + '\n'
                            + "Roll : " + roll_val.ToString() + '\n'
                            + "Altitude : " + alti_val.ToString() + '\n'
                            + "Lat DDM (DD) : " + efalcongps.GetLatDDMString() + " (" + efalcongps.GetLatDecimal().ToString("#.########") + ")\n"
                            + "Lon DDM (DD) : " + efalcongps.GetLonDDMString() + " (" + efalcongps.GetLonDecimal().ToString("#.########") + ")\n"
                        );
                        #endif
                        #endregion
                        break;

                    case (byte)'M':
                        fmode = dataIn[1];

                        #region Flight mode debugging
                        #if DEBUGDATA
                        Debug.WriteLine("Flight Mode updated : " + fmode.ToString("X2"));
                        #endif
                        #endregion
                        break;

                    case (byte)'B':
                        batt_volt = BitConverter.ToSingle(dataIn, 1);
                        batt_cur = BitConverter.ToSingle(dataIn, 5);

                        #region Battery data debugging
                        #if DEBUGDATA
                        Debug.WriteLine("Battery updated :\n"
                            + "Volt : " + batt_volt.ToString() + '\n'
                            + "Arus : " + batt_cur.ToString() + '\n'
                        );
                        #endif
                        #endregion
                        break;

                    case (byte)'T':
                        tracker_yaw = BitConverter.ToSingle(dataIn, 1);
                        tracker_pitch = BitConverter.ToSingle(dataIn, 5);

                        byte[] thobytes = new byte[5];
                        Array.Copy(dataIn, 9, thobytes, 0, 5);
                        trackergps.SetLatitude(thobytes);
                        thobytes = new byte[6];
                        Array.Copy(dataIn, 14, thobytes, 0, 6);
                        trackergps.SetLongitude(thobytes);

                        #region Tracker data debugging
                        #if DEBUGDATA
                        Debug.WriteLine("Tracker data updated :\n"
                            + "Yaw : " + tracker_yaw.ToString() + '\n'
                            + "Pitch : " + tracker_pitch.ToString() + '\n'
                            + "Lat DDM (DD) : " + trackergps.GetLatDDMString() + " (" + trackergps.GetLatDecimal().ToString("#.########") + ")\n"
                            + "Lon DDM (DD) : " + trackergps.GetLonDDMString() + " (" + trackergps.GetLonDecimal().ToString("#.########") + ")\n"
                        );
                        #endif
                        #endregion
                        break;

                    default:
                        Debug.WriteLine("There was data, but no identifier recognized, nothing updated");
                        return;
                }
            } catch (Exception e)
            {
                Debug.WriteLine("ParseDataAsync :" + e.Message);
            }

            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(UpdateFlightData), dataIn[0]);
        }

        //Update Data on UI
        bool isFirstNav = true, isFirstMode = true, isFirstBatt = true, isFirstTracker = true;
        bool isBlackBoxRecord = true;
        //double lastlat = 0, lastlng = 0;
        private void UpdateFlightData(char dataType)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);

            in_stream.Text = fmode + " | " + heading_val + " | " + pitch_val + " | " + roll_val + " | " + airspeed_val + " | " + alti_val + " | " + efalcongps.GetLatDecimal() + " | " + efalcongps.GetLonDecimal() + " | " + batt_volt;

            try
            {
                switch (dataType)
                {
                    case 'N':
                        if (isFirstNav)
                        {
                            isFirstNav = false;
                            win.ToggleWaktuTerbang();
                            win.map_Ctrl.StartPosWahana(efalcongps.GetLatDecimal(), efalcongps.GetLonDecimal(), heading_val);
                            win.setConnStat(true, false);
                        }
                        win.map_Ctrl.SetPosWahana(efalcongps.GetLatDecimal(), efalcongps.GetLonDecimal(), heading_val);
                        win.track_Ctrl.SetKoorWahana(efalcongps.GetLatDecimal(), efalcongps.GetLonDecimal(), alti_val);

                        //if (win.track_Ctrl.isTrackerReady) win.track_Ctrl.ArahkanTracker();

                        tb_yaw.Text = heading_val.ToString("0.00", CultureInfo.InvariantCulture) + "°";
                        ind_heading.SetHeadingIndicatorParameters(Convert.ToInt32(heading_val));

                        tb_pitch.Text = pitch_val.ToString("0.00", CultureInfo.InvariantCulture) + "°";
                        tb_roll.Text = roll_val.ToString("0.00", CultureInfo.InvariantCulture) + "°";
                        ind_attitude.SetAttitudeIndicatorParameters(pitch_val, -roll_val);

                        //tb_airspeed.Text = airspeed_val.ToString(CultureInfo.CurrentUICulture) + " km/j";
                        //ind_airspeed.SetAirSpeedIndicatorParameters(airspeed_val);

                        tb_alti.Text = alti_val.ToString("0.00", CultureInfo.InvariantCulture) + " m";

                        tb_lat.Text = efalcongps.GetLatDecimal().ToString("0.00000000", CultureInfo.InvariantCulture);
                        tb_longt.Text = efalcongps.GetLonDecimal().ToString("0.00000000", CultureInfo.InvariantCulture);

                        win.stats_Ctrl.addToStatistik(heading_val, pitch_val, roll_val, win.waktuTerbang);
                        break;

                    case 'M':
                        if (isFirstMode)
                        {
                            isFirstMode = false;
                            win.map_Ctrl.FmodeEnable(true);
                        }
                        win.map_Ctrl.SetMode(fmode);
                        break;

                    case 'B':
                        if (isFirstBatt) isFirstBatt = false;
                        win.SetBaterai(batt_volt, batt_cur);
                        break;

                    case 'T':
                        if (isFirstTracker)
                        {
                            isFirstTracker = false;
                            win.track_Ctrl.Integration(true);
                            win.track_Ctrl.isTrackerReady = true;
                            win.track_Ctrl.btn_tracking.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            win.map_Ctrl.StartPosGCS(trackergps.GetLatDecimal(), trackergps.GetLonDecimal());
                        }
                        win.track_Ctrl.SetKoorGCS(trackergps.GetLatDecimal(), trackergps.GetLonDecimal(), 1.5f);
                        win.track_Ctrl.SetAttitude(tracker_yaw, tracker_pitch);
                        break;
                }
            }catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            if (!isFirstNav && !isFirstMode && !isFirstBatt)
            {
                if(menulis == null) menulis = new CsvFileWriter(new FileStream(docpath + "BlackBox_" + DateTime.Now.ToString("(HH.mm)(G\\MTz)_[dd-MM-yy]") + ".csv", FileMode.Create, FileAccess.Write));
                WriteBlackBox();
            }

            //if (!win.track_Ctrl.isTrackerReady) win.track_Ctrl.SetKoorTrack(lat, longt);
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
             *  0. TimeSpan WaktuTerbang (ticks)
                1. Fmode
                2. Heading
                3. Pitch
                4. Roll
                5. Airspeed
                6. Altitude
                7. Latitude
                8. Longitude
                9. Batt Tegangan
                10. Batt Arus
            */
            MainWindow win = (MainWindow)Window.GetWindow(this);
            baris.Add(win.waktuTerbang.Ticks.ToString());
            baris.Add(fmode.ToString("D"));
            baris.Add(heading_val.ToString());
            baris.Add(pitch_val.ToString());
            baris.Add(roll_val.ToString());
            baris.Add(airspeed_val.ToString());
            baris.Add(alti_val.ToString());
            baris.Add(efalcongps.GetLatDecimal().ToString("0.########", CultureInfo.CurrentUICulture));
            baris.Add(efalcongps.GetLonDecimal().ToString("0.########", CultureInfo.CurrentUICulture));
            baris.Add(batt_volt.ToString());
            baris.Add(batt_cur.ToString());

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
            if (!connected) return;
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

        //dont forget to change to gateway
        public async void SendToConnection(byte[] data) { if (isUsingWifi) udpSocket.Send(data, data.Length, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12727)); }

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

        #region CameraStream prepare

        public ObservableCollection<FilterInfo> Cameras { get; set; }

        private FilterInfo currCam;
        public FilterInfo CurrentCamera
        {
            get { return currCam; }
            set { currCam = value; OnPropertyChanged("CurrentCamera"); }
        }

        FileSystemWatcher watcher;
        private async void PrepareWebcam()
        {
            cb_cams.IsEnabled = false;
            Cameras = new ObservableCollection<FilterInfo>();
            getCameras();
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

        public void stopControl() => StopCam(); //Exit Trigger
        private void StopCam()
        {
            if(liveStream != null)
            {
                liveStream.SignalToStop();
                liveStream.NewFrame -= new NewFrameEventHandler(cam_AvailFrame);
                btn_livestream.Content = "Start Stream";
                liveStream.Stop();
            }
        }

        private IVideoSource liveStream;
        private void startOnboardCam(object sender, RoutedEventArgs e)
        {
            if (liveStream != null) StopCam();
            else if (CurrentCamera != null)
            {
                liveStream = new VideoCaptureDevice(CurrentCamera.MonikerString);
                liveStream.NewFrame += cam_AvailFrame;
                liveStream.Start();
                btn_livestream.Content = "Stop Stream";
            }
        }

        private void cam_AvailFrame(object sender, NewFrameEventArgs eventArgs)
        {
            BitmapImage bi;
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
                MessageBox.Show("Error camera stream:\n" + exc.Message + "\n\nMemberhentikan camera stream...", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopCam();
            }
        }

        #endregion

        #region CameraStream captures

        string capturefolder;
        bool isFirstCapture = true;
        byte i = 1;
        private void AmbilGambar(object sender, RoutedEventArgs e)
        {
            if (liveCam.Source == null) return;
            FileStream file;

            if (isFirstCapture)
            {
                isFirstCapture = false;
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(capturefolder));
                watcher = new FileSystemWatcher()
                {
                    Path = capturefolder,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    Filter = "*.jpeg",
                    EnableRaisingEvents = true
                };
                watcher.Created += GambarBaru;
            }

            try { file = new FileStream(capturefolder + "Capture_" + DateTime.Now.ToString("HH.mm.ss") + ".jpeg", FileMode.CreateNew); }
            catch { file = new FileStream(capturefolder + "Capture_" + DateTime.Now.ToString("HH.mm.ss_") + (i++).ToString() + ".jpeg", FileMode.CreateNew); }

            var saveFile = new JpegBitmapEncoder();
            saveFile.Frames.Add(BitmapFrame.Create((BitmapImage)liveCam.Source));
            saveFile.Save(file);
            file.Dispose();
        }

        private delegate void UpdateScreenshotList(string path);
        private void GambarBaru(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains("Capture_"))
            {
                Dispatcher.BeginInvoke(new UpdateScreenshotList(ShowSavedCaptures), e.FullPath);
            }
        }

        private void ShowSavedCaptures(string path)
        {
            screenshot_List.Children.Insert(0, new System.Windows.Controls.Image()
            {
                Source = new BitmapImage(new Uri(path)),
                Margin = new Thickness(8, 8, 10, 8),
                
            });
            RenderOptions.SetBitmapScalingMode(screenshot_List.Children[0], BitmapScalingMode.HighQuality);
            screenshot_List.Children[0].MouseLeftButtonDown += (sender, mousevent) => Process.Start(path);
            ((System.Windows.Controls.Image)screenshot_List.Children[0]).RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            screenshot_List.Children[0].MouseEnter += (sender, mousevent) => {
                ((System.Windows.Controls.Image)sender).RenderTransform = new ScaleTransform(1.1, 1.1);
                Cursor = Cursors.Hand;
            };
            screenshot_List.Children[0].MouseLeave += (sender, mousevent) => {
                ((System.Windows.Controls.Image)sender).RenderTransform = new ScaleTransform(1.0, 1.0);
                Cursor = Cursors.Arrow;
            };
        }

        #endregion

        #region Avionics Instrument Controling

        public void hideAvionics() => avInst.Visibility = Visibility.Hidden;
        public void showAvionics() => avInst.Visibility = Visibility.Visible;

        #endregion

        #region Property Binding Handler

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            switch (propertyName)
            {
                case "CurrentCamera":
                    if (liveStream != null)
                    {
                        btn_livestream.Content = "Ubah Stream";
                    }
                    break;
            }
        }

        #endregion
    }
}
