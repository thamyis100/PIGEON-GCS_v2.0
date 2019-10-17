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
        //private byte exitCode;
        public exitPop(byte theCode = 0)
        {
            InitializeComponent();
            if (theCode == 1)
            {
                delayExit("Keluar", true);
                tb_info.Text = "EFALCON MASIH TERHUBUNG!\nYakin ingin keluar?";
            }
            if (theCode == 2)
            {
                tb_info.Text = "PASTIKAN WAHANA MENDARAT DAN TELAH DIMATIKAN SEBELUM DISCONNECT!";
                delayExit("Disconnect", false);
            }
        }

        private async void delayExit(string konten, bool isexit)
        {
            btn_lanjut.IsEnabled = false;
            tb_info.Foreground = Brushes.DarkRed;

            byte i = 0;
            if (isexit) i = 5;
            else i = 3;
            while(i > 0)
            {
                btn_lanjut.Content = konten + "(" + i + ")";
                await Task.Delay(1000);
                i--;
            }
            btn_lanjut.Content = konten;
            btn_lanjut.IsEnabled = true;
        }

        private void batal(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void keluar(object sender, RoutedEventArgs e)
        {
            DialogResult = true; Close();
        }
        //{
        //    win = (MainWindow)GetWindow(Owner);
        //    win.flight_Ctrl.stopControl();
        //    Application.Current.Shutdown();
        //}
    }
}
