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
        }

        #region BUTTONS

        private async void ToggleConnection(object sender, RoutedEventArgs e)
        {
            switch (ConnectionType)
            {
                case ConnType.Internet:
                    DoInternetConn();
                    break;

                case ConnType.WIFI:
                    if (!await ConnectWIFIAsync())
                        return;
                    break;

                case ConnType.SerialPort:
                    if (!await ConnectSerialPort(SelectedConn, SelectedBaud))
                        return;
                    break;

                default:
                    if (Sp_Used != null) Sp_Used.Dispose();
                    else if (WIFISocket != null) WIFISocket.Dispose();
                    else if (WIFIAsyncEvent != null) WIFIAsyncEvent.Dispose();
                    else return;

                    IsConnected = false;
                    SetConnection(false);
                    
                    return;
            }

            SetConnection(true);
            IsConnected = true;
        }

        private void SetConnection(bool s)
        {
            // We are connected
            if (s)
            {
                cb_ports.IsEnabled = false;
                cb_bauds.IsEnabled = false;

                stream_panel.IsEnabled = true;

                //img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-connected-80.png"));
                img_conn.Source = Properties.Resources.icons8_connected_80.ToBitmapImage();
                ind_conn_status.Content = "Connected";
            }

            // We are disconnected
            else
            {
                cb_ports.IsEnabled = true;
                cb_bauds.IsEnabled = true;

                stream_panel.IsEnabled = false;

                //img_conn.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-disconnected-80.png"));
                img_conn.Source = Properties.Resources.icons8_disconnected_80.ToBitmapImage();
                ind_conn_status.Content = "Disconnected";

                if (WhiteBoxWriter != null) WhiteBoxWriter.Dispose();
            }
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
            #region Dummy data server

            Task.Run(() =>
            {
                byte[] txBuf = new byte[]
                {
                    (byte)BufferHeader.EFALCON4,
                    (byte)FlightMode.MANUAL,
                    (byte)90,
                    (byte)75,
                    0xb8, 0x9e, 0x15, 0x43,
                    0x71, 0x3d, 0x6c, 0xc1,
                    0x66, 0x66, 0xba, 0x40,
                    0x00, 0x00, 0xcc, 0x41,
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

                new Timer((state) =>
                {
                    theClient.Client.Send(txBuf);
                }, null, 0, 1000);
            });

            #endregion

            WIFISocket = new TcpClient() { NoDelay = true };
            WIFIAsyncEvent = new SocketAsyncEventArgs();

            WIFIAsyncEvent.Completed += WIFIAsyncEvent_Completed;

            List<NetworkInterface> interfaces = new List<NetworkInterface>(NetworkInterface.GetAllNetworkInterfaces().Where(intf => intf.OperationalStatus == OperationalStatus.Up));
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
                    Debug.WriteLine($"Try Connecting to [ {gatewayIP.Address} ]");

                    if (await Task.WhenAny(WIFISocket.ConnectAsync(gatewayIP.Address, 61258), Task.Delay(1000)) != null && WIFISocket.Connected)
                    {
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
            Debug.Write("ParseDataAsync: ASCII-> ");
            Debug.WriteLine(Encoding.ASCII.GetString(RxBuf));

            Debug.Write(" HEX-> ");
            for (int i = 0; i < BufLen; i++)
            {
                Debug.Write(RxBuf[i].ToString("X2") + ' ');
            }
            Debug.WriteLine("");
            #endregion

            if (!Enum.IsDefined(typeof(BufferHeader), RxBuf[0])) return;

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

                App.CurrWahana.Tipe = TipeDevice.WAHANA;

                App.CurrWahana.FlightMode = (FlightMode)RxBuf[1];

                App.CurrWahana.Battery = RxBuf[2];

                App.CurrWahana.Signal = RxBuf[3];

                App.CurrWahana.IMU.Yaw = BitConverter.ToSingle(RxBuf, 4);
                App.CurrWahana.IMU.Pitch = BitConverter.ToSingle(RxBuf, 8);
                App.CurrWahana.IMU.Roll = BitConverter.ToSingle(RxBuf, 12);

                App.CurrWahana.Altitude = BitConverter.ToInt32(RxBuf, 16);

                App.CurrWahana.Speed = BitConverter.ToSingle(RxBuf, 20);

                App.CurrWahana.GPS.Latitude = BitConverter.ToSingle(RxBuf, 24);
                App.CurrWahana.GPS.Longitude = BitConverter.ToSingle(RxBuf, 28);

                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUIDelegate(UpdateUI));
            }

            #endregion

            #region TRITON

            else if (RxBuf[0] == (byte)BufferHeader.TRITON)
            {
                #region Urutan data Triton
                /* (angka terakhir itu index)
                * 
                * [Header]      = HEADER_BUF uint8          (1 byte) 0
                * 
                * [Yaw]		    = derajat float32		    (4 byte) 1
                * [Pitch]	    = derajat float32		    (4 byte) 5
                *
                * [Altitude]    = milimeter int32		    (4 byte) 9
                * 
                * [Lat]		    = decimal degrees float32	(4 byte) 13
                * [Lon]         = decimal degrees float32	(4 byte) 17
                */
                #endregion

                App.CurrWahana.Tipe = TipeDevice.TRACKER;

                App.CurrWahana.IMU.Yaw = BitConverter.ToSingle(RxBuf, 4);
                App.CurrWahana.IMU.Pitch = BitConverter.ToSingle(RxBuf, 8);

                App.CurrWahana.Altitude = BitConverter.ToInt32(RxBuf, 16);

                App.CurrWahana.Speed = BitConverter.ToSingle(RxBuf, 20);

                App.CurrWahana.GPS.Latitude = BitConverter.ToSingle(RxBuf, 24);
                App.CurrWahana.GPS.Longitude = BitConverter.ToSingle(RxBuf, 28);

                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUIDelegate(UpdateUI));
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
                            App.CurrWahana.FlightMode = FlightMode.MANUAL;
                            break;
                        case MavModeFlag.StabilizeEnabled:
                            App.CurrWahana.FlightMode = FlightMode.STABILIZER;
                            break;
                        case MavModeFlag.AutoEnabled:
                            App.CurrWahana.FlightMode = FlightMode.LOITER;
                            break;
                    }
                    //UpdateFlightModeMavlink();
                    break;

                case UasSysStatus SysMsg:
                    App.CurrWahana.Battery = (byte)SysMsg.BatteryRemaining;
                    App.CurrWahana.Signal = 100 - SysMsg.DropRateComm;
                    //UpdateSysStatusMavlink();
                    break;

                case UasAttitude AttMsg:
                    App.CurrWahana.IMU.Yaw = (float)(AttMsg.Yaw * 180 / Math.PI);
                    App.CurrWahana.IMU.Pitch = (float)(AttMsg.Pitch * 180 / Math.PI);
                    App.CurrWahana.IMU.Roll = (float)(AttMsg.Roll * 180 / Math.PI);
                    //UpdateAttitudeMavlink();
                    break;

                case UasGlobalPositionInt PosMsg:
                    App.CurrWahana.GPS.Latitude = PosMsg.Lat / 10000000.0;
                    App.CurrWahana.GPS.Longitude = PosMsg.Lon / 10000000.0;
                    App.CurrWahana.Altitude = PosMsg.Alt;
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

        bool IsFirstData { get; set; } = true;

        private void UpdateUI()
        {
            var win = (MainWindow)App.Current.MainWindow;

            in_stream.Text =
                App.CurrWahana.FlightMode.ToString("X2") + " | "
                
                + App.CurrWahana.Battery + " | "

                + App.CurrWahana.Signal + " | "

                + App.CurrWahana.IMU.Yaw + " | "
                + App.CurrWahana.IMU.Pitch + " | "
                + App.CurrWahana.IMU.Roll + " | "

                + App.CurrWahana.Altitude + " | "

                + App.CurrWahana.Speed + " | "

                + App.CurrWahana.GPS.Latitude + " | "
                + App.CurrWahana.GPS.Longitude + " | "
                ;

            switch (App.CurrWahana.Tipe)
            {
                case TipeDevice.WAHANA:
                    if (IsFirstData)
                    {
                        IsFirstData = false;

                        win.StartWaktuTerbang();
                        win.map_Ctrl.StartPosWahana(App.CurrWahana.GPS.Latitude, App.CurrWahana.GPS.Longitude, App.CurrWahana.IMU.Yaw);
                        win.SetConnStat(TipeDevice.WAHANA, true);
                    }

                    win.map_Ctrl.SetPosWahana(App.CurrWahana.GPS.Latitude, App.CurrWahana.GPS.Longitude, App.CurrWahana.IMU.Yaw);

                    win.track_Ctrl.SetKoorWahana(App.CurrWahana.GPS.Latitude, App.CurrWahana.GPS.Longitude, App.CurrWahana.Altitude);

                    win.SetBaterai(App.CurrWahana.Battery);

                    win.SetSignal(App.CurrWahana.Signal);

                    tb_yaw.Text = App.CurrWahana.IMU.Yaw.ToString("0.00", CultureInfo.InvariantCulture) + "°";
                    ind_heading.SetHeadingIndicatorParameters(Convert.ToInt32(App.CurrWahana.IMU.Yaw));

                    tb_pitch.Text = App.CurrWahana.IMU.Pitch.ToString("0.00", CultureInfo.InvariantCulture) + "°";
                    tb_roll.Text = App.CurrWahana.IMU.Roll.ToString("0.00", CultureInfo.InvariantCulture) + "°";
                    ind_attitude.SetAttitudeIndicatorParameters(App.CurrWahana.IMU.Pitch, -App.CurrWahana.IMU.Roll);

                    tb_airspeed.Text = App.CurrWahana.Speed.ToString(CultureInfo.InvariantCulture) + " km/j";
                    ind_airspeed.SetAirSpeedIndicatorParameters((int)App.CurrWahana.Speed * 50);

                    tb_alti.Text = App.CurrWahana.Altitude.ToString("0.00", CultureInfo.InvariantCulture) + " m";

                    tb_lat.Text = App.CurrWahana.GPS.Latitude.ToString("0.00000000", CultureInfo.InvariantCulture);
                    tb_longt.Text = App.CurrWahana.GPS.Longitude.ToString("0.00000000", CultureInfo.InvariantCulture);

                    win.stats_Ctrl.addToStatistik(App.CurrWahana.IMU.Yaw, App.CurrWahana.IMU.Pitch, App.CurrWahana.IMU.Roll, win.WaktuTerbang);
                    
                    break;

                case TipeDevice.TRACKER:
                    if (IsFirstData)
                    {
                        IsFirstData = false;


                    }
                    break;
            }
        }

        #region NOT USED

        //Update Data on UI
        //bool isFirstNav = true, isFirstMode = true, isFirstBatt = true, isFirstTracker = true;
        //bool isBlackBoxRecord = true;
        //double lastlat = 0, lastlng = 0;
        //private void UpdateFlightData(char dataType)
        //{
        //    MainWindow win = (MainWindow)Window.GetWindow(this);

        //    in_stream.Text = fmode + " | " + heading_val + " | " + pitch_val + " | " + roll_val + " | " + airspeed_val + " | " + alti_val + " | " + efalcongps.Latitude + " | " + efalcongps.Longitude + " | " + batt_volt;

        //    try
        //    {
        //        switch (dataType)
        //        {
        //            case 'N':
        //                if (isFirstNav)
        //                {
        //                    isFirstNav = false;
        //                    win.StartWaktuTerbang();
        //                    win.map_Ctrl.StartPosWahana(efalcongps.Latitude, efalcongps.Longitude, heading_val);
        //                    win.SetConnStat(TipeDevice.WAHANA, true);
        //                }
        //                win.map_Ctrl.SetPosWahana(efalcongps.Latitude, efalcongps.Longitude, heading_val);
        //                win.track_Ctrl.SetKoorWahana(efalcongps.Latitude, efalcongps.Longitude, alti_val);

        //                //if (win.track_Ctrl.isTrackerReady) win.track_Ctrl.ArahkanTracker();

        //                tb_yaw.Text = heading_val.ToString("0.00", CultureInfo.InvariantCulture) + "°";
        //                ind_heading.SetHeadingIndicatorParameters(Convert.ToInt32(heading_val));

        //                tb_pitch.Text = pitch_val.ToString("0.00", CultureInfo.InvariantCulture) + "°";
        //                tb_roll.Text = roll_val.ToString("0.00", CultureInfo.InvariantCulture) + "°";
        //                ind_attitude.SetAttitudeIndicatorParameters(pitch_val, -roll_val);

        //                //tb_airspeed.Text = airspeed_val.ToString(CultureInfo.CurrentUICulture) + " km/j";
        //                //ind_airspeed.SetAirSpeedIndicatorParameters(airspeed_val);

        //                tb_alti.Text = alti_val.ToString("0.00", CultureInfo.InvariantCulture) + " m";

        //                tb_lat.Text = efalcongps.Latitude.ToString("0.00000000", CultureInfo.InvariantCulture);
        //                tb_longt.Text = efalcongps.Longitude.ToString("0.00000000", CultureInfo.InvariantCulture);

        //                win.stats_Ctrl.addToStatistik(heading_val, pitch_val, roll_val, win.WaktuTerbang);
        //                break;

        //            case 'M':
        //                if (isFirstMode)
        //                {
        //                    isFirstMode = false;
        //                    win.map_Ctrl.FmodeEnable(true);
        //                }
        //                win.map_Ctrl.SetMode(fmode);
        //                break;

        //            case 'B':
        //                if (isFirstBatt) isFirstBatt = false;
        //                win.SetBaterai(batt_volt, batt_cur);
        //                break;

        //            case 'T':
        //                if (isFirstTracker)
        //                {
        //                    isFirstTracker = false;
        //                    win.track_Ctrl.Integration(true);
        //                    win.track_Ctrl.isTrackerReady = true;
        //                    win.track_Ctrl.btn_tracking.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        //                    win.map_Ctrl.StartPosGCS(trackergps.Latitude, trackergps.Longitude);
        //                }
        //                win.track_Ctrl.SetKoorGCS(trackergps.Latitude, trackergps.Longitude, 1.5f);
        //                win.track_Ctrl.SetAttitude(tracker_yaw, tracker_pitch);
        //                break;
        //        }
        //    }catch(Exception e)
        //    {
        //        Debug.WriteLine(e.Message);
        //    }
        //    if (!isFirstNav && !isFirstMode && !isFirstBatt)
        //    {
        //        if(menulis == null) menulis = new CsvFileWriter(new FileStream(docpath + "BlackBox_" + DateTime.Now.ToString("(HH.mm)(G\\MTz)_[dd-MM-yy]") + ".csv", FileMode.Create, FileAccess.Write));
        //        WriteBlackBox();
        //    }

        //    //if (!win.track_Ctrl.isTrackerReady) win.track_Ctrl.SetKoorTrack(lat, longt);
        //}

        //speak out our current heading, speed, altitude
        //private void SpeakOutLoud(object sender, EventArgs e)
        //{
        //    MainWindow win = (MainWindow)Window.GetWindow(this);
        //    var str = "<s>Heading <emphasis><say-as interpret-as=\"number\">"+heading_val.ToString("0")+"</say-as></emphasis> derajat</s>" +
        //        "<s>Ketinggian <emphasis><say-as interpret-as=\"number\">" + alti_val.ToString("0") + "</say-as></emphasis> meter</s>" +
        //        "<s>Kecepatan <emphasis><say-as interpret-as=\"number\">" + airspeed_val.ToString("0") + "</say-as></emphasis> kilometer per jam</s>";
        //    win.SpeakOutloud(str);
        //}

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

                App.CurrWahana.Tipe.ToString(),

                App.CurrWahana.FlightMode.ToString(),

                App.CurrWahana.Battery.ToString(),

                App.CurrWahana.Signal.ToString(),

                App.CurrWahana.IMU.Yaw.ToString(),
                App.CurrWahana.IMU.Pitch.ToString(),
                App.CurrWahana.IMU.Roll.ToString(),

                App.CurrWahana.GPS.Latitude.ToString("0.########", CultureInfo.CurrentUICulture),
                App.CurrWahana.GPS.Longitude.ToString("0.########", CultureInfo.CurrentUICulture)
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
        private void img_conn_0(object sender, MouseEventArgs e)
        {
            if (IsConnected)
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
            if (IsConnected)
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
        private void ToggleCamStream(object sender, RoutedEventArgs e)
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

        string CapFolder = App.DocsPath + "/Camera Captures/" + DateTime.Now.ToString("MMM dd yyyy") + '/';
        bool isFirstCapture = true;
        byte i = 1;
        private void AmbilGambar(object sender, RoutedEventArgs e)
        {
            if (liveCam.Source == null) return;
            FileStream file;

            if (isFirstCapture)
            {
                isFirstCapture = false;
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
