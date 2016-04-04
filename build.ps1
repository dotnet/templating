[cmdletbinding(DefaultParameterSetName='build')]
param(
    [Parameter(ParameterSetName='build',Position=0)]
    [string]$configuration = 'Release',

    [Parameter(ParameterSetName='build',Position=1)]
    [switch]$SkipInstallDotNet,

    [Parameter(ParameterSetName='build',Position=2)]
    [switch]$publishToNuget,

    [Parameter(ParameterSetName='build',Position=3)]
    [string]$nugetApiKey = ($env:NuGetApiKey),

    [Parameter(ParameterSetName='build',Position=4)]
    [Parameter(ParameterSetName='install',Position=1)]
    [string]$dotnetNugetFeedSource=$null,

    [Parameter(ParameterSetName='build',Position=5)]
    [Parameter(ParameterSetName='install',Position=2)]
    [string]$dotnetInstallChannel = 'preview',

    # TODO: Revert back to https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.ps1 when bug fixed
    [Parameter(ParameterSetName='build',Position=5)]
    [Parameter(ParameterSetName='install',Position=3)]
    [string]$dotnetInstallUrl = 'https://raw.githubusercontent.com/sayedihashimi/cli/issue2236/scripts/obtain/install.ps1',

    # version parameters
    [Parameter(ParameterSetName='setversion',Position=10)]
    [switch]$setversion,

    [Parameter(ParameterSetName='setversion',Position=11,Mandatory=$true)]
    [string]$newversion,

    [Parameter(ParameterSetName='getversion',Position=0)]
    [switch]$getversion
)

#$dotnetNugetFeedSource='https://dotnet.myget.org/f/dotnet-cli'
#$dotnetNugetFeedSource='https://api.nuget.org/v3/index.json'
$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition

[System.IO.FileInfo]$slnfile = (join-path $scriptDir 'Mutant.Chicken.sln')
[System.IO.FileInfo[]]$csProjects = (Join-Path $scriptDir 'src\Mutant.Chicken.Net4\Mutant.Chicken.Net4.csproj'),(Join-Path $scriptDir 'src\Mutant.Chicken.Net4.Demo\Mutant.Chicken.Net4.Demo.csproj' )
[System.IO.FileInfo[]]$projectJsonToBuild = (Join-Path $scriptDir 'src\Mutant.Chicken\project.json')
[System.IO.DirectoryInfo]$outputroot=(join-path $scriptDir 'OutputRoot')
[System.IO.DirectoryInfo]$outputPathNuget = (Join-Path $outputroot '_nuget-pkg')
$localNugetFolder = 'c:\temp\nuget\local'

<#
.SYNOPSIS
    You can add this to you build script to ensure that psbuild is available before calling
    Invoke-MSBuild. If psbuild is not available locally it will be downloaded automatically.
#>
function EnsurePsbuildInstlled{
    [cmdletbinding()]
    param(
        # TODO: Change to master when 1.1.9 gets there
        [string]$psbuildInstallUri = 'https://raw.githubusercontent.com/ligershark/psbuild/dev/src/GetPSBuild.ps1',

        [System.Version]$minVersion = (New-Object -TypeName 'system.version' -ArgumentList '1.1.9.1')
    )
    process{
        # see if there is already a version loaded
        $psbuildNeedsInstall = $true
        [System.Version]$installedVersion = $null
        try{
            Import-Module psbuild -ErrorAction SilentlyContinue | Out-Null
            $installedVersion = Get-PSBuildVersion
        }
        catch{
            $installedVersion = $null
        }

        if( ($installedVersion -ne $null) -and ($installedVersion.CompareTo($minVersion) -ge 0) ){
            'Skipping psbuild install because version [{0}] detected' -f $installedVersion.ToString() | Write-Verbose
        }
        else{
            'Installing psbuild from [{0}]' -f $psbuildInstallUri | Write-Verbose
            (new-object Net.WebClient).DownloadString($psbuildInstallUri) | iex

            # make sure it's loaded and throw if not
            if(-not (Get-Command "Invoke-MsBuild" -errorAction SilentlyContinue)){
                throw ('Unable to install/load psbuild from [{0}]' -f $psbuildInstallUri)
            }
        }
    }
}
function EnsureFileReplacerInstlled{
    [cmdletbinding()]
    param()
    begin{
        Import-NuGetPowershell
    }
    process{
        if(-not (Get-Command -Module file-replacer -Name Replace-TextInFolder -errorAction SilentlyContinue)){
            $fpinstallpath = (Get-NuGetPackage -name file-replacer -version '0.4.0-beta' -binpath)
            if(-not (Test-Path $fpinstallpath)){ throw ('file-replacer folder not found at [{0}]' -f $fpinstallpath) }
            Import-Module (Join-Path $fpinstallpath 'file-replacer.psm1') -DisableNameChecking
        }

        # make sure it's loaded and throw if not
        if(-not (Get-Command -Module file-replacer -Name Replace-TextInFolder -errorAction SilentlyContinue)){
            throw ('Unable to install/load file-replacer')
        }
    }
}

