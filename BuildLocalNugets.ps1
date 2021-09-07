
[CmdletBinding()]
Param([Parameter(Mandatory=$true)][string]$Version,
[Parameter()][System.IO.DirectoryInfo]$TargetDir)

.\nuget\UpdateNuspecFiles.ps1 -Version $Version

dotnet pack "AutoDI.sln" -p:Version=$Version -p:Configuration=Debug --output ./

if ($TargetDir){
    Move-Item "AutoDI.*.nupkg" $TargetDir -Force
    
    Write-Verbose "Moved nugets to $TargetDir"
}