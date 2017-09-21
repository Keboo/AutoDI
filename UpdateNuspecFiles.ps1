[CmdletBinding()]
Param([Parameter(Mandatory=$true)][Version]$Version)

[Version]$MaxVersion = "{0}.{1}.{2}" -f $Version.Major, ($Version.Minor + 1), ($Version.Build)


foreach($nuspecFile in Get-ChildItem "NuGet\**\*.nuspec") {
    [xml] $file = Get-Content $nuspecFile
    $metadata = $file.package.metadata
    $metadata.copyright = "Copyright " + (Get-Date).Year
    $metadata.version = $Version.ToString(3)

    $autoDiDependency = $metadata.dependencies.dependency | Where-Object { $_.id -eq "AutoDI" } | Select-Object -First 1
    if ($autoDiDependency) {
        $autoDiDependency.version = "[{0},{1})" -f $Version.ToString(3), $MaxVersion.ToString(3)
    }

    $file.Save($nuspecFile)
}