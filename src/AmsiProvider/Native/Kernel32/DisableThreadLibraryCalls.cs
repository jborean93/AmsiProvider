using System.Runtime.InteropServices;

namespace AmsiProvider.Native;

internal static partial class Kernel32
{
    [LibraryImport("Kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DisableThreadLibraryCalls(
        nint hLibModule);
}
