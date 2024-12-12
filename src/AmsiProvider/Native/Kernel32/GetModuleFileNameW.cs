using System.Runtime.InteropServices;

namespace AmsiProvider.Native;

internal static partial class Kernel32
{
    [LibraryImport("Kernel32.dll", SetLastError = true)]
    public static unsafe partial int GetModuleFileNameW(
        nint hModule,
        char* lpFilename,
        int nSize);
}
