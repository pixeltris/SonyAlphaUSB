using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SonyAlphaUSB.Interop;

namespace SonyAlphaUSB
{
    public class SonyCamera
    {
        public static readonly string[] SupportedCameras =
        {
            "ILCE-7M3"
        };

        internal IWiaItem device;
        internal IWiaItemExtras deviceExtras;

        public string UniqueId { get; private set; }
        public string Name { get; private set; }
        public bool Connected { get; private set; }

        public HashSet<ushort> AvailableMainSettingIds { get; private set; }
        public HashSet<ushort> AvailableSubSettingIds { get; private set; }

        public byte[] LiveViewImage { get; private set; }
        public bool LiveViewEnabled
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.LiveViewState);
                return setting != null && setting.Value != 0;
            }
        }

        public int FNumber
        {
            get { return GetFNumber(); }
        }

        Dictionary<SettingIds, CameraSetting> cameraSettings = new Dictionary<SettingIds, CameraSetting>();

        public SonyCamera(string uniqueId, string name)
        {
            UniqueId = uniqueId;
            Name = name;
            AvailableMainSettingIds = new HashSet<ushort>();
            AvailableSubSettingIds = new HashSet<ushort>();
        }

        public bool Connect()
        {
            return Connect(true);
        }

        internal bool Connect(bool fullConnect)
        {
            Connected = false;
            device = WIA.Connect(this);
            deviceExtras = device as IWiaItemExtras;
            if (fullConnect)
            {
                if (device != null && deviceExtras != null && Handshake())
                {
                    Connected = true;
                }
            }
            else
            {
                Connected = device != null && deviceExtras != null;
            }
            if (!Connected)
            {
                device = null;
                deviceExtras = null;
            }
            return Connected;
        }

        private bool SimpleSend(OpCodes opcode, string hex)
        {
            using (Packet request = new Packet(opcode))
            {
                request.WriteHexString(hex);
                using (Packet response = Packet.Reader(WIA.SendCommand(this, request.GetBuffer())))
                {
                    return IsValidResponse(response);
                }
            }
        }

        private bool IsValidResponse(Packet packet)
        {
            int tempIndex = packet.Index;
            packet.Index = 0;
            bool isValid = packet.ReadByte() == 1 && packet.ReadByte() == 0x20;
            packet.Index = tempIndex;
            return isValid;
        }

        public bool Handshake()
        {
            if (!SimpleSend(OpCodes.Connect, "00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00 03 00 00 00") ||
                !SimpleSend(OpCodes.Connect, "00 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00 03 00 00 00"))
            {
                return false;
            }

            using (Packet request = new Packet(OpCodes.SettingsList))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00 C8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 03 00 00 00");
                using (Packet response = Packet.Reader(WIA.SendCommand(this, request.GetBuffer())))
                {
                    if (response.ReadByte() != 1 || response.ReadByte() != 0x20)
                    {
                        return false;
                    }

                    AvailableMainSettingIds.Clear();
                    AvailableSubSettingIds.Clear();

                    response.Index = 30;
                    ushort unk = response.ReadUInt16();//200?
                    
                    int mainSettingsCount = response.ReadInt32();
                    for (int i = 0; i < mainSettingsCount; i++)
                    {
                        AvailableMainSettingIds.Add(response.ReadUInt16());
                    }

                    int subSettingsCount = response.ReadInt32();
                    for (int i = 0; i < subSettingsCount; i++)
                    {
                        AvailableSubSettingIds.Add(response.ReadUInt16());
                    }
                }
            }


            if (!SimpleSend(OpCodes.Connect, "00 00 00 00 00 00 00 00 03 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00 03 00 00 00"))
            {
                return false;
            }

            //if (!Update(false))
            {
              //  return false;
            }

            return true;
        }

        private bool DoSettingU8(OpCodes opcode, SettingIds id, byte value)
        {
            using (Packet request = new Packet(opcode))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00");
                request.WriteUInt16((ushort)id);
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 04 00 00 00");
                request.WriteByte(value);
                byte[] buffer = WIA.SendCommand(this, request.GetBuffer());
                using (Packet response = Packet.Reader(buffer))
                {
                    if (!IsValidResponse(response))
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        private bool DoSettingI16(OpCodes opcode, SettingIds id, short value)
        {
            using (Packet request = new Packet(opcode))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00");
                request.WriteUInt16((ushort)id);
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 04 00 00 00");
                request.WriteInt16(value);
                byte[] buffer = WIA.SendCommand(this, request.GetBuffer());
                using (Packet response = Packet.Reader(buffer))
                {
                    if (!IsValidResponse(response))
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        private bool DoSettingI16(OpCodes opcode, SettingIds id, short value1, short value2)
        {
            using (Packet request = new Packet(opcode))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00");
                request.WriteUInt16((ushort)id);
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 04 00 00 00");
                request.WriteInt16(value1);
                request.WriteInt16(value2);
                byte[] buffer = WIA.SendCommand(this, request.GetBuffer());
                using (Packet response = Packet.Reader(buffer))
                {
                    if (!IsValidResponse(response))
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        private bool DoMainSettingI16(SettingIds id, short value)
        {
            return DoSettingI16(OpCodes.MainSetting, id, value);
        }

        private bool DoMainSettingI16(SettingIds id, short value1, short value2)
        {
            return DoSettingI16(OpCodes.MainSetting, id, value1, value2);
        }

        private bool DoSubSettingI16(SettingIds id, short value)
        {
            return DoSettingI16(OpCodes.SubSetting, id, value);
        }

        private bool DoSubSettingI16(SettingIds id, short value1, short value2)
        {
            return DoSettingI16(OpCodes.SubSetting, id, value1, value2);
        }

        private bool DoSubSettingI16(SettingIds id, byte value)
        {
            return DoSettingI16(OpCodes.SubSetting, id, value);
        }

        private byte[] GetImage(bool liveView)
        {
            using (Packet response = Packet.Reader(GetImageRequest(liveView, true, 0)))
            {
                if (!IsValidResponse(response))
                {
                    return null;
                }

                response.Index = 32;
                int numImages = response.ReadInt16();//Num images?
                Debug.Assert(numImages == 0 || numImages == 1);
                if (numImages != 1)
                {
                    return null;
                }

                int imageInfoUnk = response.ReadInt32();//14337(01 38 00 00)
                int imageSizeInBytes = response.ReadInt32();
                //Debug.Assert(imageInfoUnk == 14337);//file format?
                if (imageSizeInBytes <= 0)
                {
                    return null;
                }
                response.Index = 82;
                string imageName = response.ReadFixedString(response.ReadByte(), Encoding.Unicode);

                using (Packet dataResponse = Packet.Reader(GetImageRequest(liveView, false, imageSizeInBytes)))
                {
                    if (!IsValidResponse(dataResponse))
                    {
                        return null;
                    }

                    dataResponse.Index = 30;
                    if (liveView)
                    {
                        int unkBufferSize = dataResponse.ReadInt32();
                        int liveViewBufferSize = dataResponse.ReadInt32();
                        byte[] unkBuff = dataResponse.ReadBytes(unkBufferSize - 8);
                        
                        byte[] buff = dataResponse.ReadRemaining();
                        Debug.Assert(buff.Length == liveViewBufferSize);
                        return buff;
                    }
                    else
                    {
                        byte[] buff = dataResponse.ReadRemaining();
                        Debug.Assert(buff.Length == imageSizeInBytes);
                        return buff;
                    }
                }
            }
        }

        private byte[] GetImageRequest(bool liveView, bool info, int imageSizeInBytes)
        {
            // Should be only 29 bytes of extra space required, add a little extra just in case
            int responseSize = imageSizeInBytes == 0 ? 1024 : imageSizeInBytes + 32;

            OpCodes opcode = info ? OpCodes.GetImageInfo : OpCodes.GetImageData;
            using (Packet request = new Packet(opcode))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00");
                if (liveView)
                {
                    // Get the image data from the live view feed
                    request.WriteHexString("02 C0");
                }
                else
                {
                    // Get the image data from a regular photo taken by the camera
                    request.WriteHexString("01 C0");
                }
                request.WriteHexString("FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 03 00 00 00");
                return WIA.SendCommand(this, request.GetBuffer(), responseSize);
            }
        }

        public bool Update()
        {
            return Update(true);
        }

        private bool Update(bool imageData)
        {
            using (Packet request = new Packet(OpCodes.Settings))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00");
                byte[] buffer = WIA.SendCommand(this, request.GetBuffer());
                using (Packet response = Packet.Reader(buffer))
                {
                    if (!IsValidResponse(response))
                    {
                        return false;
                    }

                    response.Index = 30;
                    int numSettings = response.ReadInt32();
                    int unk = response.ReadInt32();
                    Debug.Assert(unk == 0);//always 0?

                    for (int i = 0; i < numSettings; i++)
                    {
                        ushort settingId = response.ReadUInt16();
                        int currentDataStartIndex = response.Index;
                        bool knownSetting = false;

                        CameraSetting setting;
                        if (!cameraSettings.TryGetValue((SettingIds)settingId, out setting))
                        {
                            setting = new CameraSetting((SettingIds)settingId);
                            cameraSettings.Add((SettingIds)settingId, setting);
                        }

                        if (AvailableMainSettingIds.Contains(settingId) ||
                            AvailableSubSettingIds.Contains(settingId))
                        {
                            knownSetting = true;

                            ushort dataType = response.ReadUInt16();
                            switch (dataType)
                            {
                                case 1:
                                    {
                                        response.Skip(3);
                                        setting.Value = response.ReadByte();
                                        byte subDataType = response.ReadByte();
                                        switch (subDataType)
                                        {
                                            case 1:
                                                response.Skip(3);
                                                break;
                                            default:
                                                knownSetting = false;
                                                break;
                                        }
                                    }
                                    break;
                                case 2:
                                    {
                                        response.Skip(3);
                                        setting.Value = response.ReadByte();
                                        byte subDataType = response.ReadByte();
                                        switch (subDataType)
                                        {
                                            case 1:
                                                response.Skip(3);
                                                break;
                                            case 2:
                                                int num = response.ReadUInt16();
                                                setting.AcceptedValues.Clear();
                                                for (int j = 0; j < num; j++)
                                                {
                                                    setting.AcceptedValues.Add(response.ReadByte());
                                                }
                                                break;
                                            default:
                                                knownSetting = false;
                                                break;
                                        }
                                    }
                                    break;
                                case 3:
                                    {
                                        response.Skip(6);
                                        byte subDataType = response.ReadByte();
                                        switch (subDataType)
                                        {
                                            case 1:
                                                response.Skip(6);
                                                break;
                                            case 2:
                                                int num = response.ReadUInt16();
                                                setting.AcceptedValues.Clear();
                                                for (int j = 0; j < num; j++)
                                                {
                                                    setting.AcceptedValues.Add(response.ReadInt16());
                                                }
                                                break;
                                            default:
                                                knownSetting = false;
                                                break;
                                        }
                                    }
                                    break;
                                case 4:
                                    {
                                        response.Skip(4);
                                        setting.Value = response.ReadInt16();
                                        byte subDataType = response.ReadByte();
                                        switch (subDataType)
                                        {
                                            case 1:
                                                response.Skip(2);
                                                setting.DecrementValue = response.ReadInt16();// Decrement value
                                                setting.IncrementValue = response.ReadInt16();// Iecrement value
                                                break;
                                            case 2:
                                                int num = response.ReadUInt16();
                                                setting.AcceptedValues.Clear();
                                                for (int j = 0; j < num; j++)
                                                {
                                                    setting.AcceptedValues.Add(response.ReadInt16());
                                                }
                                                break;
                                            default:
                                                knownSetting = false;
                                                break;
                                        }
                                    }
                                    break;
                                case 6:
                                    {
                                        response.Skip(6);
                                        setting.Value = response.ReadInt16();
                                        setting.SubValue = response.ReadInt16();
                                        byte subDataType = response.ReadByte();
                                        switch (subDataType)
                                        {
                                            case 1:
                                                response.Skip(12);
                                                break;
                                            case 2:
                                                int num = response.ReadUInt16();
                                                setting.AcceptedValues.Clear();
                                                for (int j = 0; j < num; j++)
                                                {
                                                    setting.AcceptedValues.Add(response.ReadInt32());
                                                }
                                                break;
                                            default:
                                                knownSetting = false;
                                                break;
                                        }
                                    }
                                    break;
                                default:
                                    knownSetting = false;
                                    break;
                            }
                        }

                        Debug.Assert(knownSetting);
                    }
                }
            }

            if (imageData && LiveViewEnabled)
            {
                LiveViewImage = GetImage(true);
            }

            return true;
        }

        public CameraSetting GetSetting(SettingIds id)
        {
            CameraSetting setting;
            cameraSettings.TryGetValue(id, out setting);
            return setting;
        }

        public void CapturePhoto()
        {
            // NOTE: The camera always wants to send the data over to the PC. Is there any way to do it without transfer?
            DoMainSettingI16(SettingIds.HalfPressShutter, 2);// Enter half-press shutter mode
            DoMainSettingI16(SettingIds.CapturePhoto, 2);// Take the photo
            DoMainSettingI16(SettingIds.HalfPressShutter, 1);// Leave half-press shutter mode
            DoMainSettingI16(SettingIds.CapturePhoto, 1);// Some kind of state reset?
        }

        public int GetFNumber()
        {
            CameraSetting setting = GetSetting(SettingIds.LiveViewState);
            return setting == null ? 0 : setting.Value;
        }

        public void ModifyFNumber(short amount)
        {
            DoMainSettingI16(SettingIds.FNumber, amount);
        }

        public void SetFocusArea(FocusAreaIds focusArea)
        {
            DoSubSettingI16(SettingIds.FocusArea, (short)focusArea);
        }

        public void SetFocusAreaSpot(short x, short y)
        {
            DoMainSettingI16(SettingIds.FocusAreaSpot, y, x);
        }

        public void SetFileFormat(ImageFileFormat fileFormat)
        {
            DoSubSettingI16(SettingIds.FileFormat, (short)fileFormat);
        }

        public void SetFocusMode(FocusMode focusMode)
        {
            DoSubSettingI16(SettingIds.FocusMode, (short)focusMode);
        }

        public void SetFocusMode(FocusModeToggle focusMode)
        {
            DoMainSettingI16(SettingIds.FocusModeToggle, (short)focusMode);
        }
    }
}
