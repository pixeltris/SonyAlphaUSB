using System;
using System.Collections.Generic;
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
                    if (response.ReadByte() != 1 || response.ReadByte() != 0x20)
                    {
                        return false;
                    }
                }
            }
            return true;
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
            ModifyMainSettingI16(MainSettingIds.FNumber, modifyAmount);
        }

        private void ModifyMainSettingI16(MainSettingIds id, short amount)
        {
            using (Packet request = new Packet(OpCodes.MainSetting))
            {
                request.WriteHexString("00 00 00 00 00 00 00 00");
                request.WriteUInt16((ushort)id);
                request.WriteHexString("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 04 00 00 00");
                request.WriteInt16(amount);
                using (Packet response = Packet.Reader(WIA.SendCommand(this, request.GetBuffer())))
                {
                    // TODO: Validate the result;
                    //if (response.ReadByte() != 1 || response.ReadByte() != 0x20)
                    //{
                    //    return false;
                    //}
                }
            }

            //07 92 00 00 00 00 00 00 00 00 C9 D2 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 04 00 00 00 02 00
        }
    }
}
