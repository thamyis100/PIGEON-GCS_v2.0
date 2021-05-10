using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Threading;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Flight_Data_emulator
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
            Debug.WriteLine(propertyName + " has CHANGED");
        }

        #region Flight Data

        private float heading_val = 0.0f;
        public float Heading
        {
            get { return heading_val; }
            set
            {
                if (value != heading_val)
                {
                    heading_val = value;
                    //OnPropertyChanged("Heading");
                }
            }
        }

        #endregion

        //MavLinkSerialPortTransport it = new MavLinkSerialPortTransport()
        //{
        //    SerialPortName = "COM3",
        //    BaudRate = 115200,
        //    HeartBeatUpdateRateMs = 1500
        //};

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

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            PrepareWebSocketClient();

            //it.OnPacketReceived += NewMavlinkPacket;
            //it.Initialize();
            //it.BeginHeartBeatLoop();

            //RotateCoordinateAsync();
        }

        WebSocket websocket_client;
        WebSocketServer websocket_server;
        Cookie tes;
        private void PrepareWebSocketClient()
        {
            websocket_client = new WebSocket("ws://localhost:27772");
            //websocket_client = new WebSocket("wss://traccar.tekat.co/api/socket");

            websocket_client.OnError += Websocket_client_OnError;
            websocket_client.OnClose += Websocket_client_OnClose;
            websocket_client.OnMessage += Websocket_client_OnMessage;
            websocket_client.OnOpen += Websocket_client_OnOpen;

            if (websocket_client.IsSecure) websocket_client.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;
            //websocket_client.SetCredentials("testcreate", "testcreate", true);

            //tes = new Cookie("JSESSIONID", "node01gc6ctgsyh7s41fspc1hkku4do4998.node0");
            //websocket_client.SetCookie(tes);

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

        delegate void UpdateTBTextDelegate(string text);
        private void UpdateTB1(string text)
        {
            tb2.Text += text;
        }

        private byte[] GetLatDDM(float lat_dd)
        {
            sbyte lat_int8 = sbyte.Parse(lat_dd.ToString("#"));
            float lat_float = float.Parse(lat_dd.ToString(".######"));
            byte[] val = new byte[5];
            val[0] = (byte)lat_int8;
            byte i = 1;
            foreach (byte item in BitConverter.GetBytes(lat_float)) val[i++] = item;
            return val;
        }

        private async void RotateCoordinateAsync()
        {
            while (true)
            {
                
            }
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                websocket_client.Send(tb1.Text);
                tb1.Text = "";
            }
        }
    }
}