function InstallDotNetCli{
    [cmdletbinding()]
    param()
    process{
        $oldloc = Get-Location
        try{
            Set-Location ($slnfile.DirectoryName)
            $tempfile = '{0}.ps1' -f ([System.IO.Path]::GetTempFileName())
            (new-object net.webclient).DownloadFile($dotnetInstallUrl,$tempfile)
            $installArgs = ''
            if(-not ([string]::IsNullOrWhiteSpace($dotnetInstallChannel))){
                $installArgs = '-Channel ' + $dotnetInstallChannel
            }
            Invoke-Expression "& `"$tempfile`" $installArgs"
            $env:path+=";$env:localappdata\Microsoft\dotnet\bin"
            & dotnet --version
            Remove-Item $tempfile -ErrorAction SilentlyContinue
        }
        finally{
            Set-Location $oldloc
        }
    }
}

function InternalEnsure-DirectoryExists{
    param([Parameter(Position=0)][System.IO.DirectoryInfo]$path)
    process{
        if($path -ne $null){
            if(-not (Test-Path $path.FullName)){
                New-Item -Path $path.FullName -ItemType Directory
            }
        }
    }
}

function CleanOutputFolder{
    [cmdletbinding()]
    param()
    process{
        if( ($outputroot -eq $null) -or ([string]::IsNullOrWhiteSpace($outputroot.FullName))){
            return
        }
        elseif(Test-Path $outputroot.FullName){
            'Removing output folder at [{0}]' -f $outputroot.FullName | Write-Output
            Remove-Item $outputroot -Recurse -Force
        }
    }
}

function RestoreNuGetPackages{
    [cmdletbinding()]
    param()
    process{
        $oldloc = Get-Location
        try{
            'restoring nuget packages' | Write-Output
            Set-Location $slnfile.Directory.FullName
            Invoke-CommandString -command (Get-Nuget) -commandArgs restore
        }
        finally{
            Set-Location $oldloc
        }
    }
}

function PublishNuGetPackage{
    [cmdletbinding()]
    param(
        [Parameter(Mandatory=$true,ValueFromPipeline=$true)]
        [string]$nugetPackages,

        [Parameter(Mandatory=$true)]
        $nugetApiKey
    )
    process{
        foreach($nugetPackage in $nugetPackages){
            $pkgPath = (get-item $nugetPackage).FullName
            $cmdArgs = @('push',$pkgPath,$nugetApiKey,'-NonInteractive')

            'Publishing nuget package with the following args: [nuget.exe {0}]' -f ($cmdArgs -join ' ') | Write-Verbose
            &(Get-Nuget) $cmdArgs
        }
    }
}

function CopyStaticFilesToOutputDir{
    [cmdletbinding()]
    param()
    process{
        Get-ChildItem $scriptDir *.nuspec -File | Copy-Item -Destination $outputroot
    }
}

function Copy-PackagesToLocalNuGetFolder{
    [cmdletbinding()]
    param(
        $outputFolder = $outputPathNuget
    )
    process{
        if(Test-Path $localNugetFolder){
            Get-ChildItem $outputFolder *.nupkg -Recurse -File|Copy-Item -Destination $localNugetFolder
        }
    }
}

function Build-NuGetPackage{
    [cmdletbinding()]
    param()
    process{
        if(-not (Test-Path $outputPathNuget)){
            New-Item -Path $outputPathNuget -ItemType Directory
        }

        Push-Location
        try{
            [System.IO.FileInfo[]]$nuspecFilesToBuild = @()
            $nuspecFilesToBuild += ([System.IO.FileInfo](Get-ChildItem $outputRoot '*.nuspec' -Recurse -File))

            foreach($nufile in $nuspecFilesToBuild){
                Push-Location
                try{
                    Set-Location -Path ($nufile.Directory.FullName)
                    'Building nuget package for [{0}]' -f ($nufile.FullName) | Write-Verbose
                    Invoke-CommandString -command (Get-Nuget) -commandArgs @('pack',($nufile.Name),'-NoPackageAnalysis','-OutputDirectory',($outputPathNuget.FullName))
                }
                finally{
                    Pop-Location
                }
            }

            Copy-PackagesToLocalNuGetFolder
        }
        finally{
            Pop-Location
        }
    }
}

function BuildSolution{
    [cmdletbinding()]
    param()
    process{

        if(-not (Test-Path $slnfile.FullName)){
            throw ('Solution not found at [{0}]' -f $slnfile.FullName)
        }

        if($outputroot -eq $null){
            throw ('output path is null')
        }

        foreach($project in $csProjects){
            if(-not (Test-Path $project.FullName)){
                throw ('Project not found at [{0}]' -f $project.FullName)
            }
        }
        foreach($pj in $projectJsonToBuild){
        if(-not (Test-Path $pj.FullName)){
                throw ('Project not found at [{0}]' -f $pj.FullName)
            }
        }

        [System.IO.DirectoryInfo]$vsoutputpath = (Join-Path $outputroot.FullName "vs")
        InternalEnsure-DirectoryExists -path $vsoutputpath.FullName

        'Building projects at [{0}]' -f ($csProjects.FullName -join ';') | Write-Output
        if(-not ($onlyBuildDOtnetProjects -eq $true)){
            Invoke-MSBuild -projectsToBuild $csProjects.FullName -visualStudioVersion 14.0 -configuration $configuration -outputpath $vsoutputpath.FullName
        }

        [System.IO.DirectoryInfo]$dnoutputpath = (Join-Path $outputroot.FullName "dotnet")
        InternalEnsure-DirectoryExists -path $dnoutputpath.FullName

        $oldloc = Get-Location
        try{
            foreach($pj in $projectJsonToBuild){
                Set-Location $pj.DirectoryName
                $restoreArgs = ('restore')

                if(-not ([string]::IsNullOrWhiteSpace($dotnetNugetFeedSource))){
                    $restoreArgs += (' -s',$dotnetNugetFeedSource)
                }

                Invoke-CommandString -command dotnet -commandArgs $restoreArgs
                $buildargs = @('build','--configuration', $configuration,  '--build-base-path', $dnoutputpath.FullName, ' --no-incremental')
                Invoke-CommandString -command dotnet -commandArgs $buildargs
            }
        }
        finally{
            Set-Location $oldloc
        }
    }
}
function Update-FilesWithCommitId{
    [cmdletbinding()]
    param(
        [string]$commitId = ($env:APPVEYOR_REPO_COMMIT),

        [System.IO.DirectoryInfo]$dirToUpdate = ($outputroot),

        [Parameter(Position=2)]
        [string]$filereplacerVersion = '0.4.0-beta'
    )
    begin{
        EnsureFileReplacerInstlled
    }
    process{
        if([string]::IsNullOrEmpty($commitId)){
            try{
                $commitstr = (& git log --format="%H" -n 1)
                if($commitstr -match '\b[0-9a-f]{5,40}\b'){
                    $commitId = $commitstr
                }
            }
            catch{
                # do nothing
            }
        }

        if(![string]::IsNullOrWhiteSpace($commitId)){
            'Updating commitId from [{0}] to [{1}]' -f '$(COMMIT_ID)',$commitId | Write-Verbose

            $folder = $dirToUpdate
            $include = '*.nuspec'
            # In case the script is in the same folder as the files you are replacing add it to the exclude list
            $exclude = "$($MyInvocation.MyCommand.Name);"
            $replacements = @{
                '$(COMMIT_ID)'="$commitId"
            }
            Replace-TextInFolder -folder $folder -include $include -exclude $exclude -replacements $replacements | Write-Verbose
            'Replacement complete' | Write-Verbose
        }
    }
}

<#
.SYNOPSIS 
This will inspect the nuspec file and return the value for the Version element.
#>
function GetExistingVersion{
    [cmdletbinding()]
    param(
        [ValidateScript({test-path $_ -PathType Leaf})]
        $nuspecFile = (Join-Path $scriptDir 'mutant-chicken.nuspec')
    )
    process{
        ([xml](Get-Content $nuspecFile)).package.metadata.version
    }
}

function SetVersion{
    [cmdletbinding()]
    param(
        [Parameter(Position=1,Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$newversion,

        [Parameter(Position=2)]
        [ValidateNotNullOrEmpty()]
        [string]$oldversion = (GetExistingVersion),

        [Parameter(Position=3)]
        [string]$filereplacerVersion = '0.4.0-beta'
    )
    begin{
        EnsureFileReplacerInstlled
    }
    process{
        $folder = $scriptDir
        $include = '*.nuspec;*.ps*1'
        # In case the script is in the same folder as the files you are replacing add it to the exclude list
        $exclude = "$($MyInvocation.MyCommand.Name);"
        $exclude += ';build.ps1'
        $replacements = @{
            "$oldversion"="$newversion"
        }
        Replace-TextInFolder -folder $folder -include $include -exclude $exclude -replacements $replacements | Write-Verbose

        # update the .psd1 file if there is one
        $replacements = @{
            ($oldversion.Replace('-beta','.1'))=($newversion.Replace('-beta','.1'))
        }
        Replace-TextInFolder -folder $folder -include '*.psd1' -exclude $exclude -replacements $replacements | Write-Verbose
        'Replacement complete' | Write-Verbose
    }
}

function FullBuild{
    [cmdletbinding()]
    param()
    process{
        EnsurePsbuildInstlled
        CleanOutputFolder
        InternalEnsure-DirectoryExists -path $outputroot
        Import-NuGetPowershell

        if(-not ($SkipInstallDotNet) ){
            InstallDotNetCli
        }

        RestoreNuGetPackages

        CopyStaticFilesToOutputDir

        BuildSolution
        Update-FilesWithCommitId
        Build-NuGetPackage

        if($publishToNuget){
            (Get-ChildItem -Path ($outputPathNuget) 'mutant-*.nupkg').FullName | PublishNuGetPackage -nugetApiKey $nugetApiKey
        }
    }
}

try{
    if($getversion -eq $true){
        GetExistingVersion
    }
    elseif($setversion -eq $true){
        SetVersion -newversion $newversion
    }
    else{
        FullBuild
    }
}
catch{
    throw ("{0}`r`n{1}" -f $_.Exception,(Get-PSCallStack|format-table|Out-String))
}

