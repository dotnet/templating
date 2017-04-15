param(
    [switch]$ComputeOnly=$false,
    [string]$DevDir)

$NewPath = $DevDir + ";" + (($env:PATH.Split(';') | where {-not $_.StartsWith($PSScriptRoot)}) -join ";")
if (!$ComputeOnly) {
  [Environment]::SetEnvironmentVariable("Path", "$NewPath", [System.EnvironmentVariableTarget]::User)
  $env:Path = $NewPath
}
else {
  write-host $NewPath
}
