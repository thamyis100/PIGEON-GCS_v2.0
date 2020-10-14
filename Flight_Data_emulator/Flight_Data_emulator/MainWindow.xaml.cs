using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace Flight_Data_emulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            tb_yaw.Text += "°";
            Debug.WriteLine(propertyName + " has CHANGED : " + tb_yaw.Text);
        }

        #region Flight Data

        private float _heading_val = 92.2f;
        public float heading_val
        {
            get { return _heading_val; }
            set
            {
                if (value != _heading_val)
                {
                    _heading_val = value;
                    //OnPropertyChanged("Heading");
                }
            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            heading_val = 120.120f;
        }

    }
}
