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
    /// Interaction logic for MessagePop.xaml
    /// </summary>
    public partial class MessagePop : Window
    {
        public MessagePop(Window owner, string message, bool isLanjut = true)
        {
            InitializeComponent();

            Owner = owner;

            Width = Owner.Width / 5;
            Height = Owner.Height / 5;

            tb_info.Text = message;
            btn_lanjut.Visibility = isLanjut ? Visibility.Visible : Visibility.Hidden;
        }

        public MessagePop(Window owner, string message, int delayMs)
        {
            InitializeComponent();

            Owner = owner;

            Width = Owner.Width / 5;
            Height = Owner.Height / 5;

            tb_info.Text = message;
            AddDelay(delayMs);
        }

        private async void AddDelay(int delayMs)
        {
            btn_lanjut.IsEnabled = false;

            for (int i = delayMs / 1000; i > 0; i--)
            {
                btn_lanjut.Content = "Lanjut (" + i + ")";
                await Task.Delay(1000);
            }
            await Task.Delay(delayMs % 1000);

            btn_lanjut.Content = "Lanjut";
            btn_lanjut.IsEnabled = true;
        }

        private void Batal(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Lanjut(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
