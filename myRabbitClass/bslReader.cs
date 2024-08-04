using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using OpenCvSharp;
using Size = OpenCvSharp.Size;
using System.Drawing;
using System.Reflection;
using Point = OpenCvSharp.Point;
using ZXing.Common;
using ZXing;
using System.Collections.Generic;
using DataMatrix;
using DataMatrix.net;

namespace bslReader
{
    [Guid("6844AACB-9194-46bf-81AF-9DA74EE687DC")]
    internal interface IEventDataMatrixReader
    {
        [DispId(1)]
        int CameraPath(string Path);
        string ReadDMCode(string Path);
    }

    [Guid("70DD7E62-7D82-4301-993C-B7D914330990"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IEventDM
    {
    }

    [Guid("69EE0677-884A-4eeb-A3BD-D407845C0C70"), ClassInterface(ClassInterfaceType.None), ComSourceInterfaces(typeof(IEventDM))]
    public class BslDMReader : IEventDataMatrixReader
    {
        private VideoCapture cam;
        protected VideoCapture _reader = null;
        protected double _fps = 0;
        Mat _image;
        private bool _run = false;
        private VideoCapture _capture;
        private Thread _cameraThread;
        Window win;
        private delegate void SafeCallDelegate(string text);
        private static readonly Assembly CurrentAssembly;

        /// <summary>
        /// путь к камере
        /// </summary>
        public String Path { get; set; }
        
        public int CameraPath(string Path)
        {

            return 0;
        }
    
        public void StopCam()
        {
            _run = false;
            try
            {
                win.Close();
                _capture.Dispose();
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

            
        }
        
        public string ReadDMCode(string pathCam)
        {
            try
            {
                //_reader = new VideoCapture(0);

                _fps = 20;

                if (_fps == 0)
                {
                    _fps = 30;
                }

                //Width = _reader.FrameWidth;
                //Height = _reader.FrameHeight;
                Path = pathCam;
                _run = true;
                StartProcessing();
                
                
                return "ok";
            }
            catch (Exception e)
            {
                throw new COMException("OK" + e.ToString());
            }
            
        }

        /// <summary> Starts capturing and processing video frames. </summary>
        /// <param name="frameGrabDelay"> The frame grab delay. </param>
        /// <param name="timestampFn">    Function to generate the timestamp for each frame. This
        ///     function will get called once per frame. </param>
        protected void StartProcessing()
        {
            _cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
            _cameraThread.Start();

         //   OnProcessingStarted();
        }
        private void CaptureCameraCallback()
        {

            //Mat src = new Mat();
            //FrameSource frame = Cv2.CreateFrameSource_Camera(0);
            //frame.NextFrame(src);
               
            _capture = new VideoCapture(int.Parse(Path));
            _image = new Mat();
            win = new Window(); 
            //win2 = new Window();
            while (_run)
            {
                //Cv2.CreateFrameSource_Camera(0);
                if (!_run) break;
                var startTime = DateTime.Now;

                _capture.Read(_image);
                if (_image.Empty()) return;
                var imageRes = new Mat();
               Cv2.Resize(_image, imageRes, new Size(640, 380));
               //Cv2.Canny(imageRes, _image, 50, 200);
               win.ShowImage(imageRes);
               //win2.ShowImage(_image);
                //using (new Window("src image", imageRes))
                Cv2.WaitKey(5);
                //using (new Window("dst image", _image))
                // {
                //   Cv2.WaitKey();
                // }

                //var bmpWebCam = BitmapConverter.ToBitmap(imageRes);
                //pictureBoxWebCam.Image = bmpWebCam;
            }
        }

        private static string detectBarcode(string fileName, double thresh, bool debug = false, double rotation = 0)
        {
            Console.WriteLine("\nProcessing: {0}", fileName);

            // load the image and convert it to grayscale
            var image = new Mat(fileName);

            if (rotation != 0)
            {
                rotateImage(image, image, rotation, 1);
            }

            if (debug)
            {
                Cv2.ImShow("Source", image);
                Cv2.WaitKey(1); // do events
            }

            var gray = new Mat();
            var channels = image.Channels();
            if (channels > 1)
            {
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGRA2GRAY);
            }
            else
            {
                image.CopyTo(gray);
            }


            // compute the Scharr gradient magnitude representation of the images
            // in both the x and y direction
            var gradX = new Mat();
            Cv2.Sobel(gray, gradX, MatType.CV_32F, xorder: 1, yorder: 0, ksize: -1);
            //Cv2.Scharr(gray, gradX, MatType.CV_32F, xorder: 1, yorder: 0);

            var gradY = new Mat();
            Cv2.Sobel(gray, gradY, MatType.CV_32F, xorder: 0, yorder: 1, ksize: -1);
            //Cv2.Scharr(gray, gradY, MatType.CV_32F, xorder: 0, yorder: 1);

            // subtract the y-gradient from the x-gradient
            var gradient = new Mat();
            Cv2.Subtract(gradX, gradY, gradient);
            Cv2.ConvertScaleAbs(gradient, gradient);

            if (debug)
            {
                Cv2.ImShow("Gradient", gradient);
                Cv2.WaitKey(1); // do events
            }


            // blur and threshold the image
            var blurred = new Mat();
            Cv2.Blur(gradient, blurred, new Size(9, 9));

            var threshImage = new Mat();
            Cv2.Threshold(blurred, threshImage, thresh, 255, ThresholdTypes.Binary);

            if (debug)
            {
                Cv2.ImShow("Thresh", threshImage);
                Cv2.WaitKey(1); // do events
            }


            // construct a closing kernel and apply it to the thresholded image
            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(21, 7));
            var closed = new Mat();
            Cv2.MorphologyEx(threshImage, closed, MorphTypes.Close, kernel);

            if (debug)
            {
                Cv2.ImShow("Closed", closed);
                Cv2.WaitKey(1); // do events
            }


            // perform a series of erosions and dilations
            Cv2.Erode(closed, closed, null, iterations: 4);
            Cv2.Dilate(closed, closed, null, iterations: 4);

            if (debug)
            {
                Cv2.ImShow("Erode & Dilate", closed);
                Cv2.WaitKey(1); // do events
            }


            //find the contours in the thresholded image, then sort the contours
            //by their area, keeping only the largest one

            Point[][] contours;
            HierarchyIndex[] hierarchyIndexes;
            Cv2.FindContours(
                closed,
                out contours,
                out hierarchyIndexes,
                mode: RetrievalModes.CComp,
                method: ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                throw new NotSupportedException("Couldn't find any object in the image.");
            }

            var contourIndex = 0;
            var previousArea = 0;
            var biggestContourRect = Cv2.BoundingRect(contours[0]);
            while ((contourIndex >= 0))
            {
                var contour = contours[contourIndex];

                var boundingRect = Cv2.BoundingRect(contour); //Find bounding rect for each contour
                var boundingRectArea = boundingRect.Width * boundingRect.Height;
                if (boundingRectArea > previousArea)
                {
                    biggestContourRect = boundingRect;
                    previousArea = boundingRectArea;
                }

                contourIndex = hierarchyIndexes[contourIndex].Next;
            }

            var barcode = new Mat(image, biggestContourRect); //Crop the image
            Cv2.CvtColor(barcode, barcode, ColorConversionCodes.BGRA2GRAY);

            Cv2.ImShow("Barcode", barcode);
            Cv2.WaitKey(1); // do events

            var barcodeClone = barcode.Clone();
            var barcodeText = getBarcodeText(barcodeClone);

            if (string.IsNullOrWhiteSpace(barcodeText))
            {
                Console.WriteLine("Enhancing the barcode...");
                //Cv2.AdaptiveThreshold(barcode, barcode, 255,
                //AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 9, 1);
                //var th = 119;
                var th = 100;
                Cv2.Threshold(barcode, barcode, th, 255, ThresholdTypes.Tozero);
                Cv2.Threshold(barcode, barcode, th, 255, ThresholdTypes.Binary);
                barcodeText = getBarcodeText(barcode);
            }

            if (debug)
            {
                Cv2.ImShow("Segmented Source", image);
                Cv2.WaitKey(1); // do events
            }

            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();

            return barcodeText;
        }

        private static void rotateImage(Mat src, Mat dst, double angle, double scale)
        {
            var imageCenter = new Point2f(src.Cols / 2f, src.Rows / 2f);
            var rotationMat = Cv2.GetRotationMatrix2D(imageCenter, angle, scale);
            Cv2.WarpAffine(src, dst, rotationMat, src.Size());
        }

        private static string getBarcodeText(Mat barcode)
        {
            // `ZXing.Net` needs a white space around the barcode
            Mat barcodeWithWhiteSpace = new Mat(new Size(barcode.Width + 30, barcode.Height + 30), MatType.CV_8U, Scalar.White);
            var drawingRect = new Rect(new Point(15, 15), new Size(barcode.Width, barcode.Height));
            var roi = barcodeWithWhiteSpace[drawingRect];
            barcode.CopyTo(roi);

            Cv2.ImShow("Enhanced Barcode", barcodeWithWhiteSpace);
            Cv2.WaitKey(1); // do events
            return "";
           // return decodeBarcodeText(Bitmap. barcodeWithWhiteSpace.);
            //return decodeBarcodeText(barcodeWithWhiteSpace.ToBitmap());
        }
        private static string decodeBarcodeText(System.Drawing.Bitmap barcodeBitmap)
        {
            var source = new BitmapLuminanceSource(barcodeBitmap);

            // using http://zxingnet.codeplex.com/
            // PM> Install-Package ZXing.Net
            var reader = new BarcodeReader(null, null, ls => new GlobalHistogramBinarizer(ls))
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    //PureBarcode = true,
                    /*PossibleFormats = new List<BarcodeFormat>
                    {
                        BarcodeFormat.CODE_128
                        //BarcodeFormat.EAN_8,
                        //BarcodeFormat.CODE_39,
                        //BarcodeFormat.UPC_A
                    }*/
                }
            };

            //var newhint = new KeyValuePair<DecodeHintType, object>(DecodeHintType.ALLOWED_EAN_EXTENSIONS, new Object());
            //reader.Options.Hints.Add(newhint);

            var result = reader.Decode(source);
            if (result == null)
            {
                Console.WriteLine("Decode failed.");
                return string.Empty;
            }

            Console.WriteLine("BarcodeFormat: {0}", result.BarcodeFormat);
            Console.WriteLine("Result: {0}", result.Text);


            var writer = new BarcodeWriter
            {
                Format = result.BarcodeFormat,
                Options = { Width = 200, Height = 50, Margin = 4 },
                Renderer = new ZXing.Rendering.BitmapRenderer()
            };
            //Mat barcodeImage = writer.Write(result.Text);
            // Cv2.ImShow("BarcodeWriter", );

            return result.Text;
        }
    } 
}
