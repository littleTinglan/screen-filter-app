using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Color = System.Drawing.Color;

namespace screenFilterApp
{
    public struct MyColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public MyColor(float _r = 0, float _g = 0, float _b = 0, float _a = 1)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }
    }
    /// <summary>
    /// Interaction logic for FilterdImage.xaml
    /// </summary>
    public partial class FilterdImage : Window
    {
        // Public variables
        MyColor blindRed, blindGreen, blindBlue;

        BitmapSource defaultImg;

        public FilterdImage()
        {
            InitializeComponent();

            // defual color mode to Normal
            // Normal
            blindRed = new MyColor(1, 0, 0);
            blindGreen = new MyColor(0, 1, 0);
            blindBlue = new MyColor(0, 0, 1);

            this.Closing += FilterdImage_Closing;
        }

        public void StoreImage()
        {
            defaultImg = (BitmapSource)capturedImg.Source;
        }

        private void FilterdImage_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void ChangeColorMode(object sender, RoutedEventArgs e)
        {
            if (normal_btn.IsChecked == true)
            {
                // Normal
                blindRed = new MyColor(1, 0, 0);
                blindGreen = new MyColor(0, 1, 0);
                blindBlue = new MyColor(0, 0, 1);
            }
            else if (protanopia_btn.IsChecked == true)
            {
                // Protanopia
                blindRed = new MyColor(0.56f, 0.43f, 0);
                blindGreen = new MyColor(0.55f, 0.44f, 0);
                blindBlue = new MyColor(0, 0.24f, 0.75f);
            }
            else if (protanomaly_btn.IsChecked == true)
            {
                // Protanomaly
                blindRed = new MyColor(0.81f, 0.19f, 0);
                blindGreen = new MyColor(0.335f, 0.665f, 0);
                blindBlue = new MyColor(0, 0.125f, 0.875f);
            }
            else if (tritanomaly_btn.IsChecked == true)
            {
                // Tritanomaly
                blindRed = new MyColor(0.96f, 0.3f, 0);
                blindGreen = new MyColor(0.0f, 0.73f, 0.26f);
                blindBlue = new MyColor(0, 0.18f, 0.81f);
            }
            else if (totalColorBlind_btn.IsChecked == true)
            {
                // Achromatopsia
                blindRed = new MyColor(0.29f, 0.58f, 0.11f);
                blindGreen = new MyColor(0.29f, 0.58f, 0.11f);
                blindBlue = new MyColor(0.29f, 0.58f, 0.11f);
            }
        }

        private void ApplyColorBlindFilter(object sender, RoutedEventArgs e)
        {
            
            //WriteableBitmap bitMap = new WriteableBitmap((BitmapSource)capturedImg.Source);
            int width = (int)capturedImg.Source.Width;
            int height = (int)capturedImg.Source.Height;
            WriteableBitmap bitMap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            Bitmap imageBitmap;

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(defaultImg));
                encoder.Save(outStream);
                imageBitmap = new Bitmap(outStream);
            }

            uint[] pixels = new uint[width * height];

            int red;
            int green;
            int blue;
            int alpha;

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    Color pixColor = imageBitmap.GetPixel(x, y);
                    int i = width * y + x;

                    red = (int)(pixColor.R * blindRed.r + pixColor.G * blindRed.g + pixColor.B * blindRed.b);
                    green = (int)(pixColor.R * blindGreen.r + pixColor.G * blindGreen.g + pixColor.B * blindGreen.b);
                    blue = (int)(pixColor.R * blindBlue.r + pixColor.G * blindBlue.g + pixColor.B * blindBlue.b);
                    alpha = pixColor.A;

                    pixels[i] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                }
            }

            bitMap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            capturedImg.Source = bitMap;
        }
    }
}
