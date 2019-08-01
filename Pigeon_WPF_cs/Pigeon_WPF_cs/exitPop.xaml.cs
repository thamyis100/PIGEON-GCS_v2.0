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
        MainWindow win;
        private byte exitCode;
        public exitPop(byte theCode = 0)
        {
            InitializeComponent();
            if (theCode == 1)
            {
                delayExit();
                tb_info.Text = "EFALCON MASIH TERHUBUNG! Yakin ingin keluar?";
            }
        }

        private async void delayExit()
        {
            btn_lanjut.IsEnabled = false;
            for (var i = 5; i > 0; i--)
            {
                btn_lanjut.Content = "Keluar(" + i + ")";
                await Task.Delay(1000);
            }
            btn_lanjut.Content = "Keluar";
            btn_lanjut.IsEnabled = true;
        }

        private void batal(object sender, RoutedEventArgs e)
        {
            win = (MainWindow)Window.GetWindow(Owner);
            Owner.Effect = null;
            win.showAvionics();
            Close();
        }

        private void keluar(object sender, RoutedEventArgs e)
        {
            win = (MainWindow)Window.GetWindow(Owner);
            win.stopTheCam();
            Application.Current.Shutdown();
        }
    }
}
