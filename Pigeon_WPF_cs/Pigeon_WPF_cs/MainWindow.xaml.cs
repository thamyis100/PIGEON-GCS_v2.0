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
            //rect_flight.Visibility = Visibility.Hidden;
        }

        private void clickedWindow(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            Console.WriteLine("Window clicked");
        }

        private void popRight(object sender, MouseEventArgs e)
        {
            Button the_btn = (Button)sender;
            Thickness pop = new Thickness(25,0,0,0);
            switch (the_btn.Name)
            {
                case "btn_flight":
                    icon_flight.Margin = pop;
                    break;
                case "btn_map":
                    icon_map.Margin = pop;
                    break;
                case "btn_stats":
                    icon_stats.Margin = pop;
                    break;
                case "btn_track":
                    icon_track.Margin = pop;
                    break;
            }
        }

        private void normal(object sender, MouseEventArgs e)
        {
            ContentControl the_btn = (Button)sender;
            Thickness back = new Thickness(25, 0, 0, 0);
            
        }
    }
}
