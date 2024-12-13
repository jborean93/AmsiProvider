using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace AmsiProvider.Com;

[GeneratedComInterface]
[Guid("d9987ee2-25f9-43bc-a94f-2edeef851a65")]
internal partial interface IClassFactory
{
    unsafe nint CreateInstance(
        nint pUnkOuter,
        Guid* riid);

    void LockServer(
        [MarshalAs(UnmanagedType.Bool)] bool fLock);
}
