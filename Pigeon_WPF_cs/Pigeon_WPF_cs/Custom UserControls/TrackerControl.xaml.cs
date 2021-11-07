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
using System.Threading;
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
using Pigeon_WPF_cs.Enums;

namespace Pigeon_WPF_cs.Custom_UserControls
{
    
    /// <summary>
    /// Interaction logic for TrackerControl.xaml
    /// </summary>
    public partial class TrackerControl : UserControl
    {
        public TrackerControl()
        {
            DataContext = this;

            InitializeComponent();
        }

        #region BUTTONS

        public bool IsTracking { get; set; } = false;

        private void ToggleTracking(object sender, RoutedEventArgs e)
        {
            if (!IsTracking)
            {
                IsTracking = true;
                lbl_start_stop.Content = "Stop Tracking";
                ico_start_stop.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-stop-50.png"));
            }
            else
            {
                IsTracking = false;
                lbl_start_stop.Content = "Start Tracking";
                ico_start_stop.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-play-50.png"));
            }
        }

        #endregion

        bool IsIntegrated { get; set; } = false;

        /// <summary>
        /// Menyatakan penggunaan telemetri dengan tracker secara integrasi. Menutup koneksi tracker jika diset false
        /// </summary>
        /// <param name="status"></param>
        public void Integration(bool status)
        {
            if (status)
            {
                IsIntegrated = true;
                //conn_panel.IsEnabled = false;
                conn_panel_label.Visibility = Visibility.Visible;
            }
            else
            {
                IsIntegrated = false;
                //conn_panel.IsEnabled = true;
                conn_panel_label.Visibility = Visibility.Collapsed;
            }
        }

        #region Data Parsing

        /// <summary>
        /// Parse the incoming data hopefully asynchrounously
        /// </summary>
        /// <param name="RxBuf">Received data buffer</param>
        /// <param name="BufLen">Buffer's length</param>
        public void ParseData(byte[] RxBuf, int BufLen)
        {
            #region Urutan data Triton
            /* (angka terakhir itu index)
            * 
            * [Header]      = HEADER_BUF uint8          (1 byte) 0
            * 
            * [Yaw]		    = derajat float32		    (4 byte) 1
            * [Pitch]	    = derajat float32		    (4 byte) 5
            *
            * [Altitude]    = milimeter int32		    (4 byte) 9
            * 
            * [Lat]		    = decimal degrees float32	(4 byte) 13
            * [Lon]         = decimal degrees float32	(4 byte) 17
            */
            #endregion

            App.Tracker.Tipe = TipeDevice.TRACKER;

            App.Tracker.IMU.Yaw = BitConverter.ToSingle(RxBuf, 1);
            App.Tracker.IMU.Pitch = BitConverter.ToSingle(RxBuf, 5);

            App.Tracker.Altitude = BitConverter.ToInt32(RxBuf, 9);

            App.Tracker.GPS.Latitude = BitConverter.ToSingle(RxBuf, 13);
            App.Tracker.GPS.Longitude = BitConverter.ToSingle(RxBuf, 17);

            Dispatcher.BeginInvoke(new ThreadStart(delegate { UpdateUITracker(); }));
        }

        #endregion

        #region Update UI

