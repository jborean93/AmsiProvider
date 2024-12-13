using System.Runtime.InteropServices;

namespace AmsiProvider.Native;

internal static partial class Kernel32
{
    public const int GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x00000002;
    public const int GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x00000004;

    [LibraryImport("Kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool GetModuleHandleExW(
        int dwFlags,
        void* lpModuleName,
        out nint phModule);
}
