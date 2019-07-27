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
using System.Windows.Shapes;
using Pigeon_WPF_cs.Custom_UserControls;

namespace Pigeon_WPF_cs
{
    /// <summary>
    /// Interaction logic for exitPop.xaml
    /// </summary>
    public partial class exitPop : Window
    {
        private byte exitCode;
        public exitPop(byte theCode = 0)
        {
            InitializeComponent();
            exitCode = theCode;
        }

        private void batal(object sender, RoutedEventArgs e)
        {
            Owner.Effect = null;
            Close();
        }

        private void keluar(object sender, RoutedEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(Owner);
            win.stopTheCam();
            
            Application.Current.Shutdown();
        }
    }
}
