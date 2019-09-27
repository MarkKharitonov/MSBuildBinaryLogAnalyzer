param(
    [ValidateNotNullOrEmpty()]$BuildNumber = $env:Build_BuildNumber
)

$ErrorActionPreference = "Stop"
Set-Location "$PSScriptRoot\.."
. .\build\Dayforce.PS.Core.Bootstrap.ps1
Use-Defaults

Set-PackageQuality 'dayforce' (Get-Item *.sln).BaseName $BuildNumber 'Release'