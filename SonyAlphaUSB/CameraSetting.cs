using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SonyAlphaUSB
{
    public class CameraSetting
    {
        public SettingIds Id;
        public int Value;
        public int SubValue;
        public int IncrementValue;
        public int DecrementValue;
        public List<int> AcceptedValues;

        public CameraSetting(SettingIds id)
        {
            Id = id;
            AcceptedValues = new List<int>();
            IncrementValue = 1;
            DecrementValue = -1;
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

        public string AsOnOff()
        {
            if (IsOn())
            {
                return "ON";
            }
            else if (IsOff())
            {
                return "OFF";
            }
            else
            {
                return "???";
            }
        }

        public bool IsOn()
        {
            return Value == 2;
        }

        public bool IsOff()
        {
            return Value == 1;
        }
    }
}
