param(
    [switch]$ComputeOnly=$false,
    [string]$DevDir)

$NewPath = $DevDir + ";" + (($env:PATH.Split(';') | select -unique | where {-not $_.StartsWith($PSScriptRoot)}) -join ";")
if (!$ComputeOnly) {
  [Environment]::SetEnvironmentVariable("Path", "$NewPath", [System.EnvironmentVariableTarget]::User)
  $env:Path = $NewPath
}
else {
  $x = [string]::Concat('SET "PATH=', $NewPath, '"')
  $Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False
  [System.IO.File]::WriteAllLines("$PSScriptRoot\artifacts\NewPath.bat", $x, $Utf8NoBomEncoding)
}
