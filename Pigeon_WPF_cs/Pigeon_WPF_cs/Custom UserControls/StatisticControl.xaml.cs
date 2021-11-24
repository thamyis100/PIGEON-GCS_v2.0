using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Threading;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Helpers;
using LiveCharts.Wpf;

namespace Pigeon_WPF_cs.Custom_UserControls
{

    /// <summary>
    /// Interaction logic for StatisticControl.xaml
    /// </summary>
    public partial class StatisticControl : UserControl, INotifyPropertyChanged
    {
        public class MeasureModel
        {
            public TimeSpan TimeSpan { get; set; }
            public float Value { get; set; }
        }

        private double _axisMax;
        private double _axisMin;

        public StatisticControl()
        {
            InitializeComponent();

            DataContext = this;
            var win = (MainWindow)Window.GetWindow(this);
            //while (!damn.IsLoaded) { }
            //To handle live data easily, in this case we built a specialized type
            //the MeasureModel class, it only contains 2 properties
            //DateTime and Value
            //We need to configure LiveCharts to handle MeasureModel class
            //The next code configures MeasureModel  globally, this means
            //that LiveCharts learns to plot MeasureModel and will use this config every time
            //a IChartValues instance uses this type.
            //this code ideally should only run once
            //you can configure series in many ways, learn more at 
            //http://lvcharts.net/App/examples/v1/wpf/Types%20and%20Configuration

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.TimeSpan.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //the values property will store our values array
            YawValues = new ChartValues<MeasureModel>();
            PitchValues = new ChartValues<MeasureModel>();
            RollValues = new ChartValues<MeasureModel>();

            //lets set how to display the X Labels
            DateTimeFormatter = value => new TimeSpan((long) value).ToString("mm\\:ss\\.f");

            //lets invert heading Y axis
            //headingaxis.AxisY.Add(new Axis() { LabelFormatter = value => ((int)(value * -1)).ToString() });

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.FromMilliseconds(100).Ticks;

            //set the first axismax
            AxisMax = TimeSpan.FromSeconds(1).Ticks;
            AxisMin = 0;

            //The next code simulates data changes every 300 ms

            IsReading = false;
        }

        public ChartValues<MeasureModel> YawValues { get; set; }
        public ChartValues<MeasureModel> PitchValues { get; set; }
        public ChartValues<MeasureModel> RollValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }

        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }
        public bool IsReading { get; set; }
        public void InjectStopOnClick(object sender, RoutedEventArgs e)
        {
            IsReading = !IsReading;
        }

        public void addToStatistik()
        {
            var waktuAmbil = (App.Current.MainWindow as MainWindow).WaktuTerbang;

            YawValues.Add(new MeasureModel
            {
                TimeSpan = waktuAmbil,
                Value = App.Wahana.IMU.Yaw,
            });
            PitchValues.Add(new MeasureModel
            {
                TimeSpan = waktuAmbil,
                Value = App.Wahana.IMU.Pitch,
            });
            RollValues.Add(new MeasureModel
            {
                TimeSpan = waktuAmbil,
                Value = App.Wahana.IMU.Roll,
            });

            SetAxisLimits(waktuAmbil);
        }
        
        private void SetAxisLimits(TimeSpan now)
        {
            AxisMax = now.Ticks + TimeSpan.FromMilliseconds(1000).Ticks; // lets force the axis to be 1000 milisecond ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(8).Ticks; // and 8 seconds behind
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
