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

    public enum FMode
    {
        MANUAL = 0x00,
        STABILIZER = 0x01,
        LOITER = 0x02,
        TAKEOFF = 0x03
    }

    public enum Efalcon
    {
        TRACKER = 0x01,
        WAHANA = 0x02
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        float map(float x, float in_min, float in_max, float out_min, float out_max)
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

            setConnStat(false, false); //offline wahana
            setConnStat(false, true); //offline tracker
            MinimizeMap();
            PrepareSpeechSynth();
            PrepareRecog();

            //set bahasa indonesia
            SetBahasa(new CultureInfo("id-ID"));

            //set waktu sekarang
            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.DataBind, delegate
            {
                digital_clock.Text = DateTime.Now.ToString("HH\\:mm\\:ss (G\\MTz) dddd,\ndd MMMM yyyy", CultureInfo.CurrentUICulture);
            },Dispatcher);
        }

        #region MainWindow functions

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
        private void SetBahasa(CultureInfo theLanguage) => Thread.CurrentThread.CurrentUICulture = theLanguage;
        private void injectStats(object sender, RoutedEventArgs e) => map_Ctrl.doRotateHomepos();

        internal void SetBaterai(float baterai)
        {
            //Console.WriteLine("Baterai Convert: " + Convert.ToInt32(map(baterai, 10.5f, 12.8f, 80.0f, 750.0f)).ToString());
            icon_bat_1.Source = new CroppedBitmap(new BitmapImage(new Uri("pack://application:,,,/Resources/icons/bat-full.png")), new Int32Rect(0, 0, 500, 396));
            val_batt.Content = baterai.ToString("0.00") + " V";
        }

        #endregion

        #region Speech Synth & Recog

        #region synth

        private SpeechSynthesizer synth;
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

        bool MuteVoice = false;
        public void SpeakOutloud(string ssmltxt)
        {
            if (MuteVoice) return;
            if (ssmltxt == null || synth.State != SynthesizerState.Ready) return;
            var prompt = new PromptBuilder();
            prompt.AppendSsmlMarkup(ssmltxt);
            synth.SpeakAsync(prompt);
        }

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
        private void PrepareRecog()
        {
            recog.SpeechRecognized += speechRecognized;
            var command = new Choices("takeoff", "land","cancel");
            var grambuild = new GrammarBuilder(command);

            recog.LoadGrammar(new Grammar(grambuild));
            recog.SetInputToDefaultAudioDevice();
        }

        bool isRecog = false;
        private void toggleRecog(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.H)
            {
                if (!isRecog) { recog.RecognizeAsync(RecognizeMode.Single); isRecog = true; Console.WriteLine("Listening recognition"); }
                else { recog.RecognizeAsyncStop(); Console.WriteLine("Cancelled recognition"); isRecog = false; }
            }
        }

        private void speechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("Received: " + e.Result.Text);
            isRecog = false;
            switch (e.Result.Text)
            {
                case "takeoff":
                    Console.WriteLine("Sending TAKE_OFF command");
                    flight_Ctrl.SendToConnection(command.TAKE_OFF, Efalcon.WAHANA);
                    break;
                case "land":
                    flight_Ctrl.SendToConnection(command.LAND, Efalcon.WAHANA);
                    Console.WriteLine("Sending LAND command");
                    break;
                case "cancel":
                    flight_Ctrl.SendToConnection(command.BATALKAN, Efalcon.WAHANA);
                    Console.WriteLine("Sending BATALKAN command");
                    break;
            }
        }

        #endregion

        #endregion

        #region Connection n timer

        public bool isWahanaConnected = false;
        public bool isTrackerConnected = false;
        public void setConnStat(bool status, bool tipe)
        {
            if (status && tipe) //tracker online
            {
                isTrackerConnected = true;
                lbl_statusTracker.Visibility = Visibility.Visible;
            }
            else if (status && !tipe) //wahana online
            {
                isWahanaConnected = true;
                lbl_statusWahana.Visibility = Visibility.Visible;
            }
            else if (!status && tipe) //tracker offline
            {
                isTrackerConnected = false;
                lbl_statusTracker.Visibility = Visibility.Collapsed;
            }
            else if (!status && !tipe) //wahana offline
            {
                isWahanaConnected = false;
                lbl_statusWahana.Visibility = Visibility.Collapsed;
            }

            if (isTrackerConnected || isWahanaConnected)
            {
                lbl_statusLine.Content = "ONLINE";
                lbl_statusLine.Foreground = Brushes.Green;
            } else
            {
                lbl_statusLine.Content = "OFFLINE";
                lbl_statusLine.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF969696"));
            }
            if (isTrackerConnected && isWahanaConnected) lbl_statusPlus.Visibility = Visibility.Visible;
            else lbl_statusPlus.Visibility = Visibility.Collapsed;
            MuteVoice = !MuteVoice;
        }

        public TimeSpan waktuTerbang = TimeSpan.Zero;
        private DateTime waktuStart;
        private DispatcherTimer detikan;
        bool isTimerFirstTime = true;
        public void ToggleWaktuTerbang()
        {
            if (isTimerFirstTime)
            {
                detikan = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
                detikan.Tick += detikTerbang;
                waktuStart = DateTime.Now;
                detikan.Start();
                isTimerFirstTime = false;
                return;
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
        }

        private void detikTerbang(object sender, EventArgs e)
        {
            waktuTerbang = DateTime.Now - waktuStart;
            val_flightTime.Content = waktuTerbang.ToString(@"hh\:mm\:ss");
            Application.Current.MainWindow.Title = "T+ " + waktuTerbang.ToString(@"hh\:mm\:ss") + " - PIGEON GCS";
        }

#endregion

        #region window control

        private void clickedWindow(object sender, MouseButtonEventArgs e) { try { DragMove(); } catch { return; } }

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

        private byte appCheck()
        {
            byte code = 0;
            if (flight_Ctrl.connected) code = 1;
            return code;
        }
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

        public void MinimizeMap()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Hidden;
            Grid.SetRowSpan(mapGrid, 1);
            Grid.SetColumnSpan(mapGrid, 1);
        }

        public void MaximizeMap()
        {
            map_Ctrl.judul_map.Visibility = Visibility.Visible;
            Grid.SetRowSpan(mapGrid, 2);
            Grid.SetColumnSpan(mapGrid, 3);
        }

        #endregion

        #region statistik control

        #endregion

        #region flight view control

        #endregion

        #region Tab Control choosing
        String selectedBtn ="btn_flight";
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
                    MinimizeMap();
                    break;
            }
        }

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

        private void muteVoice(object sender, RoutedEventArgs e) => MuteVoice = !MuteVoice;

        private void flight_Ctrl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
