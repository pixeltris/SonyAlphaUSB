using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SonyAlphaUSB
{
    public delegate void CameraSettingChanged(CameraSetting setting);

    public class CameraSetting
    {
        public SettingIds Id;
        public int Value;
        public int SubValue;
        public List<int> AcceptedValues;

        internal int PrevValue;
        internal int PrevSubValue;

        public event CameraSettingChanged Changed;

        public bool HasSubValue { get; private set; }

        public CameraSetting(SettingIds id)
        {
            Id = id;
            AcceptedValues = new List<int>();

            switch (id)
            {
                case SettingIds.FocusAreaSpot:
                case SettingIds.FocusMagnifierPosition:
                case SettingIds.ShutterSpeed:
                    HasSubValue = true;
                    break;
            }
        }

        internal void OnChanged()
        {
            if (Changed != null)
            {
                Changed(this);
            }
        }

        public string AsImageFileFormat()
        {
            string result = string.Empty;
            ImageFileFormat flags = (ImageFileFormat)Value;
            if ((flags & ImageFileFormat.Raw) == ImageFileFormat.Raw)
            {
                result += "RAW ";
            }
            if ((flags & ImageFileFormat.JpegXFine) == ImageFileFormat.JpegXFine)
            {
                result += "X.FINE";
            }
            else if ((flags & ImageFileFormat.JpegFine) == ImageFileFormat.JpegFine)
            {
                result += "FINE";
            }
            else if ((flags & ImageFileFormat.JpegStandard) == ImageFileFormat.JpegStandard)
            {
                result += "STD";
            }
            return result;
        }

        public string AsDroHdr()
        {
            DroHdr droHdr = (DroHdr)Value;
            switch (droHdr)
            {
                case DroHdr.DrOff: return "D-R OFF";
                case DroHdr.DroLv1: return "DRO Lv1";
                case DroHdr.DroLv2: return "DRO Lv2";
                case DroHdr.DroLv3: return "DRO Lv3";
                case DroHdr.DroLv4: return "DRO Lv4";
                case DroHdr.DroLv5: return "DRO Lv5";
                case DroHdr.DroAuto: return "DRO AUTO";
                case DroHdr.Hdr1Ev: return "HDR 1.0EV";
                case DroHdr.Hdr2Ev: return "HDR 2.0EV";
                case DroHdr.Hdr3Ev: return "HDR 3.0EV";
                case DroHdr.Hdr4Ev: return "HDR 4.0EV";
                case DroHdr.Hdr5Ev: return "HDR 5.0EV";
                case DroHdr.Hdr6Ev: return "HDR 6.0EV";
                case DroHdr.HdrAuto: return "HDR AUTO";
                default: return "???(" + Value + ")";
            }
        }

        public string AsFocusMode()
        {
            FocusMode focusMode = (FocusMode)Value;
            switch (focusMode)
            {
                case FocusMode.MF: return "MF";
                case FocusMode.AF_SingleShot: return "AF-S";
                case FocusMode.AF_Continuous: return "AF-C";
                case FocusMode.AF_Automatic: return "AF-A";
                case FocusMode.DMF: return "DMF";
                default: return "???(" + Value + ")";
            }
        }

        public string AsFocusModeToggleResponse()
        {
            return Value == 0 ? "AF" : "MF";
        }

        public string AsAspectRatio()
        {
            AspectRatio aspectRatio = (AspectRatio)Value;
            switch (aspectRatio)
            {
                case AspectRatio.Ar3_2: return "3:2";
                case AspectRatio.Ar16_9: return "16:9";
                default: return "???(" + Value + ")";
            }
        }

        public string AsImageSize()
        {
            ImageSize imageSize = (ImageSize)Value;
            switch (imageSize)
            {
                case ImageSize.Large: return "L";
                case ImageSize.Medium: return "M";
                case ImageSize.Small: return "S";
                default: return "???(" + Value + ")";
            }
        }

        public string AsPictureEffect()
        {
            PictureEffect pictureEffect = (PictureEffect)Value;
            switch (pictureEffect)
            {
                case PictureEffect.Off: return "Off";
                case PictureEffect.ToyCamera_Normal: return "Toy Camera: Normal";
                case PictureEffect.ToyCamera_Cool: return "Toy Camera: Cool";
                case PictureEffect.ToyCamera_Warm: return "Toy Camera: Warm";
                case PictureEffect.ToyCamera_Green: return "Toy Camera: Green";
                case PictureEffect.ToyCamera_Magenta: return "Toy Camera: Magenta";
                case PictureEffect.PopColor: return "Pop Color";
                case PictureEffect.Posterization_BW: return "Posterization: B/W";
                case PictureEffect.Posterization_Color: return "Posterization: Color";
                case PictureEffect.RetroPhoto: return "Retro Photo";
                case PictureEffect.SoftHighKey: return "Soft High-key";
                case PictureEffect.PartialColor_Red: return "Partial Color: Red";
                case PictureEffect.PartialColor_Green: return "Partial Color: Green";
                case PictureEffect.PartialColor_Blue: return "Partial Color: Blue";
                case PictureEffect.PartialColor_Yellow: return "Partial Color: Yellow";
                case PictureEffect.HighContrastMono: return "High Contrast Mono.";
                case PictureEffect.RichToneMono: return "Rich-tone Mono";
                default: return "???(" + Value + ")";
            }
        }

        public string AsWhiteBalance()
        {
            WhiteBalance whiteBalance = (WhiteBalance)Value;
            switch (whiteBalance)
            {
                case WhiteBalance.Auto: return "Auto";
                case WhiteBalance.Daylight: return "Daylight";
                case WhiteBalance.Incandescent: return "Incandescent";
                case WhiteBalance.Flash: return "Flash";
                case WhiteBalance.Fluor_WarmWhite: return "Fluor: Warm White";
                case WhiteBalance.Fluor_CoolWhite: return "Fluor: Cool White";
                case WhiteBalance.Fluor_DayWhite: return "Fluor: Day White";
                case WhiteBalance.Fluor_Daylight: return "Fluor: Daylight";
                case WhiteBalance.Cloudy: return "Cloudy";
                case WhiteBalance.Shade: return "Shade";
                case WhiteBalance.CTempFilter: return "C.Temp/Filter";
                case WhiteBalance.Custom1: return "Custom 1";
                case WhiteBalance.Custom2: return "Custom 2";
                case WhiteBalance.Custom3: return "Custom 3";
                case WhiteBalance.UnderwaterAuto: return "Underwater Auto";
                default: return "???(" + Value + ")";

            }
        }

        public string AsWhiteBalanceColorTemp()
        {
            if (Value == 0)
            {
                return string.Empty;
            }
            return Value + "K";
        }

        public string AsWhiteBalanceAB()
        {
            WhiteBalanceAB whiteBalanceAB = (WhiteBalanceAB)Value;
            switch (whiteBalanceAB)
            {
                case WhiteBalanceAB.B70: return "B7";
                case WhiteBalanceAB.B65: return "B6.5";
                case WhiteBalanceAB.B60: return "B6";
                case WhiteBalanceAB.B55: return "B5.5";
                case WhiteBalanceAB.B50: return "B5";
                case WhiteBalanceAB.B45: return "B4.5";
                case WhiteBalanceAB.B40: return "B4";
                case WhiteBalanceAB.B35: return "B3.5";
                case WhiteBalanceAB.B30: return "B3";
                case WhiteBalanceAB.B25: return "B2.5";
                case WhiteBalanceAB.B20: return "B2";
                case WhiteBalanceAB.B15: return "B1.5";
                case WhiteBalanceAB.B10: return "B1";
                case WhiteBalanceAB.B05: return "B0.5";
                case WhiteBalanceAB.Zero: return "0";
                case WhiteBalanceAB.A05: return "A0.5";
                case WhiteBalanceAB.A10: return "A1";
                case WhiteBalanceAB.A15: return "A1.5";
                case WhiteBalanceAB.A20: return "A2";
                case WhiteBalanceAB.A25: return "A2.5";
                case WhiteBalanceAB.A30: return "A3";
                case WhiteBalanceAB.A35: return "A3.5";
                case WhiteBalanceAB.A40: return "A4";
                case WhiteBalanceAB.A45: return "A4.5";
                case WhiteBalanceAB.A50: return "A5";
                case WhiteBalanceAB.A55: return "A5.5";
                case WhiteBalanceAB.A60: return "A6";
                case WhiteBalanceAB.A65: return "A6.5";
                case WhiteBalanceAB.A70: return "A7";
                default: return "???(" + Value + ")";
            }
        }

        public string AsWhiteBalanceGM()
        {
            WhiteBalanceGM whiteBalanceGM = (WhiteBalanceGM)Value;
            switch (whiteBalanceGM)
            {
                case WhiteBalanceGM.M700: return "M7";
                case WhiteBalanceGM.M675: return "M6.75";
                case WhiteBalanceGM.M650: return "M6.5";
                case WhiteBalanceGM.M625: return "M6.25";
                case WhiteBalanceGM.M600: return "M6";
                case WhiteBalanceGM.M575: return "M5.75";
                case WhiteBalanceGM.M550: return "M5.5";
                case WhiteBalanceGM.M525: return "M5.25";
                case WhiteBalanceGM.M500: return "M5";
                case WhiteBalanceGM.M475: return "M4.75";
                case WhiteBalanceGM.M450: return "M4.5";
                case WhiteBalanceGM.M425: return "M4.25";
                case WhiteBalanceGM.M400: return "M4";
                case WhiteBalanceGM.M375: return "M3.75";
                case WhiteBalanceGM.M350: return "M3.5";
                case WhiteBalanceGM.M325: return "M3.25";
                case WhiteBalanceGM.M300: return "M3";
                case WhiteBalanceGM.M275: return "M2.75";
                case WhiteBalanceGM.M250: return "M2.5";
                case WhiteBalanceGM.M225: return "M2.25";
                case WhiteBalanceGM.M200: return "M2";
                case WhiteBalanceGM.M175: return "M1.75";
                case WhiteBalanceGM.M150: return "M1.5";
                case WhiteBalanceGM.M125: return "M1.25";
                case WhiteBalanceGM.M100: return "M1";
                case WhiteBalanceGM.M075: return "M0.75";
                case WhiteBalanceGM.M050: return "M0.5";
                case WhiteBalanceGM.M025: return "M0.25";
                case WhiteBalanceGM.Zero: return "0";
                case WhiteBalanceGM.G025: return "G0.25";
                case WhiteBalanceGM.G050: return "G0.5";
                case WhiteBalanceGM.G075: return "G0.75";
                case WhiteBalanceGM.G100: return "G1";
                case WhiteBalanceGM.G125: return "G1.25";
                case WhiteBalanceGM.G150: return "G1.5";
                case WhiteBalanceGM.G175: return "G1.75";
                case WhiteBalanceGM.G200: return "G2";
                case WhiteBalanceGM.G225: return "G2.25";
                case WhiteBalanceGM.G250: return "G2.5";
                case WhiteBalanceGM.G275: return "G2.75";
                case WhiteBalanceGM.G300: return "G3";
                case WhiteBalanceGM.G325: return "G3.25";
                case WhiteBalanceGM.G350: return "G3.5";
                case WhiteBalanceGM.G375: return "G3.75";
                case WhiteBalanceGM.G400: return "G4";
                case WhiteBalanceGM.G425: return "G4.25";
                case WhiteBalanceGM.G450: return "G4.5";
                case WhiteBalanceGM.G475: return "G4.75";
                case WhiteBalanceGM.G500: return "G5";
                case WhiteBalanceGM.G525: return "G5.25";
                case WhiteBalanceGM.G550: return "G5.5";
                case WhiteBalanceGM.G575: return "G5.75";
                case WhiteBalanceGM.G600: return "G6";
                case WhiteBalanceGM.G625: return "G6.25";
                case WhiteBalanceGM.G650: return "G6.5";
                case WhiteBalanceGM.G675: return "G6.75";
                case WhiteBalanceGM.G700: return "G7";
                default: return "???(" + Value + ")";
            }
        }

        public string AsDriveMode()
        {
            DriveMode driveMode = (DriveMode)Value;
            switch (driveMode)
            {
                case DriveMode.SingleShooting: return "Single Shooting";
                case DriveMode.ContinuousShooting_Hi: return "Continuous Shooting: Hi";
                case DriveMode.ContinuousShooting_Mid: return "Continuous Shooting: Mid";
                case DriveMode.ContinuousShooting_Lo: return "Continuous Shooting: Lo";
                case DriveMode.ContinuousShooting_HiPlus: return "Continuous Shooting: Hi+";
                case DriveMode.SelfTimerSingle_2Sec: return "Self-timer(Single): 2 sec";
                case DriveMode.SelfTimerSingle_5Sec: return "Self-timer(Single): 5 sec";
                case DriveMode.SelfTimerSingle_10Sec: return "Self-timer(Single): 10 sec";
                case DriveMode.ContBracket_03EV_3Img: return "Cont. Bracket: 0.3EV 3 Image";
                case DriveMode.ContBracket_03EV_5Img: return "Cont. Bracket: 0.3EV 5 Image";
                case DriveMode.ContBracket_03EV_9Img: return "Cont. Bracket: 0.3EV 9 Image";
                case DriveMode.ContBracket_05EV_3Img: return "Cont. Bracket: 0.5EV 3 Image";
                case DriveMode.ContBracket_05EV_5Img: return "Cont. Bracket: 0.5EV 5 Image";
                case DriveMode.ContBracket_05EV_9Img: return "Cont. Bracket: 0.5EV 9 Image";
                case DriveMode.ContBracket_07EV_3Img: return "Cont. Bracket: 0.7EV 3 Image";
                case DriveMode.ContBracket_07EV_5Img: return "Cont. Bracket: 0.7EV 5 Image";
                case DriveMode.ContBracket_07EV_9Img: return "Cont. Bracket: 0.7EV 9 Image";
                case DriveMode.ContBracket_10EV_3Img: return "Cont. Bracket: 1.0EV 3 Image";
                case DriveMode.ContBracket_10EV_5Img: return "Cont. Bracket: 1.0EV 5 Image";
                case DriveMode.ContBracket_10EV_9Img: return "Cont. Bracket: 1.0EV 9 Image";
                case DriveMode.ContBracket_20EV_3Img: return "Cont. Bracket: 2.0EV 3 Image";
                case DriveMode.ContBracket_20EV_5Img: return "Cont. Bracket: 2.0EV 5 Image";
                case DriveMode.ContBracket_30EV_3Img: return "Cont. Bracket: 3.0EV 3 Image";
                case DriveMode.ContBracket_30EV_5Img: return "Cont. Bracket: 3.0EV 5 Image";
                case DriveMode.SingleBracket_03EV_3Img: return "Single Bracket: 0.3EV 3 Image";
                case DriveMode.SingleBracket_03EV_5Img: return "Single Bracket: 0.3EV 5 Image";
                case DriveMode.SingleBracket_03EV_9Img: return "Single Bracket: 0.3EV 9 Image";
                case DriveMode.SingleBracket_05EV_3Img: return "Single Bracket: 0.5EV 3 Image";
                case DriveMode.SingleBracket_05EV_5Img: return "Single Bracket: 0.5EV 5 Image";
                case DriveMode.SingleBracket_05EV_9Img: return "Single Bracket: 0.5EV 9 Image";
                case DriveMode.SingleBracket_07EV_3Img: return "Single Bracket: 0.7EV 3 Image";
                case DriveMode.SingleBracket_07EV_5Img: return "Single Bracket: 0.7EV 5 Image";
                case DriveMode.SingleBracket_07EV_9Img: return "Single Bracket: 0.7EV 9 Image";
                case DriveMode.SingleBracket_10EV_3Img: return "Single Bracket: 1.0EV 3 Image";
                case DriveMode.SingleBracket_10EV_5Img: return "Single Bracket: 1.0EV 5 Image";
                case DriveMode.SingleBracket_10EV_9Img: return "Single Bracket: 1.0EV 9 Image";
                case DriveMode.SingleBracket_20EV_3Img: return "Single Bracket: 2.0EV 3 Image";
                case DriveMode.SingleBracket_20EV_5Img: return "Single Bracket: 2.0EV 5 Image";
                case DriveMode.SingleBracket_30EV_3Img: return "Single Bracket: 3.0EV 3 Image";
                case DriveMode.SingleBracket_30EV_5Img: return "Single Bracket: 3.0EV 5 Image";
                case DriveMode.WhiteBalanceBracket_Lo: return "White Balance Bracket: Lo";
                case DriveMode.WhiteBalanceBracket_Hi: return "White Balance Bracket: Hi";
                case DriveMode.DROBracket_Lo: return "DRO Bracket: Lo";
                case DriveMode.DROBracket_Hi: return "DRO Bracket: Hi";
                case DriveMode.SelfTimerCont_10Sec_3Img: return "Self-timer (Cont): 10 sec. 3 Img.";
                case DriveMode.SelfTimerCont_10Sec_5Img: return "Self-timer (Cont): 10 sec. 5 Img.";
                case DriveMode.SelfTimerCont_2Sec_3Img: return "Self-timer (Cont): 2 sec. 3 Img.";
                case DriveMode.SelfTimerCont_2Sec_5Img: return "Self-timer (Cont): 2 sec. 5 Img.";
                case DriveMode.SelfTimerCont_5Sec_3Img: return "Self-timer (Cont): 5 sec. 3 Img.";
                case DriveMode.SelfTimerCont_5Sec_5Img: return "Self-timer (Cont): 5 sec. 5 Img.";
                default: return "???(" + Value + ")";
            }
        }

        public string AsFNumber()
        {
            return "F" + ((float)Value / 100);
        }

        public string AsEV()
        {
            if (Value == 0)
            {
                return "±0.0";
            }
            else if (Value > 0)
            {
                return "+" + ((float)Value / 1000);
            }
            else
            {
                return ((float)Value / 1000).ToString();
            }
        }

        public string AsFlash()
        {
            return AsEV();
        }

        public string AsFlashMode()
        {
            FlashMode flashMode = (FlashMode)Value;
            switch (flashMode)
            {
                case FlashMode.AutoFlash: return "Autoflash";
                case FlashMode.FlashOff: return "Flash Off";
                case FlashMode.FillFlash: return "Fill-flash";
                case FlashMode.EyeFlashAuto: return "Eye-flash Auto";
                case FlashMode.EyeFlash: return "Eye-flash";
                case FlashMode.AltSlowSync: return "Slow Sync (alt?).";
                case FlashMode.SlowSync: return "Slow Sync.";
                case FlashMode.RearSync: return "Rear Sync.";
                case FlashMode.EyeFlashAuto_SlowSync: return "Eye-flash Autio (Slow Sync.)";
                case FlashMode.SlowWL: return "Slow WL";
                case FlashMode.RearWL: return "Rear WL";
                default: return "???(" + Value + ")";
            }
        }

        public string AsMeteringMode()
        {
            MeteringMode meteringMode = (MeteringMode)Value;
            switch (meteringMode)
            {
                case MeteringMode.Multi: return "Multi";
                case MeteringMode.Center: return "Center";
                case MeteringMode.SpotStandard: return "Spot: Standard";
                case MeteringMode.SpotLarge: return "Spot: Large";
                case MeteringMode.EntireScreenAvg: return "Entire Screen Avg.";
                case MeteringMode.Highlight: return "Highlight";
                default: return "???(" + Value + ")";
            }
        }

        public string AsFocusMagnifier()
        {
            if (Value == 0)
            {
                return "x1.0";
            }
            return "x" + string.Format("{0:F1}", (float)Value / 10);
        }

        public string AsPhotoTransferQueue()
        {
            int numPhotos = Value & 0xFF;
            string result = numPhotos.ToString();
            if (((Value >> 8) & 0xFF) == 0x80)
            {
                result += " (photo available for transfer)";
            }
            return result;
        }

        public string AsShutterSpeed()
        {
            if (Value == 0 || SubValue == 0)
            {
                return "BULB";
            }
            else if (SubValue == 1)
            {
                return "1/" + Value;
            }
            else
            {
                return ((float)SubValue / 10) + "\"";
            }
        }

        public string AsBatteryCharge()
        {
            return Value + "%";
        }
    }
}
