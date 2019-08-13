using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
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
            selectedPort = sPorts[0];
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
            if (!connected)
            {
                startSerial(selectedPort, selectedBaud);
            }
            else if (thePort.IsOpen)
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
                tb_received.IsEnabled = true;
                mainWin.setConnStat(true);
            }
            else
            {
                cb_ports.IsEnabled = true;
                cb_bauds.IsEnabled = true;
                tb_received.IsEnabled = false;
                mainWin.setConnStat(false);
            }
        }

        private delegate void UpdateUiTextDelegate(string[] text);
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

                Console.WriteLine(string.Format("[Tracker] Extracted ({0}) data from dataIn ='{1}'", dataCount.Length, dataIn));
                if (dataCount.Length == 2)
                {
                    if (!dataCount.Contains(""))
                    {
                        Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(dataMasukan), dataCount);
                        Console.WriteLine("valid");
                    }
                }
                //thePort.DiscardInBuffer();
            }
            catch (Exception exc)
            {
                Console.WriteLine("invalid");
            }
        }

        //Update Data on UI
        private void dataMasukan(string[] theData)
        {
            tb_received.Text = string.Join(" | ", theData);
            tb_pitch.Text = theData[0];
            tb_pitch.Text = theData[1];
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
            GCS.Lat = lat; GCS.Longt = longt; GCS.Alti = alti;
            MainWindow win = (MainWindow)Window.GetWindow(this);
            win.map_Ctrl.StartPosGCS(GCS.Lat, GCS.Longt);
            isTrackerReady = true;
        }
        public void SetKoorWahana(double lat, double longt, float alti)
        {
            Wahana.Lat = lat; Wahana.Longt = longt; Wahana.Alti = alti;
            tb_lat_wahana.Text = lat.ToString();
            tb_longt_wahana.Text = longt.ToString();
            tb_alti_wahana.Text = alti.ToString();
        }

        #region Kalkulasi

        private double JarakHorizon()
        {
            double R = 6371;
            double deltaLat = (Wahana.Lat - GCS.Lat) * Math.PI / 180; //selisih Latitude dalam radian
            double deltaLongt = (Wahana.Longt - GCS.Longt) * Math.PI / 180; //selisih Longitude dalam radian

            //trigonometric
            double sisiA = Math.Pow(Math.Sin(deltaLat / 2), 2)
                            + Math.Cos(GCS.Lat * Math.PI / 180) * Math.Cos(Wahana.Lat * Math.PI / 180)
                            * Math.Pow(Math.Sin(deltaLongt / 2), 2);
            double sisiB = 2 * Math.Asin(Math.Min(1, Math.Sqrt(sisiA)));
            return R * sisiB;
        }

        private bool isTrackerReady = false;

        private void pasangTracker(object sender, RoutedEventArgs e)
        {
            if (tb_lat_tracker.Text != "" && tb_longt_tracker.Text != "" && tb_tinggi_tracker.Text != "")
            {
                SetKoorGCS(double.Parse(tb_lat_tracker.Text), double.Parse(tb_longt_tracker.Text), float.Parse(tb_tinggi_tracker.Text));
            } else {
                MessageBox.Show("Masukkan Latitude, Longitude, dan Tinggi Tracker!", "Posisi Invalid!", MessageBoxButton.OK, MessageBoxImage.Question);
            }
        }

        public void ArahkanTracker(double wahanalat, double wahanalongt, float wahanaalti)
        {
            SetKoorWahana(wahanalat, wahanalat, wahanaalti);
            if (!isTrackerReady) return;
            float deltaTinggi = Wahana.Alti - GCS.Alti;

            double jarakDarat = JarakHorizon();
            double jarakLangsung = Math.Sqrt((jarakDarat * jarakDarat) + (deltaTinggi * deltaTinggi));

            //double si_x = Math.Cos(GCS.Lat) * Math.Sin(Wahana.Lat)
            //            - Math.Sin(GCS.Lat) * Math.Cos(Wahana.Lat)
            //            * Math.Cos(Wahana.Longt - GCS.Longt);
            //double si_y = Math.Sin(Wahana.Longt - GCS.Longt) * Math.Cos(Wahana.Lat);
            //double ArahHorizon = Math.Atan2(si_y, si_x);

            double ArahHorizon = Math.Atan2(Math.Sin(Wahana.Longt - GCS.Longt) * Math.Cos(Wahana.Lat),
                                Math.Cos(GCS.Lat) * Math.Sin(Wahana.Lat)
                                - Math.Sin(GCS.Lat) * Math.Cos(Wahana.Lat)
                                * Math.Cos(Wahana.Longt - GCS.Longt)) * 180 / Math.PI;

            double ArahVerti = Math.Acos(jarakDarat / jarakLangsung);

            thePort.WriteLine(string.Format("{0},{1}",ArahHorizon,ArahVerti));

            tb_jarak_horizon.Text = jarakDarat.ToString() + " m";
            tb_jarak_lsg.Text = jarakLangsung.ToString() + " m";
            tb_bearing.Text = ArahHorizon.ToString() + "°";
            tb_pitch.Text = ArahVerti.ToString() + "°";
        }

        #endregion
    }
}
