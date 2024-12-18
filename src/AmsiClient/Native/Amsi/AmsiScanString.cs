using System.Runtime.InteropServices;

namespace AmsiClient.Native;

internal static partial class Amsi
{
    [LibraryImport(AmsiDll)]
    public static partial int AmsiScanString(
        nint amsiContext,
        [MarshalAs(UnmanagedType.LPWStr)] string @string,
        [MarshalAs(UnmanagedType.LPWStr)] string contentName,
        nint amsiSession,
        out AmsiResult result);
}
