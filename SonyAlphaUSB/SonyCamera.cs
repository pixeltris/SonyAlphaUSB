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

            return true;
        }

        public void ModifyFNumber(short modifyAmount)
        {
            DoMainSettingI16(SettingIds.FNumber, modifyAmount);
        }
        
        public void CapturePhoto()
        {
            // NOTE: The camera always wants to send the data over to the PC. Is there any way to do it without transfer?
            DoMainSettingI16(SettingIds.CapturePhoto1, 2);
            DoMainSettingI16(SettingIds.CapturePhoto2, 2);//takes the photo
            DoMainSettingI16(SettingIds.CapturePhoto1, 1);
            DoMainSettingI16(SettingIds.CapturePhoto2, 1);
        }

        private bool DoMainSettingI16(SettingIds id, short amount)
        {
            using (Packet request = new Packet(OpCodes.MainSetting))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00");
                request.WriteUInt16((ushort)id);
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 04 00 00 00");
                request.WriteInt16(amount);
                byte[] buffer = WIA.SendCommand(this, request.GetBuffer());
                using (Packet response = Packet.Reader(buffer))
                {
                    // TODO: Validate the result;
                    if (!IsValidResponse(response))
                    {
                        return false;
                    }
                    return true;
                }
            }
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
                Debug.Assert(imageInfoUnk == 14337);
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
    }
}
