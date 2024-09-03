using OpenCvSharp;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
using FFmpeg.AutoGen;
using System.Text.RegularExpressions;

namespace bslCamViewer
{
    [Guid("6844AACB-9194-46bf-81AF-9DA74EE687DC")]
    internal interface IEventCamViewer
    {
        [DispId(1)]
        int CameraPath(string Path);
        bool InitCapture(string codeс);
        void SetSizeFrameView(int Width, int Height);
        void SetSizeFrameRecord(int Width, int Height);
        void SetStringWaterMark(string TextRecord);
        void SetLenPartVideo(int CountSeconds);

        string VersionDLL();

        string StartCam(string Path);
        string StopCam();

        void StartRecord(string pathFile);
        void StopRecord();
    }

    [Guid("70DD7E62-7D82-4301-993C-B7D914330990"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IEventDM
    {
    }

    [Guid("69EE0677-884A-4eeb-A3BD-D407845C0C70"), ClassInterface(ClassInterfaceType.None), ComSourceInterfaces(typeof(IEventDM))]
    public class BslCamViewer : IEventCamViewer
    {
        protected VideoCapture _reader = null;
        protected double _fps = 0;
        Mat _image;
        private bool _run = false;
        private VideoCapture _capture;
        private Thread _cameraThread;
        Window win;

        private delegate void SafeCallDelegate(string text);

        private int FrameHeightView = 640;
        private int FrameWidthView = 480;

        private int FrameHeightRecord = 640;
        private int FrameWidthRecord = 480;


        private VideoWriter _videoWriter;
        private string _outputFile = "output";
        private bool _isRecording = false;
        private string codec = "MJPG";

        private string WaterMark = "";
        private int LenChankPartVideo = 0;
        private DateTime startRecordTime;
        private int currPartVideo = 0;
        private static string extVideo = "avi";

        /// <summary>
        /// путь к камере
        /// </summary>
        public String Path { get; set; }

        public string VersionDLL()
        {
            return "1.0.10";
        }

        public bool InitCapture(string codeсk)
        {

            this.codec = codeсk;

            return true;
        }

        public int CameraPath(string Path)
        {
            return 0;
        }

        public void SetSizeFrameView(int Width, int Height)
        {
            FrameWidthView = Width;
            FrameHeightView = Height;
        }

        public void SetSizeFrameRecord(int Width, int Height)
        {
            FrameWidthRecord = Width;
            FrameHeightRecord = Height;
        }

        public void SetStringWaterMark(string TextRecord)
        {
            WaterMark = TextRecord;
        }

        public string StopCam()
        {
            _run = false;
            try
            {
                if (_videoWriter != null)
                {
                    _videoWriter.Release();
                }

                if (_capture != null)
                {
                    _capture.Dispose();
                    win.Close();
                }

                return "";
            }
            catch (Exception e)
            {
                throw new COMException("OK" + e.ToString());
            }

        }

        public string StartCam(string pathVideoSource)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("C:\\log\\log_CamDLL.log", rollingInterval: RollingInterval.Hour)
                .CreateLogger();

            try
            {
                _fps = 25;
                Path = pathVideoSource;
                _run = true;
                StartProcessing();
                Log.Information("start camera ver " + VersionDLL());

                return "start camera ver " + VersionDLL();
            }
            catch (Exception e)
            {
                Log.Error("StartCam error " + e.ToString());
                throw new COMException("OK" + e.ToString());
            }
        }

        public void StartRecord(string pathFile)
        {
            try
            {
                _outputFile = pathFile;
                _isRecording = true;
                string recordNameFile = FileNameVideo(_outputFile, extVideo, currPartVideo);
                Log.Information("record to " + recordNameFile);
                if (LenChankPartVideo != 0)
                {
                    startRecordTime = DateTime.Now;
                    currPartVideo = 0;
                    recordNameFile = FileNameVideo(_outputFile, extVideo, currPartVideo);
                };

                _videoWriter = new VideoWriter(recordNameFile, SetupCodec(), _fps, new Size(this.FrameHeightRecord, this.FrameWidthRecord), true);

            }
            catch (Exception e)
            {
                _isRecording = false;
                Log.Error("StartRecord " + e.ToString());
                throw new COMException("OK" + e.ToString());
            }

        }

        public void StopRecord()
        {
            try
            {
                _isRecording = false;
                if (_videoWriter != null)
                {
                    _videoWriter.Release();
                }
                Log.Information("StopRecord OK");
            }
            catch (Exception e)
            {
                _isRecording = false;
                Log.Error("StopRecord " + e.ToString());
                throw new COMException("OK" + e.ToString());
            }
        }

        public void SetLenPartVideo(int CountSeconds)
        {
            this.LenChankPartVideo = CountSeconds;
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
            try
            {
                if (Path == "0")
                {
                    _capture = new VideoCapture(int.Parse(Path), VideoCaptureAPIs.ANY);
                }
                else if (Path == "1")
                {
                    string npath = "RTSP://user:password@127.0.0.0:554/Streaming/Channels/1";
                    _capture = new VideoCapture(npath);
                }
                else
                {
                    _capture = new VideoCapture(Path);
                };

                Log.Information("Init capture " + Path);

                int time = Convert.ToInt32(Math.Round(1000 / 15.0));
                
                _image = new Mat();
                win = new Window(Path);
                var imageRes = new Mat();
                string text;
                
                while (_run)
                {
                    Thread.Sleep(time);
                    if (!_run) break;
                    //var startTime = DateTime.Now;
                    if (!_capture.IsOpened()){
                        continue;
                    }
                    _capture.Read(_image);
                    //Task.Delay(10).Wait();
                    if (_image.Empty())
                    {
                        Log.Information("_image Empty");

                        continue;
                        //return;
                    }
                    if (_isRecording && _videoWriter != null)
                    {
                        _videoWriter.Write(_image);
                        if (LenChankPartVideo != 0)
                        {
                            if (((int)(DateTime.Now - startRecordTime).TotalSeconds) > LenChankPartVideo)
                            {
                                _videoWriter.Release();
                                currPartVideo++;
                                startRecordTime = DateTime.Now;
                                Log.Information("record new file " + FileNameVideo(_outputFile, extVideo, currPartVideo));
                                _videoWriter = new VideoWriter(FileNameVideo(_outputFile, extVideo, currPartVideo), SetupCodec(), _fps, new Size(this.FrameHeightRecord, this.FrameWidthRecord), true);

                            };
                        };
                    }

                    try
                    {
                        Cv2.Resize(_image, imageRes, new Size(this.FrameHeightView, this.FrameWidthView));
                    }
                    catch (Exception e)
                    {
                        _isRecording = false;
                        Log.Error(e.ToString());
                        throw new COMException("OK" + e.ToString());
                    }

                    if (_isRecording)
                    {
                        text = "REC";
                    }
                    else
                    {
                        text = "STOP REC";
                    }
                    try
                    {
                        Cv2.PutText(imageRes,
                                WaterMark + "_" + text + "_" + DateTime.Now.ToString(),
                                new Point(10, 50),
                                HersheyFonts.HersheySimplex, 1.0, Scalar.Red, 2);
                        win.ShowImage(imageRes);

                    }
                    catch (Exception e)
                    {
                        Log.Error("Show error " + e.ToString());
                    }

                    var k = Cv2.WaitKey(27);

                    if (k == 27)
                    {
                        Log.Information("exit from escape");
                        break;

                    }


                }

            }
            catch (Exception e)
            {
                _isRecording = false;
                Log.Error("StopRecord " + e.ToString());
                throw new COMException("OK" + e.ToString());
            }
        }


        protected void StartProcessingFFmpeg()
        {
            _cameraThread = new Thread(new ThreadStart(CaptureCameraFFmpeg));
            _cameraThread.Start();
        }

        private void CaptureCameraFFmpeg()
        {

        }

 
        private int SetupCodec()
        {
            if (this.codec == "H264")
            {
                return VideoWriter.FourCC('H', '2', '6', '4');
            }
            else if (this.codec == "XVID")
            {
                return VideoWriter.FourCC('X', 'V', 'I', 'D');
            }
            else if (this.codec == "mp4v")
            {
                return VideoWriter.FourCC('M', 'P', '4', 'V');
            }

            return VideoWriter.FourCC('M', 'J', 'P', 'G');
        }

        private string FileNameVideo(string basePath, string extentionFile, int part)
        {
            if (LenChankPartVideo > 0)
            {
                return basePath + ".part_" + part.ToString() + "." + extentionFile;
            }
            else
            {
                return basePath + "." + extentionFile;
            }
        }

    }

}
