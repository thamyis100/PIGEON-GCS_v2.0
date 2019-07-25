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
            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/bat-full.png")), new Int32Rect(0,0,400,396));
        }

        private void clickedWindow(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            Console.WriteLine("Window clicked");
        }

        //private void normal(object sender, MouseEventArgs e)
        //{
        //    Button the_btn = (Button)sender;
        //    Thickness back = new Thickness(5, 0, 0, 0);
        //    the_btn.Background = Brushes.Transparent;
        //    switch (the_btn.Name)
        //    {
        //        case "btn_flight":
        //            icon_flight.Margin = back;
        //            break;
        //        case "btn_map":
        //            icon_map.Margin = back;
        //            break;
        //        case "btn_stats":
        //            icon_stats.Margin = back;
        //            break;
        //        case "btn_track":
        //            icon_track.Margin = back;
        //            break;
        //    }
        //}

        private void closeApp(object sender, RoutedEventArgs e)
        {
            BlurEffect theblur= new BlurEffect();
            theblur.Radius = 10;
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

        private void hoverButton(object sender, MouseEventArgs e)
        {
            Button it = (Button)sender;
            it.Background = Brushes.DarkSlateGray;
        }

        private void dehoverButton(object sender, MouseEventArgs e)
        {
            SolidColorBrush color = new SolidColorBrush();
            Button it = (Button)sender;
            it.Background = color;
        }

        private void selectTab(object sender, MouseButtonEventArgs e)
        {
            Button the_btn = (Button)sender;
            switch (the_btn.Name)
            {
                case "btn_flight":
                    tabControl.SelectedIndex = 0;
                    break;
                case "btn_map":
                    tabControl.SelectedIndex = 1;
                    break;
                case "btn_stats":
                    tabControl.SelectedIndex = 2;
                    break;
                case "btn_track":
                    tabControl.SelectedIndex = 3;
                    break;
            }
        }

        private void selectTab(object sender, RoutedEventArgs e)
        {

        }
    }
}
