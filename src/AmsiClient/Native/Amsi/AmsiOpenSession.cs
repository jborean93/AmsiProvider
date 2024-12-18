using System.Runtime.InteropServices;

namespace AmsiClient.Native;

internal static partial class Amsi
{
    [LibraryImport(AmsiDll)]
    public static partial int AmsiOpenSession(
        nint amsiContext,
        out nint amsiSession);
}
