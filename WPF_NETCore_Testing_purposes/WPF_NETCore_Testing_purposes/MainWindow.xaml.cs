using MavLinkNet;
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
    public class UasData
    {
        public byte SystemID { get; set; }

        public UasSysStatus SystemStatus { get; set; }
        public UasAttitude Attitude { get; set; }
        public UasGlobalPositionInt Navigation { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public string konten { get; set; } = "Contents here";

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            UasData currUAV = new()
            {
                SystemID = 67,
                SystemStatus = new()
                {
                    BatteryRemaining = 80,
                    DropRateComm = 15,
                    Load = 10
                },
                Attitude = new() { Yaw = 25.0f, Pitch = 25.0f, Roll = 25.0f },
                Navigation = new()
                {
                    Alt = 250,
                    Hdg = (ushort)(16.25f * 182),
                    Lat = int.Parse("-7.410753".Replace(".", "")),
                    Lon = int.Parse("112.7042528".Replace(".", ""))
                }
            };

            byte[][] messages = {
                MavLinkPacket.GetBytesForMessage(currUAV.SystemStatus, currUAV.SystemID, (byte)MavComponent.MavCompIdAll, 0, 0),
                MavLinkPacket.GetBytesForMessage(currUAV.Attitude, currUAV.SystemID, (byte)MavComponent.MavCompIdAll, 0, 0),
                MavLinkPacket.GetBytesForMessage(currUAV.Navigation, currUAV.SystemID, (byte)MavComponent.MavCompIdAll, 0, 0)
            };

            konten = "";
            foreach (var message in messages)
            {
                konten += "Message : [ ";
                Debug.Write("Message : [ ");
                foreach (var item in message)
                {
                    Debug.Write(item.ToString("X2") + ' ');
                    konten += item.ToString("X2") + ' ';
                }
                Debug.WriteLine("]");
                konten += "]\n";
            }
        }
    }
}
