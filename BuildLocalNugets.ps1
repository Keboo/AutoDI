
[CmdletBinding()]
Param([Parameter(Mandatory=$true)][string]$Version,
[Parameter()][System.IO.DirectoryInfo]$TargetDir)

msbuild "AutoDI.sln" /p:AUTODI_VERSION_FULL=$Version /p:Configuration=Debug

if (!(Test-Path "nuget.exe")) {
    Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile "nuget.exe"
}

.\nuget\UpdateNuspecFiles.ps1 -Version $Version

.\nuget pack NuGet\AutoDI\AutoDI.nuspec -Version $Version -Symbols
.\nuget pack NuGet\AutoDI.Build\AutoDI.Build.nuspec -Version $Version -Symbols
.\nuget pack NuGet\AutoDI.Generator\AutoDI.Generator.nuspec -Version $Version -Symbols

if ($TargetDir){
    Move-Item "AutoDI.*.nupkg" $TargetDir -Force
    #Move-Item "AutoDI.AspNetCore\bin\Debug\*.nupkg" $TargetDir -Force

    Write-Verbose "Moved nugets to $TargetDir"
}