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
        bool connected = false;
        public MainWindow()
        {
            InitializeComponent();
            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/bat-full.png")), new Int32Rect(0, 0, 400, 396));
            btn_flight.Background = Brushes.Teal;

        }

        public void stopTheCam()
        {
            flight_Ctrl.stopControl();
        }

        private void clickedWindow(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            //Console.WriteLine("Window clicked");
        }

        private void closeApp(object sender, RoutedEventArgs e)
        {
            BlurEffect theblur= new BlurEffect();
            theblur.Radius = 15;
            Effect = theblur;
            exitPop exitPop = new exitPop(appCheck());
            exitPop.Owner = this;
            exitPop.ShowDialog();
        }
        private byte appCheck()
        {
            byte code = 0;
            if (connected) code = 1;
            return code;
        }

        String selectedBtn="btn_flight";
        Brush savedBG;
        SolidColorBrush transparentBG;
        private void hoverButton(object sender, MouseEventArgs e)
        {
            Button it = (Button)sender;
            if(it.Name == selectedBtn) savedBG = it.Background;
            else it.Background = Brushes.DarkSlateGray;
        }

        private void dehoverButton(object sender, MouseEventArgs e)
        {
            transparentBG = new SolidColorBrush();
            Button it = (Button)sender;
            if (it.Name == selectedBtn) it.Background = savedBG;
            else it.Background = transparentBG;
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
                    break;
                case "btn_map":
                    tabControl.SelectedIndex = 1;
                    setVisible(tab_map);
                    setSelected(btn_map);
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
    }
}
