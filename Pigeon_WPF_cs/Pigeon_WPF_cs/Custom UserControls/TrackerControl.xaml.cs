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

        public void SetKoorGCS(double lat, double longt)
        {
            gcslat = lat; gcslongt = longt;
        }

        #region Kalkulasi
        
        private double gcslat, gcslongt;
        private void kalkulasiArah(double wahanalat, double wahanalongt, short tinggi)
        {
            //konversi selisih koordinat menjadi detik(derajat) menjadi meter
            double jarakX = ((wahanalat - gcslat) * 3600) * 30.416;
            double jarakY = ((wahanalongt - gcslongt) * 3600) * 30.416;

            //segitiga trigonometri
            double jarakDarat = Math.Sqrt((jarakX * jarakX) + (jarakY * jarakY));
            double jarakReal = Math.Sqrt((jarakDarat * jarakDarat) + (tinggi * tinggi));

            float arahhorizontal, arahvertikal;
            //ArahkanTracker(arahhorizontal, arahvertikal);
        }

        private void ArahkanTracker(float yaw, float pitch)
        {

        }

        #endregion
    }
}
