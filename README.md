# Test AMSI Provider
This is a [AMSI](https://learn.microsoft.com/en-us/windows/win32/amsi/antimalware-scan-interface-portal) antimalware provider written in C# that can be used to log the raw AMSI scan and notify requests from client applications. The dll is compiled through NativeAOT so requires no .NET runtime to run. NativeAOT and C# was chosen to see if it was possible to write a COM server plugin in managed code without having to touch or C++ and also build a dll that required no extra runtime depedencies. While there is liberal use of unsafe code and pointers this proves it is possible to do.

This is designed as a proof of concept and test code, there is no guarantee of quality and use at your own risk.

## What Can It Do
The code here can be used as a registered COM server for the [IAntimalware](https://learn.microsoft.com/en-us/windows/win32/api/amsi/nn-amsi-iantimalwareprovider) or [IAntimalware2](https://learn.microsoft.com/en-us/windows/win32/api/amsi/nn-amsi-iantimalwareprovider2) implementation. These providers are called by AMSI when a client sends data to AMSI to scan or notify.

The test provider is registered under the CLSID `00087ee2-25f9-43bc-a94f-2edeef851a65` and will log the raw [IAntimalwareProvider.Scan](https://learn.microsoft.com/en-us/windows/win32/api/amsi/nf-amsi-iantimalwareprovider-scan) and [IAntimalwareProvider.Notify](https://learn.microsoft.com/en-us/windows/win32/api/amsi/nf-amsi-iantimalwareprovider2-notify) calls into a text file for later parsing.

Each line in the log file is a JSON string with the following keys:

```json
{
    "Action": "Either Scan or Notify based on the COM method called",
    "AppName": "The name, version, or GUID string of the calling application",
    "ContentName": "The filename, URL, unique script ID, or similar of the content",
    "SessionId": "Unique id to associate related Scan calls, will be 0 for Notify actions",
    "Content": "The content string for the operation"
}
```

## Configuration
By default, the provider is set to write all the data to a file called `AmsiProvider.log` located next to the provider dll. It is possible to adjust this behaviour and other settings by creating a config file called `AmsiProvider.config.json` next to the DLL. This config file can control the following settings:

```json
{
    "LogPath": "The directory to write the log file to, if unset it uses the same directory as the provider dll",
    "StoreByPid": "By default a single log file is used, setting this to true will create a log file per process with the PID in the filename",
    "ContentEncoding": "Can be set to Base64/Hex/Unicode or any other valid encoding, defaults to Unicode"
}
```

The `LogPath` can also be set to either `stdout` or `stderr` to write the standard output or error of the process which is invoking the AMSI provider. If specifying a path to a directory, make sure that it is writable by the user triggering the AMSI provider as the logging is run in the same process as what is triggering AMSI. By default the log file in the path will always be `AmsiProvider.log`, this may be problematic if there are multiple processes trying to write to the log at the same time. To avoid this you can set `"StoreByPid": true` to instead use the filename `AmsiProvider.$pid.log` where `$pid` is the process id for the client requesting the AMSI scan. This avoids the clobbering issue but can result in multiple log files being used.

The `ContentEncoding` value controls the format/encoding the `"Content"` key is set to. Here is a more detailed explanation of what each option does:

+ `Unicode`: (default) will decode the raw buffers as a Unicode/UTF-16-LE encoded string
+ `Base64`: will write the buffer as a base64 encoded string of the raw bytes
+ `Hex`: will write the buffer as a hex encoded string of the raw bytes

Any other value will be used with `Encoding.GetEncoding("...")` to specify what encoding is used to decode the raw buffer bytes as a string.

## Examples
A common application that uses AMSI to scan the scripts and functions it runs is PowerShell. Using this provider we can see the data provided to AMSI from PowerShell. For example we can capture the data that PowerShell sends to AMSI

```powershell
regsvr32.exe /s .\bin\AmsiProvider\AmsiProvider.dll
pwsh.exe -Command '$path = "C:\Windows"; [System.IO.Directory]::Exists($path)'
regsvr32.exe /u /s .\bin\AmsiProvider\AmsiProvider.dll
```

```json
{"Action":"Scan","AppName":"PowerShell_C:\\Program Files\\PowerShell\\7\\pwsh.exe_7.4.6 SHA: d71d4f122db89c1bcfb5571b9445d600803c332b","ContentName":"","SessionId":26014,"Content":"$path = \u0022C:\\Windows\u0022; [System.IO.Directory]::Exists($path)"}
{"Action":"Notify","AppName":"PowerShell_C:\\Program Files\\PowerShell\\7\\pwsh.exe_7.4.6 SHA: d71d4f122db89c1bcfb5571b9445d600803c332b","ContentName":"PowerShellMemberInvocation","SessionId":0,"Content":"\u003CSystem.IO.Directory\u003E.Exists(\u003CC:\\Windows\u003E)"}
{"Action":"Scan","AppName":"PowerShell_C:\\Program Files\\PowerShell\\7\\pwsh.exe_7.4.6 SHA: d71d4f122db89c1bcfb5571b9445d600803c332b","ContentName":"","SessionId":26015,"Content":"$global:?"}
```

You can use the following PowerShell code to get a more human friendly output of the data.

```powershell
Get-Content .\bin\AmsiProvider\AmsiProvider.log | ForEach-Object { $_ | ConvertFrom-Json } | Format-List
```

```yaml
Action      : Scan
AppName     : PowerShell_C:\Program Files\PowerShell\7\pwsh.exe_7.4.6 SHA: d71d4f122db89c1bcfb5571b9445d600803c332b
ContentName :
SessionId   : 26014
Content     : $path = "C:\Windows"; [System.IO.Directory]::Exists($path)

Action      : Notify
AppName     : PowerShell_C:\Program Files\PowerShell\7\pwsh.exe_7.4.6 SHA: d71d4f122db89c1bcfb5571b9445d600803c332b
ContentName : PowerShellMemberInvocation
SessionId   : 0
Content     : <System.IO.Directory>.Exists(<C:\Windows>)

Action      : Scan
AppName     : PowerShell_C:\Program Files\PowerShell\7\pwsh.exe_7.4.6 SHA: d71d4f122db89c1bcfb5571b9445d600803c332b
ContentName :
SessionId   : 26015
Content     : $global:?
```

Breaking this down we can see that PowerShell has asked AMSI if the provided command was safe to run through the `Scan` operation. When running the code PowerShell also uses the `Notify` action to log all .NET method invocations. The example here shows what `[System.IO.Directory]::Exists($path)` was called with `C:\Windows`. Finally the last `Scan` call was PowerShell checking `$?` to set the exit code before closing the process.

The data returned can vary from application to application. This provider has mostly be tested with PowerShell but should work with any other application which writes to AMSI.

For example the `AmsiClient` included as a test with this repo will emit a scan and notify request:

```powershell
.\bin\AmsiClient\AmsiClient.exe
# AmsiNotifyOperation [testing notify] - 0 - AMSI_RESULT_NOT_DETECTED
# AmsiScanBuffer [testing scan buffer] - 0 - AMSI_RESULT_NOT_DETECTED
```

```json
{"Action":"Notify","AppName":"AmsiTest","ContentName":"notify content app","SessionId":0,"Content":"testing notify"}
{"Action":"Scan","AppName":"AmsiTest","ContentName":"scan buffer app","SessionId":29102,"Content":"testing scan buffer"}
```

## How to Build
To build this provider, the .NET SDK capable of building `net9.0-windows` is required. Once installed you can run the following to build the provider dll.

```powershell
powershell.exe -File build.ps1
```

The provider dll will be located in `bin\AmsiProvider\AmsiProvider.dll`.
The test client exe will be located in `bin\AmsiClient\AmsiClient.exe`

## How to Install
Once built the DLL needs to be registered with [regsvr32](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/regsvr32).

```powershell
regsvr32.exe .\bin\AmsiProvider\AmsiProvider.dll
```

_Note: Registration requires administrator rights_

After installing, new processes that create an AMSI session will start sending the data to the provider and will be added to the `AmsiProvider.log` file.

To uninstall run the following `regsvr32` command

```powershell
regsvr32.exe /u .\bin\AmsiProvider\AmsiProvider.dll
```

By default the registration will use the `Providers2` subkey in the AMSI registry tree which supports the `IAntimalwareProvider2` interface and the `Notify` operation. I am not sure when `Providers2` support was added but if you are using an older Windows version and the logs are not working after registration you can use the `AMSI_TEST_PROVIDER_VERSION` env var to control the provider version to register under.

```powershell
# Ensure any old registrations are removed
regsvr32.exe /u .\bin\AmsiProvider\AmsiProvider.dll

# Set the provider version to 1 for older hosts before registration
$env:AMSI_TEST_PROVIDER_VERSION = '1'
regsvr32.exe .\bin\AmsiProvider\AmsiProvider.dll

# Unset the env var now that registration is done
$env:AMSI_TEST_PROVIDER_VERSION = $null
```

The value for `AMSI_TEST_PROVIDER_VERSION` can be set to the following, any other value will result in a failure during registration.

+ `1` - The baseline version, supports the `Scan` request
+ `2` (default) - Adds support for the `Notify` request

Each version supports all the features of the versions before it.

The `regsvr32.exe` command called the [DllRegisterServer](https://learn.microsoft.com/en-us/windows/win32/api/olectl/nf-olectl-dllregisterserver) function exported by the provider which in turn set the following registry values (`Providers2` may be different depending on what `AMSI_TEST_PROVIDER_VERSION` was set to):

+ `HKLM:\SOFTWARE\Microsoft\AMSI\Providers2\{00087ee2-25f9-43bc-a94f-2edeef851a65}`
  + `(Default)` - `TestAmsiProvider`
+ `HKLM:\SOFTWARE\Classes\CLSID\{00087ee2-25f9-43bc-a94f-2edeef851a65}`
  + `(Default)` - `TestAmsiProvider`
+ `HKLM:\SOFTWARE\Classes\CLSID\{00087ee2-25f9-43bc-a94f-2edeef851a65}\InProcServer32`
  +  `(Default)` - Path to the dll provided with `regsvr32.exe`
  + `ThreadingModel` - `Both`

This registration allows AMSI to locate the DLL and load the COM server component to start receiving the data to scan.
