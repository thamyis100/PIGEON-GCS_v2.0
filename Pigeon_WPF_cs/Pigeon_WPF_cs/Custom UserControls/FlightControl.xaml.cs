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
using Pigeon_WPF_cs.Data_Classes;
using MavLinkNet;
using System.Net.NetworkInformation;
using System.Reflection;
using Pigeon_WPF_cs.Enums;

namespace Pigeon_WPF_cs.Custom_UserControls
{
    public partial class FlightControl : UserControl, INotifyPropertyChanged
    {
        // UI Update Delegates
        private delegate void UpdateUiTextDelegate(char dataType);
        private delegate void UpdateUIDelegate();

        public FlightControl()
        {
            DataContext = this;
            InitializeComponent();

            PrepConnection(); //Cari usb
            PrepareWebcam(); //Cari webcam

            #region Dummy data server

            #endregion
        }

        #region BUTTONS

        private async void ToggleConnection(object sender, RoutedEventArgs e)
        {
            cb_ports.IsEnabled = cb_bauds.IsEnabled = btn_conn.IsEnabled = false;

            if (IsConnected)
            {
                img_conn.Source = Properties.Resources.icons8_connected_80.ToBitmapSource();
                ind_conn_status.Content = "Disconnecting";

                if (Sp_Used != null) Sp_Used.Dispose();
                if (WIFISocket != null) WIFISocket.Close();
                if (WIFIAsyncEvent != null) WIFIAsyncEvent.Dispose();

                IsConnected = false;

                if (WhiteBoxWriter != null) WhiteBoxWriter.Dispose();

                ResetConnection();

                return;
            }

            img_conn.Source = Properties.Resources.icons8_disconnected_80.ToBitmapSource();
            ind_conn_status.Content = "Connecting";

            switch (ConnectionType)
            {
                case ConnType.Internet:
                    DoInternetConn();
                    break;

                case ConnType.WIFI:
                    if (!await ConnectWIFIAsync())
                    {
                        ResetConnection();
                        return;
                    }
                    break;

                case ConnType.SerialPort:
                    if (!await ConnectSerialPort(SelectedConn, SelectedBaud))
                    {
                        ResetConnection();
                        return;
                    }
                    break;
            }

            cb_ports.IsEnabled = cb_bauds.IsEnabled = btn_conn.IsEnabled = stream_panel.IsEnabled = IsConnected = true;

            img_conn.Source = Properties.Resources.icons8_connected_80.ToBitmapSource();
            ind_conn_status.Content = "Connected";
        }

        private void ResetConnection()
        {
            cb_ports.IsEnabled = cb_bauds.IsEnabled = btn_conn.IsEnabled = true;

            stream_panel.IsEnabled = false;

            img_conn.Source = Properties.Resources.icons8_disconnected_80.ToBitmapSource();
            ind_conn_status.Content = "Disconnected";
        }

        #endregion


        #region Connections

        // List of connections
        public ObservableCollection<ComboBoxItem> ConnList { get; set; } = new ObservableCollection<ComboBoxItem>();

        // 1st ComboBox option (Selected connection)
        public ComboBoxItem SelectedConn { get; set; } = new ComboBoxItem();
        ConnType ConnectionType;

        // 2nd ComboBox option (Baudrate)
        public ComboBoxItem SelectedBaud { get; set; } = new ComboBoxItem();

        public bool IsConnected = false;

        private void PrepConnection()
        {
            cb_ports.IsEnabled = cb_bauds.IsEnabled = false;

            ConnList.Add(new ComboBoxItem { Content = "KONEKSI" });
            ConnList.Add(new ComboBoxItem { Content = "..REFRESH.." });
            ConnList.Add(new ComboBoxItem { Content = "WIFI" });
            ConnList.Add(new ComboBoxItem { Content = "INTERNET" });

            ListAllSerialPorts();

            cb_ports.IsEnabled = cb_bauds.IsEnabled = true;
        }

