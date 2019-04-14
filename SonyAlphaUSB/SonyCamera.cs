using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SonyAlphaUSB.Interop;

namespace SonyAlphaUSB
{
    // NOTE: Some requests require the setting to change before another request can be sent again (e.g. WhiteBalanceAB)
    //       Sending another WhiteBalanceAB (or WhiteBalanceGM) request whilst the other is pending will result in no change.

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

        Dictionary<SettingIds, CameraSetting> cameraSettings = new Dictionary<SettingIds, CameraSetting>();
        public event CameraSettingChanged SettingChanged;

        public byte[] LiveViewImage { get; private set; }
        public bool IsLiveViewEnabled
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

        public int ISO
        {
            get { return GetISO(); }
        }

        public ShutterSpeedInfo ShutterSpeed
        {
            get { return GetShutterSpeed(); }
        }

        public bool IsFocusMagnifierEnabled
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.FocusMagnifierState);
                return setting != null && setting.Value != 0;
            }
        }

        public int BatteryChargePercent
        {
            get { return GetBatteryChargePercent(); }
        }

        public bool AEL
        {
            get { return GetAEL(); }
        }

        public bool FEL
        {
            get { return GetFEL(); }
        }

        public FocusAreaIds FocusArea
        {
            get { return GetFocusArea(); }
        }

        public AutoFocusState AutoFocusState
        {
            get { return GetAutoFocusState(); }
        }

        public float EV
        {
            get { return GetEV(); }
        }

        public float Flash
        {
            get { return GetFlash(); }
        }

        public ImageFileFormat FileFormat
        {
            get { return GetFileFormat(); }
        }

        public PictureEffect PictureEffect
        {
            get { return GetPictureEffect(); }
        }

        public DroHdr DroHdr
        {
            get { return GetDroHdr(); }
        }

        public AspectRatio AspectRatio
        {
            get { return GetAspectRatio(); }
        }
        
        public FocusMode FocusMode
        {
            get { return GetFocusMode(); }
        }

        public ShootingMode ShootingMode
        {
            get { return GetShootingMode(); }
        }

        public WhiteBalance WhiteBalance
        {
            get { return GetWhiteBalance(); }
        }

        public ushort WhiteBalanceColorTemp
        {
            get { return GetWhiteBalanceColorTemp(); }
        }

        public WhiteBalanceAB WhiteBalanceAB
        {
            get { return GetWhiteBalanceAB(); }
        }

        public WhiteBalanceGM WhiteBalanceGM
        {
            get { return GetWhiteBalanceGM(); }
        }

        public DriveMode DriveMode
        {
            get { return GetDriveMode(); }
        }

        public bool UseLiveViewDisplayEffect
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.UseLiveViewDisplayEffect);
                return setting != null && setting.Value == 1;
            }
        }

        /// <summary>
        /// Maybe call this IsFocusMagnifierUsable? focus magnifier isn't usable in some modes
        /// </summary>
        public bool IsFocusMagnifierAvailable
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.UseLiveViewDisplayEffect);
                return setting != null && setting.Value == 1;
            }
        }

        public FlashMode FlashMode
        {
            get { return GetFlashMode(); }
        }

        public MeteringMode MeteringMode
        {
            get { return GetMeteringMode(); }
        }

        public float FocusMagnifier
        {
            get { return GetFocusMagnifier(); }
        }

        public FocusMagnifierPhase FocusMagnifierPhase
        {
            get { return GetFocusMagnifierPhase(); }
        }

        public Position FocusMagnifierPosition
        {
            get { return GetFocusMagnifierPosition(); }
        }

        public RecordVideoState RecordVideoState
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.RecordVideoState);
                return setting != null ? (RecordVideoState)setting.Value : RecordVideoState.Stopped;
            }
        }

        /// <summary>
        /// The number of queued photos for transfering to the PC
        /// </summary>
        public int NumQueuedPhotos
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.PhotoTransferQueue);
                if (setting != null)
                {
                    return setting.Value & 0xFF;
                }
                return 0;
            }
        }

        /// <summary>
        /// Is a photo available for transfer to the PC
        /// </summary>
        public bool IsPhotoWaitingForTransfer
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.PhotoTransferQueue);
                if (setting != null)
                {
                    return ((setting.Value >> 8) & 0xFF) == 0x80;
                }
                return false;
            }
        }

        /// <summary>
        /// APS-C/Super 35mm (sensor crop)
        /// </summary>
        public bool IsSensorCropEnabled
        {
            get
            {
                CameraSetting setting = GetSetting(SettingIds.SensorCrop);
                return setting != null && setting.Value == 2;
            }
        }

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

        private byte[] SendCommand(byte[] buffer)
        {
            lock (cameraSettings)
            {
                return WIA.SendCommand(this, buffer);
            }
        }

        private byte[] SendCommand(byte[] buffer, int outputBufferLength)
        {
            lock (cameraSettings)
            {
                return WIA.SendCommand(this, buffer, outputBufferLength);
            }
        }

        private bool SimpleSend(OpCodes opcode, string hex)
        {
            using (Packet request = new Packet(opcode))
            {
                request.WriteHexString(hex);
                using (Packet response = Packet.Reader(SendCommand(request.GetBuffer())))
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
                using (Packet response = Packet.Reader(SendCommand(request.GetBuffer())))
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

            if (!Update(false))
            {
                return false;
            }

            return true;
        }

        private bool DoSetting(OpCodes opcode, SettingIds id, int value1, int value2, int value1DataSize, int value2DataSize)
        {
            using (Packet request = new Packet(opcode))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00");
                request.WriteUInt16((ushort)id);
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 04 00 00 00");
                for (int i = 0; i < 2; i++)
                {
                    int dataSize = i == 0 ? value1DataSize : value2DataSize;
                    int data = i == 0 ? value1 : value2;
                    switch (dataSize)
                    {
                        case 1:
                            request.WriteByte((byte)data);
                            break;
                        case 2:
                            request.WriteInt16((short)data);
                            break;
                        case 4:
                            request.WriteInt32(data);
                            break;
                    }
                }
                byte[] buffer = SendCommand(request.GetBuffer());
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

        private bool DoSettingU8(OpCodes opcode, SettingIds id, byte value)
        {
            return DoSetting(opcode, id, value, 0, 1, 0);
        }

        private bool DoSettingI32(OpCodes opcode, SettingIds id, int value)
        {
            return DoSetting(opcode, id, value, 0, 4, 0);
        }

        private bool DoSettingI16(OpCodes opcode, SettingIds id, short value)
        {
            return DoSetting(opcode, id, value, 0, 2, 0);
        }

        private bool DoSettingI16(OpCodes opcode, SettingIds id, short value1, short value2)
        {
            return DoSetting(opcode, id, value1, value2, 2, 2);
        }

        private bool DoMainSettingI32(SettingIds id, int value)
        {
            return DoSettingI32(OpCodes.MainSetting, id, value);
        }

        private bool DoMainSettingI16(SettingIds id, short value)
        {
            return DoSettingI16(OpCodes.MainSetting, id, value);
        }

        private bool DoMainSettingI16(SettingIds id, short value1, short value2)
        {
            return DoSettingI16(OpCodes.MainSetting, id, value1, value2);
        }

        private bool DoMainSettingU8(SettingIds id, byte value)
        {
            return DoSettingU8(OpCodes.MainSetting, id, value);
        }

        private bool DoSubSettingI32(SettingIds id, int value)
        {
            return DoSettingI32(OpCodes.SubSetting, id, value);
        }

        private bool DoSubSettingI16(SettingIds id, short value)
        {
            return DoSettingI16(OpCodes.SubSetting, id, value);
        }

        private bool DoSubSettingI16(SettingIds id, short value1, short value2)
        {
            return DoSettingI16(OpCodes.SubSetting, id, value1, value2);
        }

        private bool DoSubSettingU8(SettingIds id, byte value)
        {
            return DoSettingU8(OpCodes.SubSetting, id, value);
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
                return SendCommand(request.GetBuffer(), responseSize);
            }
        }

        public bool Update()
        {
            return Update(true);
        }

        public bool Update(bool imageData)
        {
            using (Packet request = new Packet(OpCodes.Settings))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00");
                byte[] buffer = SendCommand(request.GetBuffer());
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
                                        response.Skip(4);
                                        setting.Value = response.ReadInt16();
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
                                        setting.Value = response.ReadUInt16();
                                        byte subDataType = response.ReadByte();
                                        switch (subDataType)
                                        {
                                            case 1:
                                                response.Skip(2);
                                                response.ReadInt16();// Decrement value?
                                                response.ReadInt16();// Iecrement value?
                                                break;
                                            case 2:
                                                int num = response.ReadUInt16();
                                                setting.AcceptedValues.Clear();
                                                for (int j = 0; j < num; j++)
                                                {
                                                    setting.AcceptedValues.Add(response.ReadUInt16());
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
                                        if (setting.HasSubValue)
                                        {
                                            setting.Value = response.ReadUInt16();
                                            setting.SubValue = response.ReadUInt16();
                                        }
                                        else
                                        {
                                            setting.Value = response.ReadInt32();
                                        }
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

                            if (setting.Value != setting.PrevValue ||
                                setting.SubValue != setting.PrevSubValue)
                            {
                                setting.OnChanged();
                                if (SettingChanged != null)
                                {
                                    SettingChanged(setting);
                                }
                                setting.PrevValue = setting.Value;
                                setting.PrevSubValue = setting.SubValue;
                            }
                        }

                        Debug.Assert(knownSetting);
                    }
                }
            }

            if (imageData)
            {
                if (IsLiveViewEnabled)
                {
                    LiveViewImage = GetImage(true);
                }

                CameraSetting photoQueue = GetSetting(SettingIds.PhotoTransferQueue);
                if (photoQueue != null)
                {
                    // NOTE: The camera is capped at 127 (0x75) images in the queue at any given time
                    int numPhotos = photoQueue.Value & 0xFF;
                    bool photoAvailableForTransfer = ((photoQueue.Value >> 8) & 0xFF) == 0x80;
                    if (photoAvailableForTransfer)
                    {
                        byte[] img = GetImage(false);
                    }
                }
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

            // TODO: Some proper logic needs to be here depending on what type of photo is being taken. AF mode requires focus
            // to be found before the shutter is released, so we can't instantly press/release the shutter. Same goes for continuous shooting.
            // - Capture photo may need to be done in a different thread to "Update()" so we can watch for changes in AF state / captured images.

            // Press shutter button
            DoMainSettingI16(SettingIds.HalfPressShutter, 2);// Enter half-press shutter mode
            DoMainSettingI16(SettingIds.CapturePhoto, 2);// Take the photo

            //System.Threading.Thread.Sleep(100);

            // Release shutter
            DoMainSettingI16(SettingIds.HalfPressShutter, 1);// Leave half-press shutter mode
            DoMainSettingI16(SettingIds.CapturePhoto, 1);// Some kind of state reset?
        }

        public void PressShutter()
        {
            PressShutterHalf();
            PressShutterFull();
        }

        public void PressShutterHalf()
        {
            DoMainSettingI16(SettingIds.HalfPressShutter, 2);
        }

        public void PressShutterFull()
        {
            DoMainSettingI16(SettingIds.CapturePhoto, 2);
        }

        public void ReleaseShutter()
        {
            ReleaseShutterHalf();
            ReleaseShutterFull();
        }

        public void ReleaseShutterHalf()
        {
            DoMainSettingI16(SettingIds.HalfPressShutter, 1);
        }

        public void ReleaseShutterFull()
        {
            DoMainSettingI16(SettingIds.CapturePhoto, 1);
        }

        public void StartRecordingVideo()
        {
            DoMainSettingI16(SettingIds.RecordVideo, 2);
        }

        public void StopRecordingVideo()
        {
            DoMainSettingI16(SettingIds.RecordVideo, 1);
        }

        public int GetFNumber()
        {
            CameraSetting setting = GetSetting(SettingIds.LiveViewState);
            return setting != null ? setting.Value : 0;
        }

        public void ModifyFNumber(short steps)
        {
            DoMainSettingI16(SettingIds.FNumber, steps);
        }

        public bool IsAutoISO(int iso)
        {
            return iso == 0xFFFFFF;
        }

        public bool IsAutoISO()
        {
            return IsAutoISO(GetISO());
        }

        public int GetISO()
        {
            CameraSetting setting = GetSetting(SettingIds.ISO);
            return setting != null ? setting.Value : 0;
        }

        public void ModifyISO(int steps)
        {
            DoMainSettingI32(SettingIds.ISO, steps);
        }

        public ShutterSpeedInfo GetShutterSpeed()
        {
            CameraSetting setting = GetSetting(SettingIds.ShutterSpeed);
            if (setting != null)
            {
                return new ShutterSpeedInfo(
                    setting.SubValue == 1 ? setting.Value : (float)setting.SubValue / 10,
                    setting.SubValue == 1);
            }
            else
            {
                return default(ShutterSpeedInfo);
            }
        }

        public void ModifyShutterSpeed(int steps)
        {
            DoMainSettingI32(SettingIds.ShutterSpeed, steps);
        }

        public int GetBatteryChargePercent()
        {
            CameraSetting setting = GetSetting(SettingIds.BatteryInfo);
            return setting != null ? setting.Value : 0;
        }

        public bool GetAEL()
        {
            CameraSetting setting = GetSetting(SettingIds.AEL_State);
            return setting != null ? setting.Value == 2 : false;
        }

        public void SetAEL(bool enable)
        {
            if (enable)
            {
                DoMainSettingI16(SettingIds.AEL, 1);
                DoMainSettingI16(SettingIds.AEL, 2);
            }
            else
            {
                DoMainSettingI16(SettingIds.AEL, 2);
                DoMainSettingI16(SettingIds.AEL, 1);
            }
        }

        public bool GetFEL()
        {
            CameraSetting setting = GetSetting(SettingIds.FEL_State);
            return setting != null ? setting.Value == 2 : false;
        }

        public void SetFEL(bool enable)
        {
            if (enable)
            {
                DoMainSettingI16(SettingIds.FEL, 1);
                DoMainSettingI16(SettingIds.FEL, 2);
            }
            else
            {
                DoMainSettingI16(SettingIds.FEL, 2);
                DoMainSettingI16(SettingIds.FEL, 1);
            }
        }

        public FocusAreaIds[] GetAvailableFocusAreas()
        {
            List<FocusAreaIds> result = new List<FocusAreaIds>();
            CameraSetting setting = GetSetting(SettingIds.FocusArea);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((FocusAreaIds)value);
            }
            return result.ToArray();
        }

        public FocusAreaIds GetFocusArea()
        {
            CameraSetting setting = GetSetting(SettingIds.FocusArea);
            return setting != null ? (FocusAreaIds)setting.Value : default(FocusAreaIds);
        }

        public void SetFocusArea(FocusAreaIds focusArea)
        {
            DoSubSettingI16(SettingIds.FocusArea, (short)focusArea);
        }

        public Position GetFocusAreaSpot()
        {
            CameraSetting setting = GetSetting(SettingIds.FocusAreaSpot);
            return setting != null ? new Position(setting.SubValue, setting.Value) : default(Position);
        }

        public void SetFocusAreaSpot(short x, short y)
        {
            DoMainSettingI16(SettingIds.FocusAreaSpot, y, x);
        }

        public AutoFocusState GetAutoFocusState()
        {
            CameraSetting setting = GetSetting(SettingIds.AutoFocusState);
            return setting != null ? (AutoFocusState)setting.Value : default(AutoFocusState);
        }

        public float GetEV()
        {
            CameraSetting setting = GetSetting(SettingIds.EV);
            return setting != null ? (float)setting.Value / 1000 : 0;
        }

        public void ModifyEV(short steps)
        {
            DoMainSettingI16(SettingIds.EV, steps);
        }

        public float GetFlash()
        {
            CameraSetting setting = GetSetting(SettingIds.Flash);
            return setting != null ? (float)setting.Value / 1000 : 0;
        }

        public void ModifyFlash(short steps)
        {
            DoMainSettingI16(SettingIds.Flash, steps);
        }

        public ImageFileFormat GetFileFormat()
        {
            CameraSetting setting = GetSetting(SettingIds.FileFormat);
            return setting != null ? (ImageFileFormat)setting.Value : default(ImageFileFormat);
        }

        public void SetFileFormat(ImageFileFormat fileFormat)
        {
            DoSubSettingI16(SettingIds.FileFormat, (short)fileFormat);
        }

        public PictureEffect[] GetAvailablePictureEffects()
        {
            List<PictureEffect> result = new List<PictureEffect>();
            CameraSetting setting = GetSetting(SettingIds.PictureEffect);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((PictureEffect)value);
            }
            return result.ToArray();
        }

        public PictureEffect GetPictureEffect()
        {
            CameraSetting setting = GetSetting(SettingIds.PictureEffect);
            return setting != null ? (PictureEffect)setting.Value : default(PictureEffect);
        }

        public void SetPictureEffect(PictureEffect pictureEffect)
        {
            DoSubSettingI16(SettingIds.PictureEffect, (short)pictureEffect);
        }

        public DroHdr[] GetAvailableDroHdr()
        {
            List<DroHdr> result = new List<DroHdr>();
            CameraSetting setting = GetSetting(SettingIds.DroHdr);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((DroHdr)value);
            }
            return result.ToArray();
        }

        public DroHdr GetDroHdr()
        {
            CameraSetting setting = GetSetting(SettingIds.DroHdr);
            return setting != null ? (DroHdr)setting.Value : default(DroHdr);
        }

        public void SetDroHdr(DroHdr droHdr)
        {
            DoSubSettingI16(SettingIds.DroHdr, (short)droHdr);
        }

        public AspectRatio[] GetAvailableAspectRatios()
        {
            List<AspectRatio> result = new List<AspectRatio>();
            CameraSetting setting = GetSetting(SettingIds.AspectRatio);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((AspectRatio)value);
            }
            return result.ToArray();
        }

        public AspectRatio GetAspectRatio()
        {
            CameraSetting setting = GetSetting(SettingIds.AspectRatio);
            return setting != null ? (AspectRatio)setting.Value : default(AspectRatio);
        }

        public void SetAspectRatio(AspectRatio aspectRatio)
        {
            DoSubSettingU8(SettingIds.AspectRatio, (byte)aspectRatio);
        }

        public FocusMode[] GetAvailableFocusModes()
        {
            List<FocusMode> result = new List<FocusMode>();
            CameraSetting setting = GetSetting(SettingIds.FocusMode);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((FocusMode)value);
            }
            return result.ToArray();
        }

        public FocusMode GetFocusMode()
        {
            CameraSetting setting = GetSetting(SettingIds.FocusMode);
            return setting != null ? (FocusMode)setting.Value : default(FocusMode);
        }

        public void SetFocusMode(FocusMode focusMode)
        {
            DoSubSettingI16(SettingIds.FocusMode, (short)focusMode);
        }

        public void SetFocusMode(FocusModeToggle focusMode)
        {
            DoMainSettingI16(SettingIds.FocusModeToggleRequest, (short)focusMode);
        }

        /// <summary>
        /// Maximum value is 7/-7
        /// </summary>
        /// <param name="steps"></param>
        public void ModifyFocusDistance(int steps)
        {
            // TODO: If the steps value is larger than 7 then use a loop?
            DoMainSettingI16(SettingIds.FocusDistance, (short)steps);
        }

        public ShootingMode GetShootingMode()
        {
            CameraSetting setting = GetSetting(SettingIds.ShootingMode);
            return setting != null ? (ShootingMode)setting.Value : default(ShootingMode);
        }

        public WhiteBalance[] GetAvailableWhiteBalance()
        {
            List<WhiteBalance> result = new List<WhiteBalance>();
            CameraSetting setting = GetSetting(SettingIds.WhiteBalance);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((WhiteBalance)value);
            }
            return result.ToArray();
        }

        public WhiteBalance GetWhiteBalance()
        {
            CameraSetting setting = GetSetting(SettingIds.WhiteBalance);
            return setting != null ? (WhiteBalance)setting.Value : default(WhiteBalance);
        }

        public void SetWhiteBalance(WhiteBalance whiteBalance)
        {
            DoSubSettingI16(SettingIds.WhiteBalance, (short)whiteBalance);
        }

        public ushort GetWhiteBalanceColorTemp()
        {
            CameraSetting setting = GetSetting(SettingIds.WhiteBalanceColorTemp);
            return setting != null ? (ushort)setting.Value : (ushort)0;
        }

        /// <summary>
        /// Accepted values: 2500k-9900k (in increments of 100)
        /// </summary>
        public void SetWhiteBalanceColorTemp(ushort value)
        {
            DoSubSettingI16(SettingIds.WhiteBalanceColorTemp, (short)value);
        }

        public WhiteBalanceAB GetWhiteBalanceAB()
        {
            CameraSetting setting = GetSetting(SettingIds.WhiteBalanceAB);
            return setting != null ? (WhiteBalanceAB)setting.Value : default(WhiteBalanceAB);
        }

        public void SetWhiteBalanceAB(WhiteBalanceAB value)
        {
            DoSubSettingU8(SettingIds.WhiteBalanceAB, (byte)value);
        }

        public WhiteBalanceGM GetWhiteBalanceGM()
        {
            CameraSetting setting = GetSetting(SettingIds.WhiteBalanceGM);
            return setting != null ? (WhiteBalanceGM)setting.Value : default(WhiteBalanceGM);
        }

        public void SetWhiteBalanceGM(WhiteBalanceGM value)
        {
            DoSubSettingU8(SettingIds.WhiteBalanceGM, (byte)value);
        }

        public DriveMode[] GetAvailableDriveModes()
        {
            List<DriveMode> result = new List<DriveMode>();
            CameraSetting setting = GetSetting(SettingIds.DriveMode);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((DriveMode)value);
            }
            return result.ToArray();
        }

        public DriveMode GetDriveMode()
        {
            CameraSetting setting = GetSetting(SettingIds.DriveMode);
            return setting != null ? (DriveMode)setting.Value : default(DriveMode);
        }

        public void SetDriveMode(DriveMode driveMode)
        {
            DoSubSettingI16(SettingIds.DriveMode, (short)driveMode);
        }

        public FlashMode[] GetAvailableFlashModes()
        {
            List<FlashMode> result = new List<FlashMode>();
            CameraSetting setting = GetSetting(SettingIds.FlashMode);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((FlashMode)value);
            }
            return result.ToArray();
        }

        public FlashMode GetFlashMode()
        {
            CameraSetting setting = GetSetting(SettingIds.FlashMode);
            return setting != null ? (FlashMode)setting.Value : default(FlashMode);
        }

        public void SetFlashMode(FlashMode flashMode)
        {
            DoSubSettingI16(SettingIds.FlashMode, (short)flashMode);
        }

        public MeteringMode[] GetAvailableMeteringModes()
        {
            List<MeteringMode> result = new List<MeteringMode>();
            CameraSetting setting = GetSetting(SettingIds.MeteringMode);
            foreach (int value in setting.AcceptedValues)
            {
                result.Add((MeteringMode)value);
            }
            return result.ToArray();
        }

        public MeteringMode GetMeteringMode()
        {
            CameraSetting setting = GetSetting(SettingIds.MeteringMode);
            return setting != null ? (MeteringMode)setting.Value : default(MeteringMode);
        }

        public void SetMeteringMode(MeteringMode value)
        {
            DoSubSettingI16(SettingIds.MeteringMode, (short)value);
        }

        public float GetFocusMagnifier()
        {
            CameraSetting setting = GetSetting(SettingIds.MeteringMode);
            return setting != null && setting.Value != 0 ? (float)setting.Value / 10 : 1;
        }

        public FocusMagnifierPhase GetFocusMagnifierPhase()
        {
            CameraSetting setting = GetSetting(SettingIds.MeteringMode);
            return setting != null ? (FocusMagnifierPhase)setting.Value : default(FocusMagnifierPhase);
        }

        public Position GetFocusMagnifierPosition()
        {
            CameraSetting setting = GetSetting(SettingIds.FocusMagnifierPosition);
            return setting != null ? new Position(setting.SubValue, setting.Value) : default(Position);
        }

        public void StepFocusMagnifier(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                DoMainSettingI16(SettingIds.FocusMagnifierRequest, 2);
                DoMainSettingI16(SettingIds.FocusMagnifierRequest, 1);
            }
        }

        public void MoveFocusMagnifier(FocusMagnifierDirection direction, int steps)
        {
            SettingIds opcode;
            switch (direction)
            {
                case FocusMagnifierDirection.Left:
                    opcode = SettingIds.FocusMagnifierMoveLeftRequest;
                    break;
                case FocusMagnifierDirection.Right:
                    opcode = SettingIds.FocusMagnifierMoveRightRequest;
                    break;
                case FocusMagnifierDirection.Up:
                    opcode = SettingIds.FocusMagnifierMoveUpRequest;
                    break;
                case FocusMagnifierDirection.Down:
                    opcode = SettingIds.FocusMagnifierMoveDownRequest;
                    break;
                default:
                    return;
            }
            for (int i = 0; i < steps; i++)
            {
                DoMainSettingI16(opcode, 2);
                DoMainSettingI16(opcode, 1);
            }
        }
    }

    public struct ShutterSpeedInfo
    {
        public float Value;

        /// <summary>
        /// e.g. 1/80 (as opposed to a value like 0.4")
        /// </summary>
        public bool IsFraction;

        public ShutterSpeedInfo(float value, bool isFraction)
        {
            Value = value;
            IsFraction = isFraction;
        }
    }

    public struct Position
    {
        public short X;
        public short Y;

        public Position(short x, short y)
        {
            X = x;
            Y = y;
        }

        public Position(int x, int y)
        {
            X = (short)x;
            Y = (short)y;
        }
    }
}
