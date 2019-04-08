using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SonyAlphaUSB.Interop;

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
            //WIALogger.Run();
            //return;

            SonyCamera[] cameras = WIA.FindCameras();
            foreach (SonyCamera camera in cameras)
            {
                if (camera.Connect())
                {
                    //camera.CapturePhoto();
                    /*while (true)
                    {
                        camera.ModifyFNumber(-100);
                        System.Threading.Thread.Sleep(5000);
                        camera.ModifyFNumber(100);
                        System.Threading.Thread.Sleep(5000);
                    }*/
                }
            }
        }
    }
}