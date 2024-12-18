using namespace System.IO

#Requires -Module Pester
#Requires -RunAsAdministrator
#Requires -Version 7.4

[CmdletBinding()]
param ()

$ErrorActionPreference = 'Stop'

$configuration = [PesterConfiguration]::Default
$configuration.Output.Verbosity = 'Detailed'
$configuration.Run.Path = "$PSScriptRoot\tests\*.ps1"
$configuration.Run.Throw = $true

Invoke-Pester -Configuration $configuration -WarningAction Ignore
