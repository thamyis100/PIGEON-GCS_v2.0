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
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            ArahkanTracker();
        }

        //tracker
        double lat_1 = -7.276594641754208
            , lon_1 = 112.79384655689269
            , alti1 = 0;
        //wahana
        double lat_2 = -7.276480235629137
            , lon_2 = 112.79392031765727
            , alti2 = 20;

        public void ArahkanTracker()
        {
            // radius bumi (Kilo Meter)
            int R = 6371;

            double lat1 = lat_1 * Math.PI / 180.0;
            double lon1 = lon_1 * Math.PI / 180.0;

            double lat2 = lat_2 * Math.PI / 180.0;
            double lon2 = lon_2 * Math.PI / 180.0;


            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;

            /* Jarak (Haversine) */
            double A = Math.Pow(Math.Sin(deltaLat / 2), 2)
                            + (Math.Cos(lat1) * Math.Cos(lat2)
                            * Math.Pow(Math.Sin(deltaLon / 2), 2));

            double B = 2 * Math.Atan2(Math.Sqrt(A), Math.Sqrt(1 - A));

            double jarakDarat = R * B;
            tb1.Text = $"Jarak Horizon = {jarakDarat} km\r\n";


            /* Arah Horizon (Bearing) */
            double y = Math.Sin(deltaLon) * Math.Cos(lat2);

            double x = Math.Cos(lat1) * Math.Sin(lat2)
                - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

            double bearing = Math.Atan2(y, x);
            tb1.Text += $"Bearing = {((bearing * 180.0 / Math.PI ) + 360.0) % 360.0}\r\n";


            /* Beda altitude (Meter) */
            double deltaTinggi = alti2 - alti1;

            /* Pitch Phytagoras */
            double ArahVerti = Math.Atan2(deltaTinggi, jarakDarat * 1000);
            tb1.Text += $"Pitch = {ArahVerti * 180.0 / Math.PI}\r\n";


            /* Jarak Langsung */
            double jarakLangsung = Math.Sqrt(jarakDarat * 1000 * (jarakDarat * 1000) + deltaTinggi * deltaTinggi);
            tb1.Text += $"Jarak Langsung = {jarakLangsung} m\r\n";


            /* Send data to Tracker */
            var win = (MainWindow)App.Current.MainWindow;

            // The Sent Data Goes : #,00,000 {#,Pitch,Yaw}
            string data = "#," + (ArahVerti * 180.0 / Math.PI).ToString("0") + ',' + (((bearing * 180.0 / Math.PI) + 360.0) % 360.0).ToString("0") + "\r\n";
            tb1.Text += data;

        }
    }
}
