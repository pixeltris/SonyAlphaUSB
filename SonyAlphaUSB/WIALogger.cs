using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using SonyAlphaUSB.Interop;

namespace SonyAlphaUSB
{
    public class WIALogger
    {
        const string targetProcessName = "Remote";
        const string targetProcessNameEx = targetProcessName + ".exe";
        const string loaderDll = "SonyAlphaUSBLoader.dll";

        static bool showConsole = true;

        /// <summary>
        /// This will dump all messages to WIALogger.txt. Only use this briefly or block the image viewer message (09 92 / 08 10 / 09 10)
        /// </summary>
        static bool dumpLog = false;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate HRESULT EscapeDelegate(IntPtr thisPtr, int dwEscapeCode, IntPtr lpInData, int cbInDataSize, IntPtr pOutData, int dwOutDataSize, out int pdwActualDataSize);

        private static IntPtr EscapeHookPtr;
        private static EscapeDelegate EscapeHook;
        private static IntPtr EscapeOriginalPtr;
        private static EscapeDelegate EscapeOriginal;

        static HashSet<int> inOpcodes = new HashSet<int>();
        static HashSet<int> outOpcodes = new HashSet<int>();

        static HashSet<ushort> availableMainSettingIds = new HashSet<ushort>();
        static HashSet<ushort> availableSubSettingIds = new HashSet<ushort>();

        static Dictionary<SettingIds, CameraSetting> cameraSettings = new Dictionary<SettingIds, CameraSetting>();

        static byte[] lastCameraSettingsBuffer = null;
        static Dictionary<ushort, byte[]> lastCameraSettingsValues = new Dictionary<ushort, byte[]>();

