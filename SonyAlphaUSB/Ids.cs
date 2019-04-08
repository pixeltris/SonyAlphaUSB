using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SonyAlphaUSB
{
    // Are these big endian? Or an op/sub op?
    public enum OpCodes
    {
        Connect = 37377,//01 92
        
        /// <summary>
        /// The response contains a list of available main/sub settings (just their ids)
        /// </summary>
        SettingsList = 37378,//02 92
        
        MainSetting = 37383,//07 92
        SubSetting = 37381,//05 92

        /// <summary>
        /// Image info for the live view / captured photo
        /// </summary>
        GetImageInfo = 4104,//08 10

        /// <summary>
        /// Image data for the live view / captured photo
        /// </summary>
        GetImageData = 4105,//09 10
    }

    public enum MainSettingIds
    {
        FNumber = 20487,//07 50
        EV = 20496,//10 50
        Flash = 53760,//00 D2
        ISO = 53790,//1E D2

        /// <summary>
        /// The AEL button locks the exposure (AE lock). Use this button to shoot images in the following situations.
        /// - When you want to set the focus and the exposure separately.
        /// - When you want to shoot images continuously with a fixed exposure.
        /// </summary>
        AEL = 53955,//C3 D2

        /// <summary>
        /// The FEL button locks the flash level (FEL lock). Use this button to shoot different subjects with the same brightness.
        /// </summary>
        FEL = 53961,//C9 D2

        /// <summary>
        /// Capture a still image
        /// </summary>
        CapturePhoto1 = 53953,//C1 D2

        /// <summary>
        /// Capture a still image
        /// </summary>
        CapturePhoto2 = 53954,//C2 D2

        /// <summary>
        /// Start / stop recording videos
        /// </summary>
        RecordVideo = 53960,//C8 D2
    }

    public enum SubSettingIds
    {
    }
    
    // Sony A7III main setting ids
    //04 50
    //05 50
    //07 50
    //0A 50
    //0B 50
    //0C 50
    //0E 50
    //10 50
    //13 50
    //00 D2
    //01 D2
    //03 D2
    //0D D2
    //0E D2
    //0F D2
    //11 D2
    //13 D2
    //1E D2
    //1B D2
    //1D D2
    //1F D2
    //17 D2
    //18 D2
    //19 D2
    //12 D2
    //10 D2
    //1C D2
    //22 D2
    //2C D2
    //2D D2
    //2E D2
    //2F D2
    //30 D2
    //31 D2
    //32 D2
    //33 D2
    //35 D2
    //36 D2
    //21 D2
    //14 D2
    //15 D2
    //20 D2

    // Sony A7III sub setting ids
    //C1 D2
    //C2 D2
    //C3 D2
    //C9 D2
    //C8 D2
    //C5 D2
    //C7 D2
    //CB D2
    //CC D2
    //CD D2
    //CE D2
    //CF D2
    //D0 D2
    //D2 D2
    //D3 D2
    //D4 D2
    //D1 D2
}
