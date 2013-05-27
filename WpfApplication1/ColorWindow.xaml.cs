using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace WpfApplication1
{

    public partial class ColorWindow : Window
    {
        KinectSensor kinect;
        public ColorWindow(KinectSensor sensor) : this()
        {
            kinect = sensor;
        }

        public ColorWindow()
        {
            InitializeComponent();
            Loaded += ColorWindow_Loaded;
            Unloaded += ColorWindow_Unloaded;
        }
        void ColorWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.DepthStream.Disable();
                kinect.SkeletonStream.Disable();
                kinect.AllFramesReady -= mykinect_AllFramesReady;
                kinect.Stop();
            }
        }
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private WriteableBitmap _DepthImageBitmap;
        private Int32Rect _DepthImageBitmapRect;
        private int _DepthImageStride;
        void ColorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                ColorImageStream colorStream = kinect.ColorStream;
                kinect.ColorStream.Enable();
                _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,
                    colorStream.FrameHeight, 96, 96,
                    PixelFormats.Bgra32, null);
                _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,colorStream.FrameHeight);
                _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorData.Source = _ColorImageBitmap;

                DepthImageStream depthStream = kinect.DepthStream;
                //kinect.DepthStream.Enable();  
                //降低深度影像解析度
                kinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30); 

                _DepthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _DepthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _DepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                DepthData.Source = _DepthImageBitmap;

                kinect.SkeletonStream.Enable();

                kinect.AllFramesReady += mykinect_AllFramesReady;

                kinect.Start();
            }
        }

        DepthImageFrame depthframe;
        short[] depthpixelData;
        DepthImagePixel[] depthPixel; 
        ColorImageFrame colorframe;
        byte[] colorpixelData;
        void mykinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            depthframe = e.OpenDepthImageFrame();          
            colorframe = e.OpenColorImageFrame();

            if (depthframe == null || colorframe == null)
                return;

            depthpixelData = new short[depthframe.PixelDataLength];                
            depthframe.CopyPixelDataTo(depthpixelData);
            _DepthImageBitmap.WritePixels(_DepthImageBitmapRect, depthpixelData, _DepthImageStride, 0);
            
            depthPixel = new DepthImagePixel[depthframe.PixelDataLength];
            depthframe.CopyDepthImagePixelDataTo(depthPixel);
            
            colorpixelData = new byte[colorframe.PixelDataLength];
            colorframe.CopyPixelDataTo(colorpixelData);
            
            if (depthpixelData != null)
                UserFilter();
            
            _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, colorpixelData, _ColorImageStride, 0);
            
            depthframe.Dispose();
            colorframe.Dispose();
        }


        ColorImagePoint[] colorpoints;
        void UserFilter()
        {
            colorpoints = new ColorImagePoint[depthpixelData.Length] ;
            kinect.CoordinateMapper.MapDepthFrameToColorFrame(
                depthframe.Format, 
                depthPixel, 
                colorframe.Format, 
                colorpoints);

            for (int i = 0; i < depthpixelData.Length; i++)
            {
                PixelInRange(i);
            }
        }

        //深度攝影機解析度為 640x480版本
        //void PixelInRange(int i)
        //{
        //    int playerIndex = depthPixel[i].PlayerIndex;

        //    if (playerIndex > 0)
        //    {
        //        ColorImagePoint p = colorpoints[i];
        //        int colorimageindex = (p.X + (p.Y * colorframe.Width)) * colorframe.BytesPerPixel;
        //        colorpixelData[colorimageindex + 3] = 0xFF;
        //    }
        //}

        //深度攝影機解析度為 320x240版本
        void PixelInRange(int i)
        {
            int playerIndex = depthPixel[i].PlayerIndex;

            if (playerIndex > 0)
            {
                ColorImagePoint p = colorpoints[i];
                int colorimageindex = (p.X + (p.Y * colorframe.Width)) * colorframe.BytesPerPixel;

                colorpixelData[colorimageindex + 3] = 0xFF; //(X,Y)
                colorpixelData[colorimageindex + colorframe.BytesPerPixel + 3] = 0xFF; //(X+1,Y)

                int nextcolorimageindex = (p.X + ((p.Y + 1) * colorframe.Width)) * colorframe.BytesPerPixel;
                colorpixelData[nextcolorimageindex + 3] = 0xFF; //(X,Y+1)
                colorpixelData[nextcolorimageindex + colorframe.BytesPerPixel + 3] = 0xFF; //(X+1,Y+1)
            }
        }

    }
}
