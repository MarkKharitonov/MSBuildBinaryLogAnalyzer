$RepoName = [io.path]::GetFileNameWithoutExtension([io.path]::GetFileNameWithoutExtension($PSCommandPath))
$BootstrapImplFilePath = [io.path]::GetFullPath("$PSScriptRoot\$RepoName.Bootstrap.Impl.ps1")
$BootstrapImpl = Get-Item $BootstrapImplFilePath -ErrorAction SilentlyContinue
if (!$BootstrapImpl -or (([datetime]::UtcNow - $BootstrapImpl.LastWriteTimeUtc).TotalHours -gt 24)) {
    $Url = "http://tdc1tfsapp01.dayforce.com:8080/tfs/DefaultCollection/DFDevOps/_apis/git/repositories/$RepoName/items?path=$RepoName.Bootstrap.Impl.ps1&api-version=4.1"
    try {
        Invoke-RestMethod -UseDefaultCredentials -Uri $Url -OutFile $BootstrapImplFilePath
    }
    catch {
        $ErrorMessagePrefix = "Failed to download the latest $RepoName bootstrap implementation - $($_.Exception.Message)"
        if ($BootstrapImpl) {
            Write-Host -ForegroundColor Yellow "$ErrorMessagePrefix. Using the cached version (potentially out of date)."
        }
        else {
            throw "$ErrorMessagePrefix and no cached version is found."    
        }
    }
}
. $BootstrapImplFilePath