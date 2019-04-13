using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SonyAlphaUSB
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "wlog":
                        WIALogger.Run();
                        return;
                }
            }

            List<SonyCamera> cameras = WIA.FindCameras().ToList();
            foreach (SonyCamera camera in new List<SonyCamera>(cameras))
            {
                if (!camera.Connect())
                {
                    cameras.Remove(camera);
                }
                else
                {
                    //camera.CapturePhoto();
                    //camera.SetFocusMode(FocusMode.MF);
                    //camera.SetFocusMode(FocusModeToggle.Manual);
                    //camera.SetWhiteBalanceAB(WhiteBalanceAB.B20);
                    //camera.SetWhiteBalanceGM(WhiteBalaceGM.G250);
                    //camera.SetWhiteBalanceColorTemp(5000);
                    //camera.SetMeteringMode(MeteringMode.SpotLarge);
                }
            }

            Stopwatch stopwatch = new Stopwatch();
            int updateDelay = 41;// 24fps
            //int updateDelay = 33;// 30fps

            while (true)
            {
                stopwatch.Restart();

                foreach (SonyCamera camera in cameras)
                {
                    camera.Update();
                }

                while (stopwatch.ElapsedMilliseconds < updateDelay)
                {
                    // This may result in stuttering as sleep can take longer than requested (maybe use a Timer?)
                    System.Threading.Thread.Sleep(1);
                }
            }
        }
    }
}