# run the build script
$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition

[System.IO.FileInfo]$buildFile = (Join-Path $scriptDir 'build.ps1')

try{
    if($env:APPVEYOR_REPO_BRANCH -eq 'release' -and ([string]::IsNullOrWhiteSpace($env:APPVEYOR_PULL_REQUEST_NUMBER) )) {
        . $buildFile.FullName -publishToNuget
    }
    else{
        . $buildFile.FullName -verbose
    }
    
}
catch{
    throw ( 'Build error {0} {1}' -f $_.Exception, (Get-PSCallStack|Out-String) )
}
'After build'|Write-Output