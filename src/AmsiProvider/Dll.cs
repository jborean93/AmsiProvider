using AmsiProvider.Com;
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

    private const int S_OK = 0;
    private const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

    private const int MAX_PATH = 260;

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
        Guid* rclsid,
        Guid* riid,
        void** ppv)
    {
        const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
        try
        {
            string clsid = rclsid->ToString();
            string riidString = riid->ToString();
            Log($"DllGetClassObject CLSID '{clsid}' - '{riidString}'");

            if (!string.Equals(typeof(IClassFactory).GUID.ToString(), clsid, StringComparison.OrdinalIgnoreCase))
            {
                return CLASS_E_CLASSNOTAVAILABLE;
            }

            Log("DllGetClassObject - class match");
            *ppv = ComInterfaceMarshaller<IClassFactory>.ConvertToUnmanaged(new AntimalwareClassFactory());
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
    private static unsafe int RegisterServer()
    {
        try
        {
            // We can't safely use DllMain to store the hinst of the dll in
            // NAOT and Marshal.GetHINSTANCE returns -1 for a NAOT dll. Use
            // GetModuleHandleExW with GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS
            // to get it from the address of this function.
            if (!Kernel32.GetModuleHandleExW(
                Kernel32.GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT | Kernel32.GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,
                (delegate* unmanaged[Stdcall]<int>)&RegisterServer,
                out nint hinstDLL))
            {
                return Marshal.GetLastPInvokeError();
            }

            Span<char> pathBuffer = stackalloc char[MAX_PATH];
            int size;
            fixed (char* pathPtr = pathBuffer)
            {
                size = Kernel32.GetModuleFileNameW(hinstDLL, pathPtr, MAX_PATH);
                if (size >= MAX_PATH)
                {
                    return E_UNEXPECTED;
                }
            }
            string modulePath = pathBuffer[..size].ToString();
            Log($"DllRegisterServer - ModulePath '{modulePath}'");

            DllRegistration.Register(typeof(IClassFactory).GUID.ToString(), ProviderName, modulePath);

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
            DllRegistration.Unregister(typeof(IClassFactory).GUID.ToString());

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
