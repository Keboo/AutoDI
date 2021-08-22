
[CmdletBinding()]
Param([Parameter(Mandatory=$true)][string]$Version,
[Parameter()][System.IO.DirectoryInfo]$TargetDir)

.\nuget\UpdateNuspecFiles.ps1 -Version $Version

dotnet pack "AutoDI.sln" /p:AUTODI_VERSION_FULL=$Version /p:Configuration=Debug

if ($TargetDir){
    Move-Item "AutoDI.*.nupkg" $TargetDir -Force
    
    Write-Verbose "Moved nugets to $TargetDir"
}