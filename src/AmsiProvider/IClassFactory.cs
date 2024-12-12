using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace AmsiProvider;

[GeneratedComInterface]
[Guid(Dll.ProviderClsid)]
internal partial interface IClassFactory
{
    unsafe nint CreateInstance(
        nint pUnkOuter,
        Guid* riid);

    void LockServer(
        [MarshalAs(UnmanagedType.Bool)] bool fLock);
}
