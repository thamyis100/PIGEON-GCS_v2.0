using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace ESC_Tuning
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window//, INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        //private string _currADCOffset = "0.01";
        //public string currADCOffset
        //{
        //    get { return _currADCOffset; }
        //    set
        //    {
        //        if (value != _currADCOffset)
        //        {
        //            _currADCOffset = value;
        //            OnPropertyChanged("currADCOffset");
        //        }
        //    }
        //}
        //private string _currOC5Value = "0.01";
        //public string currOC5Value
        //{
        //    get { return _currOC5Value; }
        //    set
        //    {
        //        if (value != _currOC5Value)
        //        {
        //            _currOC5Value = value;
        //            OnPropertyChanged("currOC5Value");
        //        }
        //    }
        //}

        public MainWindow()
        {
            InitializeComponent();

            //currOC5Value = "test";
        }

        private void changed(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine("CHANGED");
        }

        //protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChangedEventHandler handler = this.PropertyChanged;
        //    if (handler != null)
        //    {
        //        var e = new PropertyChangedEventArgs(propertyName);
        //        handler(this, e);
        //    }
        //}
    }
}
