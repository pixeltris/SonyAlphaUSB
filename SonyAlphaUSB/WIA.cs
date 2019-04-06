using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SonyAlphaUSB.Interop;

namespace SonyAlphaUSB
{
    // The regular WIA COM wrapper lacks functionality required to talk to Sony cameras
    // Interop.WIA.dll
    // Microsoft Windows Image Acquisition Library v2.0

    public static class WIA
    {
        static IWiaDevMgr deviceManager;

        public static SonyCamera[] FindCameras()
        {
            Dictionary<string, SonyCamera> cameras = new Dictionary<string, SonyCamera>();

            if (deviceManager == null)
            {
                deviceManager = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("a1f4e726-8cf1-11d1-bf92-0060081ed811"))) as IWiaDevMgr;
            }
            if (deviceManager == null)
            {
                return new SonyCamera[0];
            }

            IEnumWIA_DEV_INFO deviceInfo;
            if (Success(deviceManager.EnumDeviceInfo(Defs.WIA_DEVINFO_ENUM_LOCAL, out deviceInfo)) &&
                Success(deviceInfo.Reset()))
            {
                IWiaPropertyStorage propertyStorage;
                uint numFetchedStorage = 0;
                while (Success(deviceInfo.Next(1, out propertyStorage, ref numFetchedStorage)) && numFetchedStorage > 0)
                {
                    IEnumSTATPROPSTG propsEnumerator;
                    if (Success(propertyStorage.Enum(out propsEnumerator)) &&
                        Success(propsEnumerator.Reset()))
                    {
                        bool knownName = false;
                        bool knownManufacturer = false;
                        string deviceId = null;
                        string deviceName = null;

                        uint numFetchedProps;
                        STATPROPSTG[] props = new STATPROPSTG[1];
                        while (Success(propsEnumerator.Next(1, props, out numFetchedProps)) && numFetchedProps > 0)
                        {
                            for (int i = 0; i < numFetchedProps; i++)
                            {
                                STATPROPSTG prop = props[i];
                                switch (prop.lpwstrName)
                                {
                                    case "Manufacturer":
                                        switch (GetPropertyValue(propertyStorage, prop.propid))
                                        {
                                            case "Sony Corporation":
                                                knownManufacturer = true;
                                                break;
                                        }
                                        break;
                                    case "Name":
                                        deviceName = GetPropertyValue(propertyStorage, prop.propid);
                                        if (SonyCamera.SupportedCameras.Contains(deviceName))
                                        {
                                            knownName = true;
                                        }
                                        break;
                                    case "Unique Device ID":
                                        deviceId = GetPropertyValue(propertyStorage, prop.propid);
                                        break;
                                }
                                //Console.WriteLine(prop.lpwstrName + " " + GetPropertyValue(propertyStorage, prop.propid));
                            }
                        }

                        if (!string.IsNullOrEmpty(deviceId) && knownManufacturer && knownName)
                        {
                            cameras[deviceId] = new SonyCamera(deviceId, deviceName);
                        }
                    }
                }
            }

            return cameras.Values.ToArray();
        }

        public static IWiaItem Connect(SonyCamera camera)
        {
            IWiaItem item;
            if (!Success(deviceManager.CreateDevice(camera.UniqueId, out item)))
            {
                item = null;
            }
            return item;
        }

        public static byte[] SendCommand(SonyCamera camera, byte[] buffer)
        {
            return SendCommand(camera, buffer, 0x100000);
        }

        public static byte[] SendCommand(SonyCamera camera, byte[] buffer, int outputBufferLength)
        {
            byte[] outputBuffer = new byte[outputBufferLength];
            int readBytes;
            if (Success(camera.deviceExtras.Escape(256, buffer, buffer.Length, outputBuffer, outputBufferLength, out readBytes)) && readBytes > 0)
            {
                byte[] result = new byte[readBytes];
                Buffer.BlockCopy(outputBuffer, 0, result, 0, result.Length);
                return result;
            }
            return null;
        }

        public static bool Success(HRESULT result)
        {
            return result.Value == IntPtr.Zero;
        }

        private static string GetPropertyValue(IWiaPropertyStorage propertyStorage, uint propid)
        {
            PROPVARIANT[] propvars = new PROPVARIANT[1];
            PROPSPEC[] propspec = new PROPSPEC[1];
            propspec[0].ulKind = (uint)1;//PRSPEC.PROPID;
            propspec[0].u.propId = propid;
            try
            {
                if (Success(propertyStorage.ReadMultiple(1, propspec, propvars)))
                {
                    return propvars[0].Value.ToString();
                }
            }
            finally
            {
                propvars[0].Clear();
            }
            return null;
        }
    }
}

namespace SonyAlphaUSB.Interop
{
    //CLSID_WiaDevMgr - a1f4e726-8cf1-11d1-bf92-0060081ed811
    //CLSID_WiaDevMgr2 - b6c292bc-7c88-41ee-8b54-8ec92617e599

