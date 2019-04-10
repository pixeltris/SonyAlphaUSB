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

        CameraState = 37385,//09 92

        /// <summary>
        /// Image info for the live view / captured photo
        /// </summary>
        GetImageInfo = 4104,//08 10

        /// <summary>
        /// Image data for the live view / captured photo
        /// </summary>
        GetImageData = 4105,//09 10
    }

    public enum SettingIds
    {
        _Unk5004 = 0x5004,//04 50
        _Unk5005 = 0x5005,//05 50

        FNumber = 0x5007,//07 50

        _Unk500A = 0x500A,//0A 50
        _Unk500B = 0x500B,//0B 50
        _Unk500C = 0x500C,//0C 50
        _Unk500E = 0x500E,//0E 50

        EV = 0x5010,//10 50

        _Unk5013 = 0x5013,//13 50

        Flash = 0xD200,//00 D2

        _UnkD201 = 0xD201,//01 D2
        _UnkD203 = 0xD203,//03 D2
        _UnkD20D = 0xD20D,//0D D2
        _UnkD20E = 0xD20E,//0E D2
        _UnkD20F = 0xD20F,//0F D2
        _UnkD210 = 0xD210,//10 D2
        _UnkD211 = 0xD211,//11 D2
        _UnkD212 = 0xD212,//12 D2
        _UnkD213 = 0xD213,//13 D2
        _UnkD214 = 0xD214,//14 D2
        _UnkD215 = 0xD215,//15 D2
        _UnkD217 = 0xD217,//17 D2
        _UnkD218 = 0xD218,//18 D2
        _UnkD219 = 0xD219,//19 D2
        _UnkD21B = 0xD21B,//1B D2
        _UnkD21C = 0xD21C,//1C D2
        _UnkD21D = 0xD21D,//1D D2

        ISO = 0xD21E,//1E D2

        _UnkD21F = 0xD21F,//1F D2
        _UnkD221 = 0xD221,//21 D2
        _UnkD222 = 0xD222,//22 D2
        _UnkD22C = 0xD22C,//2C D2
        _UnkD22D = 0xD22D,//2D D2
        _UnkD22E = 0xD22E,//2E D2
        _UnkD22F = 0xD22F,//2F D2
        _UnkD230 = 0xD230,//30 D2
        _UnkD231 = 0xD231,//31 D2
        _UnkD232 = 0xD232,//32 D2
        _UnkD233 = 0xD233,//33 D2
        _UnkD235 = 0xD235,//35 D2
        _UnkD236 = 0xD236,//36 D2

        /// <summary>
        /// Capture a still image
        /// </summary>
        CapturePhoto1 = 0xD2C1,//C1 D2

        /// <summary>
        /// Capture a still image
        /// </summary>
        CapturePhoto2 = 0xD2C2,//C2 D2

        /// <summary>
        /// The AEL button locks the exposure (AE lock). Use this button to shoot images in the following situations.
        /// - When you want to set the focus and the exposure separately.
        /// - When you want to shoot images continuously with a fixed exposure.
        /// </summary>
        AEL = 0xD2C3,//C3 D2

        _UnkD2C5 = 0xD2C5,//C5 D2
        _UnkD2C7 = 0xD2C7,//C7 D2

        /// <summary>
        /// Start / stop recording videos
        /// </summary>
        RecordVideo = 0xD2C8,//C8 D2

        /// <summary>
        /// The FEL button locks the flash level (FEL lock). Use this button to shoot different subjects with the same brightness.
        /// </summary>
        FEL = 0xD2C9,//C9 D2

        _UnkD2CB = 0xD2CB,//CB D2
        _UnkD2CC = 0xD2CC,//CC D2
        _UnkD2CD = 0xD2CD,//CD D2
        _UnkD2CE = 0xD2CE,//CE D2
        _UnkD2CF = 0xD2CF,//CF D2
        _UnkD2D0 = 0xD2D0,//D0 D2
        _UnkD2D1 = 0xD2D1,//D1 D2
        _UnkD2D2 = 0xD2D2,//D2 D2
        _UnkD2D3 = 0xD2D3,//D3 D2
        _UnkD2D4 = 0xD2D4,//D4 D2
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
