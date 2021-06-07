using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

namespace Pigeon_WPF_cs
{
    /// <summary>
    /// Daftar perintah untuk dikirim ke wahana
    /// </summary>
    public enum command
    {
        /// <summary>
        /// Perintah auto take off
        /// </summary>
        TAKE_OFF = 0xAA,
        /// <summary>
        /// Perintah auto landing
        /// </summary>
        LAND = 0xCC,
        /// <summary>
        /// Perintah membatalkan auto take-off
        /// </summary>
        BATALKAN = 0xBB
    }

    /// <summary>
    /// Daftar mode penerbangan
    /// </summary>
    public enum FlightMode
    {
        /// <summary>
        /// Terbang secara manual
        /// </summary>
        MANUAL = 0x00,
        /// <summary>
        /// Terbang dengan stabilisasi
        /// </summary>
        STABILIZER = 0x01,
        /// <summary>
        /// Terbang mengitari tempat
        /// </summary>
        LOITER = 0x02,
        /// <summary>
        /// Sedang takeoff
        /// </summary>
        TAKEOFF = 0x03
    }

    /// <summary>
    /// Identifier penggunaan efalcon
    /// </summary>
    public enum TipeEfalcon
    {
        TRACKER = 0x01,
        WAHANA = 0x02
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Fungsi memetakan satu rentang nilai ke rentang nilai yang lain
        /// </summary>
        float Map(float x, float in_min, float in_max, float out_min, float out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        public MainWindow()
        {
            InitializeComponent();

            //contoh kapasitas baterai
            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/bat-full.png")), new Int32Rect(0, 0, 400, 396));

            //inisialisasi semua tab
            setCurrentlyActive(tab_track, btn_track, track_Ctrl);
            setCurrentlyActive(tab_stats, btn_stats, stats_Ctrl);
            setCurrentlyActive(tab_map, btn_map, map_Ctrl);
            setCurrentlyActive(tab_flight, btn_flight, flight_Ctrl);

            SetConnStat(TipeEfalcon.WAHANA, false); //offline wahana
            SetConnStat(TipeEfalcon.TRACKER, false); //offline tracker

            //kecilkan ukuran map
            MinimizeMap();

            //siapkan vokalisasi data terbang
            //PrepareSpeechSynth();
            //siapkan pengenalan perintah suara
            //PrepareRecog();

            //set bahasa indonesia
            SetBahasa(new CultureInfo("id-ID"));

            //set waktu sekarang
            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.DataBind, delegate
            {
                digital_clock.Text = DateTime.Now.ToString("HH\\:mm\\:ss (G\\MTz) dddd,\ndd MMMM yyyy", CultureInfo.CurrentUICulture);
            },Dispatcher);
        }

        #region MainWindow functions

        /// <summary>
        /// Event tombol ubah bahasa
        /// </summary>
        private void ubah_bahasa(object sender, RoutedEventArgs e)
        {
            bhs_indo.Background = bhs_inggris.Background = null;
            Button bahasa = (Button)sender;
            switch (bahasa.Name)
            {
                case "bhs_indo":
                    SetBahasa(new CultureInfo("id-ID"));
                    bhs_lbl.Content = "Language :";
                    bhs_indo.Background = Brushes.Lime;
                    break;
                case "bhs_inggris":
                    SetBahasa(new CultureInfo("en-EN"));
                    bhs_lbl.Content = "Bahasa :";
                    bhs_inggris.Background = Brushes.Lime;
                    break;
            }
        }

        /// <summary>
        /// Mengubah bahasa tampilan
        /// </summary>
        private void SetBahasa(CultureInfo theLanguage) => Thread.CurrentThread.CurrentUICulture = theLanguage;

        /// <summary>
        /// Set kapasitas baterai
        /// <br/><paramref name="tegangan"/> dalam Volt
        /// <br/><paramref name="arus"/> dalam Ampere
        /// </summary>
        internal void SetBaterai(float tegangan, float arus, byte jmlcell = 3)
        {
            Debug.WriteLine("SetBaterai : \n"
                + "jml cell : " + jmlcell.ToString()
                + "tegangan : " + tegangan.ToString() + " V"
                + "arus : " + arus.ToString() + " A"
            );

            int persen_px = (int)Map(tegangan/jmlcell, 3.0f, 4.2f, 70.0f, 640.0f);
            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/bat-full.png")), new Int32Rect(0, 0, persen_px, 396));
            val_batt.Content = tegangan.ToString("#.##") + " V | " + arus.ToString("#.###") + " A";
        }

