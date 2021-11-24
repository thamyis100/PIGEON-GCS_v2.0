using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

                if (/*IsTracking && App.Wahana.GPS.IsValid &&*/ App.Tracker.GPS.IsValid)
                {
                    TrackTimer.Elapsed += ArahkanTracker;
                    TrackTimer.Interval = 500;
                    TrackTimer.Start();
                }

                lbl_start_stop.Content = "Stop Tracking";
                ico_start_stop.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-stop-50.png"));
            }
            else
            {
                IsTracking = false;

                TrackTimer.Stop();

                lbl_start_stop.Content = "Start Tracking";
                ico_start_stop.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/icons/icons8-play-50.png"));
            }
        }

        #endregion


        #region Functions

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

        #endregion

        
        #region Update UI

        public void UpdateUITracker()
        {
            var win = (MainWindow)App.Current.MainWindow;

            //tb_received.Text =
            //            App.Wahana.FlightMode.ToString("X2") + " | "

            //            + App.Wahana.Battery + " | "

            //            + App.Wahana.Signal + " | "

            //            + App.Wahana.IMU.Yaw + " | "
            //            + App.Wahana.IMU.Pitch + " | "
            //            + App.Wahana.IMU.Roll + " | "

            //            + App.Wahana.Altitude + " | "

            //            + App.Wahana.Speed + " | "

            //            + App.Wahana.GPS.Latitude + " | "
            //            + App.Wahana.GPS.Longitude + " | ";

            if (win.flight_Ctrl.IsFirstDataTracker)
            {
                win.flight_Ctrl.IsFirstDataTracker = false;

                win.SetConnStat(TipeDevice.TRACKER, true);

                Integration(true);
            }

            if (win.flight_Ctrl.IsConnected)
                btn_tracking.IsEnabled = true;

            tb_bearing.Text = App.Tracker.IMU.Yaw.ToString("0.00", CultureInfo.InvariantCulture) + "°";
            tb_pitch.Text = App.Tracker.IMU.Pitch.ToString("0.00", CultureInfo.InvariantCulture) + "°";

            if (App.Tracker.GPS.IsValid)
            {
                win.map_Ctrl.UpdatePosTracker();

                tb_lat_tracker.Text = (App.Tracker.GPS.Latitude / 10000000.0).ToString("0.00000000", CultureInfo.InvariantCulture);
                tb_longt_tracker.Text = (App.Tracker.GPS.Longitude / 10000000.0).ToString("0.00000000", CultureInfo.InvariantCulture);
            }

            tb_tinggi_tracker.Text = App.Tracker.Altitude.ToString("0.00", CultureInfo.InvariantCulture) + " m";
        }

        #endregion


        #region Arahkan Tracker

        System.Timers.Timer TrackTimer = new System.Timers.Timer();

        public void ArahkanTracker(object sender, System.Timers.ElapsedEventArgs e)
        {
            // radius bumi (Kilo Meter)
            int R = 6371;

            double lat1 = App.Tracker.GPS.Latitude / 10000000.0 * Math.PI / 180.0;
            double lon1 = App.Tracker.GPS.Longitude / 10000000.0 * Math.PI / 180.0;

            double lat2 = App.Wahana.GPS.Latitude / 10000000.0 * Math.PI / 180.0;
            double lon2 = App.Wahana.GPS.Longitude / 10000000.0 * Math.PI / 180.0;


            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;

            /* Jarak (Haversine) */
            double A = Math.Pow(Math.Sin(deltaLat / 2), 2)
                            + (Math.Cos(lat1) * Math.Cos(lat2)
                            * Math.Pow(Math.Sin(deltaLon / 2), 2));

            double B = 2 * Math.Atan2(Math.Sqrt(A), Math.Sqrt(1 - A));

            double jarakDarat = R * B; // Kilo meter


            /* Arah Horizon (Bearing) */
            double y = Math.Sin(deltaLon) * Math.Cos(lat2);

            double x = Math.Cos(lat1) * Math.Sin(lat2)
                - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

            double bearing = Math.Atan2(y, x); // radians -pi ... pi


            /* Beda altitude (Meter) */
            double deltaTinggi = App.Wahana.AltitudeFloat - App.Tracker.Altitude;

            /* Pitch Phytagoras */
            double ArahVerti = Math.Atan2(deltaTinggi, jarakDarat * 1000);


            /* Jarak Langsung (Meter)*/
            double jarakLangsung = Math.Sqrt(jarakDarat * jarakDarat + deltaTinggi * deltaTinggi);


            // The Sent Data Goes : #,00,000 {#,Pitch,Yaw}
            string data = "#," + (ArahVerti * 180.0 / Math.PI).ToString("0") + ',' + (((bearing * 180.0 / Math.PI) + 360.0) % 360.0).ToString("0") + "\r\n";
            //Debug.WriteLine("TRACK ARAH : " + data);

            /* Send data to connection */
            Dispatcher.Invoke(() => UpdateTracking(jarakDarat, jarakLangsung, data));
        }

        private void UpdateTracking(double jarakDarat, double jarakLangsung, string data)
        {
            ((MainWindow)App.Current.MainWindow).flight_Ctrl.SendBytesToConnection(Encoding.ASCII.GetBytes(data));

            tb_received.Text = data;

            if (jarakDarat <= 1.0f)
                tb_jarak_horizon.Text = (jarakDarat * 1000).ToString("0.00", CultureInfo.InvariantCulture) + " m";
            else
                tb_jarak_horizon.Text = jarakDarat.ToString("0.00", CultureInfo.InvariantCulture) + " km";

            if (jarakLangsung / 1000 >= 1.0f)
                tb_jarak_lsg.Text = (jarakLangsung / 1000).ToString("0.00", CultureInfo.InvariantCulture) + " km";
            else
                tb_jarak_lsg.Text = jarakLangsung.ToString("0.00", CultureInfo.InvariantCulture) + " m";
        }

        #endregion
    }
}
