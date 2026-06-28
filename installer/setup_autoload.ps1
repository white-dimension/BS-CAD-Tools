$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$projectDir = Join-Path $repoRoot "src\BS.CAD.Tools"
$dllSourceDir = Join-Path $projectDir "bin\Debug\net10.0-windows"
$dllSource = Join-Path $dllSourceDir "BS.CAD.Tools.dll"
$pdbSource = Join-Path $dllSourceDir "BS.CAD.Tools.pdb"

$bundleRoot = "C:\ProgramData\Autodesk\ApplicationPlugins\BS-CAD-Tools.bundle"
$contentsDir = Join-Path $bundleRoot "Contents"
$manifestDest = Join-Path $bundleRoot "PackageContents.xml"
$dllDest = Join-Path $contentsDir "BS.CAD.Tools.dll"
$pdbDest = Join-Path $contentsDir "BS.CAD.Tools.pdb"

$manifestSource = Join-Path $PSScriptRoot "auto_load.xml"

if (-not (Test-Path $dllSource)) {
    throw "Build output not found: $dllSource. Please run: dotnet build src\BS.CAD.Tools\BS.CAD.Tools.csproj"
}

New-Item -ItemType Directory -Path $bundleRoot -Force | Out-Null
New-Item -ItemType Directory -Path $contentsDir -Force | Out-Null

Copy-Item -Path $manifestSource -Destination $manifestDest -Force
Copy-Item -Path $dllSource -Destination $dllDest -Force
if (Test-Path $pdbSource) {
    Copy-Item -Path $pdbSource -Destination $pdbDest -Force
}

Write-Host "[OK] BS-CAD-Tools manifest installed to $manifestDest"
Write-Host "[OK] BS-CAD-Tools dll installed to $dllDest"
Write-Host "Restart AutoCAD, or NETLOAD: $dllDest"