        #endregion

        #region Speech Synth & Recog

        #region synth

        private SpeechSynthesizer synth;
        /// <summary>
        /// Siapkan resources untuk synthesize suara<br/>
        /// <i><b>SEDANG RUSAK, JANGAN DIGUNAKAN</b></i>
        /// </summary>
        private void PrepareSpeechSynth()
        {
            synth = new SpeechSynthesizer();
            //synth.StateChanged += new EventHandler<StateChangedEventArgs>(synth_StateChanged);
            //synth.SpeakStarted += new EventHandler<SpeakStartedEventArgs>(synth_SpeakStarted);
            //synth.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(synth_SpeakProgress);
            //synth.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(synth_SpeakCompleted);
            //synth.SelectVoice("Microsoft Andika");
            synth.Volume = 100;
            synth.Rate = 3;
        }

        /// <summary>
        /// Suarakan <paramref name="ssmltxt"/> dengan synthesizer
        /// </summary>
        public void SpeakOutloud(string ssmltxt)
        {
            if (ssmltxt == null || synth.State != SynthesizerState.Ready) return;
            var prompt = new PromptBuilder();
            prompt.AppendSsmlMarkup(ssmltxt);
            synth.SpeakAsync(prompt);
        }

        /// <summary>
        /// Dummy synth untuk uji coba
        /// </summary>
        private void injectSpeak(object sender, RoutedEventArgs e)
        {
            synth.SelectVoice("Microsoft Andika");
            synth.Volume = 100;
            synth.Rate = 3;
            PromptBuilder prompt = new PromptBuilder();
            //string ssmlText = "<emph>" +
            //"Heading: 330,34." +
            //"Pitch: -43,21." +
            //"Roll: -92,34." +
            //"Kecepatan: 99." +
            //"Ketinggian: 20,52." +
            //"Latitude: -7,125125." +
            //"Longitude: 112,552125." +
            //"</emph>";
            string ssmltxt = "<s>Heading <emphasis><say-as interpret-as=\"number\">330</say-as></emphasis></s>";
            ssmltxt += "<s>Ketinggian <emphasis><say-as interpret-as=\"number\">20</say-as></emphasis> meter</s>";
            ssmltxt += "<s>Kecepatan <emphasis><say-as interpret-as=\"number\">90</say-as></emphasis> kilometer/jam</s>";
            prompt.AppendSsmlMarkup(ssmltxt);

            switch (synth.State)
            {
                case SynthesizerState.Ready:
                    synth.SpeakAsync(prompt);
                    break;
                case SynthesizerState.Paused:
                    synth.Resume();
                    break;
                case SynthesizerState.Speaking:
                    synth.Pause();
                    break;
            }
        }

        #endregion

        #region Recog

        private SpeechRecognitionEngine recog = new SpeechRecognitionEngine();
        /// <summary>
        /// Siapkan pengenalan suara dengan mikrofon
        /// <br/><i><b>SEDANG RUSAK, JANGAN DIGUNAKAN</b></i>
        /// </summary>
        private void PrepareRecog()
        {
            recog.SpeechRecognized += speechRecognized;
            var command = new Choices("takeoff", "land","cancel");
            var grambuild = new GrammarBuilder(command);

            recog.LoadGrammar(new Grammar(grambuild));
            recog.SetInputToDefaultAudioDevice();
        }

        bool isRecog = false;
        /// <summary>
        /// Toggle mengenali suara
        /// <br/><i><b>SEDANG RUSAK, JANGAN DIGUNAKAN</b></i>
        /// </summary>
        private void toggleRecog(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.H)
            {
                if (!isRecog) { recog.RecognizeAsync(RecognizeMode.Single); isRecog = true; Console.WriteLine("Listening recognition"); }
                else { recog.RecognizeAsyncStop(); Console.WriteLine("Cancelled recognition"); isRecog = false; }
            }
        }

