#
# Find all the csproj files and determine the versions of SolidCI.Azure used.
# Export the powershell folder from that location.
#
$nugetPackageLocation=Resolve-Path "~"
$nugetPackageLocation=Join-Path $nugetPackageLocation ".nuget"
$nugetPackageLocation=Join-Path $nugetPackageLocation "packages"

if(-not (Test-Path $nugetPackageLocation))
{
    throw "Cannot locate nuget packages @ $nugetPackageLocation"
}

$solidCiVersion=$null
Get-ChildItem -Path $PSScriptRoot -Name "*.csproj" -Recurse | ForEach-Object {
    $fileName=[System.IO.Path]::Combine($PSScriptRoot, $_)
    $packageReference=Select-Xml -Path $fileName -XPath "/Project/ItemGroup/PackageReference[@Include='SolidCI.Azure']"
    if($packageReference) {
        $solidCiVersion = $packageReference[0].Node.Attributes.GetNamedItem("Version").Value
    }
}

$solidCiNugetPackageLocation=Join-Path $nugetPackageLocation "solidci.azure"
$solidCiNugetPackageLocation=Join-Path $solidCiNugetPackageLocation $solidCiVersion

if(-not (Test-Path $solidCiNugetPackageLocation))
{
    throw "Cannot locate rpc nuget package @ $solidCiNugetPackageLocation. Missing a nuget restore?"
}

$solidCiPowershellLocation=Join-Path $solidCiNugetPackageLocation "content"
$solidCiPowershellLocation=Join-Path $solidCiPowershellLocation "Content" 
$solidCiPowershellLocation=Join-Path $solidCiPowershellLocation "Powershell"

Write-Host "Using $solidCiPowershellLocation as location for rpcPowershellScripts variable"
Write-Output ("##vso[task.setvariable variable=rpcPowershellScripts;]$solidCiPowershellLocation")
