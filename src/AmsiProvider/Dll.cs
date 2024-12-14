using AmsiProvider.Com;
using AmsiProvider.Native;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace AmsiProvider;

public static class Dll
{
    public const string ProviderName = "TestAmsiProvider";

    [UnmanagedCallersOnly(
        EntryPoint = "DllCanUnloadNow",
        CallConvs = [typeof(CallConvStdcall)])]
    private static int CanUnloadNow()
    {
        return ComReturnValue.S_FALSE;
    }

    [UnmanagedCallersOnly(
        EntryPoint = "DllGetClassObject",
        CallConvs = [typeof(CallConvStdcall)])]
    private unsafe static int GetClassObject(
        Guid* rclsid,
        Guid* riid,
        void** ppv)
    {
        try
        {
            string clsid = rclsid->ToString();
            if (!string.Equals(typeof(IClassFactory).GUID.ToString(), clsid, StringComparison.OrdinalIgnoreCase))
            {
                return ComReturnValue.CLASS_E_CLASSNOTAVAILABLE;
            }

            ProviderConfig config = ProviderConfig.Load();
            *ppv = ComInterfaceMarshaller<IClassFactory>.ConvertToUnmanaged(new AntimalwareClassFactory(config));
            return ComReturnValue.S_OK;
        }
        catch
        {
            return ComReturnValue.CLASS_E_CLASSNOTAVAILABLE;
        }
    }

    [UnmanagedCallersOnly(
        EntryPoint = "DllRegisterServer",
        CallConvs = [typeof(CallConvStdcall)])]
    private static int RegisterServer()
    {
        try
        {
            if (!TryGetDllPath(out string? modulePath, out int errorCode))
            {
                return errorCode;
            }

            DllRegistration.Register(typeof(IClassFactory).GUID.ToString(), ProviderName, modulePath);
            return ComReturnValue.S_OK;
        }
        catch
        {
            return ComReturnValue.E_UNEXPECTED;
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
            return ComReturnValue.S_OK;
        }
        catch
        {
            return ComReturnValue.E_UNEXPECTED;
        }
    }

    public static bool TryGetDllPath(
        [NotNullWhen(true)] out string? path,
        out int errorCode)
    {
        const int MAX_PATH = 260;

        path = null;
        errorCode = 0;

        unsafe
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
                errorCode = Marshal.GetLastPInvokeError();
                return false;
            }

            Span<char> pathBuffer = stackalloc char[MAX_PATH];
            int size;
            fixed (char* pathPtr = pathBuffer)
            {
                size = Kernel32.GetModuleFileNameW(hinstDLL, pathPtr, MAX_PATH);
                if (size == 0)
                {
                    errorCode = Marshal.GetLastPInvokeError();
                    return false;
                }
                else if (size >= MAX_PATH)
                {
                    errorCode = ComReturnValue.E_UNEXPECTED;
                    return false;
                }
            }
            path = pathBuffer[..size].ToString();
        }

        return true;
    }
}
