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
using Pigeon_WPF_cs.Enums;

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

            InitializeFirstTimeOpen();
        }

        private Task<bool> InitializeFirstTimeOpen()
        {
            SetBaterai((float)75.25);

            SetCurrentlyActive(tab_flight, btn_flight, flight_Ctrl);
            ResizeMapFlight();

            SetConnStat(TipeDevice.WAHANA, false); //offline wahana
            SetConnStat(TipeDevice.TRACKER, false); //offline tracker

            // Jam Digital waktu sekarang
            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.DataBind, delegate
            {
                digital_clock.Text = DateTime.Now.ToString("HH\\:mm\\:ss (G\\MTz) dddd,\ndd MMMM yyyy", CultureInfo.CurrentUICulture);
            }, Dispatcher);

            return Task.FromResult(true);
        }

        #region MainWindow control

        #region Top Bar Control

        /// <summary>
        /// Mengubah tampilan angka dan tanggal sesuai bahasa
        /// </summary>
        private void SetBahasa(CultureInfo Language) => Thread.CurrentThread.CurrentUICulture = Language;

        /// <summary>
        /// Set kapasitas baterai
        /// </summary>
        /// <param name="tegangan">Tegangan baterai dalam satuan Mili Volt (mV)</param>
        /// <param name="arus">Tegangan baterai dalam satuan Mili Ampere (mA)</param>
        internal void SetBaterai(float kapasitas, ushort tegangan = 0, ushort arus = 0)
        {
            int persen_px = (int)kapasitas.Map(0.0f, 255.0f, 70.0f, 640.0f);

            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(
                new Uri(App.ResourcePackUri + "icons/bat-full.png")),
                new Int32Rect(0, 0, persen_px, 396));

            val_batt.Content = tegangan.Map(0, 4095, 0, 3.3f).ToString("0.00") + " V | "
                + arus.Map(0, 4095, -20, 8).ToString("0.00") + " A";
        }

        /// <summary>
        /// Set kualitas sinyal koneksi
        /// </summary>
        /// <param name="signal">Kualitas sinyal dalam satuan Persen (%)</param>
        internal void SetSignal(float signal)
        {
            int persen_px = (int)signal.Map(0, 255.0f, 8.0f, 44.0f);

            icon_signal_1.Source = new CroppedBitmap(Properties.Resources.icons8_wi_fi_filled_50.ToBitmapSource(),
                new Int32Rect(0, 50 - persen_px, 50, persen_px));

            val_signal.Content = signal.Map(0, 255.0f, 0, 100).ToString("0") + "%";
        }

        #endregion

        #region Other

        private void MainWindow_Clicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (WindowState != WindowState.Normal) WindowState = WindowState.Normal;
                DragMove();
            }
            catch { return; }
        }

        /// <summary>
        /// Tutup aplikasi
        /// </summary>
        private void BtnExit_Clicked(object sender, RoutedEventArgs e)
        {
            MessagePop exitPop;
            if (flight_Ctrl.IsConnected)
            {
                exitPop = new MessagePop(this, "Wahana masih terkoneksi! Silakan disconnect untuk melanjutkan!", false);
            }
            else
            {
                exitPop = new MessagePop(this, "Anda menekan tombol EXIT, dan akan keluar dari PIGEON GCS");
            }

            Effect = new BlurEffect() { Radius = 15 };
            flight_Ctrl.ShowAvionics(false);

            if (exitPop.ShowDialog() == true)
            {
                flight_Ctrl.StopCam();
                Close();
            }
            else
            {
                Effect = null;
                flight_Ctrl.ShowAvionics(true);
            }
        }

        /// <summary>
        /// Event tombol ubah bahasa
        /// </summary>
        private void TombolBahasa(object sender, RoutedEventArgs e)
        {
            bhs_indo.Background = bhs_inggris.Background = Brushes.Transparent;

            Button bahasa = (Button)sender;
            switch (bahasa.Name)
            {
                case "bhs_indo":
                    SetBahasa(new CultureInfo("id-ID"));

                    // Header/Sidebar
                    bhs_lbl.Content = "Bahasa:";
                    lbl_signal.Content = "Sinyal";
                    lbl_flightTime.Content = "Waktu Terbang";
                    lbl_batt.Content = "Baterai";

                    // Flight
                    flight_Ctrl.judul_flight.Content = "Tampilan Terbang";
                    flight_Ctrl.stream_panel_read_btn.Content = "BACA";
                    flight_Ctrl.btn_take_picture.Content = "Ambil Gambar";
                    flight_Ctrl.btn_livestream.Content = "Mulai Siaran";
                    flight_Ctrl.label.Content = "Koneksi :";
                    flight_Ctrl.ind_conn_status.Content = "Terputus";

                    // Stats 
                    stats_Ctrl.judul_stats.Content = "Statistik Data IMU";
                    stats_Ctrl.yaw_axis_x.Title = "Waktu Terbang";
                    stats_Ctrl.pitch_axis_x.Title = "Waktu Terbang";
                    stats_Ctrl.roll_axis_x.Title = "Waktu Terbang";
                    bhs_indo.Background = Brushes.Lime;

                    break;

                case "bhs_inggris":
                    SetBahasa(new CultureInfo("en-US"));

                    // Header/Sidebar
                    bhs_lbl.Content = "Lang:";
                    lbl_signal.Content = "Signal";
                    lbl_flightTime.Content = "Flight Time";
                    lbl_batt.Content = "Battery";

                    // Flight
                    flight_Ctrl.judul_flight.Content = "Flight View";
                    flight_Ctrl.stream_panel_read_btn.Content = "READ";
                    flight_Ctrl.btn_take_picture.Content = "Take Picture";
                    flight_Ctrl.label.Content = "Connection :";
                    flight_Ctrl.ind_conn_status.Content = "Disconnected";

                    // Stats
                    stats_Ctrl.judul_stats.Content = "IMU Data Statistics";
                    stats_Ctrl.yaw_axis_x.Title = "Flight Time";
                    stats_Ctrl.pitch_axis_x.Title = "Flight Time";
                    stats_Ctrl.roll_axis_x.Title = "Flight Time";
                    bhs_inggris.Background = Brushes.Lime;

                    break;
            }
        }

        #endregion

        #endregion

        #region Speech Synth & Recog

        #region Synthesize

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

        #region Recognition

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
                    flight_Ctrl.SendToConnection(Command.TAKE_OFF, TipeDevice.WAHANA);
                    break;
                case "land":
                    flight_Ctrl.SendToConnection(Command.LAND, TipeDevice.WAHANA);
                    Console.WriteLine("Sending LAND command");
                    break;
                case "cancel":
                    flight_Ctrl.SendToConnection(Command.BATALKAN, TipeDevice.WAHANA);
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
        public bool IsWahanaConnected { get; private set; } = false;

        /// <summary>
        /// Apakah terkoneksi dengan tracker?
        /// </summary>
        public bool IsTrackerConnected { get; private set; } = false;

        /// <summary>
        /// Set status koneksi PIGEON dengan Flight Controller / Antenna Tracker
        /// </summary>
        /// <param name="tipe">Tipe alat</param>
        /// <param name="status"></param>
        public void SetConnStat(TipeDevice tipe, bool status)
        {
            switch (tipe)
            {
                case TipeDevice.WAHANA:
                    IsWahanaConnected = status;
                    lbl_statusWahana.Visibility = status ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case TipeDevice.TRACKER:
                    IsTrackerConnected = status;
                    lbl_statusTracker.Visibility = status ? Visibility.Visible : Visibility.Collapsed;
                    break;
            }

            if (IsWahanaConnected || IsTrackerConnected)
            {
                lbl_statusLine.Content = "ONLINE";
                lbl_statusLine.Foreground = Brushes.Green;
            }
            else
            {
                lbl_statusLine.Content = "OFFLINE";
                lbl_statusLine.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF969696"));
            }

            lbl_statusPlus.Visibility = (IsWahanaConnected && IsTrackerConnected) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Total waktu terbang
        /// </summary>
        public TimeSpan WaktuTerbang = TimeSpan.Zero;

        /// <summary>
        /// Tanggal & Waktu mulai terbang
        /// </summary>
        private DateTime WaktuStartTerbang;

        /// <summary>
        /// Stopwatch untuk waktu terbang
        /// </summary>
        private DispatcherTimer StopWatchTerbang = new DispatcherTimer();

        public bool StartWaktuTerbang()
        {
            if (StopWatchTerbang.IsEnabled) return false;
            else
            {
                StopWatchTerbang = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(250) };
                StopWatchTerbang.Tick += StopWatchTerbang_Tick;
                StopWatchTerbang.Start();

                WaktuStartTerbang = DateTime.Now;

                return true;
            }
        }

        public void ResetWaktuTerbang()
        {
            StopWatchTerbang.Stop();

            WaktuTerbang = TimeSpan.Zero;

            val_flightTime.Content = WaktuTerbang.ToString(@"hh\:mm\:ss");

            Title = "PIGEON GCS";

            return;
        }

        private void StopWatchTerbang_Tick(object sender, EventArgs e)
        {
            WaktuTerbang = DateTime.Now - WaktuStartTerbang;
            val_flightTime.Content = WaktuTerbang.ToString(@"hh\:mm\:ss");
            Title = "T+ " + WaktuTerbang.ToString(@"hh\:mm\:ss") + " - PIGEON GCS";
        }

        #endregion

        #region Waypoint control

        /// <summary>
        /// Kecilkan ukuran map view
        /// </summary>
        public void ResizeMapFlight()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Hidden;
            Grid.SetColumnSpan(map_Ctrl, 1);
            map_Ctrl.Margin = new Thickness(0,0,0,32.5);
        }
        /// <summary>
        /// Kecilkan ukuran map view ke tracker view
        /// </summary>
        public void ResizeMapTracker()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Hidden;
            Grid.SetColumnSpan(map_Ctrl, 2);
            map_Ctrl.Margin = new Thickness(0, 0, 0, 0);
        }
        /// <summary>
        /// Besarkan ukuran map view
        /// </summary>
        public void ResizeMapMax()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Visible;
            Grid.SetColumnSpan(map_Ctrl, 3);
            map_Ctrl.Margin = new Thickness(0, 0, 0, 0);
        }

        #endregion

        #region Tab Control choosing

        /// <summary>
        /// Tab yang sedang dipilih/aktif
        /// </summary>
        string SelectedTabBtn = "btn_flight";

        private void Btn_OnHover(object sender, MouseEventArgs e)
        {
            if((sender as Button).Name != SelectedTabBtn)
                (sender as Button).Background = Brushes.DarkSlateGray;
        }

        private void Btn_OnDehover(object sender, MouseEventArgs e)
        {
            if ((sender as Button).Name != SelectedTabBtn)
                (sender as Button).Background = null;
        }

        /// <summary>
        /// Pilihan tab dengan button
        /// </summary>
        private void TabSelect(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Name)
            {
                case "btn_flight":
                    SetCurrentlyActive(tab_flight, btn_flight, flight_Ctrl);
                    ResizeMapFlight();
                    break;
                case "btn_map":
                    SetCurrentlyActive(tab_map, btn_map, map_Ctrl);
                    ResizeMapMax();
                    break;
                case "btn_stats":
                    SetCurrentlyActive(tab_stats, btn_stats, stats_Ctrl);
                    break;
                case "btn_track":
                    SetCurrentlyActive(tab_track, btn_track, track_Ctrl);
                    ResizeMapTracker();
                    break;
            }
        }

        /// <summary>
        /// Aktifkan view pada tab
        /// </summary>
        private void SetCurrentlyActive(Rectangle theBox, Button theBtn, UserControl theCtrl)
        {
            //sembunyikan semua box
            tab_flight.Visibility = tab_map.Visibility = tab_stats.Visibility = tab_track.Visibility = Visibility.Hidden;

            //transparankan semua tombol pilihan
            btn_flight.Background = btn_map.Background = btn_stats.Background = btn_track.Background = Brushes.Transparent;

            //sembunyikan semua control kecuali waypoint
            flight_Ctrl.Visibility = stats_Ctrl.Visibility = track_Ctrl.Visibility = Visibility.Hidden;

            //tampilkan control yang dipilih
            theCtrl.Visibility = Visibility.Visible;

            //warnai tombol yang dipilih
            theBtn.Background = Brushes.Teal;

            // Jadikan tombol aktif
            SelectedTabBtn = theBtn.Name;

            //tampilkan box yang dipilih
            theBox.Visibility = Visibility.Visible;
        }

        #endregion
    }
}
