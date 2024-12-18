#Requires -RunAsAdministrator

using namespace Sstem.IO

Describe "AmsiProvider tests" {
    BeforeAll {
        $amsiClient = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiClient\AmsiClient.exe")
        $logPath = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiProvider\AmsiProvider.log")
        $dllPath = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiProvider\AmsiProvider.dll")
        $configPath = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiProvider\AmsiProvider.config.json")

        $proc = Start-Process regsvr32.exe -ArgumentList "/s $dllPath" -Wait -PassThru
        if ($proc.ExitCode) {
            throw "Failed to register dll $($proc.ExitCode)"
        }
    }
    AfterAll {
        $proc = Start-Process regsvr32.exe -ArgumentList "/u /s $dllPath" -Wait -PassThru
        if ($proc.ExitCode) {
            throw "Failed to register dll $($proc.ExitCode)"
        }
    }

    BeforeEach {
        if (Test-Path -LiteralPath $logPath) {
            Remove-Item -LiteralPath $logPath
        }
        if (Test-Path -LiteralPath $configPath) {
            Remove-Item -LiteralPath $configPath
        }
    }

    It "Captures scan and notify from AmsiClient" {
        $out = & $amsiClient
        $LASTEXITCODE | Should -Be 0
        $out.Count | Should -Be 2
        $out[0] | Should -Be 'AmsiNotifyOperation [testing notify] - 0 - AMSI_RESULT_NOT_DETECTED'
        $out[1] | Should -Be 'AmsiScanBuffer [testing scan buffer] - 0 - AMSI_RESULT_NOT_DETECTED'

        $log = Get-Content $logPath | ConvertFrom-Json
        $log.Count | Should -Be 2

        $log[0].Action | Should -Be Notify
        $log[0].AppName | Should -Be 'AmsiTest'
        $log[0].ContentName | Should -Be 'notify content app'
        $log[0].SessionId | Should -Be 0
        $log[0].Content | Should -Be 'testing notify'

        $log[1].Action | Should -Be Scan
        $log[1].AppName | Should -Be 'AmsiTest'
        $log[1].ContentName | Should -Be 'scan buffer app'
        $log[1].SessionId | Should -BeGreaterThan 0
        $log[1].Content | Should -Be 'testing scan buffer'
    }

    It "Writes log to stdout" {
        Set-Content -LiteralPath $configPath -Value (ConvertTo-Json @{
            LogPath = 'stdout'
        })

        $out = & $amsiClient
        $LASTEXITCODE | Should -Be 0
        $out.Count | Should -Be 4

        $outEntries = @()
        $jsonEntries = @()
        $out | ForEach-Object {
            if ($_.StartsWith('{')) {
                $jsonEntries += $_ | ConvertFrom-Json
            }
            else {
                $outEntries += $_
            }
        }

        $outEntries.Count | Should -Be 2
        $outEntries[0] | Should -Be 'AmsiNotifyOperation [testing notify] - 0 - AMSI_RESULT_NOT_DETECTED'
        $outEntries[1] | Should -Be 'AmsiScanBuffer [testing scan buffer] - 0 - AMSI_RESULT_NOT_DETECTED'

        $jsonEntries.Count | Should -Be 2
        $jsonEntries[0].Action | Should -Be Notify
        $jsonEntries[0].AppName | Should -Be 'AmsiTest'
        $jsonEntries[0].ContentName | Should -Be 'notify content app'
        $jsonEntries[0].SessionId | Should -Be 0
        $jsonEntries[0].Content | Should -Be 'testing notify'

        $jsonEntries[1].Action | Should -Be Scan
        $jsonEntries[1].AppName | Should -Be 'AmsiTest'
        $jsonEntries[1].ContentName | Should -Be 'scan buffer app'
        $jsonEntries[1].SessionId | Should -BeGreaterThan 0
        $jsonEntries[1].Content | Should -Be 'testing scan buffer'
    }

    It "Captures pwsh output" {
        $pwshPath = (Get-Command pwsh.exe).Path
        $appNamePattern = "PowerShell_$pwshPath* SHA: *"

        $out = pwsh.exe -Command '$var = "foo"; [Console]::WriteLine($var)'

        $out.Count | Should -Be 1
        $out | Should -Be 'foo'

        $log = Get-Content $logPath | ConvertFrom-Json
        $log.Count | Should -Be 3

        $log[0].Action | Should -Be Scan
        $log[0].AppName | Should -BeLike $appNamePattern
        $log[0].ContentName | Should -BeNullOrEmpty
        $log[0].SessionId | Should -BeGreaterThan 0
        $log[0].Content | Should -Be '$var = "foo"; [Console]::WriteLine($var)'

        $log[1].Action | Should -Be Notify
        $log[1].AppName | Should -BeLike $appNamePattern
        $log[1].ContentName | Should -Be 'PowerShellMemberInvocation'
        $log[1].SessionId | Should -Be 0
        $log[1].Content | Should -Be '<System.Console>.WriteLine(<foo>)'

        $log[2].Action | Should -Be Scan
        $log[2].AppName | Should -BeLike $appNamePattern
        $log[2].ContentName | Should -BeNullOrEmpty
        $log[2].SessionId | Should -BeGreaterThan 0
        $log[2].Content | Should -Be '$global:?'
    }
}
