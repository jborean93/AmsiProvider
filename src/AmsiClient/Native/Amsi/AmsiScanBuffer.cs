using System.Runtime.InteropServices;

namespace AmsiClient.Native;

internal static partial class Amsi
{
    [LibraryImport(AmsiDll)]
    public static partial int AmsiScanBuffer(
        nint amsiContext,
        nint buffer,
        int length,
        [MarshalAs(UnmanagedType.LPWStr)] string contentName,
        nint amsiSession,
        out AmsiResult result);
}