        private static HRESULT OnEscape(IntPtr thisPtr, int dwEscapeCode, IntPtr lpInData, int cbInDataSize, IntPtr pOutData, int dwOutDataSize, out int pdwActualDataSize)
        {
            HRESULT result = EscapeOriginal(thisPtr, dwEscapeCode, lpInData, cbInDataSize, pOutData, dwOutDataSize, out pdwActualDataSize);
            if (dwEscapeCode == 256)
            {
                if (WIA.Success(result) && pdwActualDataSize <= dwOutDataSize)
                {
                    byte[] inData = new byte[cbInDataSize];
                    Marshal.Copy(lpInData, inData, 0, inData.Length);

                    byte[] outData = new byte[pdwActualDataSize];
                    Marshal.Copy(pOutData, outData, 0, outData.Length);

                    using (Packet inPacket = new Packet(true, inData))
                    using (Packet outPacket = new Packet(true, outData))
                    {
                        switch ((OpCodes)inPacket.Opcode)
                        {
                            case OpCodes.Connect:
                                {
                                    Log("Connect " + inPacket);
                                }
                                break;
                            case OpCodes.SettingsList:
                                {
                                    outPacket.Index = 30;
                                    ushort unk = outPacket.ReadUInt16();//200?

                                    availableMainSettingIds.Clear();
                                    availableSubSettingIds.Clear();

                                    int mainSettingsCount = outPacket.ReadInt32();
                                    for (int i = 0; i < mainSettingsCount; i++)
                                    {
                                        availableMainSettingIds.Add(outPacket.ReadUInt16());
                                    }

                                    int subSettingsCount = outPacket.ReadInt32();
                                    for (int i = 0; i < subSettingsCount; i++)
                                    {
                                        availableSubSettingIds.Add(outPacket.ReadUInt16());
                                    }

                                    Log("SettingsList (" + mainSettingsCount + " main settings, " + subSettingsCount + " sub settings)");
                                }
                                break;
                            case OpCodes.MainSetting:
                                {
                                    inPacket.Index = 10;
                                    ushort mainSettingId = inPacket.ReadUInt16();
                                    string mainSettingName = ((SettingIds)mainSettingId).ToString();
                                    int temp1;
                                    if (int.TryParse(mainSettingName, out temp1))
                                    {
                                        mainSettingName = "Unknown";
                                    }
                                    Log("MainSetting (" +  mainSettingName + " / " + Packet.ToHexStringU16(mainSettingId) + ") " + inPacket);
                                    //Log("Response: " + outPacket);
                                }
                                break;
                            case OpCodes.SubSetting:
                                {
                                    inPacket.Index = 10;
                                    ushort subSettingId = inPacket.ReadUInt16();
                                    string subSettingName = ((SettingIds)subSettingId).ToString();
                                    int temp2;
                                    if (int.TryParse(subSettingName, out temp2))
                                    {
                                        subSettingName = "Unknown";
                                    }
                                    Log("SubSetting (" + subSettingName + " / " + Packet.ToHexStringU16(subSettingId) + ") " + inPacket);
                                    //Log("Response: " + outPacket);
                                }
                                break;
                            case OpCodes.Settings:
                                {
                                    byte[] cameraSettingsBuffer = outPacket.GetBuffer();
                                    if (lastCameraSettingsBuffer == null || !lastCameraSettingsBuffer.SequenceEqual(cameraSettingsBuffer))
                                    {
                                        lastCameraSettingsBuffer = cameraSettingsBuffer;
                                        //Log("CameraSettingsUpdate " + outPacket);

                                        outPacket.Index = 30;
                                        int numSettings = outPacket.ReadInt32();
                                        int unk = outPacket.ReadInt32();
                                        Debug.Assert(unk == 0);//always 0?

                                        ushort prevSettingId = 0;

                                        for (int i = 0; i < numSettings; i++)
                                        {
                                            ushort settingId = outPacket.ReadUInt16();
                                            int currentDataStartIndex = outPacket.Index;
                                            bool knownSetting = false;

                                            CameraSetting setting;
                                            if (!cameraSettings.TryGetValue((SettingIds)settingId, out setting))
                                            {
                                                setting = new CameraSetting((SettingIds)settingId);
                                                cameraSettings.Add((SettingIds)settingId, setting);
                                            }

                                            if (availableMainSettingIds.Contains(settingId) ||
                                                availableSubSettingIds.Contains(settingId))
                                            {
                                                knownSetting = true;
                                                
                                                ushort dataType = outPacket.ReadUInt16();
                                                switch (dataType)
                                                {
                                                    case 1:
                                                        {
                                                            outPacket.Skip(4);
                                                            byte subDataType = outPacket.ReadByte();
                                                            switch (subDataType)
                                                            {
                                                                case 1:
                                                                    outPacket.Skip(3);
                                                                    break;
                                                                default:
                                                                    knownSetting = false;
                                                                    break;
                                                            }
                                                        }
                                                        break;
                                                    case 2:
                                                        {
                                                            outPacket.Skip(4);
                                                            byte subDataType = outPacket.ReadByte();
                                                            switch (subDataType)
                                                            {
                                                                case 1:
                                                                    outPacket.Skip(3);
                                                                    break;
                                                                case 2:
                                                                    int num = outPacket.ReadUInt16();
                                                                    setting.AcceptedValues.Clear();
                                                                    for (int j = 0; j < num; j++)
                                                                    {
                                                                        setting.AcceptedValues.Add(outPacket.ReadByte());
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
                                                            outPacket.Skip(6);
                                                            byte subDataType = outPacket.ReadByte();
                                                            switch (subDataType)
                                                            {
                                                                case 1:
                                                                    outPacket.Skip(6);
                                                                    break;
                                                                case 2:
                                                                    int num = outPacket.ReadUInt16();
                                                                    setting.AcceptedValues.Clear();
                                                                    for (int j = 0; j < num; j++)
                                                                    {
                                                                        setting.AcceptedValues.Add(outPacket.ReadInt16());
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
                                                            outPacket.Skip(4);
                                                            setting.Value = outPacket.ReadInt16();
                                                            byte subDataType = outPacket.ReadByte();
                                                            switch (subDataType)
                                                            {
                                                                case 1:
                                                                    outPacket.Skip(2);
                                                                    setting.DecrementValue = outPacket.ReadInt16();// Decrement value
                                                                    setting.IncrementValue = outPacket.ReadInt16();// Iecrement value
                                                                    break;
                                                                case 2:
                                                                    int num = outPacket.ReadUInt16();
                                                                    setting.AcceptedValues.Clear();
                                                                    for (int j = 0; j < num; j++)
                                                                    {
                                                                        setting.AcceptedValues.Add(outPacket.ReadInt16());
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
                                                            outPacket.Skip(6);
                                                            setting.Value = outPacket.ReadInt16();
                                                            setting.SubValue = outPacket.ReadInt16();
                                                            byte subDataType = outPacket.ReadByte();
                                                            switch (subDataType)
                                                            {
                                                                case 1:
                                                                    outPacket.Skip(12);
                                                                    break;
                                                                case 2:
                                                                    int num = outPacket.ReadUInt16();
                                                                    setting.AcceptedValues.Clear();
                                                                    for (int j = 0; j < num; j++)
                                                                    {
                                                                        setting.AcceptedValues.Add(outPacket.ReadInt32());
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

                                            int currentDataEndIndex = outPacket.Index;
                                            string settingIdHex = Packet.ToHexStringU16(settingId);

                                            if (!knownSetting || currentDataStartIndex == currentDataEndIndex)
                                            {
                                                if (availableMainSettingIds.Count == 0 && availableSubSettingIds.Count == 0)
                                                {
                                                    Log("Available settings list not found (injection was too slow). Restart the program.");
                                                }
                                                else
                                                {
                                                    string prevSettingIdHex = Packet.ToHexStringU16(prevSettingId);
                                                    Log("Couldn't determine the length of the camera setting data (" + settingIdHex + ") prev(" + prevSettingIdHex + ")");
                                                    Log(outPacket.ToString());
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                outPacket.Index = currentDataStartIndex;
                                                byte[] settingData = outPacket.ReadBytes(currentDataEndIndex - currentDataStartIndex);
                                                outPacket.Index = currentDataStartIndex;
                                                byte[] lastSettingData;
                                                if (lastCameraSettingsValues.TryGetValue(settingId, out lastSettingData) &&
                                                    !lastSettingData.SequenceEqual(settingData))
                                                {
                                                    Log("Setting (" + (SettingIds)settingId + " / " + settingIdHex + ") changed");
                                                    Log(Packet.ToHexString(settingData));

                                                    if (setting != null)
                                                    {
                                                        switch ((SettingIds)settingId)
                                                        {
                                                            case SettingIds.FNumber:
                                                                Log((SettingIds)settingId + ": " + ((float)setting.Value / 100));
                                                                break;
                                                            case SettingIds.FocusArea:
                                                                Log((SettingIds)settingId + ": " + (FocusAreaIds)setting.Value);
                                                                break;
                                                            case SettingIds.ISO:
                                                                Log((SettingIds)settingId + ": " + setting.Value);
                                                                break;
                                                            case SettingIds.ShutterSpeed:
                                                                Log((SettingIds)settingId + ": " + setting.AsShutterSpeed());
                                                                break;
                                                        }
                                                    }
                                                }
                                                lastCameraSettingsValues[settingId] = settingData;
                                                outPacket.Index = currentDataEndIndex;
                                            }

                                            prevSettingId = settingId;
                                        }
                                    }
                                }
                                break;
                            case OpCodes.GetImageInfo:
                                {
                                    inPacket.Index = 10;
                                    byte imageType = inPacket.ReadByte();
                                    if (imageType != 1 && imageType != 2)//1=photo, 2=live view
                                    {
                                        Log("Unknown GetImageInfo type " + imageType);
                                    }
                                    outPacket.Index = 32;
                                    short numImages = outPacket.ReadInt16();//Num images?
                                    if (numImages > 0)
                                    {
                                        int imageInfoUnk = outPacket.ReadInt32();//14337(01 38 00 00)
                                        int imageSizeInBytes = outPacket.ReadInt32();
                                        if (imageInfoUnk != 14337)
                                        {
                                            Log("Unknown GetImageInfo value " + imageInfoUnk + " - " + outPacket);
                                        }
                                        outPacket.Index = 82;
                                        string imageName = outPacket.ReadFixedString(outPacket.ReadByte(), Encoding.Unicode);

                                        if (imageType == 1)
                                        {
                                            Log("Captured photo!");
                                        }
                                    }
                                }
                                break;
                            case OpCodes.GetImageData:
                                break;
                            default:
                                Log("Unknown request " + inPacket.Opcode);
                                break;
                        }

                        if (dumpLog)
                        {
                            System.IO.File.AppendAllText("WIALogger.txt", "OnEscape req: " + inPacket.Opcode + " resp:" + inPacket.Opcode + " reqLen: " + inData.Length + " respLen: " + outData.Length + " time: " + DateTime.Now.TimeOfDay + Environment.NewLine + inPacket + Environment.NewLine + outPacket + Environment.NewLine);
                        }
                    }
                }
            }
            return result;
        }

        private static void Log(string msg)
        {
            if (showConsole)
            {
                Console.WriteLine(msg);
            }
            System.IO.File.AppendAllText("WIALogger.txt", "[" + DateTime.Now.TimeOfDay + "] " + msg + Environment.NewLine);
        }

        public static int DllMain(string arg)
        {
            if (showConsole)
            {
                ConsoleHelper.ShowConsole();
            }

            Hook.WL_InitHooks();

            IntPtr escapePtr = IntPtr.Zero;

            SonyCamera[] cameras = WIA.FindCameras();
            if (cameras != null && cameras.Length > 0)
            {
                foreach (SonyCamera camera in cameras)
                {
                    if (camera.Connect(false))
                    {
                        IntPtr devicePtr = Marshal.GetIUnknownForObject(camera.device);
                        Guid guid = new Guid("6291ef2c-36ef-4532-876a-8e132593778d");//IWiaItemExtras
                        IntPtr deviceExtrasPtr;
                        Marshal.QueryInterface(devicePtr, ref guid, out deviceExtrasPtr);
                        IntPtr vtable = Marshal.ReadIntPtr(deviceExtrasPtr);
                        escapePtr = Marshal.ReadIntPtr(vtable, 4 * IntPtr.Size);//IWiaItemExtras::Escape
                        Marshal.Release(devicePtr);
                        Marshal.Release(deviceExtrasPtr);
                        break;
                    }
                }
            }

            if (escapePtr != IntPtr.Zero)
            {
                EscapeHook = OnEscape;
                EscapeHookPtr = Marshal.GetFunctionPointerForDelegate(EscapeHook);
                Hook.WL_CreateHook(escapePtr, EscapeHookPtr, ref EscapeOriginalPtr);
                if (EscapeOriginalPtr != IntPtr.Zero)
                {
                    EscapeOriginal = (EscapeDelegate)Marshal.GetDelegateForFunctionPointer(EscapeOriginalPtr, typeof(EscapeDelegate));
                }
                Hook.WL_EnableHook(escapePtr);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Couldn't find a sony camera!");
            }

            return 0;
        }

        public static void Run()
        {
            //ProcessLauncher.Inject();
            ProcessLauncher.Launch();
        }

        static class Hook
        {
            [DllImport(loaderDll)]
            public static extern int WL_InitHooks();
            [DllImport(loaderDll)]
            public static extern int WL_HookFunction(IntPtr target, IntPtr detour, ref IntPtr original);
            [DllImport(loaderDll)]
            public static extern int WL_CreateHook(IntPtr target, IntPtr detour, ref IntPtr original);
            [DllImport(loaderDll)]
            public static extern int WL_RemoveHook(IntPtr target);
            [DllImport(loaderDll)]
            public static extern int WL_EnableHook(IntPtr target);
            [DllImport(loaderDll)]
            public static extern int WL_DisableHook(IntPtr target);
        }

        class ProcessLauncher
        {
            // This injects a dll before the exe entry point runs.
            // TODO: Fix a crash which sometimes occurs.

            public static void Inject()
            {
                Process[] processes = null;
                Dictionary<int, Process> injectedProcesses = new Dictionary<int, Process>();
                HashSet<int> closesProcesses = new HashSet<int>();

                try
                {
                    processes = Process.GetProcessesByName("Remote");
                    foreach (Process process in processes)
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(process.MainModule.FileName);
                            if (fileInfo.Exists && File.Exists(Path.Combine(fileInfo.Directory.FullName, "LjCore.dll")))
                            {
                                if (!injectedProcesses.ContainsKey(process.Id))
                                {
                                    bool alreadyInjected = false;
                                    foreach (ProcessModule processModule in process.Modules)
                                    {
                                        if (processModule.ModuleName.Equals(loaderDll, StringComparison.OrdinalIgnoreCase))
                                        {
                                            alreadyInjected = true;
                                            break;
                                        }
                                    }

                                    if (!alreadyInjected && DllInjector.Inject(process, loaderDll))
                                    {
                                        injectedProcesses[process.Id] = process;
                                        Console.WriteLine("Injected into " + process.Id);
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    while (injectedProcesses.Count != closesProcesses.Count)
                    {
                        foreach (KeyValuePair<int, Process> process in injectedProcesses)
                        {
                            try
                            {
                                if (!closesProcesses.Contains(process.Key) && process.Value.HasExited)
                                {
                                    closesProcesses.Add(process.Key);
                                }
                            }
                            catch
                            {
                                closesProcesses.Add(process.Key);
                            }
                        }
                        Thread.Sleep(1000);
                    }
                }
                finally
                {
                    if (processes != null)
                    {
                        foreach (Process process in processes)
                        {
                            try
                            {
                                process.Close();
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }

            public static unsafe bool Launch()
            {
                string exePath = targetProcessNameEx;
                if (!File.Exists(exePath))
                {
                    return false;
                }

                STARTUPINFO si = default(STARTUPINFO);
                PROCESS_INFORMATION pi = default(PROCESS_INFORMATION);

                try
                {
                    bool success = CreateProcess(exePath, null, IntPtr.Zero, IntPtr.Zero, false, DEBUG_ONLY_THIS_PROCESS, IntPtr.Zero, null, ref si, out pi);
                    if (!success)
                    {
                        return false;
                    }

                    IntPtr entryPoint = IntPtr.Zero;
                    byte[] entryPointInst = new byte[2];

                    success = false;
                    bool complete = false;
                    while (!complete)
                    {
                        DEBUG_EVENT debugEvent;
                        if (!WaitForDebugEvent(out debugEvent, 5000))
                        {
                            break;
                        }

                        switch (debugEvent.dwDebugEventCode)
                        {
                            case CREATE_PROCESS_DEBUG_EVENT:
                                {
                                    IntPtr hFile = debugEvent.CreateProcessInfo.hFile;
                                    if (hFile != IntPtr.Zero && hFile != INVALID_HANDLE_VALUE)
                                    {
                                        CloseHandle(hFile);
                                    }
                                }
                                break;
                            case EXIT_PROCESS_DEBUG_EVENT:
                                complete = true;
                                break;
                            case LOAD_DLL_DEBUG_EVENT:
                                {
                                    LOAD_DLL_DEBUG_INFO loadDll = debugEvent.LoadDll;

                                    switch (TryStealEntryPoint(ref pi, ref entryPoint, entryPointInst))
                                    {
                                        case StealEntryPointResult.FailGetModules:
                                            // Need to wait for more modules to load
                                            break;
                                        case StealEntryPointResult.FailAlloc:
                                        case StealEntryPointResult.FailRead:
                                        case StealEntryPointResult.FailWrite:
                                        case StealEntryPointResult.FailFindTargetModule:
                                            complete = true;
                                            entryPoint = IntPtr.Zero;
                                            break;
                                        case StealEntryPointResult.Success:
                                            complete = true;
                                            break;
                                    }

                                    IntPtr hFile = loadDll.hFile;
                                    if (hFile != IntPtr.Zero && hFile != INVALID_HANDLE_VALUE)
                                    {
                                        CloseHandle(hFile);
                                    }
                                }
                                break;
                        }

                        ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, DBG_CONTINUE);
                    }

                    success = false;

                    DebugSetProcessKillOnExit(false);
                    DebugActiveProcessStop((int)pi.dwProcessId);

                    if (entryPoint != IntPtr.Zero)
                    {
                        CONTEXT64 context64 = default(CONTEXT64);
                        context64.ContextFlags = CONTEXT_FLAGS.CONTROL;
                        GetThreadContext(pi.hThread, ref context64);

                        for (int i = 0; i < 100 && context64.Rip != (ulong)entryPoint; i++)
                        {
                            Thread.Sleep(50);

                            context64.ContextFlags = CONTEXT_FLAGS.CONTROL;
                            GetThreadContext(pi.hThread, ref context64);
                        }

                        // If we are at the entry point inject the dll and then restore the entry point instructions
                        if (context64.Rip == (ulong)entryPoint && DllInjector.Inject(pi.hProcess, loaderDll))
                        {
                            SuspendThread(pi.hThread);

                            IntPtr byteCount;
                            if (WriteProcessMemory(pi.hProcess, entryPoint, entryPointInst, (IntPtr)2, out byteCount) && (int)byteCount == 2)
                            {
                                success = true;
                            }

                            ResumeThread(pi.hThread);
                        }
                    }

                    if (!success)
                    {
                        TerminateProcess(pi.hProcess, 0);
                    }
                    else
                    {
                        using (Process process = Process.GetProcessById((int)pi.dwProcessId))
                        {
                            while (!process.HasExited)
                            {
                                Thread.Sleep(1000);
                            }
                        }
                    }

                    return success;
                }
                finally
                {
                    if (pi.hThread != IntPtr.Zero)
                    {
                        CloseHandle(pi.hThread);
                    }
                    if (pi.hProcess != IntPtr.Zero)
                    {
                        CloseHandle(pi.hProcess);
                    }
                }
            }

            private static unsafe StealEntryPointResult TryStealEntryPoint(ref PROCESS_INFORMATION pi, ref IntPtr entryPoint, byte[] entryPointInst)
            {
                int modSize = IntPtr.Size * 1024;
                IntPtr hMods = Marshal.AllocHGlobal(modSize);

                try
                {
                    if (hMods == IntPtr.Zero)
                    {
                        return StealEntryPointResult.FailAlloc;
                    }

                    int modsNeeded;
                    bool gotZeroMods = false;
                    while (!EnumProcessModulesEx(pi.hProcess, hMods, modSize, out modsNeeded, LIST_MODULES_ALL) || modsNeeded == 0)
                    {
                        if (modsNeeded == 0)
                        {
                            if (!gotZeroMods)
                            {
                                Thread.Sleep(100);
                                gotZeroMods = true;
                                continue;
                            }
                            else
                            {
                                // process has exited?
                                return StealEntryPointResult.FailGetModules;
                            }
                        }

                        // try again w/ more space...
                        Marshal.FreeHGlobal(hMods);
                        hMods = Marshal.AllocHGlobal(modsNeeded);
                        if (hMods == IntPtr.Zero)
                        {
                            return StealEntryPointResult.FailGetModules;
                        }
                        modSize = modsNeeded;
                    }

                    int totalNumberofModules = (int)(modsNeeded / IntPtr.Size);
                    for (int i = 0; i < totalNumberofModules; i++)
                    {
                        IntPtr hModule = Marshal.ReadIntPtr(hMods, i * IntPtr.Size);

                        MODULEINFO moduleInfo;
                        if (GetModuleInformation(pi.hProcess, hModule, out moduleInfo, sizeof(MODULEINFO)))
                        {
                            StringBuilder moduleNameSb = new StringBuilder(1024);
                            if (GetModuleFileNameEx(pi.hProcess, hModule, moduleNameSb, moduleNameSb.Capacity) != 0)
                            {
                                try
                                {
                                    string moduleName = Path.GetFileName(moduleNameSb.ToString());
                                    if (moduleName.Equals(targetProcessNameEx, StringComparison.OrdinalIgnoreCase))
                                    {
                                        IntPtr byteCount;
                                        if (ReadProcessMemory(pi.hProcess, moduleInfo.EntryPoint, entryPointInst, (IntPtr)2, out byteCount) && (int)byteCount == 2)
                                        {
                                            // TODO: We should probably use VirtualProtect here to ensure read/write/execute

                                            byte[] infLoop = { 0xEB, 0xFE };// JMP -2
                                            if (WriteProcessMemory(pi.hProcess, moduleInfo.EntryPoint, infLoop, (IntPtr)infLoop.Length, out byteCount) &&
                                                (int)byteCount == infLoop.Length)
                                            {
                                                entryPoint = moduleInfo.EntryPoint;
                                                return StealEntryPointResult.Success;
                                            }
                                            else
                                            {
                                                return StealEntryPointResult.FailWrite;
                                            }
                                        }
                                        else
                                        {
                                            return StealEntryPointResult.FailRead;
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }

                    return StealEntryPointResult.FailFindTargetModule;
                }
                finally
                {
                    if (hMods != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(hMods);
                    }
                }
            }

            enum StealEntryPointResult
            {
                FailAlloc,
                FailGetModules,
                FailFindTargetModule,
                FailRead,
                FailWrite,
                Success,
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,
                bool bInheritHandles, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll")]
            static extern uint ResumeThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            static extern uint SuspendThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            static extern bool TerminateProcess(IntPtr hProcess, uint exitCode);

            [DllImport("psapi.dll", CharSet = CharSet.Auto)]
            static extern bool EnumProcessModulesEx([In] IntPtr hProcess, IntPtr lphModule, int cb, [Out] out int lpcbNeeded, int dwFilterFlag);

            [DllImport("psapi.dll", SetLastError = true)]
            static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, int cb);

            [DllImport("psapi.dll", CharSet = CharSet.Auto)]
            static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

            [DllImport("kernel32.dll")]
            static extern bool WaitForDebugEvent(out DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool ContinueDebugEvent(int processId, int threadId, uint continuteStatus);

            [DllImport("kernel32.dll")]
            static extern void DebugSetProcessKillOnExit(bool killOnExit);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool DebugActiveProcessStop(int processId);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern Int32 CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr dwSize, out IntPtr lpNumberOfBytesRead);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, IntPtr size, out IntPtr lpNumberOfBytesWritten);

            [DllImport("kernel32.dll", SetLastError = true)]
            static unsafe extern bool GetThreadContext(IntPtr hThread, CONTEXT64* lpContext);

            static unsafe bool GetThreadContext(IntPtr hThread, ref CONTEXT64 lpContext)
            {
                // Hack to align to 16 byte boundry
                byte* buff = stackalloc byte[Marshal.SizeOf(typeof(CONTEXT64)) + 16];
                buff += (ulong)(IntPtr)buff % 16;
                CONTEXT64* ptr = (CONTEXT64*)buff;
                *ptr = lpContext;

                bool result = GetThreadContext(hThread, ptr);
                lpContext = *ptr;
                if (!result && Marshal.GetLastWin32Error() == 998)
                {
                    // Align hack failed

                }
                return result;
            }

            [Flags]
            enum ThreadAccess : uint
            {
                Terminate = 0x00001,
                SuspendResume = 0x00002,
                GetContext = 0x00008,
                SetContext = 0x00010,
                SetInformation = 0x00020,
                QueryInformation = 0x00040,
                SetThreadToken = 0x00080,
                Impersonate = 0x00100,
                DirectImpersonation = 0x00200,
                All = 0x1F03FF
            }

            const int DEBUG_ONLY_THIS_PROCESS = 0x00000002;
            const int CREATE_SUSPENDED = 0x00000004;

            const int LIST_MODULES_DEFAULT = 0x00;
            const int LIST_MODULES_32BIT = 0x01;
            const int LIST_MODULES_64BIT = 0x02;
            const int LIST_MODULES_ALL = 0x03;

            const uint CREATE_PROCESS_DEBUG_EVENT = 3;
            const uint EXIT_PROCESS_DEBUG_EVENT = 5;
            const uint LOAD_DLL_DEBUG_EVENT = 6;

            static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            const uint DBG_CONTINUE = 0x00010002;

            [StructLayout(LayoutKind.Sequential)]
            unsafe struct MODULEINFO
            {
                public IntPtr lpBaseOfDll;
                public uint SizeOfImage;
                public IntPtr EntryPoint;
            }

            struct STARTUPINFO
            {
                public uint cb;
                public string lpReserved;
                public string lpDesktop;
                public string lpTitle;
                public uint dwX;
                public uint dwY;
                public uint dwXSize;
                public uint dwYSize;
                public uint dwXCountChars;
                public uint dwYCountChars;
                public uint dwFillAttribute;
                public uint dwFlags;
                public short wShowWindow;
                public short cbReserved2;
                public IntPtr lpReserved2;
                public IntPtr hStdInput;
                public IntPtr hStdOutput;
                public IntPtr hStdError;
            }

            struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public uint dwProcessId;
                public uint dwThreadId;
            }

            [StructLayout(LayoutKind.Explicit)]
            struct DEBUG_EVENT
            {
                [FieldOffset(0)]
                public uint dwDebugEventCode;
                [FieldOffset(4)]
                public int dwProcessId;
                [FieldOffset(8)]
                public int dwThreadId;

                // x64(offset:16, size:164)
                // x86(offset:12, size:86)
                [FieldOffset(16)]
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 164, ArraySubType = UnmanagedType.U1)]
                public byte[] debugInfo;

                public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo
                {
                    get { return GetDebugInfo<CREATE_PROCESS_DEBUG_INFO>(); }
                }

                public LOAD_DLL_DEBUG_INFO LoadDll
                {
                    get { return GetDebugInfo<LOAD_DLL_DEBUG_INFO>(); }
                }

                private T GetDebugInfo<T>() where T : struct
                {
                    GCHandle handle = GCHandle.Alloc(this.debugInfo, GCHandleType.Pinned);
                    T result = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                    handle.Free();
                    return result;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            struct LOAD_DLL_DEBUG_INFO
            {
                public IntPtr hFile;
                public IntPtr lpBaseOfDll;
                public uint dwDebugInfoFileOffset;
                public uint nDebugInfoSize;
                public IntPtr lpImageName;
                public ushort fUnicode;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct CREATE_PROCESS_DEBUG_INFO
            {
                public IntPtr hFile;
                public IntPtr hProcess;
                public IntPtr hThread;
                public IntPtr lpBaseOfImage;
                public uint dwDebugInfoFileOffset;
                public uint nDebugInfoSize;
                public IntPtr lpThreadLocalBase;
                public IntPtr lpStartAddress;
                public IntPtr lpImageName;
                public ushort fUnicode;
            }

            [StructLayout(LayoutKind.Explicit, Size = 1232)]
            unsafe struct CONTEXT64
            {
                // Register Parameter Home Addresses
                [FieldOffset(0x0)]
                internal ulong P1Home;
                [FieldOffset(0x8)]
                internal ulong P2Home;
                [FieldOffset(0x10)]
                internal ulong P3Home;
                [FieldOffset(0x18)]
                internal ulong P4Home;
                [FieldOffset(0x20)]
                internal ulong P5Home;
                [FieldOffset(0x28)]
                internal ulong P6Home;
                // Control Flags
                [FieldOffset(0x30)]
                internal CONTEXT_FLAGS ContextFlags;
                [FieldOffset(0x34)]
                internal uint MxCsr;
                // Segment Registers and Processor Flags
                [FieldOffset(0x38)]
                internal ushort SegCs;
                [FieldOffset(0x3a)]
                internal ushort SegDs;
                [FieldOffset(0x3c)]
                internal ushort SegEs;
                [FieldOffset(0x3e)]
                internal ushort SegFs;
                [FieldOffset(0x40)]
                internal ushort SegGs;
                [FieldOffset(0x42)]
                internal ushort SegSs;
                [FieldOffset(0x44)]
                internal uint EFlags;
                // Debug Registers
                [FieldOffset(0x48)]
                internal ulong Dr0;
                [FieldOffset(0x50)]
                internal ulong Dr1;
                [FieldOffset(0x58)]
                internal ulong Dr2;
                [FieldOffset(0x60)]
                internal ulong Dr3;
                [FieldOffset(0x68)]
                internal ulong Dr6;
                [FieldOffset(0x70)]
                internal ulong Dr7;
                // Integer Registers
                [FieldOffset(0x78)]
                internal ulong Rax;
                [FieldOffset(0x80)]
                internal ulong Rcx;
                [FieldOffset(0x88)]
                internal ulong Rdx;
                [FieldOffset(0x90)]
                internal ulong Rbx;
                [FieldOffset(0x98)]
                internal ulong Rsp;
                [FieldOffset(0xa0)]
                internal ulong Rbp;
                [FieldOffset(0xa8)]
                internal ulong Rsi;
                [FieldOffset(0xb0)]
                internal ulong Rdi;
                [FieldOffset(0xb8)]
                internal ulong R8;
                [FieldOffset(0xc0)]
                internal ulong R9;
                [FieldOffset(0xc8)]
                internal ulong R10;
                [FieldOffset(0xd0)]
                internal ulong R11;
                [FieldOffset(0xd8)]
                internal ulong R12;
                [FieldOffset(0xe0)]
                internal ulong R13;
                [FieldOffset(0xe8)]
                internal ulong R14;
                [FieldOffset(0xf0)]
                internal ulong R15;
                // Program Counter
                [FieldOffset(0xf8)]
                internal ulong Rip;
                // Floating Point State
                [FieldOffset(0x100)]
                internal ulong FltSave;
                [FieldOffset(0x120)]
                internal ulong Legacy;
                [FieldOffset(0x1a0)]
                internal ulong Xmm0;
                [FieldOffset(0x1b0)]
                internal ulong Xmm1;
                [FieldOffset(0x1c0)]
                internal ulong Xmm2;
                [FieldOffset(0x1d0)]
                internal ulong Xmm3;
                [FieldOffset(0x1e0)]
                internal ulong Xmm4;
                [FieldOffset(0x1f0)]
                internal ulong Xmm5;
                [FieldOffset(0x200)]
                internal ulong Xmm6;
                [FieldOffset(0x210)]
                internal ulong Xmm7;
                [FieldOffset(0x220)]
                internal ulong Xmm8;
                [FieldOffset(0x230)]
                internal ulong Xmm9;
                [FieldOffset(0x240)]
                internal ulong Xmm10;
                [FieldOffset(0x250)]
                internal ulong Xmm11;
                [FieldOffset(0x260)]
                internal ulong Xmm12;
                [FieldOffset(0x270)]
                internal ulong Xmm13;
                [FieldOffset(0x280)]
                internal ulong Xmm14;
                [FieldOffset(0x290)]
                internal ulong Xmm15;
                // Vector Registers
                [FieldOffset(0x300)]
                internal ulong VectorRegister;
                [FieldOffset(0x4a0)]
                internal ulong VectorControl;
                // Special Debug Control Registers
                [FieldOffset(0x4a8)]
                internal ulong DebugControl;
                [FieldOffset(0x4b0)]
                internal ulong LastBranchToRip;
                [FieldOffset(0x4b8)]
                internal ulong LastBranchFromRip;
                [FieldOffset(0x4c0)]
                internal ulong LastExceptionToRip;
                [FieldOffset(0x4c8)]
                internal ulong LastExceptionFromRip;
            }

            [Flags]
            enum CONTEXT_FLAGS : uint
            {
                i386 = 0x10000,
                i486 = 0x10000,   //  same as i386
                CONTROL = i386 | 0x01, // SS:SP, CS:IP, FLAGS, BP
                INTEGER = i386 | 0x02, // AX, BX, CX, DX, SI, DI
                SEGMENTS = i386 | 0x04, // DS, ES, FS, GS
                FLOATING_POINT = i386 | 0x08, // 387 state
                DEBUG_REGISTERS = i386 | 0x10, // DB 0-3,6,7
                EXTENDED_REGISTERS = i386 | 0x20, // cpu specific extensions
                FULL = CONTROL | INTEGER | SEGMENTS,
                ALL = CONTROL | INTEGER | SEGMENTS | FLOATING_POINT | DEBUG_REGISTERS | EXTENDED_REGISTERS
            }

            static class DllInjector
            {
                [DllImport("kernel32.dll", SetLastError = true)]
                static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, int dwProcessId);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern Int32 CloseHandle(IntPtr hObject);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern IntPtr GetModuleHandle(string lpModuleName);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint dwFreeType);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, IntPtr size, out IntPtr lpNumberOfBytesWritten);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

                const uint MEM_COMMIT = 0x1000;
                const uint MEM_RESERVE = 0x2000;
                const uint MEM_RELEASE = 0x8000;

                const uint PAGE_EXECUTE = 0x10;
                const uint PAGE_EXECUTE_READ = 0x20;
                const uint PAGE_EXECUTE_READWRITE = 0x40;
                const uint PAGE_EXECUTE_WRITECOPY = 0x80;
                const uint PAGE_NOACCESS = 0x01;

                public static bool Inject(Process process, string dllPath)
                {
                    bool result = false;
                    IntPtr hProcess = OpenProcess((0x2 | 0x8 | 0x10 | 0x20 | 0x400), 1, process.Id);
                    if (hProcess != IntPtr.Zero)
                    {
                        result = Inject(hProcess, dllPath);
                        CloseHandle(hProcess);
                    }
                    return result;
                }

                public static bool Inject(IntPtr process, string dllPath)
                {
                    if (process == IntPtr.Zero)
                    {
                        LogError("Process handle is 0");
                        return false;
                    }

                    if (!File.Exists(dllPath))
                    {
                        LogError("Couldn't find the dll to inject (" + dllPath + ")");
                        return false;
                    }

                    //dllPath = Path.GetFullPath(dllPath);
                    byte[] buffer = Encoding.ASCII.GetBytes(dllPath);

                    IntPtr libAddr = IntPtr.Zero;
                    IntPtr memAddr = IntPtr.Zero;
                    IntPtr threadAddr = IntPtr.Zero;

                    try
                    {
                        if (process == IntPtr.Zero)
                        {
                            LogError("Unable to attach to process");
                            return false;
                        }

                        libAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                        if (libAddr == IntPtr.Zero)
                        {
                            LogError("Unable to find address of LoadLibraryA");
                            return false;
                        }

                        memAddr = VirtualAllocEx(process, IntPtr.Zero, (IntPtr)buffer.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                        if (memAddr == IntPtr.Zero)
                        {
                            LogError("Unable to allocate memory in the target process");
                            return false;
                        }

                        IntPtr bytesWritten;
                        if (!WriteProcessMemory(process, memAddr, buffer, (IntPtr)buffer.Length, out bytesWritten) ||
                            (int)bytesWritten != buffer.Length)
                        {
                            LogError("Unable to write to target process memory");
                            return false;
                        }

                        IntPtr thread = CreateRemoteThread(process, IntPtr.Zero, IntPtr.Zero, libAddr, memAddr, 0, IntPtr.Zero);
                        if (thread == IntPtr.Zero)
                        {
                            LogError("Unable to start thread in target process");
                            return false;
                        }

                        return true;
                    }
                    finally
                    {
                        if (threadAddr != IntPtr.Zero)
                        {
                            CloseHandle(threadAddr);
                        }
                        if (memAddr != IntPtr.Zero)
                        {
                            VirtualFreeEx(process, memAddr, IntPtr.Zero, MEM_RELEASE);
                        }
                    }
                }

                private static void LogError(string str)
                {
                    string error = "DllInjector error: " + str + " - ErrorCode: " + Marshal.GetLastWin32Error();
                    Console.WriteLine(error);
                    System.Diagnostics.Debug.WriteLine(error);
                }
            }
        }

        class ConsoleHelper
        {
            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("kernel32.dll")]
            private static extern bool AllocConsole();

            [DllImport("kernel32.dll")]
            private static extern bool FreeConsole();

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetConsoleWindow();

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetStdHandle(UInt32 nStdHandle);

            [DllImport("kernel32.dll")]
            private static extern void SetStdHandle(UInt32 nStdHandle, IntPtr handle);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool IsWindowVisible(IntPtr hWnd);

            private const UInt32 StdOutputHandle = 0xFFFFFFF5;

            private static IntPtr consoleHandle;
            internal static TextWriter output;

            private const int SW_SHOW = 5;
            private const int SW_HIDE = 0;

            private static string title;
            public static string Title
            {
                get
                {
                    if (consoleHandle == IntPtr.Zero)
                    {
                        return title;
                    }
                    StringBuilder stringBuilder = new StringBuilder(1024);
                    GetConsoleTitle(stringBuilder, (uint)stringBuilder.Capacity);
                    return stringBuilder.ToString();
                }
                set
                {
                    title = value;
                    if (consoleHandle != IntPtr.Zero)
                    {
                        SetConsoleTitle(value);
                    }
                }
            }

            static ConsoleHelper()
            {
                output = new ConsoleTextWriter();
                Console.SetOut(output);

                consoleHandle = GetConsoleWindow();
                if (consoleHandle != IntPtr.Zero)
                {
                    Title = title;
                }
            }

            public static bool IsConsoleVisible
            {
                get { return (consoleHandle = GetConsoleWindow()) != IntPtr.Zero && IsWindowVisible(consoleHandle); }
                //get { return (consoleHandle = GetConsoleWindow()) != IntPtr.Zero; }
            }

            public static void ToggleConsole()
            {
                consoleHandle = GetConsoleWindow();
                if (consoleHandle == IntPtr.Zero)
                {
                    AllocConsole();
                }
                else
                {
                    FreeConsole();
                }
            }

            public static void ShowConsole()
            {
                consoleHandle = GetConsoleWindow();
                if (consoleHandle == IntPtr.Zero)
                {
                    AllocConsole();
                    consoleHandle = GetConsoleWindow();
                }
                else
                {
                    ShowWindow(consoleHandle, SW_SHOW);
                }

                if (consoleHandle != IntPtr.Zero)
                {
                    Title = title != null ? title : string.Empty;
                }
            }

            public static void HideConsole()
            {
                consoleHandle = GetConsoleWindow();
                if (consoleHandle != IntPtr.Zero)
                {
                    ShowWindow(consoleHandle, SW_HIDE);
                }
            }

            public static void CloseConsole()
            {
                consoleHandle = GetConsoleWindow();
                if (consoleHandle != IntPtr.Zero)
                {
                    FreeConsole();
                }
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern uint GetConsoleTitle(StringBuilder lpConsoleTitle, uint nSize);

            [DllImport("kernel32.dll")]
            static extern bool SetConsoleTitle(string lpConsoleTitle);
        }

        public class ConsoleTextWriter : TextWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }

            // TODO: WriteConsole may not write all the data, chunk this data into several calls if nessesary

            // WriteConsoleW issues reference:
            // https://svn.apache.org/repos/asf/logging/log4net/tags/log4net-1_2_9/src/Appender/ColoredConsoleAppender.cs

            public override void Write(string value)
            {
                uint written;
                if (!WriteConsoleW(new IntPtr(7), value, (uint)value.Length, out written, IntPtr.Zero) || written < value.Length)
                {
                    if (GetConsoleWindow() != IntPtr.Zero)
                    {
                        //System.Diagnostics.Debugger.Break();
                    }
                }
            }

            public override void WriteLine(string value)
            {
                value = value + Environment.NewLine;
                uint written;
                if (!WriteConsoleW(new IntPtr(7), value, (uint)value.Length, out written, IntPtr.Zero) || written < value.Length)
                {
                    if (GetConsoleWindow() != IntPtr.Zero)
                    {
                        //System.Diagnostics.Debugger.Break();
                    }
                }
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            static extern bool WriteConsoleW(IntPtr hConsoleOutput, [MarshalAs(UnmanagedType.LPWStr)] string lpBuffer,
               uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten,
               IntPtr lpReserved);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            static extern bool WriteConsole(IntPtr hConsoleOutput, string lpBuffer,
               uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten,
               IntPtr lpReserved);

            [DllImport("kernel32.dll")]
            static extern bool SetConsoleCP(int wCodePageID);

            [DllImport("kernel32.dll")]
            static extern uint GetACP();

            [DllImport("kernel32.dll")]
            static extern IntPtr GetConsoleWindow();
        }
    }
}
