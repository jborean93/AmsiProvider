using AmsiProvider.Native;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace AmsiProvider;

public static class Dll
{
    public const string ProviderName = "TestAmsiProvider";
    public const string ProviderClsid = "d9987ee2-25f9-43bc-a94f-2edeef851a65";

    private const int DLL_PROCESS_DETACH = 0;
    private const int DLL_PROCESS_ATTACH = 1;

    private const int S_OK = 0;
    private const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

    private const int MAX_PATH = 260;

    private static nint DllInstance;

    [UnmanagedCallersOnly(
        EntryPoint = "DllMain",
        CallConvs = [typeof(CallConvStdcall)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static bool Main(
        nint hinstDLL,
        int fdwReason,
        nint lpvReserved)
    {
        switch (fdwReason)
        {
            case DLL_PROCESS_ATTACH:
                DllInstance = hinstDLL;
                Kernel32.DisableThreadLibraryCalls(hinstDLL);
                break;
            case DLL_PROCESS_DETACH:
                break;
        }
        return true;
    }

    [UnmanagedCallersOnly(
        EntryPoint = "DllCanUnloadNow",
        CallConvs = [typeof(CallConvStdcall)])]
    private static int CanUnloadNow()
    {
        Log("DllCanUnloadNow");
        return S_OK;
    }

    [UnmanagedCallersOnly(
        EntryPoint = "DllGetClassObject",
        CallConvs = [typeof(CallConvStdcall)])]
    private unsafe static int GetClassObject(
        nint rclsid,
        nint riid,
        void** ppv)
    {
        const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
        try
        {
            string clsid = ((Guid*)rclsid)->ToString();
            Log($"DllGetClassObject CLSID '{clsid}'");

            if (!string.Equals(ProviderClsid, clsid, StringComparison.OrdinalIgnoreCase))
            {
                return CLASS_E_CLASSNOTAVAILABLE;
            }

            Log("DllGetClassObject - class match");

            *ppv = ComInterfaceMarshaller<IAntimalwareProvider>.ConvertToUnmanaged(new AntimalwareProvider());
        }
        catch (Exception e)
        {
            Log($"DllGetClassObject error\n{e.ToString}");
            return CLASS_E_CLASSNOTAVAILABLE;
        }

        Log("DllGetClassObject - returning");
        return S_OK;
    }

    [UnmanagedCallersOnly(
        EntryPoint = "DllRegisterServer",
        CallConvs = [typeof(CallConvStdcall)])]
    private static int RegisterServer()
    {
        try
        {
            string modulePath;
            Span<char> pathBuffer = stackalloc char[MAX_PATH];
            unsafe
            {
                fixed (char* pathPtr = pathBuffer)
                {
                    int size = Kernel32.GetModuleFileNameW(DllInstance, pathPtr, MAX_PATH);
                    if (size >= MAX_PATH)
                    {
                        return E_UNEXPECTED;
                    }
                    modulePath = pathBuffer[..size].ToString();
                }
            }

            DllRegistration.Register(ProviderClsid, ProviderName, modulePath);

            return S_OK;
        }
        catch
        {
            return E_UNEXPECTED;
        }
    }

    [UnmanagedCallersOnly(
        EntryPoint = "DllUnregisterServer",
        CallConvs = [typeof(CallConvStdcall)])]
    private static int UnregisterServer()
    {
        try
        {
            DllRegistration.Unregister(ProviderClsid);

            return S_OK;
        }
        catch
        {
            return E_UNEXPECTED;
        }
    }

    public static void Log(string msg)
    {
        const string logPath = @"D:\AmsiProvider\provider.log";
        string now = DateTime.Now.ToString("[HH:mm:ss.fff]");
        File.AppendAllLines(logPath, [$"{now} - {msg}"]);
    }
}
