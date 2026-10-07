<#
.SYNOPSIS
    Builds the Creatio Marketplace release archive of the Data Binding kit for Creatio app.

.DESCRIPTION
    1. Checks that the app version (Files/app-descriptor.json) matches the package version (descriptor.json).
    2. Builds the package for .NET Framework (dev-nf) and .NET (dev-n8) as a compile check.
    3. Packs a clean copy of the package into AcdDataBinding.gz with clio. An Assembly package ("Type": 1 in
       descriptor.json) ships its prebuilt DLLs. A General package (no "Type") ships source-code schemas only: the
       installer compiles them, so the prebuilt DLLs and local build files (*.csproj, Directory.Build.*) are left out.
    4. Puts the .gz at the root of release/<App-name>-<X.Y.Z>.zip, as Marketplace requires, and keeps a copy
       as release/AcdDataBinding.gz for direct installs (clio push-pkg).

    Needs the reference binaries in .application/ (see README.md) and clio on PATH.
#>
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$packageName = "AcdDataBinding"
$packageDir = Join-Path $root "packages\$packageName"

$app = Get-Content (Join-Path $packageDir "Files\app-descriptor.json") -Raw | ConvertFrom-Json
$package = (Get-Content (Join-Path $packageDir "descriptor.json") -Raw | ConvertFrom-Json).Descriptor
if ($app.Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "App version '$($app.Version)' must use the X.Y.Z format."
}
if ($app.Version -ne $package.PackageVersion) {
    throw "App version $($app.Version) and package version $($package.PackageVersion) differ."
}

foreach ($configuration in "dev-n8", "dev-nf") {
    dotnet build (Join-Path $packageDir "Files\$packageName.csproj") -c $configuration --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $configuration." }
}

$isAssemblyPackage = $package.Type -eq 1

# Creatio redirects older Terrasoft assembly versions to the installed one, never newer ones. An assembly built
# against a Creatio newer than RequiredPlatformVersion fails to load there, and its web service returns 404.
# Only an Assembly package ships prebuilt DLLs; a General package is compiled by the installer.
$required = [version]$app.RequiredPlatformVersion
$shippedDlls = if ($isAssemblyPackage) {
    @("Files\Bin\$packageName.dll", "Files\Bin\netstandard\$packageName.dll")
} else {
    @()
}
foreach ($dll in $shippedDlls) {
    $dllPath = Join-Path $packageDir $dll
    $references = [Reflection.Assembly]::LoadFile($dllPath).GetReferencedAssemblies() |
        Where-Object { $_.Name -like "Terrasoft.*" -and $_.Name -ne "Terrasoft.ServiceModel.Primitives" }
    foreach ($reference in $references) {
        $referenced = New-Object Version $reference.Version.Major, $reference.Version.Minor, $reference.Version.Build
        if ($referenced -gt $required) {
            throw "$dll references $($reference.Name) $($reference.Version), newer than the minimum Creatio " +
                "version $required. Build against the minimum version's binaries (.application/net-framework-floor)."
        }
    }
}

$releaseDir = Join-Path $root "release"
$stagingDir = Join-Path $releaseDir "staging"
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $stagingDir | Out-Null

$stagedPackage = Join-Path $stagingDir $packageName
Copy-Item $packageDir $stagedPackage -Recurse
Remove-Item (Join-Path $stagedPackage "Files\obj") -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem $stagedPackage -Recurse -Include *.pdb, *.deps.json | Remove-Item -Force
if (-not $isAssemblyPackage) {
    Remove-Item (Join-Path $stagedPackage "Files\Bin") -Recurse -Force -ErrorAction SilentlyContinue
    Get-ChildItem (Join-Path $stagedPackage "Files") -File -Include *.csproj, Directory.Build.* -Recurse |
        Remove-Item -Force
}

$gzPath = Join-Path $stagingDir "$packageName.gz"
clio generate-pkg-zip $stagedPackage -d $gzPath
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $gzPath)) { throw "clio generate-pkg-zip failed." }

$zipName = "$(($app.Name -replace '\s+', '-'))-$($app.Version).zip"
$zipPath = Join-Path $releaseDir $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $gzPath -DestinationPath $zipPath
$gzCopy = Join-Path $releaseDir "$packageName.gz"
Copy-Item $gzPath $gzCopy -Force
Remove-Item $stagingDir -Recurse -Force

Write-Host "Release archive: $zipPath"
Write-Host "Package archive: $gzCopy"
