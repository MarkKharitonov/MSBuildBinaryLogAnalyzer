function GetExePath([switch]$NoBuild)
{
    $ExePath = "$PSScriptRoot\src\MSBuildBinaryLogAnalyzer\bin\Release\net6\MSBuildBinaryLogAnalyzer.exe"
    if (Test-Path $ExePath)
    {
        $DebugExePath = $ExePath.Replace('\Release\','\Debug\')
        if ($NoBuild -or !(Test-Path $DebugExePath) -or ((Get-Item $ExePath).LastWriteTimeUtc -gt (Get-Item $DebugExePath).LastWriteTimeUtc))
        {
            return $ExePath
        }
    }
    Push-Location $PSScriptRoot
    try
    {
        dotnet build -c:Release | Write-Host
        if ($LastExitCode)
        {
            throw "Aborted"
        }
        $ExePath
    }
    finally
    {
        Pop-Location
    }
}

$ExePath = GetExePath
& $ExePath $args
