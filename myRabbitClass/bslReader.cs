using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using OpenCvSharp;
using Size = OpenCvSharp.Size;
using System.Drawing;
using System.Reflection;
using Point = OpenCvSharp.Point;

namespace bslReader
{
    [Guid("6844AACB-9194-46bf-81AF-9DA74EE687DC")]
    internal interface IEventDataMatrixReader
    {
        [DispId(1)]
        int CameraPath(string Path);
        
        string StartCam(string Path);
        string StopCam();

        void SetSizeFrame(int Width, int Height);
        void StartRecord(string pathFile);
        void StopRecord();
    }

    [Guid("70DD7E62-7D82-4301-993C-B7D914330990"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IEventDM
    {
    }

    [Guid("69EE0677-884A-4eeb-A3BD-D407845C0C70"), ClassInterface(ClassInterfaceType.None), ComSourceInterfaces(typeof(IEventDM))]
    public class BslDMReader : IEventDataMatrixReader
    {
        //private VideoCapture cam;
        protected VideoCapture _reader = null;
        protected double _fps = 0;
        Mat _image;
        private bool _run = false;
        private VideoCapture _capture;
        private Thread _cameraThread;
        Window win;
        private delegate void SafeCallDelegate(string text);
        //private static readonly Assembly CurrentAssembly;
        private int FrameWidth =  720;
        private int FrameHeight = 1280;
        private VideoWriter _videoWriter;
        private string _outputFile = "output.avi";
        private bool _isRecording = false;



        /// <summary>
        /// путь к камере
        /// </summary>
        public String Path { get; set; }
        
        public int CameraPath(string Path)
        {
            return 0;
        }
    
        public void SetSizeFrame(int Width, int Height)
        {
            FrameWidth = Width;
            FrameHeight = Height;
        }

        public string StopCam()
        {
            _run = false;
            try
            {
                if (_videoWriter != null)
                {
                    _videoWriter.Dispose();
                }

                if (_capture != null)
                {
                    _capture.Dispose();
                    win.Close();

                }



                return "";
            }
            catch (Exception e) {
                return e.ToString();
            }

            
        }
        
        public string StartCam(string pathSource)
        {
            try
            {
                _fps = 20;

                if (_fps == 0)
                {
                    _fps = 30;
                }

                Path = pathSource;
                _run = true;
                StartProcessing();


                return "ok";
            }
            catch (Exception e)
            {
                throw new COMException("OK" + e.ToString());
            }
        }

        public void StartRecord(string pathFile)
        {
            _outputFile = pathFile;
            _isRecording = true;
            _videoWriter = new VideoWriter(_outputFile, VideoWriter.FourCC('M', 'J', 'P', 'G'), _fps, new Size(this.FrameHeight, this.FrameWidth), true);
        }

        public void StopRecord()
        {
            _isRecording = false;
            _videoWriter.Dispose();
        }

  
        /// <summary> Starts capturing and processing video frames. </summary>
        /// <param name="frameGrabDelay"> The frame grab delay. </param>
        /// <param name="timestampFn">    Function to generate the timestamp for each frame. This
        ///     function will get called once per frame. </param>
        protected void StartProcessing()
        {
            _cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
            _cameraThread.Start();
        }
        private void CaptureCameraCallback()
        {

            _capture = new VideoCapture(int.Parse(Path));
            _image = new Mat();
            win = new Window(); 

            while (_run)
            {
                if (!_run) break;
                //var startTime = DateTime.Now;

               _capture.Read(_image);
               if (_image.Empty()) return;
                var imageRes = new Mat();

               Cv2.Resize(_image, imageRes, new Size(this.FrameHeight, this.FrameWidth));
                if (_isRecording)
                {
                    _videoWriter.Write(_image);
                }

               win.ShowImage(imageRes);
               Cv2.WaitKey(5);
            }
        }
        
    } 
}
//