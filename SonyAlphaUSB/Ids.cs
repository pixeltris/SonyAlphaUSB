using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SonyAlphaUSB
{
    public enum OpCodes
    {
        Connect = 37377,//01 92
        
        /// <summary>
        /// The response contains a list of available main/sub settings (just their ids)
        /// </summary>
        SettingsList = 37378,//02 92
        
        /// <summary>
        /// A request to change a "main" setting. Watch the response of <see cref="OpCodes.Settings"/> for the change in value.
        /// </summary>
        MainSetting = 37383,//07 92

        /// <summary>
        /// A request to change a "sub" setting. Watch the response of <see cref="OpCodes.Settings"/> for the change in value.
        /// </summary>
        SubSetting = 37381,//05 92

        /// <summary>
        /// All of the camera setting values
        /// </summary>
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
        
        MeteringMode = 0x500B,//0B 50

        FlashMode = 0x500C,//0C 50

        ShootingMode = 0x500E,//0E 50

        EV = 0x5010,//10 50

        /// <summary>
        /// The camera drive mode (single shooting, continuous shooting, etc)
        /// </summary>
        DriveMode = 0x5013,//13 50

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
        
        WhiteBalanceColorTemp = 0xD20F,//0F D2
        WhiteBalanceGM = 0xD210,//10 D2

        AspectRatio = 0xD211,//11 D2

        _UnkD212 = 0xD212,//12 D2

        /// <summary>
        /// Green focus state icon (circle / rings icon)
        /// </summary>
        FocusState = 0xD213,//13 D2

        _UnkD214 = 0xD214,//14 D2

        PhotoTransferQueue = 0xD215,//15 D2

        /// <summary>
        /// This is the actual state (on/off) as opposed to the AEL button in the Remote UI
        /// </summary>
        AEL_State = 0xD217,//17 D2

        BatteryInfo = 0xD218,//18 D2

        _UnkD219 = 0xD219,//19 D2

        PictureEffect = 0xD21B,//1B D2
        
        WhiteBalanceAB = 0xD21C,//1C D2

        RecordVideoState = 0xD21D,//1D D2

        ISO = 0xD21E,//1E D2

        /// <summary>
        /// This is the actual state (on/off) as opposed to the FEL button in the Remote UI.
        /// (if "on" this will show the FEL lock icon in the Remote UI (in the top right))
        /// </summary>
        FEL_State = 0xD21F,//1F D2

        LiveViewState = 0xD221,//21 D2

        _UnkD222 = 0xD222,//22 D2

        FocusArea = 0xD22C,//2C D2
        
        FocusMagnifierPhase = 0xD22D,//2D D2

        _UnkD22E = 0xD22E,//2E D2

        FocusMagnifier = 0xD22F,//2F D2
        FocusMagnifierPosition = 0xD230,//30 D2

        UseLiveViewDisplayEffect = 0xD231,//31 D2

        FocusAreaSpot = 0xD232,//32 D2

        /// <summary>
        /// Focus Magnifier button state in Remote UI (0=disabled, 1=enabled)
        /// </summary>
        FocusMagnifierState = 0xD233,//33 D2

        /// <summary>
        /// Signifies that MF/AF changed
        /// 02 00 00 01 00 01 02 02 00 00 01 - MF mode
        /// 02 00 00 00 00 00 02 02 00 00 01 - AF mode
        /// (This is a response only. See FocusModeToggleResponse.)
        /// </summary>
        FocusModeToggleResponse = 0xD235,//35 D2

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

        /// <summary>
        /// A request to increment the focus magnifier value
        /// </summary>
        FocusMagnifierRequest = 0xD2CB,//CB D2

        /// <summary>
        /// A request to reset the focus magnifier value
        /// </summary>
        FocusMagnifierResetRequest = 0xD2CC,//CC D2

        FocusMagnifierMoveUpRequest = 0xD2CD,//CD D2
        FocusMagnifierMoveDownRequest = 0xD2CE,//CE D2
        FocusMagnifierMoveLeftRequest = 0xD2CF,//CF D2
        FocusMagnifierMoveRightRequest = 0xD2D0,//D0 D2

        _UnkD2D1 = 0xD2D1,//D1 D2

        /// <summary>
        /// Used to toggle AF/MF (1=MF, 2=AF).
        /// Used to switch between MF/AF without modifying the current AF setting.
        /// (This is a request only. See FocusModeToggleResponse)
        /// </summary>
        FocusModeToggleRequest = 0xD2D2,//D2 D2

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
        //03 00 - A
        //04 00 - S
        //01 00 - M 
        //50 80 - Movie (P)
        //51 80 - Movie (A)
        //52 80 - Movie (S)
        //53 80 - Movie (M)
        //84 80 - S&Q (P)
        //85 80 - S&Q (A)
        //86 80 - S&Q (S)
        //87 80 - S&Q (M)
        //07 00 - SCN
        //11 80 - Moving objects? (running person icon)
        //15 80 - Flower icon
        //14 80 - Mountains icon
        //12 80 - Sun icon?
        //13 80 - Moon icon
        //17 80 - Moon icon with person

        M = 0x0001,
        P = 0x0002,
        A = 0x0003,
        S = 0x0004,
        SCN = 0x0007,

        AUTO = 0x8000,

        Movie_P = 0x8050,
        Movie_A = 0x8051,
        Movie_S = 0x8052,
        Movie_M = 0x8053,

        //S&Q
        SQ_P = 0x8084,
        SQ_A = 0x8085,
        SQ_S = 0x8086,
        SQ_M = 0x8087,
    }

    public enum WhiteBalance
    {
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

    /// <summary>
    /// Warmth bias used by AB (amber-blue)
    /// </summary>
    public enum WhiteBalanceAB
    {
        B70 = 0xA4,
        B65 = 0xA6,
        B60 = 0xA8,
        B55 = 0xAA,
        B50 = 0xAC,
        B45 = 0xAE,
        B40 = 0xB0,
        B35 = 0xB2,
        B30 = 0xB4,
        B25 = 0xB6,
        B20 = 0xB8,
        B15 = 0xBA,
        B10 = 0xBC,
        B05 = 0xBE,
        Zero = 0xC0,
        A05 = 0xC2,
        A10 = 0xC4,
        A15 = 0xC6,
        A20 = 0xC8,
        A25 = 0xCA,
        A30 = 0xCC,
        A35 = 0xCE,
        A40 = 0xD0,
        A45 = 0xD2,
        A50 = 0xD4,
        A55 = 0xD6,
        A60 = 0xD8,
        A65 = 0xDA,
        A70 = 0xDC
    }

    /// <summary>
    /// Warmth bias used by GM (green-magenta)
    /// </summary>
    public enum WhiteBalaceGM
    {
        M700 = 0xA4,
        M675 = 0xA5,
        M650 = 0xA6,
        M625 = 0xA7,
        M600 = 0xA8,
        M575 = 0xA9,
        M550 = 0xAA,
        M525 = 0xAB,
        M500 = 0xAC,
        M475 = 0xAD,
        M450 = 0xAE,
        M425 = 0xAF,
        M400 = 0xB0,
        M375 = 0xB1,
        M350 = 0xB2,
        M325 = 0xB3,
        M300 = 0xB4,
        M275 = 0xB5,
        M250 = 0xB6,
        M225 = 0xB7,
        M200 = 0xB8,
        M175 = 0xB9,
        M150 = 0xBA,
        M125 = 0xBB,
        M100 = 0xBC,
        M075 = 0xBD,
        M050 = 0xBE,
        M025 = 0xBF,
        Zero = 0xC0,
        G025 = 0xC1,
        G050 = 0xC2,
        G075 = 0xC3,
        G100 = 0xC4,
        G125 = 0xC5,
        G150 = 0xC6,
        G175 = 0xC7,
        G200 = 0xC8,
        G225 = 0xC9,
        G250 = 0xCA,
        G275 = 0xCB,
        G300 = 0xCC,
        G325 = 0xCD,
        G350 = 0xCE,
        G375 = 0xCF,
        G400 = 0xD0,
        G425 = 0xD1,
        G450 = 0xD2,
        G475 = 0xD3,
        G500 = 0xD4,
        G525 = 0xD5,
        G550 = 0xD6,
        G575 = 0xD7,
        G600 = 0xD8,
        G625 = 0xD9,
        G650 = 0xDA,
        G675 = 0xDB,
        G700 = 0xDC
    }

    public enum DriveMode
    {
        SingleShooting = 0x0001,

        // Continuous shooting

        ContinuousShooting_Hi = 0x0002,
        ContinuousShooting_Mid = 0x8015,
        ContinuousShooting_Lo = 0x8012,
        ContinuousShooting_HiPlus = 0x8010,

        // Self timer (single)

        SelfTimerSingle_2Sec = 0x8005,
        SelfTimerSingle_5Sec = 0x8003,
        SelfTimerSingle_10Sec = 0x8004,

        // Cont. Bracket

        ContBracket_03EV_3Img = 0x8337,
        ContBracket_03EV_5Img = 0x8537,
        ContBracket_03EV_9Img = 0x8937,

        ContBracket_05EV_3Img = 0x8357,
        ContBracket_05EV_5Img = 0x8557,
        ContBracket_05EV_9Img = 0x8957,

        ContBracket_07EV_3Img = 0x8377,
        ContBracket_07EV_5Img = 0x8577,
        ContBracket_07EV_9Img = 0x8977,

        ContBracket_10EV_3Img = 0x8311,
        ContBracket_10EV_5Img = 0x8511,
        ContBracket_10EV_9Img = 0x8911,

        ContBracket_20EV_3Img = 0x8321,
        ContBracket_20EV_5Img = 0x8521,

        ContBracket_30EV_3Img = 0x8331,
        ContBracket_30EV_5Img = 0x8531,

        // Single Bracket

        SingleBracket_03EV_3Img = 0x8336,
        SingleBracket_03EV_5Img = 0x8536,
        SingleBracket_03EV_9Img = 0x8936,

        SingleBracket_05EV_3Img = 0x8356,
        SingleBracket_05EV_5Img = 0x8556,
        SingleBracket_05EV_9Img = 0x8956,

        SingleBracket_07EV_3Img = 0x8376,
        SingleBracket_07EV_5Img = 0x8576,
        SingleBracket_07EV_9Img = 0x8976,

        SingleBracket_10EV_3Img = 0x8310,
        SingleBracket_10EV_5Img = 0x8510,
        SingleBracket_10EV_9Img = 0x8910,

        SingleBracket_20EV_3Img = 0x8320,
        SingleBracket_20EV_5Img = 0x8520,

        SingleBracket_30EV_3Img = 0x8330,
        SingleBracket_30EV_5Img = 0x8530,

        // White Balance Bracket

        WhiteBalanceBracket_Lo = 0x8018,
        WhiteBalanceBracket_Hi = 0x8028,

        // DRO Bracket

        DROBracket_Lo = 0x8019,
        DROBracket_Hi = 0x8029,

        // Self timer (continuous)

        SelfTimerCont_10Sec_3Img = 0x8008,
        SelfTimerCont_10Sec_5Img = 0x8009,

        SelfTimerCont_2Sec_3Img = 0x800E,
        SelfTimerCont_2Sec_5Img = 0x800F,

        SelfTimerCont_5Sec_3Img = 0x800C,
        SelfTimerCont_5Sec_5Img = 0x800D
    }

    public enum FlashMode
    {
        AutoFlash = 1,
        FlashOff = 2,
        FillFlash = 3,
        EyeFlashAuto = 4,// unsure of offical name
        EyeFlash = 5,// unsure of offical name

        AltSlowSync = 0x8001,
        RearSync = 0x8003,

        EyeFlashAuto_SlowSync = 0x8031,// unsure of offical name
        SlowSync = 0x8032,

        SlowWL = 0x8041,
        RearWL = 0x8042
    }

    public enum MeteringMode
    {
        //01 80 
        //02 80 
        //04 80 
        //05 80 
        //03 80 
        //06 80

        Multi = 0x8001,
        Center = 0x8002,
        EntireScreenAvg = 0x8003,
        SpotStandard = 0x8004,
        SpotLarge = 0x8005,
        Highlight = 0x8006,
    }
    
    public enum FocusMagnifierPhase
    {
        /// <summary>
        /// The magnifier isn't currently being used
        /// </summary>
        Inactive = 0,
        /// <summary>
        /// x1.0, this phase is used to select the screen region to magnify
        /// </summary>
        SelectRegion = 1,
        /// <summary>
        /// Actively magnifying a region of the screen
        /// </summary>
        Magnify = 2,
    }

    public enum FocusMagnifierDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public enum RecordVideoState
    {
        Stopped = 0,
        Recording = 1,
        UnableToRecord = 2
    }
}