        /// <summary>
        /// Suara berhasil dikenali
        /// <br/><i><b>SEDANG RUSAK, JANGAN DIGUNAKAN</b></i>
        /// </summary>
        private void speechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("Received: " + e.Result.Text);
            isRecog = false;
            switch (e.Result.Text)
            {
                case "takeoff":
                    Console.WriteLine("Sending TAKE_OFF command");
                    flight_Ctrl.SendToConnection(command.TAKE_OFF, TipeEfalcon.WAHANA);
                    break;
                case "land":
                    flight_Ctrl.SendToConnection(command.LAND, TipeEfalcon.WAHANA);
                    Console.WriteLine("Sending LAND command");
                    break;
                case "cancel":
                    flight_Ctrl.SendToConnection(command.BATALKAN, TipeEfalcon.WAHANA);
                    Console.WriteLine("Sending BATALKAN command");
                    break;
            }
        }

        #endregion

        #endregion

        #region Connection n timer

        /// <summary>
        /// Apakah terkoneksi dengan wahana?
        /// </summary>
        public bool isWahanaConnected = false;
        /// <summary>
        /// Apakah terkoneksi dengan tracker?
        /// </summary>
        public bool isTrackerConnected = false;
        /// <summary>
        /// Set status koneksi
        /// <br/><paramref name="tipe"/> adalah TipeEfalcon
        /// <br/><paramref name="status"/> :
        /// <br/>true = online
        /// <br/>false = offline
        /// </summary>
        public void SetConnStat(TipeEfalcon tipe, bool status)
        {
            switch (tipe)
            {
                case TipeEfalcon.WAHANA:
                    isWahanaConnected = status;
                    lbl_statusWahana.Visibility = status ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case TipeEfalcon.TRACKER:
                    isTrackerConnected = status;
                    lbl_statusTracker.Visibility = status ? Visibility.Visible : Visibility.Collapsed;
                    break;
            }

            if (isTrackerConnected || isWahanaConnected)
            {
                lbl_statusLine.Content = "ONLINE";
                lbl_statusLine.Foreground = Brushes.Green;
            }
            else
            {
                lbl_statusLine.Content = "OFFLINE";
                lbl_statusLine.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF969696"));
            }

            lbl_statusPlus.Visibility = (isTrackerConnected && isWahanaConnected) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Total waktu terbang
        /// </summary>
        public TimeSpan waktuTerbang = TimeSpan.Zero;
        /// <summary>
        /// Tanggal & Waktu mulai terbang
        /// </summary>
        private DateTime waktuStart;
        /// <summary>
        /// Stopwatch untuk waktu terbang
        /// </summary>
        private DispatcherTimer detikan;
        bool isTimerFirstTime = true;
        /// <summary>
        /// Resume/Pause stopwatch waktu penerbangan
        /// <br/><paramref name="status"/> :
        /// <br/>true = Resume
        /// <br/>false = Pause
        /// </summary>
        public bool ToggleWaktuTerbang(bool status)
        {
            if (status)
            {
                if (isTimerFirstTime)
                {
                    detikan = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(100) };
                    detikan.Tick += TickTerbang;
                    waktuStart = DateTime.Now;
                    detikan.Start();
                    isTimerFirstTime = false;
                }

                if (detikan.IsEnabled)
                {
                    detikan.Stop();
                    isTimerFirstTime = true;
                    waktuTerbang = TimeSpan.Zero;
                    val_flightTime.Content = waktuTerbang.ToString(@"hh\:mm\:ss");
                    Application.Current.MainWindow.Title = "PIGEON GCS";
                }
                else detikan.Start();

                return true;
            }
            else
            {
                return false;
            }
        }

        public void ResetWaktuTerbang()
        {

        }

        private void TickTerbang(object sender, EventArgs e)
        {
            waktuTerbang = DateTime.Now - waktuStart;
            val_flightTime.Content = waktuTerbang.ToString(@"hh\:mm\:ss.fff");
            Application.Current.MainWindow.Title = "T+ " + waktuTerbang.ToString(@"hh\:mm\:ss.fff") + " - PIGEON GCS";
        }

        #endregion

        #region window control

        private void clickedWindow(object sender, MouseButtonEventArgs e) 
        { 
            try {
                if (WindowState != WindowState.Normal) WindowState = WindowState.Normal;
                DragMove(); 
            } catch { return; } }

        /// <summary>
        /// Cek apakah wahana aman (eg. sudah mendarat)
        /// </summary>
        public bool isWahanaLanded()
        {
            Effect = new BlurEffect() { Radius = 5 };
            flight_Ctrl.hideAvionics();

            var diskonekFirst = new exitPop(2);
            diskonekFirst.Owner = this;
            if (diskonekFirst.ShowDialog() == true)
            {
                Effect = null;
                flight_Ctrl.showAvionics();
                return true;
            }
            else
            {
                Effect = null;
                flight_Ctrl.showAvionics();
                return false;
            }
        }

        /// <summary>
        /// Cek apakah sedang terkoneksi
        /// </summary>
        private byte appCheck()
        {
            byte code = 0;
            if (flight_Ctrl.connected) code = 1;
            return code;
        }

        /// <summary>
        /// Tutup aplikasi
        /// </summary>
        private void closeApp(object sender, RoutedEventArgs e)
        {
            Effect = new BlurEffect() { Radius = 15 };
            flight_Ctrl.hideAvionics();

            var exitting = new exitPop(appCheck());
            exitting.Owner = this;
            if (exitting.ShowDialog() == true) { flight_Ctrl.stopControl(); Application.Current.Shutdown(); }
            else
            {
                Effect = null;
                flight_Ctrl.showAvionics();
            }
        }

        #endregion

        #region Waypoint control

        /// <summary>
        /// Kecilkan ukuran map view
        /// </summary>
        public void MinimizeMap()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Hidden;
            Grid.SetRowSpan(mapGrid, 1);
            Grid.SetColumnSpan(mapGrid, 1);
            mapGrid.Margin = new Thickness(0, 0, 0, 0);
        }
        /// <summary>
        /// Kecilkan ukuran map view ke tracker view
        /// </summary>
        public void MinimizeMapTracker()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Hidden;
            Grid.SetRowSpan(mapGrid, 2);
            Grid.SetColumnSpan(mapGrid, 1);
            mapGrid.Margin = new Thickness(0, 0, -130, 0);
        }
        /// <summary>
        /// Besarkan ukuran map view
        /// </summary>
        public void MaximizeMap()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Visible;
            Grid.SetRowSpan(mapGrid, 2);
            Grid.SetColumnSpan(mapGrid, 2);
            mapGrid.Margin = new Thickness(0, 0, 0, 0);
        }

        #endregion

        #region statistik control

        #endregion

        #region flight view control

        #endregion

        #region Tab Control choosing

        string selectedBtn ="btn_flight";
        private void hoverButton(object sender, MouseEventArgs e)
        {
            Button it = (Button)sender;
            if(it.Name != selectedBtn) it.Background = Brushes.DarkSlateGray;
        }

        private void dehoverButton(object sender, MouseEventArgs e)
        {
            Button it = (Button)sender;
            if (it.Name != selectedBtn) it.Background = null;
        }

        /// <summary>
        /// Pilihan tab dengan button
        /// </summary>
        private void selectTab(object sender, RoutedEventArgs e)
        {
            Button the_btn = (Button)sender;
            switch (the_btn.Name)
            {
                case "btn_flight":
                    setCurrentlyActive(tab_flight, btn_flight, flight_Ctrl);
                    MinimizeMap();
                    break;
                case "btn_map":
                    setCurrentlyActive(tab_map, btn_map, map_Ctrl);
                    MaximizeMap();
                    break;
                case "btn_stats":
                    setCurrentlyActive(tab_stats, btn_stats, stats_Ctrl);
                    break;
                case "btn_track":
                    setCurrentlyActive(tab_track, btn_track, track_Ctrl);
                    MinimizeMapTracker();
                    break;
            }
        }

        /// <summary>
        /// Aktifkan view pada tab
        /// </summary>
        private void setCurrentlyActive(Rectangle theBox, Button theBtn, UserControl theCtrl)
        {
            //sembunyikan semua box
            tab_flight.Visibility = tab_map.Visibility = tab_stats.Visibility = tab_track.Visibility = Visibility.Hidden;

            //transparankan semua tombol pilihan
            btn_flight.Background = btn_map.Background = btn_stats.Background = btn_track.Background = null;

            //sembunyikan semua control kecuali waypoint
            flight_Ctrl.Visibility = stats_Ctrl.Visibility = track_Ctrl.Visibility = Visibility.Hidden;

            //tampilkan control yang dipilih
            theCtrl.Visibility = Visibility.Visible;

            //warnai tombol yang dipilih
            theBtn.Background = Brushes.Teal;
            selectedBtn = theBtn.Name;

            //tampilkan box yang dipilih
            theBox.Visibility = Visibility.Visible;
        }

        #endregion

        //private void muteVoice(object sender, RoutedEventArgs e) => MuteVoice = !MuteVoice;
    }
}
