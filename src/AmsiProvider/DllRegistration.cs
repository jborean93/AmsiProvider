
using Microsoft.Win32;

namespace AmsiProvider;

internal static class DllRegistration
{
    private const string AmsiProviderKeyPath = @"SOFTWARE\Microsoft\AMSI\Providers";
    private const string ClsidKeyPath = @"SOFTWARE\Classes\CLSID";

    public static void Register(
        string clsid,
        string name,
        string modulePath)
    {
        string amsiPath = $"{AmsiProviderKeyPath}\\{{{clsid}}}";
        string clsidPath = $"{ClsidKeyPath}\\{{{clsid}}}";

        using RegistryKey hklm = RegistryKey.OpenBaseKey(
            RegistryHive.LocalMachine,
            RegistryView.Default);

        using RegistryKey clsidClassKey = hklm.OpenSubKey(clsidPath, true)
            ?? hklm.CreateSubKey(clsidPath, true);
        clsidClassKey.SetValue(null, name, RegistryValueKind.String);

        using RegistryKey clsidServerKey = clsidClassKey.OpenSubKey("InProcServer32", true)
            ?? clsidClassKey.CreateSubKey("InProcServer32", true);
        clsidServerKey.SetValue(null, modulePath, RegistryValueKind.String);
        clsidServerKey.SetValue("ThreadingModel", "Both", RegistryValueKind.String);

        using RegistryKey amsiProviderKey = hklm.OpenSubKey(amsiPath, true)
            ?? hklm.CreateSubKey(amsiPath, true);
        amsiProviderKey.SetValue(null, name, RegistryValueKind.String);
    }

    public static void Unregister(string clsid)
    {
        using RegistryKey hklm = RegistryKey.OpenBaseKey(
            RegistryHive.LocalMachine,
            RegistryView.Default);

        hklm.DeleteSubKeyTree($"{AmsiProviderKeyPath}\\{{{clsid}}}", false);
        hklm.DeleteSubKeyTree($"{ClsidKeyPath}\\{{{clsid}}}", false);
    }
}
