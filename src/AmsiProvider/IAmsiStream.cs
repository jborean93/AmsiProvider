using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace AmsiProvider;

[GeneratedComInterface]
[Guid("3e47f2e5-81d4-4d3b-897f-545096770373")]
internal partial interface IAmsiStream
{
    [PreserveSig]
    int GetAttribute(
        int attribute,
        int dataSize,
        nint data,
        out int retData);

    int Read(
        long position,
        int size,
        nint buffer);
}
