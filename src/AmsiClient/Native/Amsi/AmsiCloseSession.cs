using System.Runtime.InteropServices;

namespace AmsiClient.Native;

internal static partial class Amsi
{
    [LibraryImport(AmsiDll)]
    public static partial void AmsiCloseSession(
        nint amsiContext,
        nint amsiSession);
}