        public void UpdateUITracker()
        {
            var win = (MainWindow)App.Current.MainWindow;

            tb_received.Text =
                        App.Wahana.FlightMode.ToString("X2") + " | "

                        + App.Wahana.Battery + " | "

                        + App.Wahana.Signal + " | "

                        + App.Wahana.IMU.Yaw + " | "
                        + App.Wahana.IMU.Pitch + " | "
                        + App.Wahana.IMU.Roll + " | "

                        + App.Wahana.Altitude + " | "

                        + App.Wahana.Speed + " | "

                        + App.Wahana.GPS.Latitude + " | "
                        + App.Wahana.GPS.Longitude + " | ";

            if (win.flight_Ctrl.IsFirstDataTracker)
            {
                win.flight_Ctrl.IsFirstDataTracker = false;

                Integration(true);

                win.map_Ctrl.StartPosTracker();
            }

            win.map_Ctrl.UpdatePosTracker();

            if (win.flight_Ctrl.IsConnected)
                btn_tracking.IsEnabled = true;

            tb_bearing.Text = App.Tracker.IMU.Yaw.ToString("0.00", CultureInfo.InvariantCulture) + "°";
            tb_pitch.Text = App.Tracker.IMU.Pitch.ToString("0.00", CultureInfo.InvariantCulture) + "°";

            tb_lat_tracker.Text = App.Tracker.GPS.Latitude.ToString("0.00000000", CultureInfo.InvariantCulture);
            tb_longt_tracker.Text = App.Tracker.GPS.Longitude.ToString("0.00000000", CultureInfo.InvariantCulture);

            tb_tinggi_tracker.Text = App.Tracker.Altitude.ToString("0.00", CultureInfo.InvariantCulture) + " m";
        }

        #endregion


        #region Arahkan Tracker

        public void ArahkanTracker()
        {
            int R = 6371000;

            double lat1 = App.Tracker.GPS.Latitude * Math.PI / 180.0;
            double lon1 = App.Tracker.GPS.Longitude * Math.PI / 180.0;

            double lat2 = App.Wahana.GPS.Latitude * Math.PI / 180.0;
            double lon2 = App.Wahana.GPS.Longitude * Math.PI / 180.0;

            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;

            /* Haversine */
            double sisiA = Math.Pow(Math.Sin(deltaLat / 2), 2)
                            + ((Math.Cos(lat1) * Math.Cos(lat2))
                            * Math.Pow(Math.Sin(deltaLon / 2), 2));

            double sisiB = 2 * Math.Atan2(Math.Sqrt(sisiA), Math.Sqrt(1 - sisiA));

            double jarakDarat = R * sisiB;

            /* Phytagoras */
            double deltaTinggi = App.Wahana.Altitude - App.Tracker.Altitude;

            double jarakLangsung = Math.Sqrt((deltaLon * deltaLon) + (deltaLat * deltaLat));

            /* Arah Horizon (Bearing) */
            double y = Math.Sin(deltaLon) * Math.Cos(lat2);

            double x = (Math.Cos(lat1) * Math.Sin(lat2))
                - (Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon));

            double arahHorizon = ((Math.Atan2(y, x) * 180.0 / Math.PI) + 360.0) % 360.0;

            /* Phytagoras */
            double ArahVerti = Math.Atan2(deltaTinggi, jarakLangsung) * 180.0 / Math.PI;

            /* Send data to Tracker */

            var win = (MainWindow)App.Current.MainWindow;

            // The Sent Data Goes :
            // [0] = '#' command start identifier   (0)
            // [1] = Pan (000.00) 4 byte            (1-4)
            // [2] = Tilt (00.00) 4 byte            (5-8)
            // \n endline
            string data = "i," + ArahVerti.ToString("0.00") + ',' + arahHorizon.ToString("0.00");

            win.flight_Ctrl.SendToConnection(Encoding.ASCII.GetBytes(data));

            if (jarakDarat / 1000 >= 1.0f)
                tb_jarak_horizon.Text = (jarakDarat / 1000).ToString("0.00", CultureInfo.InvariantCulture) + " km";
            else
                tb_jarak_horizon.Text = jarakDarat.ToString("0.00", CultureInfo.InvariantCulture) + " m";

            if (jarakLangsung / 1000 >= 1.0f)
                tb_jarak_lsg.Text = (jarakLangsung / 1000).ToString("0.00", CultureInfo.InvariantCulture) + " km";
            else
                tb_jarak_lsg.Text = jarakLangsung.ToString("0.00", CultureInfo.InvariantCulture) + " m";
        }

        #endregion
    }
}
