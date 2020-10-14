using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace ESC_Tuning
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            switch (propertyName)
            {
                case "currADCOffset":
                    Debug.WriteLine("current ADC Offset changed");
                    SendToSerial(currADCOffset, "ADC");
                    break;
                case "currOC5Value":
                    Debug.WriteLine("current OC5 Value changed");
                    SendToSerial(currOC5Value, "OC5");
                    break;
            }
        }

        private double _currADCOffset = 0.0d;
        public double currADCOffset
        {
            get { return _currADCOffset; }
            set
            {
                if (value != _currADCOffset)
                {
                    _currADCOffset = value;
                    OnPropertyChanged("currADCOffset");
                }
            }
        }

        private ushort _currOC5Value = 0;
        public ushort currOC5Value
        {
            get { return _currOC5Value; }
            set
            {
                if (value != _currOC5Value)
                {
                    _currOC5Value = value;
                    OnPropertyChanged("currOC5Value");
                }
            }
        }

        private double _minADCOffset = 0.0d;
        public double minADCOffset
        {
            get { return _minADCOffset; }
            set
            {
                if (value != _minADCOffset)
                {
                    _minADCOffset = value;
                    OnPropertyChanged("minADCOffset");
                }
            }
        }

        private ushort _minOC5Value = 0;
        public ushort minOC5Value
        {
            get { return _minOC5Value; }
            set
            {
                if (value != _minOC5Value)
                {
                    _minOC5Value = value;
                    OnPropertyChanged("minOC5Value");
                }
            }
        }

        private double _maxADCOffset = 10.0d;
        public double maxADCOffset
        {
            get { return _maxADCOffset; }
            set
            {
                if (value != _maxADCOffset)
                {
                    _maxADCOffset = value;
                    OnPropertyChanged("maxADCOffset");
                }
            }
        }

        private ushort _maxOC5Value = 1000;
        public ushort maxOC5Value
        {
            get { return _maxOC5Value; }
            set
            {
                if (value != _maxOC5Value)
                {
                    _maxOC5Value = value;
                    OnPropertyChanged("maxOC5Value");
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            PrepareUSBConn();
            ADCOffsetStack.IsEnabled = false;
            OC5ValueStack.IsEnabled = false;
        }

        #region USBSerial

        public ObservableCollection<ComboBoxItem> sPorts { get; set; }
        public ComboBoxItem selectedPort { get; set; }
        public ComboBoxItem selectedBaud { get; set; }
        private SerialPort thePort;
        public bool connected = false, isCurrentlyRecv = false;
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
            else cb_bauds.IsEnabled = true;
        }

        private void refreshPortList()
        {
            while (sPorts.Count > 3)
            {
                sPorts.RemoveAt(3);
            }
            getPortList();
        }

        private void ToggleSerial(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!connected)
                {
                    if (startSerial(selectedPort, cb_bauds.Text) == 1) Debug.WriteLine("Serial is fine");
                    else return;
                    ADCOffsetStack.IsEnabled = true;
                    OC5ValueStack.IsEnabled = true;
                }
                else
                {
                    if (thePort != null) thePort.Close();
                    isCurrentlyRecv = false;
                    connected = false;
                    toggleConn(false);
                    ADCOffsetStack.IsEnabled = false;
                    OC5ValueStack.IsEnabled = false;
                }
            }
            catch (Exception none) { Debug.WriteLine("ToggleSerial: " + none.StackTrace); }
        }

        private int startSerial(ComboBoxItem comPort, string baud)
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
                thePort = new SerialPort(comPort.Content.ToString(), int.Parse(baud), Parity.None, 8, StopBits.One);
                thePort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                thePort.NewLine = "\n";
                thePort.ReadTimeout = 5000;
                thePort.ReceivedBytesThreshold = 8;
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
                Debug.WriteLine(ex.StackTrace);
                return 0;
            }
            return 1;
        }

        private void toggleConn(bool s)
        {
            if (s)
            {
                cb_ports.IsEnabled = false;
                cb_bauds.IsEnabled = false;
                ind_conn_status.Content = "Connected";
            }
            else
            {
                cb_ports.IsEnabled = true;
                cb_bauds.IsEnabled = true;
                ind_conn_status.Content = "Disconnected";
                isCurrentlyRecv = false;
            }
        }

        private delegate void UpdateUiTextDelegate(byte[] text);
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            List<byte> dataIn = new List<byte>();
            SerialPort receive = (SerialPort)sender;

            try
            {
                while (receive.BytesToRead > 0)
                {
                    byte bytein = (byte)receive.ReadByte();
                    if (bytein == 0x0A) break;
                    dataIn.Add(bytein);
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("invalid");
                Debug.WriteLine(exc.StackTrace);
            }
        }

        private async void SendToSerial(double value, string val)
        {
            try
            {
                switch (val)
                {
                    case "ADC":
                        Debug.WriteLine("#ADO=" + value.ToString("0.000"));
                        thePort.Write("#ADO=" + value.ToString("0.000"));
                        break;
                    case "OC5":
                        Debug.WriteLine("#OC5=" + value.ToString());
                        thePort.Write("#OC5=" + value.ToString());
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        #endregion
    }
}
