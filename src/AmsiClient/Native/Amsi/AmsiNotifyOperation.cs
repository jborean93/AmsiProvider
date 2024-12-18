using System.Runtime.InteropServices;

namespace AmsiClient.Native;

internal static partial class Amsi
{
    [LibraryImport(AmsiDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int AmsiNotifyOperation(
        nint amsiContext,
        nint buffer,
        int length,
        string contentName,
        out AmsiResult result);
}
