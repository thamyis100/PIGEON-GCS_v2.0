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
using WebSocketSharp.NetCore;
using WebSocketSharp.NetCore.Net;
using WebSocketSharp.NetCore.Server;

namespace WPF_NETCore_Testing_purposes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        delegate void UpdateTBTextDelegate(string text);
        private delegate void UpdateUiTextDelegate();

        WebSocket websocket_client;
        Cookie tes;

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

            Loaded += MainWindow_Loaded;

            return;

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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PrepareWebSocketClient();
        }

        private void PrepareWebSocketClient()
        {
            websocket_client = new WebSocket("ws://localhost:27772?waw=waw1234&test=create");
            //websocket_client = new WebSocket("wss://echo.websocket.org");
            //websocket_client = new WebSocket("wss://traccar.tekat.co/api/socket");

            websocket_client.OnError += Websocket_client_OnError;
            websocket_client.OnClose += Websocket_client_OnClose;
            websocket_client.OnMessage += Websocket_client_OnMessage;
            websocket_client.OnOpen += Websocket_client_OnOpen;

            if(websocket_client.IsSecure) websocket_client.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;
            websocket_client.SetCredentials("device", "device", true);

            //websocket_client.set

            //tes = new Cookie("JSESSIONID", "node01gc6ctgsyh7s41fspc1hkku4do4998.node0");
            //websocket_client.SetCookie(tes);

            websocket_client.SetCookie(new Cookie("testosteron", "testosteron4"));
            websocket_client.SetCookie(new Cookie("testosteron1", "testosteron5"));
            websocket_client.SetCookie(new Cookie("testosteron2", "testosteron6"));

            Debug.WriteLine("Try connecting to " + websocket_client.Url.ToString());
            websocket_client.Connect();
        }

        private void Websocket_client_OnOpen(object sender, EventArgs e)
        {
            Debug.WriteLine("[CLIENT][CONNECTED]");
        }

        private void Websocket_client_OnMessage(object sender, MessageEventArgs e)
        {
            Debug.WriteLine("[CLIENT][NEW DATA]: ");
            Debug.WriteLine(e.Data);
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateTBTextDelegate(UpdateTB1), "[CLIENT][NEW DATA]: " + e.Data);

            //PrintTB1("[CLIENT][NEW DATA]: " + e.Data);
        }

        private void Websocket_client_OnClose(object sender, CloseEventArgs e)
        {
            Debug.WriteLine("[CLIENT][CLOSED]: ");
            Debug.WriteLine(e.Reason + " (" + e.Code.ToString() + ')');
        }

        private void Websocket_client_OnError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine("[CLIENT][ERROR]: ");
            Debug.WriteLine(e.Message);
        }

        private void UpdateTB1(string text)
        {
            tb1.Text += text;
        }

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

        private void send_tb_keypressed(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                websocket_client.Send(send_tb.Text);
                send_tb.Text = "";
            }
        }
    }
}
