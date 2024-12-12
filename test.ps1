using namespace System.Runtime.InteropServices

$ErrorActionPreference = 'Stop'

$dllPath = "$PSScriptRoot\bin\AmsiProvider\AmsiProvider.dll"
# $lib = [NativeLibrary]::Load($dllPath)
# try {
#     Write-Host loaded
# }
# finally {
#     [NativeLibrary]::Free($lib)
# }

$regProc = Start-Process regsvr32 "/s ""$dllPath""" -Wait -PassThru
if ($regProc.ExitCode) {
    throw "Failed to register provider dll: $($regProc.ExitCode)"
}

try {
    Write-Host "Running test"
    pwsh -Command 'echo "hi"'
    Write-Host "Test end"
}
finally {
    $unregProc = Start-Process regsvr32 "/s /u ""$dllPath""" -Wait -PassThru
    if ($unregProc.ExitCode) {
        throw "Failed to unregister provider dll: $($unregProc.ExitCode)"
    }   
}