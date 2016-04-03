# run the build script
$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition

[System.IO.FileInfo]$buildFile = (Join-Path $scriptDir 'build.ps1')

<#
try{
    

    if($env:APPVEYOR_REPO_BRANCH -eq 'release' -and ([string]::IsNullOrWhiteSpace($env:APPVEYOR_PULL_REQUEST_NUMBER) )) {
        . $buildFile.FullName -publishToNuget
    }
    else{
        . $buildFile.FullName
    }
    
}
catch{
    throw ( 'Build error {0} {1}' -f $_.Exception, (Get-PSCallStack|Out-String) )
}
#>

$channelList = 'future','preview',$null
$feedList = 'https://dotnet.myget.org/f/dotnet-cli','https://api.nuget.org/v3/index.json'
$installUrlList = 'https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.ps1','https://raw.githubusercontent.com/sayedihashimi/cli/issue2236/scripts/obtain/install.ps1'
$originalPath = $env:Path

foreach($installUrl in $installUrlList){
    # rest path to original
    $env:path = $originalPath
    'Trying with installer from [{0}]' -f $installUrlList | Write-Host
    try{
        $installdir = "$env:LOCALAPPDATA\Microsoft\dotnet"
        if(Test-Path $installdir){
            Remove-Item $installdir -Recurse
        }

        . $buildFile.FullName -installOnly

        foreach($channel in $channelList){
            'Trying build with [channel={0},nugeturl=<null>]' -f $channel | Write-Host
            . $buildFile.FullName -dotnetInstallChannel $channel -dotnetNugetFeedSource $null -SkipInstallDotNet -onlyBuildDOtnetProjects

            foreach($feed in $feedList){
                try{
                    'Trying build with [channel={0},nugeturl={1}]' -f $channel,$feed | Write-Host
                    . $buildFile.FullName -dotnetInstallChannel $channel -dotnetNugetFeedSource $feed -SkipInstallDotNet -onlyBuildDOtnetProjects
                }
                catch{
                    ( 'Build error {0} {1}' -f $_.Exception, (Get-PSCallStack|Out-String) ) | Write-Error
                }
            }
        }
    }
    catch{
        'Unable to install from url [{0}]' -f $installUrlList | Write-Error
    }
}

