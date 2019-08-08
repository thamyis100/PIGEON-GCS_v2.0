using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Threading;
using LiveCharts;
using LiveCharts.Configurations;

namespace Pigeon_WPF_cs.Custom_UserControls
{

    /// <summary>
    /// Interaction logic for StatisticControl.xaml
    /// </summary>
    public partial class StatisticControl : UserControl, INotifyPropertyChanged
    {
        public class MeasureModel
        {
            public DateTime DateTime { get; set; }
            public float Value { get; set; }
        }

        private double _axisMax;
        private double _axisMin;

        public StatisticControl()
        {
            InitializeComponent();

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
                .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //the values property will store our values array
            YawValues = new ChartValues<MeasureModel>();
            PitchValues = new ChartValues<MeasureModel>();
            RollValues = new ChartValues<MeasureModel>();

            //lets set how to display the X Labels
            DateTimeFormatter = value => new DateTime((long)value).ToString("mm:ss.f");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            //The next code simulates data changes every 300 ms

            IsReading = false;

            DataContext = this;
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
            if (IsReading) Task.Factory.StartNew(Read);
        }

        private void Read()
        {
            var r = new Random();
            //var s = new Random();

            while (IsReading)
            {
                Thread.Sleep(200);

                var ey = r.Next(-40, 40);
                var now = DateTime.Now;
                YawValues.Add(new MeasureModel
                {
                    DateTime = now,
                    Value = ey
                });
                Console.Write("Added : {0} |", ey);
                ey = r.Next(-21, 52);
                PitchValues.Add(new MeasureModel
                {
                    DateTime = now,
                    Value = ey
                });
                Console.Write(" {0} |", ey);
                ey = r.Next(-61, 2);
                RollValues.Add(new MeasureModel
                {
                    DateTime = now,
                    Value = ey
                });
                Console.WriteLine(" {0}", ey);

                SetAxisLimits(now);

                //lets only use the last 150 values
                if (YawValues.Count > 150) YawValues.RemoveAt(0);
                if (PitchValues.Count > 150) PitchValues.RemoveAt(0);
                if (RollValues.Count > 150) RollValues.RemoveAt(0);
            }
        }

        public void addToStatistik(float yaw, float pitch, float roll)
        {
            var now = DateTime.Now;
            YawValues.Add(new MeasureModel
            {
                DateTime = now,
                Value = yaw
            });
            PitchValues.Add(new MeasureModel
            {
                DateTime = now,
                Value = pitch
            });
            RollValues.Add(new MeasureModel
            {
                DateTime = now,
                Value = roll
            });

            SetAxisLimits(now);
        }

        long lastTick = 0;
        private void SetAxisLimits(DateTime now)
        {
            if (now.Ticks - lastTick > TimeSpan.TicksPerSecond)
            {
                lastTick = now.Ticks;
                AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
                AxisMin = now.Ticks - TimeSpan.FromSeconds(15).Ticks; // and 8 seconds behind
            }

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