        private void ConnSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            switch (SelectedConn.Content)
            {
                case "..REFRESH..":
                    cb_ports.IsEnabled = cb_bauds.IsEnabled = false;

                    while (ConnList.Count > 4)
                    {
                        ConnList.RemoveAt(4);
                    }

                    ListAllSerialPorts();

                    cb_ports.SelectedIndex = 0;
                    cb_bauds.SelectedIndex = 0;

                    cb_ports.IsEnabled = cb_bauds.IsEnabled = true;
                    break;

                case "WIFI":
                    cb_bauds.IsEnabled = false;
                    ConnectionType = ConnType.WIFI;
                    break;

                case "INTERNET":
                    cb_bauds.IsEnabled = false;
                    ConnectionType = ConnType.Internet;
                    break;

                default:
                    cb_bauds.IsEnabled = true;
                    ConnectionType = ConnType.SerialPort;
                    break;
            }
        }

        #endregion

        #region Internet Connect

        private bool IsUsingInternet = false;
        private TcpClient IntSocket;

        private async void DoInternetConn()
        {
            await IntSocket.ConnectAsync("tekat.co", 16767);
            NetworkStream it = IntSocket.GetStream();

            while (IntSocket.Connected)
            {

            }
        }

        void closeInetConn()
        {
            IntSocket.Close();
        }

        #endregion

        #region WIFI Connect

        TcpClient WIFISocket;
        SocketAsyncEventArgs WIFIAsyncEvent;

