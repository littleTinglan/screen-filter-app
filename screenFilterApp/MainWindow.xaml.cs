﻿using System;
using System.Collections.Generic;
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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using System.Windows.Threading;
using NativeHelpers;

namespace screenFilterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : PerMonitorDPIWindow
    {
        // Global var
        FilterdImage imageWindow;
        DispatcherTimer myTimer;
        double factor = 1.0;
        /* Struct for RECT --- equivalent to C++ LPRECT
           Returns:
               left (int)   : topLeft.x
               top (int)    : topLeft.y
               right (int)  : botRight.x
               bottom (int) : botRight.y
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        /* Get Client Rect
           Return:
               RECT (struct) : the coordinates of the top left corner and bottom right corner of the client 
        */
        public static RECT GetClientRect(IntPtr hWnd)
        {
            RECT result;
            GetClientRect(hWnd, out result);
            return result;
        }

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        public MainWindow()
        {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close,
                new ExecutedRoutedEventHandler(delegate (object sender, ExecutedRoutedEventArgs args) { this.Close(); })));

            // Initiate the FilteredImage window
            imageWindow = new FilterdImage();

            // Timer setup
            myTimer = new System.Windows.Threading.DispatcherTimer();
            myTimer.Interval = new TimeSpan(0, 0, 0); 

        }

        // User Controls
        public void DragWindow(object sender, MouseButtonEventArgs args)
        {
            DragMove();
        }

        public void CloseClicked(object sender, RoutedEventArgs e)
        {
            imageWindow.Close();
            this.Close();
        }

        private void CaptureScreenButtonClick(object sender, RoutedEventArgs e)
        {
            StopUpdatenButtonClick(sender,e);

            // Hide UI when capture
            imageWindow.Hide();
            // Wait for the Hide() to finish
            Thread.Sleep(140);

            this.Hide();

            factor = this.CurrentDPI / 96;

            // Calculate the current client width and hight
            RECT clientRect = GetClientRect(new WindowInteropHelper(this).Handle);
            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;

            TakeScreenShot();
            this.Show();
        }

        private void LiveUpdatenButtonClick(object sender, RoutedEventArgs e)
        {
            // Hide the "LiveUpdateBtn"
            liveUpdateBtn.Visibility = Visibility.Collapsed;
            if (stopUpdateBtn.Visibility == Visibility.Collapsed)
            {
                stopUpdateBtn.Visibility = Visibility.Visible;
            }

            imageWindow.isContinousUpdate = true;

            myTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            myTimer.Start();
        }

        private void StopUpdatenButtonClick(object sender, RoutedEventArgs e)
        {
            // Show the "LiveUpdateBtn"
            stopUpdateBtn.Visibility = Visibility.Collapsed;
            if(liveUpdateBtn.Visibility == Visibility.Collapsed)
            {
                liveUpdateBtn.Visibility = Visibility.Visible;
            }

            imageWindow.isContinousUpdate = false;

            myTimer.Stop();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            factor = this.CurrentDPI / 96;

            TakeScreenShot();
        }

        //helper function
        private void TakeScreenShot()
        {
            // Create an empty Bitmap to store the screen shot
            Bitmap screenshotBmp;
            screenshotBmp = new System.Drawing.Bitmap((int)(myImgPanel.ActualWidth * factor), (int)(myImgPanel.ActualHeight * factor), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Get a graphics context from the empty bitmap
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(screenshotBmp))
            {
                g.CopyFromScreen((int)(this.Left * factor), (int)((this.Top + myTitle.ActualHeight) * factor), 0, 0, screenshotBmp.Size);
            }

            IntPtr handle = IntPtr.Zero;
            try
            {
                // Get the GDI andle for the Bitmap
                handle = screenshotBmp.GetHbitmap();

                // convert from the .NET image format to the WPF image format
                imageWindow.capturedImg.Source = Imaging.CreateBitmapSourceFromHBitmap(handle,
                                                            IntPtr.Zero, Int32Rect.Empty,
                                                            BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
            }
            imageWindow.StoreImage();
            imageWindow.Show();
        }
    }

}
