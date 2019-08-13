using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Pigeon_WPF_cs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            //contoh kapasitas baterai
            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/bat-full.png")), new Int32Rect(0, 0, 400, 396));
            setCurrentlyActive(tab_track, btn_track, track_Ctrl);
            setCurrentlyActive(tab_stats, btn_stats, stats_Ctrl);
            setCurrentlyActive(tab_map, btn_map, map_Ctrl);
            setCurrentlyActive(tab_flight, btn_flight, flight_Ctrl);
            MinimizeMap();

            SetBahasa(new CultureInfo("id-ID"));

            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.DataBind, delegate
            {
                digital_clock.Text = DateTime.Now.ToString("HH\\:mm\\:ss (G\\MTz) dddd,\ndd MMMM yyyy", CultureInfo.CurrentUICulture);
            },Dispatcher);
        }

        private void ubah_bahasa(object sender, RoutedEventArgs e)
        {
            bhs_indo.Background = bhs_inggris.Background = null;
            Button bahasa = (Button)sender;
            switch (bahasa.Name)
            {
                case "bhs_indo":
                    SetBahasa(new CultureInfo("id-ID"));
                    bhs_lbl.Content = "Language :";
                    bhs_indo.Background = Brushes.Lime;
                    break;
                case "bhs_inggris":
                    SetBahasa(new CultureInfo("en-EN"));
                    bhs_lbl.Content = "Bahasa :";
                    bhs_inggris.Background = Brushes.Lime;
                    break;
            }
        }
        private void SetBahasa(CultureInfo theLanguage) => Thread.CurrentThread.CurrentUICulture = theLanguage;

        private void injectStats(object sender, RoutedEventArgs e) => stats_Ctrl.InjectStopOnClick(sender, e);
        public void setConnStat(bool status)
        {
            if (status)
            {
                lbl_statusLine.Content = "ONLINE";
                lbl_statusLine.Foreground = Brushes.Green;
            }
            else
            {
                lbl_statusLine.Content = "OFFLINE";
                lbl_statusLine.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF969696"));
            }
        }
        private void clickedWindow(object sender, MouseButtonEventArgs e) => DragMove();
        private byte appCheck()
        {
            byte code = 0;
            if (flight_Ctrl.connected) code = 1;
            return code;
        }
        private void closeApp(object sender, RoutedEventArgs e)
        {
            BlurEffect theblur = new BlurEffect();
            theblur.Radius = 15;
            Effect = theblur;
            flight_Ctrl.hideAvionics();
            exitPop exitPop = new exitPop(appCheck());
            exitPop.Owner = this;
            exitPop.ShowDialog();
        }

        #region Waypoint control

        public void MinimizeMap(int width = 470, int height = 580)
        {
            map_Ctrl.judul_map.Visibility = Visibility.Hidden;
            mapGrid.Width = width;
            mapGrid.Height = height;
        }

        public void MaximizeMap()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Visible;
            mapGrid.Width = 1132;
            mapGrid.Height = 615;
        }

        #endregion

        #region statistik control

        #endregion

        #region flight view control

        #endregion

        #region Tab Control choosing
        String selectedBtn ="btn_flight";
        private void hoverButton(object sender, MouseEventArgs e)
        {
            Button it = (Button)sender;
            if(it.Name != selectedBtn) it.Background = Brushes.DarkSlateGray;
        }

        private void dehoverButton(object sender, MouseEventArgs e)
        {
            Button it = (Button)sender;
            if (it.Name != selectedBtn) it.Background = null;
        }
        
        private void selectTab(object sender, RoutedEventArgs e)
        {
            Button the_btn = (Button)sender;
            switch (the_btn.Name)
            {
                case "btn_flight":
                    setCurrentlyActive(tab_flight, btn_flight, flight_Ctrl);
                    MinimizeMap();
                    break;
                case "btn_map":
                    setCurrentlyActive(tab_map, btn_map, map_Ctrl);
                    MaximizeMap();
                    break;
                case "btn_stats":
                    setCurrentlyActive(tab_stats, btn_stats, stats_Ctrl);
                    break;
                case "btn_track":
                    setCurrentlyActive(tab_track, btn_track, track_Ctrl);
                    MinimizeMap(585, 615);
                    break;
            }
        }

        private void setCurrentlyActive(Rectangle theBox, Button theBtn, UserControl theCtrl)
        {
            //sembunyikan semua box
            tab_flight.Visibility = tab_map.Visibility = tab_stats.Visibility = tab_track.Visibility = Visibility.Hidden;

            //transparankan semua tombol pilihan
            btn_flight.Background = btn_map.Background = btn_stats.Background = btn_track.Background = null;

            //sembunyikan semua control kecuali waypoint
            flight_Ctrl.Visibility = stats_Ctrl.Visibility = track_Ctrl.Visibility = Visibility.Hidden;

            //tampilkan control yang dipilih
            theCtrl.Visibility = Visibility.Visible;

            //warnai tombol yang dipilih
            theBtn.Background = Brushes.Teal;
            selectedBtn = theBtn.Name;

            //tampilkan box yang dipilih
            theBox.Visibility = Visibility.Visible;
        }

        #endregion

    }
}
