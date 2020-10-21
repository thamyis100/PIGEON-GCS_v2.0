using MavLinkNet;
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
            Debug.WriteLine(propertyName + " has CHANGED");
        }

        #region Flight Data

        private float heading_val = 0.0f;
        public float Heading
        {
            get { return heading_val; }
            set
            {
                if (value != heading_val)
                {
                    heading_val = value;
                    //OnPropertyChanged("Heading");
                }
            }
        }

        #endregion

        MavLinkSerialPortTransport it = new MavLinkSerialPortTransport()
        {
            SerialPortName = "COM3",
            BaudRate = 115200,
            HeartBeatUpdateRateMs = 1500
        };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            it.OnPacketReceived += NewMavlinkPacket;
            it.Initialize();
            it.BeginHeartBeatLoop();

            //HeadingAsync();
        }

        private void NewMavlinkPacket(object sender, MavLinkPacket packet)
        {
            Debug.WriteLine("\nReceived :");
            //Debug.WriteLine(packet.)
        }

        private async void RotateCoordinateAsync()
        {
            while (true)
            {
                for (; ; )
                {
                    Heading += 5.0f;
                    await Task.Delay(100);
                    if (Heading > 180.0f) break;
                }
                for (; ; )
                {
                    Heading -= 5.0f;
                    await Task.Delay(100);
                    if (Heading < -180.0f) break;
                }
            }
        }
    }
}