    // NOTE: Only CLSID_WiaDevMgr seems to work (which Sony uses) (even though the docs say to use CLSID_WiaDevMgr2 on vista+)

    // NOTE: Most of these interfaces haven't been tested!

    // Some structures / interfaces taken from:
    // https://github.com/SolidEdgeCommunity/SolidEdge.Community.Reader/blob/master/src/SolidEdge.Community.Reader/Native/Interfaces.cs
    // https://github.com/SolidEdgeCommunity/SolidEdge.Community.Reader/blob/master/src/SolidEdge.Community.Reader/Native/Structures.cs

    public static class Defs
    {
        public const int WIA_DEVINFO_ENUM_ALL = 0x0000000f;
        public const int WIA_DEVINFO_ENUM_LOCAL = 0x00000010;
    }

    public struct HRESULT
    {
        public IntPtr Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PROPSPECUNION
    {
        [FieldOffset(0)]
        public uint propId;//PROPID(ULONG)
        [FieldOffset(0)]
        public IntPtr name;//LPOLESTR
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPSPEC
    {
        public uint ulKind;
        public PROPSPECUNION u;
    }

    // Credit: http://blogs.msdn.com/b/adamroot/archive/2008/04/11/interop-with-propvariants-in-net.aspx
    /// <summary>
    /// Represents the OLE struct PROPVARIANT.
    /// </summary>
    /// <remarks>
    /// Must call Clear when finished to avoid memory leaks. If you get the value of
    /// a VT_UNKNOWN prop, an implicit AddRef is called, thus your reference will
    /// be active even after the PropVariant struct is cleared.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct PROPVARIANT
    {
        // The layout of these elements needs to be maintained.
        //
        // NOTE: We could use LayoutKind.Explicit, but we want
        //       to maintain that the IntPtr may be 8 bytes on
        //       64-bit architectures, so we'll let the CLR keep
        //       us aligned.
        //
        // NOTE: In order to allow x64 compat, we need to allow for
        //       expansion of the IntPtr. However, the BLOB struct
        //       uses a 4-byte int, followed by an IntPtr, so
        //       although the p field catches most pointer values,
        //       we need an additional 4-bytes to get the BLOB
        //       pointer. The p2 field provides this, as well as
        //       the last 4-bytes of an 8-byte value on 32-bit
        //       architectures.

        // This is actually a VarEnum value, but the VarEnum type
        // shifts the layout of the struct by 4 bytes instead of the
        // expected 2.
        ushort vt;
        ushort wReserved1;
        ushort wReserved2;
        ushort wReserved3;
        public IntPtr p;
        int p2;

        sbyte cVal // CHAR cVal;
        {
            get { return (sbyte)GetDataBytes()[0]; }
        }

        byte bVal // UCHAR bVal;
        {
            get { return GetDataBytes()[0]; }
        }

        short iVal // SHORT iVal;
        {
            get { return BitConverter.ToInt16(GetDataBytes(), 0); }
        }

        ushort uiVal // USHORT uiVal;
        {
            get { return BitConverter.ToUInt16(GetDataBytes(), 0); }
        }

        int lVal // LONG lVal;
        {
            get { return BitConverter.ToInt32(GetDataBytes(), 0); }
        }

        uint ulVal // ULONG ulVal;
        {
            get { return BitConverter.ToUInt32(GetDataBytes(), 0); }
        }

        long hVal // LARGE_INTEGER hVal;
        {
            get { return BitConverter.ToInt64(GetDataBytes(), 0); }
        }

        ulong uhVal // ULARGE_INTEGER uhVal;
        {
            get { return BitConverter.ToUInt64(GetDataBytes(), 0); }
        }

        float fltVal // FLOAT fltVal;
        {
            get { return BitConverter.ToSingle(GetDataBytes(), 0); }
        }

        double dblVal // DOUBLE dblVal;
        {
            get { return BitConverter.ToDouble(GetDataBytes(), 0); }
        }

        bool boolVal // VARIANT_BOOL boolVal;
        {
            get { return (iVal == 0 ? false : true); }
        }

        int scode // SCODE scode;
        {
            get { return lVal; }
        }

        decimal cyVal // CY cyVal;
        {
            get { return decimal.FromOACurrency(hVal); }
        }

        DateTime date // DATE date;
        {
            get { return DateTime.FromOADate(dblVal); }
        }

        private byte[] GetBlobData()
        {
            var blobData = new byte[lVal];
            IntPtr pBlobData;

            try
            {
                switch (IntPtr.Size)
                {
                    case 4:
                        pBlobData = new IntPtr(p2);
                        break;

                    case 8:
                        pBlobData = new IntPtr(BitConverter.ToInt64(GetDataBytes(), sizeof(int)));
                        break;

                    default:
                        throw new NotSupportedException();
                }

                Marshal.Copy(pBlobData, blobData, 0, lVal);
            }
            catch
            {
                return null;
            }

            return blobData;
        }

        /// <summary>
        /// Gets a byte array containing the data bits of the struct.
        /// </summary>
        /// <returns>A byte array that is the combined size of the data bits.</returns>
        private byte[] GetDataBytes()
        {
            var ret = new byte[IntPtr.Size + sizeof(int)];

            if (IntPtr.Size == 4)
            {
                BitConverter.GetBytes(p.ToInt32()).CopyTo(ret, 0);
            }
            else if (IntPtr.Size == 8)
            {
                BitConverter.GetBytes(p.ToInt64()).CopyTo(ret, 0);
            }

            return ret;
        }

        /// <summary>
        /// Called to clear the PropVariant's referenced and local memory.
        /// </summary>
        /// <remarks>
        /// You must call Clear to avoid memory leaks.
        /// </remarks>
        public void Clear()
        {
            // Can't pass "this" by ref, so make a copy to call PropVariantClear with
            PROPVARIANT var = this;
            PropVariantClear(ref var);

            // Since we couldn't pass "this" by ref, we need to clear the member fields manually
            // NOTE: PropVariantClear already freed heap data for us, so we are just setting
            //       our references to null.
            vt = (ushort)VarEnum.VT_EMPTY;
            wReserved1 = wReserved2 = wReserved3 = 0;
            p = IntPtr.Zero;
            p2 = 0;
        }

        /// <summary>
        /// Gets the variant type.
        /// </summary>
        public VarEnum Type
        {
            get { return (VarEnum)vt; }
        }

        /// <summary>
        /// Gets the variant value.
        /// </summary>
        public object Value
        {
            get
            {
                switch ((VarEnum)vt)
                {
                    case VarEnum.VT_I1:
                        return cVal;
                    case VarEnum.VT_UI1:
                        return bVal;
                    case VarEnum.VT_I2:
                        return iVal;
                    case VarEnum.VT_UI2:
                        return uiVal;
                    case VarEnum.VT_I4:
                    case VarEnum.VT_INT:
                        return lVal;
                    case VarEnum.VT_UI4:
                    case VarEnum.VT_UINT:
                        return ulVal;
                    case VarEnum.VT_I8:
                        return hVal;
                    case VarEnum.VT_UI8:
                        return uhVal;
                    case VarEnum.VT_R4:
                        return fltVal;
                    case VarEnum.VT_R8:
                        return dblVal;
                    case VarEnum.VT_BOOL:
                        return boolVal;
                    case VarEnum.VT_ERROR:
                        return scode;
                    case VarEnum.VT_CY:
                        return cyVal;
                    case VarEnum.VT_DATE:
                        return date;
                    case VarEnum.VT_FILETIME:
                        if (hVal > 0)
                        {
                            return DateTime.FromFileTime(hVal);
                        }
                        else
                        {
                            return null;
                        }
                    case VarEnum.VT_BSTR:
                        return Marshal.PtrToStringBSTR(p);
                    case VarEnum.VT_LPSTR:
                        return Marshal.PtrToStringAnsi(p);
                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(p);
                    case VarEnum.VT_UNKNOWN:
                        return Marshal.GetObjectForIUnknown(p);
                    case VarEnum.VT_DISPATCH:
                        return p;
                    case VarEnum.VT_CLSID:
                        return Marshal.PtrToStructure(p, typeof(Guid));
                    //default:
                    //    throw new NotSupportedException("The type of this variable is not support ('" + vt.ToString() + "')");
                }

                return null;
            }
        }

        [DllImport("ole32.dll")]
        extern static int PropVariantClear(ref PROPVARIANT pvar);
    }

    public struct STATPROPSETSTG
    {
#pragma warning disable 0649
        public Guid fmtid;                                              //FMTID
        public Guid clsid;                                              //CLSID
        public uint grfFlags;                                           //DWORD
        public System.Runtime.InteropServices.ComTypes.FILETIME mtime;  //FILETIME
        public System.Runtime.InteropServices.ComTypes.FILETIME ctime;  //FILETIME
        public System.Runtime.InteropServices.ComTypes.FILETIME atime;  //FILETIME
        public uint dwOSVersion;                                        //DWORD
#pragma warning restore 0649
    }

    public struct STATPROPSTG
    {
#pragma warning disable 0649
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpwstrName;   //LPOLESTR
        public uint propid;         //PROPID(ULONG)
        public ushort vt;           //VARTYPE(unsigned short)
#pragma warning restore 0649
    }

    [ComImport]
    [Guid("00000139-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumSTATPROPSTG
    {
        [PreserveSig]
        HRESULT Next([In] uint celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] STATPROPSTG[] rgelt, out uint pceltFetched);
        [PreserveSig]
        HRESULT Skip([In] uint celt);
        [PreserveSig]
        HRESULT Reset();
        [PreserveSig]
        HRESULT Clone(out IEnumSTATPROPSTG ppEnum);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WIA_DITHER_PATTERN_DATA
    {
        public int lSize;
        [MarshalAs(UnmanagedType.BStr)]
        public string bstrPatternName;
        public int lPatternWidth;
        public int lPatternLength;
        public int cbPattern;
        public IntPtr pbPattern;//BYTE*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WIA_FORMAT_INFO
    {
        public Guid guidFormatID;
        public int lTymed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WIA_PROPID_TO_NAME
    {
        public uint propid;//PROPID
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszName;//LPOLESTR
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WIA_DATA_TRANSFER_INFO
    {
        public uint ulSize;
        public uint ulSection;
        public uint ulBufferSize;
        public bool bDoubleBuffer;
        public uint ulReserved1;
        public uint ulReserved2;
        public int ulReserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WIA_EXTENDED_TRANSFER_INFO
    {
        public uint ulSize;
        public uint ulMinBufferSize;
        public uint ulOptimalBufferSize;
        public uint ulMaxBufferSize;
        public uint ulNumBuffers;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WIA_DEV_CAP
    {
        public Guid guid;
        public uint ulFlags;
        [MarshalAs(UnmanagedType.BStr)]
        public string bstrName;
        [MarshalAs(UnmanagedType.BStr)]
        public string bstrDescription;
        [MarshalAs(UnmanagedType.BStr)]
        public string bstrIcon;
        [MarshalAs(UnmanagedType.BStr)]
        public string bstrCommandline;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WiaTransferParams
    {
        public int lMessage;
        public int lPercentComplete;
        public ulong ulTransferredBytes;
        public HRESULT hrErrorStatus;
    }

    [ComImport]
    [Guid("5eb2502a-8cf1-11d1-bf92-0060081ed811")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaDevMgr
    {
        [PreserveSig]
        HRESULT EnumDeviceInfo(int lFlag, out IEnumWIA_DEV_INFO ppIEnum);
        [PreserveSig]
        HRESULT CreateDevice([MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, out IWiaItem ppWiaItemRoot);
        [PreserveSig]
        HRESULT SelectDeviceDlg(IntPtr hwndParent, int lDeviceType, int lFlags, [MarshalAs(UnmanagedType.BStr)] ref string pbstrDeviceID, ref IWiaItem ppItemRoot);
        [PreserveSig]
        HRESULT SelectDeviceDlgID(IntPtr hwndParent, int lDeviceType, int lFlags, [MarshalAs(UnmanagedType.BStr)] ref string pbstrDeviceID, ref IWiaItem ppItemRoot);
        [PreserveSig]
        HRESULT GetImageDlg(IntPtr hwndParent, int lDeviceType, int lFlags, int lIntent, IWiaItem pItemRoot, [MarshalAs(UnmanagedType.BStr)] string bstrFilename, ref Guid pguidFormat);
        [PreserveSig]
        HRESULT RegisterEventCallbackProgram(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, ref Guid pEventGUID, [MarshalAs(UnmanagedType.BStr)] string bstrCommandline, [MarshalAs(UnmanagedType.BStr)] string bstrName, [MarshalAs(UnmanagedType.BStr)] string bstrDescription, [MarshalAs(UnmanagedType.BStr)] string bstrIcon);
        [PreserveSig]
        HRESULT RegisterEventCallbackInterface(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, ref Guid pEventGUID, ref IWiaEventCallback pIWiaEventCallback, out object pEventObject);//IUnknown
        [PreserveSig]
        HRESULT RegisterEventCallbackCLSID(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, ref Guid pEventGUID, ref Guid pClsID, [MarshalAs(UnmanagedType.BStr)] string bstrName, [MarshalAs(UnmanagedType.BStr)] string bstrDescription, [MarshalAs(UnmanagedType.BStr)] string bstrIcon);
        [PreserveSig]
        HRESULT AddDeviceDlg(IntPtr hwndParent, int lFlags);
    }

    [ComImport]
    [Guid("5e38b83c-8cf1-11d1-bf92-0060081ed811")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumWIA_DEV_INFO
    {
        [PreserveSig]
        HRESULT Next(uint celt, out IWiaPropertyStorage rgelt, ref uint pceltFetched);
        [PreserveSig]
        HRESULT Skip(uint celt);
        [PreserveSig]
        HRESULT Reset();
        [PreserveSig]
        HRESULT Clone(out IEnumWIA_DEV_INFO ppIEnum);
        [PreserveSig]
        HRESULT GetCount(out uint celt);
    }

    [ComImport]
    [Guid("ae6287b0-0084-11d2-973b-00a0c9068f2e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaEventCallback
    {
        [PreserveSig]
        HRESULT ImageEventCallback(ref Guid pEventGUID, [MarshalAs(UnmanagedType.BStr)] string bstrEventDescription, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceDescription, int dwDeviceType, [MarshalAs(UnmanagedType.BStr)] string bstrFullItemName, ref int pulEventType, uint ulReserved);
    }

    [ComImport]
    [Guid("a558a866-a5b0-11d2-a08f-00c04f72dc3c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaDataCallback
    {
        [PreserveSig]
        HRESULT BandedDataCallback(int lMessage, int lStatus, int lPercentComplete, int lOffset, int lLength, int lReserved, int lResLength, byte[] pbBuffer);//BYTE*
    }

    [ComImport]
    [Guid("a6cef998-a5b0-11d2-a08f-00c04f72dc3c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaDataTransfer
    {
        [PreserveSig]
        HRESULT idtGetData(IntPtr pMedium, IWiaDataCallback pIWiaDataCallback);//LPSTGMEDIUM
        [PreserveSig]
        HRESULT idtGetBandedData(IntPtr pWiaDataTransInfo, IWiaDataCallback pIWiaDataCallback);//PWIA_DATA_TRANSFER_INFO
        [PreserveSig]
        HRESULT idtQueryGetData(ref WIA_FORMAT_INFO pfe);
        [PreserveSig]
        HRESULT idtEnumWIA_FORMAT_INFO(out IEnumWIA_FORMAT_INFO ppEnum);
        [PreserveSig]
        HRESULT idtGetExtendedTransferInfo(out WIA_EXTENDED_TRANSFER_INFO pExtendedTransferInfo);
    }

    [ComImport]
    [Guid("4db1ad10-3391-11d2-9a33-00c04fa36145")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaItem
    {
        [PreserveSig]
        HRESULT GetItemType(out int pItemType);
        [PreserveSig]
        HRESULT AnalyzeItem(int lFlags);
        [PreserveSig]
        HRESULT EnumChildItems(out IEnumWiaItem ppIEnumWiaItem);
        [PreserveSig]
        HRESULT DeleteItem(int lFlags);
        [PreserveSig]
        HRESULT CreateChildItem(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrItemName, [MarshalAs(UnmanagedType.BStr)] string bstrFullItemName, out IWiaItem ppIWiaItem);
        [PreserveSig]
        HRESULT EnumRegisterEventInfo(int lFlags, ref Guid pEventGUID, out IEnumWIA_DEV_CAPS ppIEnum);
        [PreserveSig]
        HRESULT FindItemByName(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrFullItemName, out IWiaItem ppIWiaItem);
        [PreserveSig]
        HRESULT DeviceDlg(IntPtr hwndParent, int lFlags, int lIntent, out int plItemCount, out IWiaItem ppIWiaItem);//tripple pointer? this might need to be an IntPtr
        [PreserveSig]
        HRESULT DeviceCommand(int lFlags, ref Guid pCmdGUID, ref IWiaItem pIWiaItem);
        [PreserveSig]
        HRESULT GetRootItem(out IWiaItem ppIWiaItem);
        [PreserveSig]
        HRESULT EnumDeviceCapabilities(int lFlags, out IEnumWIA_DEV_CAPS ppIEnumWIA_DEV_CAPS);
        [PreserveSig]
        HRESULT DumpItemData([MarshalAs(UnmanagedType.BStr)] string bstrData);
        [PreserveSig]
        HRESULT DumpDrvItemData([MarshalAs(UnmanagedType.BStr)] string bstrData);
        [PreserveSig]
        HRESULT DumpTreeItemData([MarshalAs(UnmanagedType.BStr)] string bstrData);
        [PreserveSig]
        HRESULT Diagnostic(uint ulSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] pBuffer);//BYTE*
    }

    [ComImport]
    [Guid("98B5E8A0-29CC-491a-AAC0-E6DB4FDCCEB6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaPropertyStorage
    {
        // IPropertyStorage
        [PreserveSig]
        HRESULT ReadMultiple([In] uint cpspec, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] PROPSPEC[] rgpspec, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] PROPVARIANT[] rgpropvar);
        [PreserveSig]
        HRESULT WriteMultiple([In] uint cpspec, [In] PROPSPEC[] rgpspec, [In] PROPVARIANT[] rgpropvar, [In] uint propidNameFirst);
        [PreserveSig]
        HRESULT DeleteMultiple([In] uint cpspec, [In] PROPSPEC[] rgpspec);
        [PreserveSig]
        HRESULT ReadPropertyNames([In] uint cpropid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] uint[] rgpropid, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0)] [In, Out] string[] rglpwstrName);
        [PreserveSig]
        HRESULT WritePropertyNames([In] uint cpropid, [In] uint[] rgpropid, [In] string[] rglpwstrName);
        [PreserveSig]
        HRESULT DeletePropertyNames([In] uint cpropid, [In] uint[] rgpropid);
        [PreserveSig]
        HRESULT Commit([In] uint grfCommitFlags);
        [PreserveSig]
        HRESULT Revert();
        [PreserveSig]
        HRESULT Enum(out IEnumSTATPROPSTG ppEnum);
        [PreserveSig]
        HRESULT SetTimes([In] System.Runtime.InteropServices.ComTypes.FILETIME[] pctime, [In] System.Runtime.InteropServices.ComTypes.FILETIME[] patime, [In] System.Runtime.InteropServices.ComTypes.FILETIME[] pmtime);
        [PreserveSig]
        HRESULT SetClass([In] ref Guid clsid);
        [PreserveSig]
        HRESULT Stat([Out] out STATPROPSETSTG pstatpsstg);
        // IWiaPropertyStorage
        [PreserveSig]
        HRESULT GetPropertyAttributes([In] uint cpspec, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [In] PROPSPEC[] rgpspec, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] ulong[] rgflags, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] [Out] PROPVARIANT[] rgpropvar);
        [PreserveSig]
        HRESULT GetCount(out uint pulNumProps);
        [PreserveSig]
        HRESULT GetPropertyStream(out Guid pCompatibilityId, out IntPtr ppIStream);//IStream**
        [PreserveSig]
        HRESULT SetPropertyStream(ref Guid pCompatibilityId, IntPtr pIStream);//IStream*
    }

    [ComImport]
    [Guid("5e8383fc-3391-11d2-9a33-00c04fa36145")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumWiaItem
    {
        [PreserveSig]
        HRESULT Next(uint celt, out IWiaItem ppIWiaItem, ref uint pceltFetched);
        [PreserveSig]
        HRESULT Skip(uint celt);
        [PreserveSig]
        HRESULT Reset();
        [PreserveSig]
        HRESULT Clone(out IEnumWiaItem ppIEnum);
        [PreserveSig]
        HRESULT GetCount(out uint celt);
    }

    [ComImport]
    [Guid("1fcc4287-aca6-11d2-a093-00c04f72dc3c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumWIA_DEV_CAPS
    {
        [PreserveSig]
        HRESULT Next(uint celt, out WIA_DEV_CAP rgelt, ref uint pceltFetched);
        [PreserveSig]
        HRESULT Skip(uint celt);
        [PreserveSig]
        HRESULT Reset();
        [PreserveSig]
        HRESULT Clone(out IEnumWIA_DEV_CAPS ppIEnum);
        [PreserveSig]
        HRESULT GetCount(out uint celt);
    }

    [ComImport]
    [Guid("81BEFC5B-656D-44f1-B24C-D41D51B4DC81")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumWIA_FORMAT_INFO
    {
        [PreserveSig]
        HRESULT Next(uint celt, out WIA_FORMAT_INFO rgelt, ref uint pceltFetched);
        [PreserveSig]
        HRESULT Skip(uint celt);
        [PreserveSig]
        HRESULT Reset();
        [PreserveSig]
        HRESULT Clone(out IEnumWIA_FORMAT_INFO ppIEnum);
        [PreserveSig]
        HRESULT GetCount(out uint celt);
    }

    [ComImport]
    [Guid("A00C10B6-82A1-452f-8B6C-86062AAD6890")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaLog
    {
        [PreserveSig]
        HRESULT InitializeLog(int hInstance);
        [PreserveSig]
        HRESULT hResult(int hResult);
        [PreserveSig]
        HRESULT Log(int lFlags, int lResID, int lDetail, [MarshalAs(UnmanagedType.BStr)] string bstrText);
    }

    [ComImport]
    [Guid("AF1F22AC-7A40-4787-B421-AEb47A1FBD0B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaLogEx
    {
        [PreserveSig]
        HRESULT InitializeLogEx(IntPtr hInstance);//BYTE*
        [PreserveSig]
        HRESULT hResult(int hResult);
        [PreserveSig]
        HRESULT Log(int lFlags, int lResID, int lDetail, [MarshalAs(UnmanagedType.BStr)] string bstrText);
        [PreserveSig]
        HRESULT hResultEx(int lMethodId, int hResult);
        [PreserveSig]
        HRESULT LogEx(int lMethodId, int lFlags, int lResID, int lDetail, [MarshalAs(UnmanagedType.BStr)] string bstrText);
    }

    [ComImport]
    [Guid("70681EA0-E7BF-4291-9FB1-4E8813A3F78E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaNotifyDevMgr
    {
        [PreserveSig]
        HRESULT NewDeviceArrival();
    }

    [ComImport]
    [Guid("6291ef2c-36ef-4532-876a-8e132593778d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaItemExtras
    {
        [PreserveSig]
        HRESULT GetExtendedErrorInfo([MarshalAs(UnmanagedType.BStr)] out string bstrErrorText);
        [PreserveSig]
        HRESULT Escape(int dwEscapeCode, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] lpInData, int cbInDataSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] pOutData, int dwOutDataSize, out int pdwActualDataSize);
        [PreserveSig]
        HRESULT CancelPendingIO();
    }

    [ComImport]
    [Guid("6C16186C-D0A6-400c-80F4-D26986A0E734")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaAppErrorHandler
    {
        [PreserveSig]
        HRESULT GetWindow(out IntPtr phwnd);
        [PreserveSig]
        HRESULT ReportStatus(int lFlags, IWiaItem2 pWiaItem2, HRESULT hrStatus, int lPercentComplete);
    }

    [ComImport]
    [Guid("0e4a51b1-bc1f-443d-a835-72e890759ef3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaErrorHandler
    {
        [PreserveSig]
        HRESULT ReportStatus(int lFlags, IntPtr hwndParent, IWiaItem2 pWiaItem2, HRESULT hrStatus, int lPercentComplete);
        [PreserveSig]
        HRESULT GetStatusDescription(int lFlags, IWiaItem2 pWiaItem2, HRESULT hrStatus, [MarshalAs(UnmanagedType.BStr)] out string pbstrDescription);
    }

    [ComImport]
    [Guid("c39d6942-2f4e-4d04-92fe-4ef4d3a1de5a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaTransfer
    {
        [PreserveSig]
        HRESULT Download(int lFlags, IWiaTransferCallback pIWiaTransferCallback);
        [PreserveSig]
        HRESULT Upload(int lFlags, IntPtr pSource, IWiaTransferCallback pIWiaTransferCallback);//IStream* (2nd param)
        [PreserveSig]
        HRESULT Cancel();
        [PreserveSig]
        HRESULT EnumWIA_FORMAT_INFO(out IEnumWIA_FORMAT_INFO ppEnum);
    }

    [ComImport]
    [Guid("27d4eaaf-28a6-4ca5-9aab-e678168b9527")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaTransferCallback
    {
        [PreserveSig]
        HRESULT TransferCallback(int lFlags, ref WiaTransferParams pWiaTransferParams);
        [PreserveSig]
        HRESULT GetNextStream(int lFlags, [MarshalAs(UnmanagedType.BStr)] out string bstrItemName, [MarshalAs(UnmanagedType.BStr)] out string bstrFullItemName, out IntPtr ppDestination);//IStream**
    }

    [ComImport]
    [Guid("EC46A697-AC04-4447-8F65-FF63D5154B21")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaSegmentationFilter
    {
        [PreserveSig]
        HRESULT DetectRegions(int lFlags, IntPtr pInputStream, IWiaItem2 pWiaItem2);//IStream* (2nd param)
    }

    [ComImport]
    [Guid("A8A79FFA-450B-41f1-8F87-849CCD94EBF6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaImageFilter
    {
        [PreserveSig]
        HRESULT InitializeFilter(IWiaItem2 pWiaItem2, IWiaTransferCallback pWiaTransferCallback);
        [PreserveSig]
        HRESULT SetNewCallback(IWiaTransferCallback pWiaTransferCallback);
        [PreserveSig]
        HRESULT FilterPreviewImage(int lFlags, IWiaItem2 pWiaChildItem2, RECT InputImageExtents, IntPtr pInputStream);//IStream*
        [PreserveSig]
        HRESULT ApplyProperties(IWiaPropertyStorage pWiaPropertyStorage);
    }

    [ComImport]
    [Guid("95C2B4FD-33F2-4d86-AD40-9431F0DF08F7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaPreview
    {
        [PreserveSig]
        HRESULT GetNewPreview(int lFlags, IWiaItem2 pWiaItem2, IWiaTransferCallback pWiaTransferCallback);
        [PreserveSig]
        HRESULT UpdatePreview(int lFlags, IWiaItem2 pChildWiaItem2, IWiaTransferCallback pWiaTransferCallback);
        [PreserveSig]
        HRESULT DetectRegions(int lFlags);
        [PreserveSig]
        HRESULT Clear();
    }

    [ComImport]
    [Guid("59970AF4-CD0D-44d9-AB24-52295630E582")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IEnumWiaItem2
    {
        [PreserveSig]
        HRESULT Next(uint celt, out IWiaItem2 ppIWiaItem2, ref uint pceltFetched);
        [PreserveSig]
        HRESULT Skip(uint celt);
        [PreserveSig]
        HRESULT Reset();
        [PreserveSig]
        HRESULT Clone(out IEnumWiaItem2 ppIEnum);
        [PreserveSig]
        HRESULT GetCount(out uint celt);
    }

    [ComImport]
    [Guid("6CBA0075-1287-407d-9B77-CF0E030435CC")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaItem2
    {
        [PreserveSig]
        HRESULT CreateChildItem(int lItemFlags, int lCreationFlags, [MarshalAs(UnmanagedType.BStr)] string bstrItemName, out IWiaItem2 ppIWiaItem2);
        [PreserveSig]
        HRESULT DeleteItem(int lFlags);
        [PreserveSig]
        HRESULT EnumChildItems(ref Guid pCategoryGUID, out IEnumWiaItem2 ppIEnumWiaItem2);
        [PreserveSig]
        HRESULT FindItemByName(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrFullItemName, out IWiaItem2 ppIWiaItem2);
        [PreserveSig]
        HRESULT GetItemCategory(out Guid pItemCategoryGUID);
        [PreserveSig]
        HRESULT GetItemType(out int pItemType);
        [PreserveSig]
        HRESULT DeviceDlg(int lFlags, IntPtr hwndParent, [MarshalAs(UnmanagedType.BStr)] string bstrFolderName, [MarshalAs(UnmanagedType.BStr)] string bstrFilename, ref int plNumFiles, [MarshalAs(UnmanagedType.BStr, SizeParamIndex = 4)] string[] ppbstrFilePaths, out IWiaItem2 ppItem);//tripple pointer? this might need to be an IntPtr
        [PreserveSig]
        HRESULT DeviceCommand(int lFlags, ref Guid pCmdGUID, ref IWiaItem2 ppIWiaItem2);
        [PreserveSig]
        HRESULT EnumDeviceCapabilities(int lFlags, out IEnumWIA_DEV_CAPS ppIEnumWIA_DEV_CAPS);
        [PreserveSig]
        HRESULT CheckExtension(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrName, IntPtr riidExtensionInterface, out bool pbExtensionExists);//REFIID(2nd param)
        [PreserveSig]
        HRESULT GetExtension(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrName, IntPtr riidExtensionInterface, out IntPtr ppOut);//REFIID(2nd param)
        [PreserveSig]
        HRESULT GetParentItem(out IWiaItem2 ppIWiaItem2);
        [PreserveSig]
        HRESULT GetRootItem(out IWiaItem2 ppIWiaItem2);
        [PreserveSig]
        HRESULT GetPreviewComponent(int lFlags, out IWiaPreview ppWiaPreview);
        [PreserveSig]
        HRESULT EnumRegisterEventInfo(int lFlags, ref Guid pEventGUID, out IEnumWIA_DEV_CAPS ppIEnum);
        [PreserveSig]
        HRESULT Diagnostic(uint ulSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] pBuffer);//BYTE*
    }

    [ComImport]
    [Guid("79C07CF1-CBDD-41ee-8EC3-F00080CADA7A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWiaDevMgr2
    {
        [PreserveSig]
        HRESULT EnumDeviceInfo(int lFlags, out IEnumWIA_DEV_INFO ppIEnum);
        [PreserveSig]
        HRESULT CreateDevice(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, out IWiaItem2 ppWiaItem2Root);
        [PreserveSig]
        HRESULT SelectDeviceDlg(IntPtr hwndParent, int lDeviceType, int lFlags, [MarshalAs(UnmanagedType.BStr)] ref string pbstrDeviceID, out IWiaItem2 ppItemRoot);
        [PreserveSig]
        HRESULT SelectDeviceDlgID(IntPtr hwndParent, int lDeviceType, int lFlags, [MarshalAs(UnmanagedType.BStr)] out string pbstrDeviceID);
        [PreserveSig]
        HRESULT RegisterEventCallbackInterface(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, ref Guid pEventGUID, IWiaEventCallback pIWiaEventCallback, out object pEventObject);//IUnknown**
        [PreserveSig]
        HRESULT RegisterEventCallbackProgram(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, ref Guid pEventGUID, [MarshalAs(UnmanagedType.BStr)] string bstrFullAppName, [MarshalAs(UnmanagedType.BStr)] string bstrCommandLineArg, [MarshalAs(UnmanagedType.BStr)] string bstrName, [MarshalAs(UnmanagedType.BStr)] string bstrDescription, [MarshalAs(UnmanagedType.BStr)] string bstrIcon);
        [PreserveSig]
        HRESULT RegisterEventCallbackCLSID(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, ref Guid pEventGUID, ref Guid pClsID, [MarshalAs(UnmanagedType.BStr)] string bstrName, [MarshalAs(UnmanagedType.BStr)] string bstrDescription, [MarshalAs(UnmanagedType.BStr)] string bstrIcon);
        [PreserveSig]
        HRESULT GetImageDlg(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrDeviceID, IntPtr hwndParent, [MarshalAs(UnmanagedType.BStr)] string bstrFolderName, [MarshalAs(UnmanagedType.BStr)] string bstrFilename, ref int plNumFiles, [MarshalAs(UnmanagedType.BStr, SizeParamIndex = 5)] string[] ppbstrFilePaths, out IWiaItem2 ppItem);
    }
}
