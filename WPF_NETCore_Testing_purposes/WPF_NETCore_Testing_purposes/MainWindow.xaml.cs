/*#define USEDEBUG*/

using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace WPF_NETCore_Testing_purposes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort sPort;
        List<byte> sPortBytes;

        [MessagePackObject]
        public class FlightData
        {
            [Key(0)]
            public ArraySegment<short> Gyro { get; set; }
            [Key(1)]
            public ArraySegment<short> Accel { get; set; }
            [Key(2)]
            public float Pitch { get; set; }
            [Key(3)]
            public float Roll { get; set; }
        }

        public FlightData flightData;

        public MainWindow()
        {
            InitializeComponent();

            flightData = new FlightData();
            sPortBytes = new List<byte>();

#if USEDEBUG
            flightData.Gyro = new short[3] { 1, -1, 1 };
            flightData.Accel = new short[3] { 352, 525, 4521 };
            flightData.Pitch = 0.1203f;
            flightData.Roll = -0.231f;
#else
            flightData.Gyro = new short[3] { 0, 0, 0 };
            flightData.Accel = new short[3] { 0, 0, 0 };
            flightData.Pitch = 0;
            flightData.Roll = 0;
#endif

            sPort = new SerialPort("COM6", 2000000, Parity.None, 8, StopBits.One);
            sPort.DataReceived += SPort_DataReceived;
            sPort.ReceivedBytesThreshold = 25;
            sPort.Open();
        }

        private delegate void UpdateUiTextDelegate();

        private void SPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort usedPort = (SerialPort)sender;
            while (usedPort.BytesToRead > 0)
            {
                sPortBytes.Add((byte)usedPort.ReadByte());
                Debug.Write(sPortBytes.Last().ToString("X") + " ");
            }
            Debug.WriteLine("");

            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(UpdateFlightData));
        }

        private void UpdateFlightData()
        {
            try
            {
                flightData = MessagePackSerializer.Deserialize<FlightData>(sPortBytes.ToArray());
                sPortBytes.Clear();

                tb1.Text = "Serialized data:\r\n";
                foreach (var bite in MessagePackSerializer.Serialize(flightData))
                {
                    tb1.Text += bite.ToString("X") + " ";
                }
                tb1.Text += "\r\n\r\n" + String.Format("Pitch: {0:F2}\r\nRoll: {1:F2}", flightData.Pitch, flightData.Roll);
            }
            catch (MessagePackSerializationException exc)
            {
                Debug.WriteLine(exc.Message);
                if (sPortBytes.Count > 30) sPortBytes.Clear();
            }
        }
    }
}
