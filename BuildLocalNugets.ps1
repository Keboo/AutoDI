
[CmdletBinding()]
Param([Parameter(Mandatory=$true)][string]$Version)


if (!(Test-Path "nuget.exe")) {
    Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile "nuget.exe"
}

.\nuget pack Nuget\AutoDI\AutoDI.nuspec -Version $Version
.\nuget pack Nuget\AutoDI.Fody\AutoDI.Fody.nuspec -Version $Version
.\nuget pack Nuget\AutoDI.Container.Fody\AutoDI.Container.Fody.nuspec -Version $Version