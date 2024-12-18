
using System;
using Microsoft.Win32;

namespace AmsiProvider;

internal static class DllRegistration
{
    private const string AmsiProviderKeyPath = @"SOFTWARE\Microsoft\AMSI\Providers";
    private const string ClsidKeyPath = @"SOFTWARE\Classes\CLSID";
    private const string AmsiProviderEnvVar = "AMSI_TEST_PROVIDER_VERSION";

    public static void Register(
        string clsid,
        string name,
        string modulePath)
    {
        using RegistryKey hklm = RegistryKey.OpenBaseKey(
            RegistryHive.LocalMachine,
            RegistryView.Default);

        string clsidPath = $"{ClsidKeyPath}\\{{{clsid}}}";
        using RegistryKey clsidClassKey = hklm.OpenSubKey(clsidPath, true)
            ?? hklm.CreateSubKey(clsidPath, true);
        clsidClassKey.SetValue(null, name, RegistryValueKind.String);

        using RegistryKey clsidServerKey = clsidClassKey.OpenSubKey("InProcServer32", true)
            ?? clsidClassKey.CreateSubKey("InProcServer32", true);
        clsidServerKey.SetValue(null, modulePath, RegistryValueKind.String);
        clsidServerKey.SetValue("ThreadingModel", "Both", RegistryValueKind.String);

        // Defaults to 2 but can be set to 1 for compatibility with older
        // Windows versions. I'm unsure when 2 was introduced but it adds the
        // IAntimalwareProvider2 interface that supports Notify operations.
        string? providerVersion = Environment.GetEnvironmentVariable(AmsiProviderEnvVar);
        if (string.IsNullOrWhiteSpace(providerVersion))
        {
            providerVersion = "2";
        }

        string amsiPath = providerVersion switch
        {
            "1" => AmsiProviderKeyPath,
            "2" => $"{AmsiProviderKeyPath}2",
            _ => throw new ArgumentException($"Unsupported AMSI provider version specified '{providerVersion}', must be 1 or 2."),
        } + $"\\{{{clsid}}}";
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
        hklm.DeleteSubKeyTree($"{AmsiProviderKeyPath}2\\{{{clsid}}}", false);
        hklm.DeleteSubKeyTree($"{ClsidKeyPath}\\{{{clsid}}}", false);
    }
}
