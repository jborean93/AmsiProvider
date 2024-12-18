using System.Runtime.InteropServices;

namespace AmsiClient.Native;

internal static partial class Amsi
{
    private const string AmsiDll = @"C:\Windows\System32\Amsi.dll";

    [LibraryImport(AmsiDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int AmsiInitialize(
        string appName,
        out nint amsiContext);
}
