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

        Settings = 37385,//09 92

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
        /// <summary>
        /// File format / image quality
        /// </summary>
        FileFormat = 0x5004,//04 50

        WhiteBalance = 0x5005,//05 50

        FNumber = 0x5007,//07 50

        FocusMode = 0x500A,//0A 50
        
        _Unk500B = 0x500B,//0B 50
        _Unk500C = 0x500C,//0C 50

        ShootingMode = 0x500E,//0E 50

        EV = 0x5010,//10 50

        _Unk5013 = 0x5013,//13 50

        Flash = 0xD200,//00 D2

        /// <summary>
        /// DRO/Auto HDR (Dynamic-Range Optimizer / Auto High Dynamic Range)
        /// </summary>
        DroHdr = 0xD201,//01 D2

        /// <summary>
        /// Image size (JPEG Image Size)
        /// </summary>
        ImageSize = 0xD203,//03 D2
        
        ShutterSpeed = 0xD20D,//0D D2

        _UnkD20E = 0xD20E,//0E D2
        _UnkD20F = 0xD20F,//0F D2
        _UnkD210 = 0xD210,//10 D2

        AspectRatio = 0xD211,//11 D2

        _UnkD212 = 0xD212,//12 D2

        /// <summary>
        /// Green focus state icon (circle / rings icon)
        /// </summary>
        FocusState = 0xD213,//13 D2

        _UnkD214 = 0xD214,//14 D2
        _UnkD215 = 0xD215,//15 D2

        // This is the actual state (on/off) as opposed to the AEL button in the Remote UI
        AEL_State = 0xD217,//17 D2

        BatteryInfo = 0xD218,//18 D2

        _UnkD219 = 0xD219,//19 D2

        PictureEffect = 0xD21B,//1B D2

        _UnkD21C = 0xD21C,//1C D2
        _UnkD21D = 0xD21D,//1D D2

        ISO = 0xD21E,//1E D2

        _UnkD21F = 0xD21F,//1F D2

        LiveViewState = 0xD221,//21 D2

        _UnkD222 = 0xD222,//22 D2

        FocusArea = 0xD22C,//2C D2

        _UnkD22D = 0xD22D,//2D D2
        _UnkD22E = 0xD22E,//2E D2
        _UnkD22F = 0xD22F,//2F D2
        _UnkD230 = 0xD230,//30 D2
        _UnkD231 = 0xD231,//31 D2

        FocusAreaSpot = 0xD232,//32 D2

        /// <summary>
        /// Something related to focus? Seems to be 00 when AF-C, otherwise 01
        /// 02 00 00 00 00 00 02 02 00 00 01 - AF-C
        /// 02 00 00 01 00 01 02 02 00 00 01 - all other focus modes
        /// </summary>
        _UnkD233 = 0xD233,//33 D2

        /// <summary>
        /// Signifies manual focus?
        /// 02 00 00 01 00 01 02 02 00 00 01 - MF mode
        /// 02 00 00 00 00 00 02 02 00 00 01 - AF mode
        /// </summary>
        _UnkD235 = 0xD235,//35 D2
        _UnkD236 = 0xD236,//36 D2

        /// <summary>
        /// Half press the shutter buttton (AF button on the Remote UI)
        /// </summary>
        HalfPressShutter = 0xD2C1,//C1 D2

        /// <summary>
        /// Capture a still image
        /// </summary>
        CapturePhoto = 0xD2C2,//C2 D2

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

        /// <summary>
        /// Used to toggle AF/MF (1=MF, 2=AF).
        /// Used to switch between MF/AF without modifying the current AF setting
        /// </summary>
        FocusModeToggle = 0xD2D2,//D2 D2

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

    public enum FocusAreaIds
    {
        Wide = 0x0001,
        Zone = 0x0002,
        Center = 0x0003,

        FlexibleSpotS = 0x0101,
        FlexibleSpotM = 0x0102,
        FlexibleSpotL = 0x0103,
        ExpandFlexibleSpot = 0x0104,

        LockOnAF_Wide = 0x0201,
        LockOnAF_Zone = 0x0202,
        LockOnAF_Center = 0x0203,

        LockOnAF_FlexibleSpotS = 0x0204,
        LockOnAF_FlexibleSpotM = 0x0205,
        LockOnAF_FlexibleSpotL = 0x0206,
        LockOnAF_ExpandFlexibleSpot = 0x0207,
    }

    public enum FocusState
    {
        // TODO: Find out what 7 means

        /// <summary>
        /// Focus state is inactive (no green circle / rings on the screen)
        /// </summary>
        Inactive = 1,
        /// <summary>
        /// Solid green circle
        /// </summary>
        Focused = 2,
        /// <summary>
        /// Blinking solid green circle (the camera cannot focus on the selected subject)
        /// </summary>
        FocusFailed = 3,
        /// <summary>
        /// Green rings
        /// </summary>
        Searching = 5,
        /// <summary>
        /// Solid green circle with green rings
        /// </summary>
        FocusedAndSearching = 6,
    }

    [Flags]
    public enum ImageFileFormat
    {
        JpegStandard = 0x02,
        JpegFine = 0x03,
        JpegXFine = 0x04,

        Raw = 0x10
    }

    public enum ImageSize
    {
        // These are defined by the list in "03 D2"
        Large = 1,
        Medium = 2,
        Small = 3
    }

    public enum PictureEffect
    {
        Off = 0x8000,
        ToyCamera_Normal = 0x8001,
        ToyCamera_Cool = 0x8002,
        ToyCamera_Warm = 0x8003,
        ToyCamera_Green = 0x8004,
        ToyCamera_Magenta = 0x8005,

        PopColor = 0x8010,

        Posterization_BW = 0x8020,
        Posterization_Color = 0x8021,

        RetroPhoto = 0x8030,
        SoftHighKey = 0x8040,

        PartialColor_Red = 0x8050,
        PartialColor_Green = 0x8051,
        PartialColor_Blue = 0x8052,
        PartialColor_Yellow = 0x8053,

        HighContrastMono = 0x8060,
        RichToneMono = 0x8090
    }

    /// <summary>
    /// DRO/Auto HDR (Dynamic-Range Optimizer / Auto High Dynamic Range)
    /// </summary>
    public enum DroHdr : byte
    {
        DrOff = 0x01,
        
        DroLv1 = 0x11,
        DroLv2 = 0x12,
        DroLv3 = 0x13,
        DroLv4 = 0x14,
        DroLv5 = 0x15,
        DroAuto = 0x1F,

        HdrAuto = 0x20,
        Hdr1Ev = 0x21,
        Hdr2Ev = 0x22,
        Hdr3Ev = 0x23,
        Hdr4Ev = 0x24,
        Hdr5Ev = 0x25,
        Hdr6Ev = 0x26
    }

    public enum AspectRatio : byte
    {
        Ar3_2 = 1,
        Ar16_9 = 2
    }

    public enum FocusMode
    {
        MF = 0x0001,
        AF_SingleShot = 0x0002,
        AF_Continuous = 0x8004,
        AF_Automatic = 0x8005,
        DMF = 0x8006,
        //Unknown = 0x8009,// Not sure what this value is, seems to be listed but unused by A7III
    }

    /// <summary>
    /// Used to switch between MF/AF without modifying the current AF setting
    /// </summary>
    public enum FocusModeToggle
    {
        Manual = 1,
        Auto = 2
    }

    public enum ShootingMode : ushort
    {
        //00 80 - AUTO
        //02 00 - P
        //03 00 
        //04 00 - S
        //01 00 - M 
        //50 80 
        //51 80 - Movie
        //52 80 
        //53 80 
        //84 80 - S&Q
        //85 80 
        //86 80 
        //87 80 
        //07 00 - SCN
        //11 80 
        //15 80 
        //14 80 
        //12 80 
        //13 80 
        //17 80

        M = 0x0001,
        P = 0x0002,
        A = 0x0003,
        S = 0x0004,
        SCN = 0x0007,

        AUTO = 0x8000,
        Movie = 0x8051,
        SQ = 0x8084//S&Q
    }

    public enum WhiteBalance
    {
        //02 00 - Auto
        //04 00 - Daylight
        //11 80 - Shade
        //10 80 - Cloudy
        //06 00 - Incandescent
        //01 80 - Fluor: Warm White
        //02 80 - Fluor: Cool White
        //03 80 - Fluor: Day White
        //04 80 - Fluor: Daylight
        //07 00 - Flash
        //30 80 - Underwater Auto
        //12 80 - C.Temp/Filter
        //20 80 - Custom 1
        //21 80 - Custom 2
        //22 80 - Custom 3

        Auto = 0x0002,
        Daylight = 0x0004,
        Incandescent = 0x0006,
        Flash = 0x0007,

        Fluor_WarmWhite = 0x8001,
        Fluor_CoolWhite = 0x8002,
        Fluor_DayWhite = 0x8003,
        Fluor_Daylight = 0x8004,

        Cloudy = 0x8010,
        Shade = 0x8011,
        CTempFilter = 0x8012,

        Custom1 = 0x8020,
        Custom2 = 0x8021,
        Custom3 = 0x8022,

        UnderwaterAuto = 0x8030,
    }
}
