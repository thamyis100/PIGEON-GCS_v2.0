using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Pigeon_Hub_Server
{
    [MessagePackObject]
    public class StationRegistrationMessage
    {

    }

    public class FlightServer : WebSocketBehavior
    {
        class Client
        {
            string ClientID, SessionID;
            public Client(string ClientID, string SessionID)
            {
                this.ClientID = ClientID;
                this.SessionID = SessionID;
            }
        }

        class Channel
        {
            Guid ChannelID;
            Client UAV;
            List<Client> Stations;

            public Channel(string AuthID) => ChannelID = new Guid(AuthID);

            public void SetUAV(string uavid, string sessionid) => UAV = new Client(uavid, sessionid);

            public void AddClient(string clienteid, string sessionid) => Stations.Add(new Client(clienteid, sessionid));
        }

        List<string> clients;

        protected override void OnMessage(MessageEventArgs e)
        {
            //// Station ask for channel and sent device ID
            //if (e.RawData[0] == 0x00) ;

            Console.WriteLine("[SERVER:2772][NEW DATA]: " + e.Data);
            Console.WriteLine("ID: " + ID);

            if(e.IsText)
            foreach (var item in Sessions.IDs)
            {
                if (item == ID) continue;
                Sessions.SendTo(e.Data, item);
            }
            else
            foreach (var item in Sessions.IDs)
            {
                if (item == ID) continue;
                Sessions.SendTo(e.RawData, item);
            }
        }

        protected override void OnOpen()
        {
            Console.WriteLine("[SERVER:2772][NEW CONN]");
            Console.WriteLine("New session : " + Sessions.IDs.Last());

            //SendAsync("You are in Public Hub, everyone can hear your data, stay caution!", null);
            foreach (var item in Context.CookieCollection)
            {
                Console.WriteLine(item);
            }
            foreach (var item in Context.QueryString.AllKeys)
            {
                Console.WriteLine(item);
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("[SERVER:2772][CLOSED CONN]: (" + e.Code + ") " + e.Reason);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine("[SERVER:2772][ERROR]: " + e.Message + " (" + e.Exception.Message + ')');
        }
    }

    class Program
    {
        static WebSocketServer websocket_server;
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.WriteLine("[========== PIGEON Hub WebSocket Server ==========]");
            Console.WriteLine("[INIT] Preparing WebSocket Server");
            //websocket_server = new WebSocketServer(27772, true);
            websocket_server = new WebSocketServer(27772);
            websocket_server.AddWebSocketService<FlightServer>("/");

            //X509Certificate2 it = X509Certificate2.CreateFromPemFile("/home/tekat/ssl.everything");
            //websocket_server.SslConfiguration.ServerCertificate = it;

            //websocket_server.AuthenticationSchemes = AuthenticationSchemes.Basic;

            Console.WriteLine("[INIT] Starting WebSocket Server");
            websocket_server.Start();

            Console.WriteLine("[OP] Operational");
            _quitEvent.WaitOne();
            Console.WriteLine("[EXIT] App Exited");
        }
    }
}
