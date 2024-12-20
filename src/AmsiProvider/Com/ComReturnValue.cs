namespace AmsiProvider.Com;

internal static class ComReturnValue
{
    public const int S_OK = 0x00000000;
    public const int S_FALSE = 0x00000001;
    public const int E_NOTIMPL = unchecked((int)0x80004001);
    public const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
    public const int E_INVALIDARG = unchecked((int)0x80070057L);
    public const int E_NOT_SUFFICIENT_BUFFER = unchecked((int)0x8007007A);
    public const int E_NOT_VALID_STATE = unchecked((int)0x8007139F);
    public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
}
