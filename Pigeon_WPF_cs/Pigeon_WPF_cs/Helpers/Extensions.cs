using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Pigeon_WPF_cs
{
    public static class Extensions
    {
        /// <summary>
        /// Fungsi memetakan satu rentang nilai ke rentang nilai yang lain
        /// </summary>
        public static float Map(this float value, float in_min, float in_max, float out_min, float out_max)
        {
            return ((value - in_min) * (out_max - out_min) / (in_max - in_min)) + out_min;
        }

        #region Custom Bitmap to BitmapImage

        /// <summary>
        /// Convert Bitmap to BitmapImage
        /// </summary>
        /// <param name="bitmap">Input bitmap</param>
        /// <returns>BitmapImage to be used in controls</returns>
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }

        #endregion
    }
}