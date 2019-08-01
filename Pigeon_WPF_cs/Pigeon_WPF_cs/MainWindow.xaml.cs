using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/bat-full.png")), new Int32Rect(0, 0, 400, 396));
            btn_flight.Background = Brushes.Teal;
            MinimizeMap();
        }

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

        public void setCurrentPos(double lat, double longt, float yaw) => map_Ctrl.setCurrentPos(lat, longt, yaw);

        public void MinimizeMap()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Hidden;
            mapGrid.Width = 470;
            mapGrid.Height = 580;
        }

        public void MaximizeMap()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Visible;
            mapGrid.Width = 1132;
            mapGrid.Height = 626;
        }

        #endregion

        #region statistik control
        public void addStatistik(float yaw, float pitch, float roll)
        {
            stats_Ctrl.addToStatistik(yaw, pitch, roll);
        }
        #endregion

        #region flight view control
        public void showAvionics() => flight_Ctrl.showAvionics();

        public void stopTheCam() => flight_Ctrl.stopControl();
        #endregion

        #region Tab Control choosing
        String selectedBtn ="btn_flight";
        Brush savedBG = Brushes.Teal;
        SolidColorBrush transparentBG;
        private void hoverButton(object sender, MouseEventArgs e)
        {
            Button it = (Button)sender;
            if(it.Name != selectedBtn) it.Background = Brushes.DarkSlateGray;
        }

        private void dehoverButton(object sender, MouseEventArgs e)
        {
            transparentBG = new SolidColorBrush();
            Button it = (Button)sender;
            if (it.Name != selectedBtn) it.Background = transparentBG;
        }
        
        private void selectTab(object sender, RoutedEventArgs e)
        {
            Button the_btn = (Button)sender;
            switch (the_btn.Name)
            {
                case "btn_flight":
                    tabControl.SelectedIndex = 0;
                    setVisible(tab_flight);
                    setSelected(btn_flight);
                    MinimizeMap();
                    break;
                case "btn_map":
                    tabControl.SelectedIndex = 1;
                    setVisible(tab_map);
                    setSelected(btn_map);
                    MaximizeMap();
                    break;
                case "btn_stats":
                    tabControl.SelectedIndex = 2;
                    setVisible(tab_stats);
                    setSelected(btn_stats);
                    break;
                case "btn_track":
                    tabControl.SelectedIndex = 3;
                    setVisible(tab_track);
                    setSelected(btn_track);
                    break;
            }
        }

        private void setVisible(Rectangle theBox)
        {
            //sembunyikan dahulu semua box
            tab_flight.Visibility = Visibility.Hidden;
            tab_map.Visibility = Visibility.Hidden;
            tab_stats.Visibility = Visibility.Hidden;
            tab_track.Visibility = Visibility.Hidden;
            
            //sekarang tampilkan box yang dipilih
            theBox.Visibility = Visibility.Visible;
        }

        private void setSelected(Button theBtn)
        {
            transparentBG = new SolidColorBrush();
            btn_flight.Background = transparentBG;
            btn_map.Background = transparentBG;
            btn_stats.Background = transparentBG;
            btn_track.Background = transparentBG;

            theBtn.Background = Brushes.Teal;
            selectedBtn = theBtn.Name;
        }
        #endregion
    }
}
