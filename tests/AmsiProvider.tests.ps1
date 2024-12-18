#Requires -RunAsAdministrator

using namespace System.IO

Describe "AmsiProvider tests" {
    BeforeAll {
        $amsiClient = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiClient\AmsiClient.exe")
        $logPath = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiProvider\AmsiProvider.log")
        $dllPath = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiProvider\AmsiProvider.dll")
        $configPath = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiProvider\AmsiProvider.config.json")

        Function Register-AmsiProvider {
            [CmdletBinding()]
            param (
                [Parameter()]
                [string]
                $Version
            )

            if ($Version) {
                $env:AMSI_TEST_PROVIDER_VERSION = $Version
            }

            $proc = Start-Process regsvr32.exe -ArgumentList "/s $dllPath" -Wait -PassThru
            $env:AMSI_TEST_PROVIDER_VERSION = $null
            if ($proc.ExitCode) {
                throw "Failed to register test provider $($proc.ExitCode)"
            }
        }

        Function Unregister-AmsiProvider {
            [CmdletBinding()]
            param ()

            $proc = Start-Process regsvr32.exe -ArgumentList "/u /s $dllPath" -Wait -PassThru
            if ($proc.ExitCode) {
                throw "Failed to unregister test provider $($proc.ExitCode)"
            }
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

    Context "V1 provider" {
        BeforeAll {
            Register-AmsiProvider -Version 1
        }
        AfterAll {
            Unregister-AmsiProvider
        }

        It "Captures scan only from AmsiClient" {
            $out = & $amsiClient
            $LASTEXITCODE | Should -Be 0
            $out.Count | Should -Be 2
            $out[0] | Should -Be 'AmsiNotifyOperation [testing notify] - 0 - AMSI_RESULT_NOT_DETECTED'
            $out[1] | Should -Be 'AmsiScanBuffer [testing scan buffer] - 0 - AMSI_RESULT_NOT_DETECTED'

            $log = Get-Content -LiteralPath $logPath | ConvertFrom-Json
            $log.Count | Should -Be 1

            $log.Action | Should -Be Scan
            $log.AppName | Should -Be 'AmsiTest'
            $log.ContentName | Should -Be 'scan buffer app'
            $log.SessionId | Should -BeGreaterThan 0
            $log.Content | Should -Be 'testing scan buffer'
        }
    }

    Context "V2 provider" {
        BeforeAll {
            Register-AmsiProvider
        }
        AfterAll {
            Unregister-AmsiProvider
        }

        It "Captures scan and notify from AmsiClient" {
            $out = & $amsiClient
            $LASTEXITCODE | Should -Be 0
            $out.Count | Should -Be 2
            $out[0] | Should -Be 'AmsiNotifyOperation [testing notify] - 0 - AMSI_RESULT_NOT_DETECTED'
            $out[1] | Should -Be 'AmsiScanBuffer [testing scan buffer] - 0 - AMSI_RESULT_NOT_DETECTED'

            $log = Get-Content -LiteralPath $logPath | ConvertFrom-Json
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

        It "Uses custom log path" {
            $logDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("TestDrive:\")
            $customLogPath = Join-Path $logDir 'AmsiProvider.log'
            Set-Content -LiteralPath $configPath -Value (ConvertTo-Json @{
                LogPath = $logDir
            })

            try {
                $out = & $amsiClient
                $LASTEXITCODE | Should -Be 0
                $out.Count | Should -Be 2
                $out[0] | Should -Be 'AmsiNotifyOperation [testing notify] - 0 - AMSI_RESULT_NOT_DETECTED'
                $out[1] | Should -Be 'AmsiScanBuffer [testing scan buffer] - 0 - AMSI_RESULT_NOT_DETECTED'

                Test-Path -LiteralPath $logPath | Should -BeFalse
                $log = Get-Content -LiteralPath $customLogPath | ConvertFrom-Json
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
            finally {
                if (Test-Path -LiteralPath $customLogPath) {
                    Remove-Item -LiteralPath $customLogPath -Force
                }
            }
        }

        It "Uses PID log files" {
            Set-Content -LiteralPath $configPath -Value (ConvertTo-Json @{
                StoreByPid = $true
            })

            $proc = Start-Process -FilePath $amsiClient -Wait -PassThru
            $logPidPath = [Path]::GetFullPath("$PSScriptRoot\..\bin\AmsiProvider\AmsiProvider.$($proc.Id).log")
            try {
                $proc.ExitCode | Should -Be 0

                Test-Path -LiteralPath $logPath | Should -BeFalse
                $log = Get-Content -LiteralPath $logPidPath | ConvertFrom-Json
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
            finally {
                if (Test-Path -LiteralPath $logPidPath) {
                    Remove-Item -LiteralPath $logPidPath
                }
            }
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

        It "Writes log to stderr" {
            Set-Content -LiteralPath $configPath -Value (ConvertTo-Json @{
                    LogPath = 'stderr'
                })

            $stdout = $null
            $stderr = . { & $amsiClient | Set-Variable -Name stdout } 2>&1 | ForEach-Object ToString
            $LASTEXITCODE | Should -Be 0

            $stdout.Count | Should -Be 2
            $stdout[0] | Should -Be 'AmsiNotifyOperation [testing notify] - 0 - AMSI_RESULT_NOT_DETECTED'
            $stdout[1] | Should -Be 'AmsiScanBuffer [testing scan buffer] - 0 - AMSI_RESULT_NOT_DETECTED'

            $stderr.Count | Should -Be 2
            $log = $stderr | ConvertFrom-Json
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

        It "Captures pwsh output" {
            $pwshPath = (Get-Command pwsh.exe).Path
            $appNamePattern = "PowerShell_$pwshPath* SHA: *"

            $out = pwsh.exe -Command '$var = "foo"; [Console]::WriteLine($var)'

            $out.Count | Should -Be 1
            $out | Should -Be 'foo'

            $log = Get-Content -LiteralPath $logPath | ConvertFrom-Json
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
}
