using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace Pigeon_WPF_cs.Custom_UserControls
{
    
    /// <summary>
    /// Interaction logic for TrackerControl.xaml
    /// </summary>
    public partial class TrackerControl : UserControl
    {
        private struct Posisi
        {
            public double Lat, Longt;
            public float Alti;
        }
        
        public TrackerControl()
        {
            InitializeComponent();
            DataContext = this;
            PrepareUSBConn();
            //foreach(ItemCollection ports in cb_ports.Items)
            //{
            //    Console.WriteLine(ports.ToString());
            //}
        }

        //our connection is integrated in efalcon tracker
        public void IntegrateTrackerConn(bool isWifi)
        {
            if (isWifi)
            {
                isUsingWifi = isWifi;
                track_conn_bt.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else toggleConn(true);
        }

        #region WIFI UDP

        private bool isUsingWifi = false;
        private UdpClient udpSocket;

        private bool isCurrentlyRecv = false;
        private void StartListening(IAsyncResult result)
        {
            if (!isCurrentlyRecv) return;
            isCurrentlyRecv = true;
            var theEndPoint = new IPEndPoint(IPAddress.Any, 60111);
            byte[] received = udpSocket.EndReceive(result, ref theEndPoint);

            string[] recvstr = Encoding.ASCII.GetString(received).Split(',');
            if (recvstr.Length == 4)
                //if (received.Length == 24)
            {
                Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataMasukan), recvstr);
            }

            udpSocket.BeginReceive(new AsyncCallback(StartListening), null);
            return;
        }
        
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
            //foreach (string it in ports)
            //{
            //    Console.WriteLine(it + "From TrackerControl");
            //}
            foreach (string port in ports) { sPorts.Add(new ComboBoxItem { Content = port }); }
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

        private void startSerial(ComboBoxItem comPort, ComboBoxItem baud)
        {
            //Console.WriteLine("Connecting to " + comPort.Content.ToString() + " on " + baud.Content.ToString());
            if (comPort.Content.ToString().Length > 5 || comPort.Content.ToString() == "")
            {
                MessageBox.Show("Tidak ada COM PORT yang dipilih!");
                return;
            }
            if (cb_bauds.SelectedIndex == 0)
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
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Port sudah digunakan,\nsilakan pilih port lain.", "Port digunakan", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening serial port :: " + ex.Message, "Error!");
                Console.WriteLine(ex.StackTrace);
                return;
            }
        }

        public void ToggleSerial(object sender, RoutedEventArgs e)
        {
            var win = (MainWindow)Window.GetWindow(this);
            if (isUsingWifi)
            {
                isUsingWifi = !isUsingWifi;
                isCurrentlyRecv = true;
                udpSocket = new UdpClient(9600);
                udpSocket.BeginReceive(new AsyncCallback(StartListening), null);
                toggleConn(true);
            }
            else if (!connected)
            {
                try { startSerial(selectedPort, selectedBaud); } catch { return; }

                toggleConn(true);
            }
            else
            {
                if (thePort != null) thePort.Close();
                isCurrentlyRecv = false;
                if (udpSocket != null) udpSocket.Close();

                toggleConn(false);
            }
        }

        private void toggleConn(bool s)
        {
            MainWindow mainWin = (MainWindow)Window.GetWindow(this);
            if (s)
            {
                connected = true;
                btn_tracking.IsEnabled = true;
                btn_postracker.IsEnabled = true;
                cb_ports.IsEnabled = false;
                cb_bauds.IsEnabled = false;
                tb_received.IsEnabled = true;
                mainWin.setConnStat(true, true);
            }
            else
            {
                cb_ports.IsEnabled = true;
                cb_bauds.IsEnabled = true;
                tb_received.IsEnabled = false;
                mainWin.setConnStat(false, true);
                connected = false;
                isTrackerReady = false;
                isTracking = false;
                btn_tracking.IsEnabled = false;
                btn_postracker.IsEnabled = false;
            }
        }

        private delegate void UpdateUiTextDelegate(string[] text);
        private delegate void UpdateUiTextDelegateByte(byte[] text);
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string dataIn = "";
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

                //Console.WriteLine(string.Format("[Tracker] Extracted ({0}) data from dataIn ='{1}'", dataCount.Length, dataIn));
                if (dataCount.Length == 2)
                {
                    if (!dataCount.Contains(""))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataMasukan), dataCount);
                        //Console.WriteLine("valid");
                    }
                }
                thePort.DiscardInBuffer();
            }
            catch (Exception exc)
            {
                Console.WriteLine("invalid");
                Console.WriteLine(exc.StackTrace);
            }
        }

        //Update Data on UI
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

        public void StartTracking()
        {
            toggleConn(true);
            track_conn_bt.IsEnabled = false;
        }

        public void SetKoorTrack(double lat, double longt)
        {
            tb_lat_tracker.Text = lat.ToString();
            GCS.Lat = lat;
            tb_longt_tracker.Text = longt.ToString();
            GCS.Longt = longt;
        }

        float heading;
        public void dataMasukan(byte[] theData)
        {
            tb_pitch.Text = BitConverter.ToSingle(SplitBytes(theData, 0, 4), 0).ToString();
            tb_bearing.Text = BitConverter.ToSingle(SplitBytes(theData, 4, 4), 0).ToString();
            GCS.Lat = BitConverter.ToSingle(SplitBytes(theData, 8, 8), 0);
            GCS.Longt = BitConverter.ToSingle(SplitBytes(theData, 16, 8), 0);
            Console.WriteLine("Track: " + tb_pitch.Text + '|' + tb_bearing.Text + '|' + GCS.Lat.ToString() + '|' + GCS.Longt.ToString());
            //tb_received.Text = Encodin;
        }

        //float pitchs, heading;
        public void dataMasukan(string[] theData)
        {
            //if (!isTracking) return;
            tb_received.Text = string.Join(" | ", theData);
            //theData[1].TrimEnd('\n');
            tb_pitch.Text = theData[0] + '°';
            tb_bearing.Text = theData[1]+'°';
            tb_lat_tracker.Text = theData[2];
            tb_longt_tracker.Text = theData[3];
            GCS.Lat = double.Parse(tb_lat_tracker.Text, CultureInfo.InvariantCulture);
            GCS.Longt = double.Parse(tb_longt_tracker.Text, CultureInfo.InvariantCulture);
            //float.TryParse(theData[0], out pitchs);
            //float.TryParse(theData[1], out heading);
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

        private Posisi GCS, Wahana;
        public void SetKoorGCS(double lat, double longt, float alti)
        {
            //GCS.Lat = lat; GCS.Longt = longt; GCS.Alti = alti;
            MainWindow win = (MainWindow)Window.GetWindow(this);
            win.map_Ctrl.StartPosGCS(GCS.Lat, GCS.Longt);
        }
        public void SetKoorWahana(double lat, double longt, float alti)
        {
            Wahana.Lat = lat; Wahana.Longt = longt; Wahana.Alti = alti;
            tb_lat_wahana.Text = lat.ToString();
            tb_longt_wahana.Text = longt.ToString();
            tb_alti_wahana.Text = alti.ToString() + " m";
        }

        public bool isTrackerReady = false;
        private void pasangTracker(object sender, RoutedEventArgs e)
        {
            if (tb_lat_tracker.Text != "" && tb_longt_tracker.Text != "" && tb_tinggi_tracker.Text != "" && connected)
            {
                SetKoorGCS(GCS.Lat, GCS.Longt, float.Parse(tb_tinggi_tracker.Text, CultureInfo.InvariantCulture));
                isTrackerReady = true;
                btn_tracking.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else if (!connected)
            {
                MessageBox.Show("Tracker tidak terkoneksi!", "Tracker Koneksi", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                MessageBox.Show("Masukkan Latitude, Longitude, dan Tinggi Tracker!", "Posisi Invalid!", MessageBoxButton.OK, MessageBoxImage.Question);
            }
        }

        bool isTracking = false;
        public bool GetTrackStatus() { return isTracking; }
        private void toggleTracking(object sender, RoutedEventArgs e)
        {
            if (!isTracking)
            {
                isTracking = true;
                lbl_start_stop.Content = "Stop Tracking";
                ico_start_stop.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-stop-50.png"));

                //DispatcherTimer rotateTracker = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200) };
                //rotateTracker.Tick += cmd_rotateTracker;
                //rotateTracker.Start();
            }
            else
            {
                isTracking = false;
                lbl_start_stop.Content = "Start Tracking";
                ico_start_stop.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-play-50.png"));
            }
        }

        short rotasi = 0; byte pitch = 0;
        private void cmd_rotateTracker(object sender, EventArgs e)
        {
            if (!isTracking)
            {
                var it = (DispatcherTimer)sender;
                it.Stop();
            }
            ArahkanTracker();
            //if (rotasi > 359) rotasi = 0;
            //if (pitch > 90) pitch = 0;
            //rotasi += 2;
            //pitch++;
            //thePort.WriteLine(pitch.ToString() + "," + rotasi.ToString());
            //Console.WriteLine("Sendin Tracker: " + pitch.ToString() + "," + rotasi.ToString());
        }

        #region Kalkulasi

        private double JarakHorizon()
        {
            double R = 6371000;
            double deltaLat = (Wahana.Lat - GCS.Lat) * Math.PI / 180; //selisih Latitude dalam radian
            double deltaLongt = (Wahana.Longt - GCS.Longt) * Math.PI / 180; //selisih Longitude dalam radian

            //trigonometric
            double sisiA = Math.Pow(Math.Sin(deltaLat / 2), 2)
                            + Math.Cos(GCS.Lat * Math.PI / 180) * Math.Cos(Wahana.Lat * Math.PI / 180)
                            * Math.Pow(Math.Sin(deltaLongt / 2), 2);
            double sisiB = 2 * Math.Asin(Math.Min(1, Math.Sqrt(sisiA)));
            return R * sisiB;
        }

        public void ArahkanTracker()//double wahanalat, double wahanalongt, float wahanaalti)
        {
            //SetKoorWahana(wahanalat, wahanalat, wahanaalti);
            if (!isTracking || Wahana.Lat == 0 || Wahana.Longt == 0) return;
            float deltaTinggi = Wahana.Alti - GCS.Alti;

            double jarakDarat = JarakHorizon();
            double jarakLangsung = Math.Sqrt((jarakDarat * jarakDarat) + (deltaTinggi * deltaTinggi));
            
            double ArahHorizon = Math.Atan2(Math.Sin(Wahana.Longt - GCS.Longt) * Math.Cos(Wahana.Lat),
                                (Math.Cos(GCS.Lat) * Math.Sin(Wahana.Lat))
                                - (Math.Sin(GCS.Lat) * Math.Cos(Wahana.Lat)
                                * Math.Cos(Wahana.Longt - GCS.Longt))) * 180 / Math.PI;
            ArahHorizon = (ArahHorizon + 360) % 360;

            //double ArahVerti = Math.Acos(jarakDarat / jarakLangsung);
            double ArahVerti = Math.Atan(Math.Tan(deltaTinggi / jarakDarat)) * (180 / Math.PI);

            try { /*thePort.WriteLine(string.Format("{0},{1}", Convert.ToInt16(ArahVerti), Convert.ToInt32(ArahHorizon)));*/
                string data = "i," + ArahVerti.ToString("0.00") + ',' + ArahHorizon.ToString("0.00") + '\n';
                byte[] datagram = Encoding.ASCII.GetBytes(data);
                udpSocket.SendAsync(datagram, datagram.Length, new IPEndPoint(IPAddress.Parse("192.168.4.1"), 60111));
                //Console.WriteLine("Sending: " + data);
            }
            catch(Exception e) { Console.WriteLine(e.StackTrace); }
            //Console.WriteLine(string.Format("Alti = {0}, Darat = {1}, Verti = {2} ", GCS.Alti, jarakDarat, ArahVerti));
            //Console.WriteLine(string.Format("Sending: {0},{1}", ArahVerti, ArahHorizon));

            MainWindow win = (MainWindow)Window.GetWindow(this);
            win.map_Ctrl.SetHeadingGCS(Convert.ToSingle(ArahHorizon));

            if (jarakDarat / 1000 >= 1.0f) tb_jarak_horizon.Text = (jarakDarat / 1000).ToString("0.000",CultureInfo.InvariantCulture) + " km";
            else tb_jarak_horizon.Text = jarakDarat.ToString("0.00", CultureInfo.InvariantCulture) + " m";
            if (jarakLangsung / 1000 >= 1.0f) tb_jarak_lsg.Text = (jarakLangsung / 1000).ToString("0.000", CultureInfo.InvariantCulture) + " km";
            else tb_jarak_lsg.Text = jarakLangsung.ToString("0.00", CultureInfo.InvariantCulture) + " m";
        }

        #endregion
        
    }
}