        private async Task<bool> ConnectWIFIAsync()
        {
            #region Dummy server

            Task.Run(() =>
            {
                Thread.CurrentThread.Name = "TEST THREAD";

                byte[] txBuf = new byte[]
                {
                    (byte)BufferHeader.EFALCON4,
                    (byte)FlightMode.MANUAL,
                    (byte)90,
                    (byte)75,
                    0xb8, 0x9e, 0x15, 0x43,
                    0x71, 0x3d, 0x6c, 0xc1,
                    0x66, 0x66, 0xba, 0x40,
                    0x76, 0x31, 0x00, 0x00,
                    0x00, 0x00, 0x80, 0x40,
                    0xeb, 0xd3, 0xe8, 0xc0,
                    0xaf, 0x96, 0xe1, 0x42,
                };

                TcpListener dummy_server = new TcpListener(IPAddress.Loopback, 61258);
                TcpClient theClient = null;

                dummy_server.Start();

                while (theClient == null)
                {
                    theClient = dummy_server.AcceptTcpClient();
                    Debug.WriteLine("Waiting for connection");
                    Thread.Sleep(1000);
                }

                while (true)
                {
                    theClient.Client.Send(txBuf);
                    Thread.Sleep(1000);
                }
            });

            #endregion

            WIFIAsyncEvent = new SocketAsyncEventArgs();
            WIFIAsyncEvent.SetBuffer(new byte[1024], 0, 1024);
            WIFIAsyncEvent.Completed += WIFIAsyncEvent_Completed;

            List<NetworkInterface> interfaces = new List<NetworkInterface>(NetworkInterface.GetAllNetworkInterfaces().Where(intf => intf.OperationalStatus == OperationalStatus.Up && intf.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));
            if (interfaces.Count == 0)
            {
                MessageBox.Show("Perangkat anda tidak memiliki WIFI, GCS tidak bisa dijalankan!\n",
                    "ERROR : Perangkat tidak didukung!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            foreach (var interf in interfaces)
            {
                foreach (var gatewayIP in interf.GetIPProperties().GatewayAddresses)
                {
                    WIFISocket = new TcpClient();

                    Debug.WriteLine($"Try Connecting to [ {gatewayIP.Address}:61258 ]");
                    if (await Task.WhenAny(WIFISocket.ConnectAsync(IPAddress.Loopback, 61258), Task.Delay(1000)) != null && WIFISocket.Connected)
                    {
                        WIFIAsyncEvent.RemoteEndPoint = WIFISocket.Client.RemoteEndPoint;
                        WIFISocket.Client.ReceiveAsync(WIFIAsyncEvent);

                        Debug.WriteLine($"Connected to {WIFISocket.Client.RemoteEndPoint}");

                        return true;
                    }
                }
            }
            MessageBox.Show("Periksa kesesuaian Access Point wahana!\n" +
                "Koneksikan sistem dengan Access Point wahana dan klik [OK].\n\n",
                "ERROR : Alat tidak ditemukan!", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void WIFIAsyncEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            Task.Run(() => ParseData(e.Buffer, e.BytesTransferred));

            WIFISocket.Client.ReceiveAsync(WIFIAsyncEvent);
        }

        #endregion

        #region USB Serial

        /// <summary>
        /// The Serial Port used for connection
        /// </summary>
        private SerialPort Sp_Used;

        private void ListAllSerialPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            for (int indeks = 0; indeks < ports.Length; indeks++)
                ConnList.Add(new ComboBoxItem { Content = ports[indeks] });
        }

        private Task<bool> ConnectSerialPort(ComboBoxItem comPort, ComboBoxItem baud)
        {
            if (!SerialPort.GetPortNames().Any(port => port == comPort.Content.ToString()))
            {
                MessageBox.Show("Tidak ada Koneksi yang dipilih!\r\n" +
                    "Atau port yang dipilih sudah tidak tersedia, Silakan refresh!");
                return Task.FromResult(false);
            }
            else if (cb_bauds.SelectedIndex == 0)
            {
                MessageBox.Show("Tidak ada Baudrate yang dipilih!");
                return Task.FromResult(false);
            }

            try
            {
                Sp_Used = new SerialPort(comPort.Content.ToString(), int.Parse(baud.Content.ToString()), Parity.None, 8, StopBits.One);
                Sp_Used.DataReceived += new SerialDataReceivedEventHandler(Sp_DataReceived);
                Sp_Used.NewLine = "\n";

                Sp_Used.Open();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Port sudah digunakan,\r\nsilakan pilih port lain.\n" + ex.Message,
                    "Port digunakan", MessageBoxButton.OK, MessageBoxImage.Information);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening serial port :: " + ex.Message, "Error!");
                Debug.WriteLine(ex.StackTrace);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private async void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var rxPort = (SerialPort)sender;

            byte[] rxBuf = new byte[rxPort.BytesToRead];
            int bufLength = await rxPort.BaseStream.ReadAsync(rxBuf, 0, rxPort.BytesToRead);

            Task.Run(() => ParseData(rxBuf, bufLength));
        }

        #endregion


        #region Data Parsing

        /// <summary>
        /// Parse the incoming data hopefully asynchrounously
        /// </summary>
        /// <param name="RxBuf">Received data buffer</param>
        /// <param name="BufLen">Buffer's length</param>
        private void ParseData(byte[] RxBuf, int BufLen)
        {
            #region Debugging Purpose
            Debug.Write("ParseData : HEX-> ");
            for (int i = 0; i < BufLen; i++)
            {
                Debug.Write(RxBuf[i].ToString("X2") + ' ');
            }
            Debug.WriteLine("");
            #endregion

            if (!Enum.IsDefined(typeof(BufferHeader), (BufferHeader)RxBuf[0])) return;

            #region EFALCON 4.0

            if (RxBuf[0] == (byte)BufferHeader.EFALCON4)
            {
                #region Urutan data Efalcon Wahana
                /* (angka terakhir itu index)
                * 
                * [Header]      = HEADER_BUF uint8          (1 byte) 0
                *
                * [Flight M.]   = FLAG uint8                (1 byte) 1
                *
                * [Bat. Cap.]   = persen uint8              (1 byte) 2
                *
                * [Sign. Str.]  = persen uint8              (1 byte) 3
                * 
                * [Yaw]		    = derajat float32		    (4 byte) 4
                * [Pitch]	    = derajat float32		    (4 byte) 8
                * [Roll]	    = derajat float32		    (4 byte) 12
                *
                * [Altitude]    = milimeter int32		    (4 byte) 16
                * 
                * [Speed]       = meter/sec float32         (4 byte) 20
                *
                * [Lat]		    = decimal degrees float32	(4 byte) 24
                * [Lon]         = decimal degrees float32	(4 byte) 28
                */
                #endregion

                App.Wahana.Tipe = TipeDevice.WAHANA;

                App.Wahana.FlightMode = (FlightMode)RxBuf[1];

                App.Wahana.Battery = RxBuf[2];

                App.Wahana.Signal = RxBuf[3];

                App.Wahana.IMU.Yaw = BitConverter.ToSingle(RxBuf, 4);
                App.Wahana.IMU.Pitch = BitConverter.ToSingle(RxBuf, 8);
                App.Wahana.IMU.Roll = BitConverter.ToSingle(RxBuf, 12);

                App.Wahana.Altitude = BitConverter.ToInt32(RxBuf, 16);

                App.Wahana.Speed = BitConverter.ToSingle(RxBuf, 20);

                App.Wahana.GPS.Latitude = BitConverter.ToSingle(RxBuf, 24);
                App.Wahana.GPS.Longitude = BitConverter.ToSingle(RxBuf, 28);

                //Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUIDelegate(UpdateUIWahana));
                Dispatcher.BeginInvoke(new ThreadStart(delegate { UpdateUIWahana(); }));
            }

            #endregion

            #region TRITON

            else if (RxBuf[0] == (byte)BufferHeader.TRITON)
            {
                (App.Current.MainWindow as MainWindow).flight_Ctrl.ParseData(RxBuf, BufLen);
            }

            #endregion

            #region MAVLink

            else
            {
                MavLinkParser.ProcessReceivedBytes(RxBuf, 0, BufLen);
            }

            #endregion
        }

        #region MAVLINK PARSING

        MavLinkAsyncWalker MavLinkParser;

        private void MavlinkPacketReceived(object sender, MavLinkPacketBase packet)
        {
            switch (packet.Message)
            {
                case UasHeartbeat HrtMsg:
                    switch (HrtMsg.BaseMode)
                    {
                        case MavModeFlag.ManualInputEnabled:
                            App.Wahana.FlightMode = FlightMode.MANUAL;
                            break;
                        case MavModeFlag.StabilizeEnabled:
                            App.Wahana.FlightMode = FlightMode.STABILIZER;
                            break;
                        case MavModeFlag.AutoEnabled:
                            App.Wahana.FlightMode = FlightMode.LOITER;
                            break;
                    }
                    //UpdateFlightModeMavlink();
                    break;

                case UasSysStatus SysMsg:
                    App.Wahana.Battery = (byte)SysMsg.BatteryRemaining;
                    App.Wahana.Signal = 100 - SysMsg.DropRateComm;
                    //UpdateSysStatusMavlink();
                    break;

                case UasAttitude AttMsg:
                    App.Wahana.IMU.Yaw = (float)(AttMsg.Yaw * 180 / Math.PI);
                    App.Wahana.IMU.Pitch = (float)(AttMsg.Pitch * 180 / Math.PI);
                    App.Wahana.IMU.Roll = (float)(AttMsg.Roll * 180 / Math.PI);
                    //UpdateAttitudeMavlink();
                    break;

                case UasGlobalPositionInt PosMsg:
                    App.Wahana.GPS.Latitude = PosMsg.Lat / 10000000.0;
                    App.Wahana.GPS.Longitude = PosMsg.Lon / 10000000.0;
                    App.Wahana.Altitude = PosMsg.Alt;
                    //UpdateGPSMavlink();
                    break;

                default:
                    Debug.WriteLine($"Mavlink {packet.Message.GetType().Name} message is not supported by this GCS");
                    break;
            }
        }

        #endregion

        #endregion

        #region Update UI

        bool IsFirstDataWahana { get; set; } = true;

        private void UpdateUIWahana()
        {
            var win = (MainWindow)App.Current.MainWindow;

            in_stream.Text =
                        ((byte)App.Wahana.FlightMode).ToString("X2") + " | "

                        + App.Wahana.Battery + " | "

                        + App.Wahana.Signal + " | "

                        + App.Wahana.IMU.Yaw + " | "
                        + App.Wahana.IMU.Pitch + " | "
                        + App.Wahana.IMU.Roll + " | "

                        + App.Wahana.Altitude + " | "

                        + App.Wahana.Speed + " | "

                        + App.Wahana.GPS.Latitude + " | "
                        + App.Wahana.GPS.Longitude + " | ";

            if (IsFirstDataWahana)
            {
                IsFirstDataWahana = false;

                win.StartWaktuTerbang();
                win.map_Ctrl.StartPosWahana();
                win.SetConnStat(TipeDevice.WAHANA, true);

                WhiteBoxWriter = new CsvFileWriter(new FileStream(App.DocsPath + "BlackBox_" + DateTime.Now.ToString("(HH.mm)(G\\MTz)_[dd-MM-yy]") + ".csv", FileMode.Create, FileAccess.Write));
            }

            switch (App.Wahana.FlightMode)
            {
                case FlightMode.MANUAL:
                    lbl_fmode.Content = "<MANUAL>";
                    lbl_fmode.Background = System.Windows.Media.Brushes.DarkRed;
                    break;

                case FlightMode.STABILIZER:
                    lbl_fmode.Content = "[STABILIZE]";
                    lbl_fmode.Background = System.Windows.Media.Brushes.LawnGreen;
                    break;

                case FlightMode.LOITER:
                    lbl_fmode.Content = "*AUTO*";
                    lbl_fmode.Background = System.Windows.Media.Brushes.BlueViolet;
                    break;

                case FlightMode.TAKEOFF:
                    break;
            }

            win.SetBaterai(App.Wahana.Battery);

            win.SetSignal(App.Wahana.Signal);

            tb_yaw.Text = App.Wahana.IMU.Yaw.ToString("0.00", CultureInfo.InvariantCulture) + "°";
            ind_heading.SetHeadingIndicatorParameters(Convert.ToInt32(App.Wahana.IMU.Yaw));

            tb_pitch.Text = App.Wahana.IMU.Pitch.ToString("0.00", CultureInfo.InvariantCulture) + "°";
            tb_roll.Text = App.Wahana.IMU.Roll.ToString("0.00", CultureInfo.InvariantCulture) + "°";
            ind_attitude.SetAttitudeIndicatorParameters(App.Wahana.IMU.Pitch, -App.Wahana.IMU.Roll);

            tb_airspeed.Text = App.Wahana.Speed.ToString(CultureInfo.InvariantCulture) + " km/j";
            ind_airspeed.SetAirSpeedIndicatorParameters((int)App.Wahana.Speed * 50);

            tb_alti.Text =
                win.track_Ctrl.tb_alti_wahana.Text =
                (App.Wahana.Altitude / 1000.0).ToString("0.00", CultureInfo.InvariantCulture) + " m";

            win.map_Ctrl.UpdatePosWahana();

            tb_lat.Text =
                win.track_Ctrl.tb_lat_wahana.Text =
                App.Wahana.GPS.Latitude.ToString("0.00000000", CultureInfo.InvariantCulture);
            tb_longt.Text =
                win.track_Ctrl.tb_longt_wahana.Text =
                App.Wahana.GPS.Longitude.ToString("0.00000000", CultureInfo.InvariantCulture);

            win.stats_Ctrl.addToStatistik(App.Wahana.IMU.Yaw, App.Wahana.IMU.Pitch, App.Wahana.IMU.Roll, win.WaktuTerbang);

            WriteWhiteBox();
        }

        public bool IsFirstDataTracker { get; set; } = true;

        #region NOT USED
        /*
        private void UpdateFlightData(char dataType)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            switch (dataType)
            {
                case 'T':
                    if (isFirstTracker)
                    {
                        isFirstTracker = false;
                        win.track_Ctrl.Integration(true);
                        win.track_Ctrl.isTrackerReady = true;
                        win.track_Ctrl.btn_tracking.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        win.map_Ctrl.StartPosGCS(trackergps.Latitude, trackergps.Longitude);
                    }
                    win.track_Ctrl.SetKoorGCS(trackergps.Latitude, trackergps.Longitude, 1.5f);
                    win.track_Ctrl.SetAttitude(tracker_yaw, tracker_pitch);
                    break;
            }

            if (!win.track_Ctrl.isTrackerReady) win.track_Ctrl.SetKoorTrack(lat, longt);
        }
        */
        #endregion

        /// <summary>
        /// CSV Writer for flight data white box
        /// </summary>
        CsvFileWriter WhiteBoxWriter;

        /// <summary>
        /// Write flight data to white box
        /// </summary>
        private void WriteWhiteBox()
        {
            /*  Urutan data yang disimpan
             *  
             *  TimeSpan WaktuTerbang (ticks)
             *  
             *  Tipe Device
             *  
             *  Flight mode
             *  
             *  Kapasitas Baterai
             *  
             *  Kualitas Sinyal
             *  
             *  Yaw
             *  Pitch
             *  Roll
             *  
             *  Speed
             *  
             *  Altitude
             *  
             *  Latitude
             *  Longitude
            */

            CsvRow baris = new CsvRow()
            {
                ((MainWindow)App.Current.MainWindow).WaktuTerbang.Ticks.ToString(),

                App.Wahana.Tipe.ToString(),

                App.Wahana.FlightMode.ToString(),

                App.Wahana.Battery.ToString(),

                App.Wahana.Signal.ToString(),

                App.Wahana.IMU.Yaw.ToString(),
                App.Wahana.IMU.Pitch.ToString(),
                App.Wahana.IMU.Roll.ToString(),

                App.Wahana.GPS.Latitude.ToString("0.########", CultureInfo.CurrentUICulture),
                App.Wahana.GPS.Longitude.ToString("0.########", CultureInfo.CurrentUICulture)
            };

            WhiteBoxWriter.WriteRow(baris);
        }

        #endregion


        #region Sending Data

        private void sendCommand(object sender, RoutedEventArgs e)
        {
            switch (out_stream.Text)
            {
                case "TAKEOFF":
                    SendToConnection(Command.TAKE_OFF, TipeDevice.WAHANA);
                    break;
                case "LAND":
                    SendToConnection(Command.LAND, TipeDevice.WAHANA);
                    break;
                case "BATAL":
                    SendToConnection(Command.BATALKAN, TipeDevice.WAHANA);
                    break;
            }
            out_stream.Text = "";
        }

        public void SendToConnection(Command cmd, TipeDevice tujuan, string track = "")
        {
            //Debug.WriteLine(cmd.ToString("X"));
            //if (true)
            //{ 
            //    switch (tujuan) {
            //        case TipeDevice.WAHANA:
            //            Sp_Used.BaseStream.WriteByte((Byte)cmd);
            //            break;
            //        case TipeDevice.TRACKER:
            //            Sp_Used.Write(string.Format("i,{0}", track));
            //            break;
            //    }
            //}
            //else { MessageBox.Show("Belum terkoneksi dengan efalcon", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information); }
        }

        public void SendToConnection(GMapMarker marker)
        {
            //string total = marker.Position.Lat.ToString() + ',' + marker.Position.Lng.ToString();
            //var data = Encoding.ASCII.GetBytes(total);
            //if (!IsConnected) return;
            //switch (IsWIFIConnected)
            //{
            //    case true: //using wifi
            //        //tcpSocket.SendAsync(data, total.Length);
            //        break;
            //    case false: //using usb serial
            //        Sp_Used.Write(total + '\n');
            //        break;
            //}
        }

        //dont forget to change to gateway
        public async void SendToConnection(byte[] data) { 
            //if (isUsingWifi) 
            //    udpSocket.Send(data, data.Length, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12727));
        }

        #endregion


        #region Animating back n forth

        private void OnConnHover(object sender, MouseEventArgs e)
        {
            if(!(sender as Button).IsEnabled) return;
            
            if (IsConnected)
            {
                img_conn.Source = Properties.Resources.icons8_disconnected_80.ToBitmapSource();
                ind_conn_status.Content = "Disconnect";
            }
            else
            {
                img_conn.Source = Properties.Resources.icons8_connected_80.ToBitmapSource();
                ind_conn_status.Content = "Connect";
            }
        }
        private void OnConnDehover(object sender, MouseEventArgs e)
        {
            if (!(sender as Button).IsEnabled) return;

            if (IsConnected)
            {
                img_conn.Source = Properties.Resources.icons8_connected_80.ToBitmapSource();
                ind_conn_status.Content = "Connected";
            }
            else
            {
                img_conn.Source = Properties.Resources.icons8_disconnected_80.ToBitmapSource();
                ind_conn_status.Content = "Disconnected";
            }
        }

        #endregion


        #region CameraStream prepare

        public ObservableCollection<FilterInfo> Cameras { get; set; } = new ObservableCollection<FilterInfo>();

        public FilterInfo CurrentCamera { get; set; }

        FileSystemWatcher watcher;
        private void PrepareWebcam()
        {
            cb_cams.IsEnabled = btn_livestream.IsEnabled = btn_take_picture.IsEnabled = false;

            if (!ListCameras()) return;

            cb_cams.SelectedIndex = 0;
            cb_cams.IsEditable = false;
            cb_cams.IsEnabled = btn_livestream.IsEnabled = btn_take_picture.IsEnabled = true;
        }

        private bool ListCameras()
        {
            foreach (FilterInfo vid_input in new FilterInfoCollection(FilterCategory.VideoInputDevice))
                Cameras.Add(vid_input);

            if (Cameras.Any()) return true;
            else return false;
        }

        private void RefreshCameras(object sender, RoutedEventArgs e)
        {
            btn_refreshcam.IsEnabled = cb_cams.IsEnabled = btn_livestream.IsEnabled = btn_take_picture.IsEnabled = false;

            Cameras.Clear();
            if (!ListCameras())
            {
                cb_cams.IsEditable = true;
                cb_cams.IsEnabled = false;
            }
            cb_cams.SelectedIndex = 0;

            btn_refreshcam.IsEnabled = cb_cams.IsEnabled = btn_livestream.IsEnabled = btn_take_picture.IsEnabled = true;
        }

        public void StopCam()
        {
            if(liveStream != null)
            {
                liveStream.SignalToStop();
                liveStream.NewFrame -= new NewFrameEventHandler(NewFrame_Available);
                btn_livestream.Content = "Start Stream";
                liveStream.Stop();
            }
        }

        private IVideoSource liveStream;
        private void ToggleCamStream(object sender, RoutedEventArgs e)
        {
            if (liveStream != null) StopCam();
            else if (CurrentCamera != null)
            {
                liveStream = new VideoCaptureDevice(CurrentCamera.MonikerString);
                liveStream.NewFrame += NewFrame_Available;
                liveStream.Start();
                btn_livestream.Content = "Stop Stream";
            }
        }

        private void NewFrame_Available(object sender, NewFrameEventArgs eventArgs)
        {
            BitmapSource bi;
            try
            {
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    bi = bitmap.ToBitmapSource();
                }
                bi.Freeze(); // avoid cross thread operations and prevents leaks
                Dispatcher.BeginInvoke(new ThreadStart(delegate { liveCam.Source = bi; }));
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error camera stream:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopCam();
            }
        }

        #endregion

        #region CameraStream captures

        string CapFolder = App.DocsPath + "/Camera Captures/" + DateTime.Now.ToString("MMM dd yyyy") + '/';

        bool IsFirstCapture = true;
        
        byte i = 1;
        private void AmbilGambar(object sender, RoutedEventArgs e)
        {
            if (liveCam.Source == null) return;
            FileStream file;

            if (IsFirstCapture)
            {
                IsFirstCapture = false;

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CapFolder));

                watcher = new FileSystemWatcher()
                {
                    Path = CapFolder,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    Filter = "*.jpeg",
                    EnableRaisingEvents = true
                };
                watcher.Created += GambarBaru;
            }

            try { file = new FileStream(CapFolder + "Capture_" + DateTime.Now.ToString("HH.mm.ss") + ".jpeg", FileMode.CreateNew); }
            catch { file = new FileStream(CapFolder + "Capture_" + DateTime.Now.ToString("HH.mm.ss_") + (i++).ToString() + ".jpeg", FileMode.CreateNew); }

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
                Margin = new Thickness(4, 4, 2, 4)
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

        public void ShowAvionics(bool status) => avInst.Visibility = status ? Visibility.Visible : Visibility.Hidden;

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
