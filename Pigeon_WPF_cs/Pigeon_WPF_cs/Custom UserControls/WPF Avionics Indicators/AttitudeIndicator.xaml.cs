using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pigeon_WPF_cs.Custom_UserControls.WPF_Avionics_Indicators
{
    /// <summary>
    /// Interaction logic for AttitudeIndicator.xaml
    /// </summary>
    public partial class AttitudeIndicator : UserControl
    {
        public AttitudeIndicator()
        {
            InitializeComponent();
            //SetAttitudeIndicatorParameters(45, -45);
        }
        System.Windows.Point rotasiPoin(float X, double Y, float drj)
        {
            double rad = (drj * Math.PI) / 180;
            double newx = (X * Math.Cos(rad)) + (Y * Math.Sin(rad));
            double newy = -(X * Math.Sin(rad)) + (Y * Math.Cos(rad));
            //Console.WriteLine("rotasipoint: " + (-newx).ToString() + "," + newy.ToString());
            return new System.Windows.Point(-newx, newy);
        }

        public void SetAttitudeIndicatorParameters(float aircraftPitchAngle, float aircraftRollAngle)
        {
            System.Windows.Point thepoint = rotasiPoin(0, aircraftPitchAngle * 5.35, aircraftRollAngle);
            TransformGroup rotatePindah = new TransformGroup();
            TranslateTransform pindahPoint = new TranslateTransform(thepoint.X, thepoint.Y);
            //Console.WriteLine("translatetransform: " + pindahPoint.X.ToString() + "," + pindahPoint.Y.ToString());
            rotatePindah.Children.Add(new RotateTransform(aircraftRollAngle));
            rotatePindah.Children.Add(pindahPoint);
            //rotatePindah.Children.Add(new ScaleTransform(3.5, 3.5));

            img_boule.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            img_boule.RenderTransform = rotatePindah;
        }
    }
}
