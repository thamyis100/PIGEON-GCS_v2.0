using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Pigeon_WPF_cs.Data_Classes;

namespace Pigeon_WPF_cs
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Flight Data untuk wahana saat ini
        /// </summary>
        public static FlightData CurrWahana;

        /// <summary>
        /// User Document path to save dataw
        /// </summary>
        public static string DocsPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Pigeon GCS/";

        public static string EmbeddedResx = "pack://application:,,,/PIGEON GCS;component/Resources/";

        public App()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath));

            InitializeComponent();

            Color src_color = ((SolidColorBrush)SystemParameters.WindowGlassBrush).Color;
            var new_brush = new SolidColorBrush(Color.FromArgb(src_color.A, (byte)(src_color.R * 0.5), (byte)(src_color.G * 0.5), (byte)(src_color.B * 0.5)));
            App.Current.Resources["WindowColor"] = new_brush;

            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
        }
        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Color src_color = ((SolidColorBrush)SystemParameters.WindowGlassBrush).Color;
            var new_brush = new SolidColorBrush(Color.FromArgb(src_color.A, (byte)(src_color.R * 0.5), (byte)(src_color.G * 0.5), (byte)(src_color.B * 0.5)));
            App.Current.Resources["WindowColor"] = new_brush;
        }
    }
}
