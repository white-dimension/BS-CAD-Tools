param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# 仓库根目录 = installer\..\
$ProjectRoot  = Resolve-Path (Join-Path $PSScriptRoot "..")
$ProjectFile  = Join-Path $ProjectRoot "src\BS.CAD.Tools\BS.CAD.Tools.csproj"
$DistRoot     = Join-Path $ProjectRoot "dist"
$BundleRoot   = Join-Path $DistRoot "BS-CAD-Tools.bundle"
$ContentsDir  = Join-Path $BundleRoot "Contents"
$PublishDir   = Join-Path $DistRoot "tmp_publish"

# ── Step 1: Publish ──
Write-Host "=== Step 1: dotnet publish ($Configuration) ===" -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item -Path $PublishDir -Recurse -Force }
dotnet publish $ProjectFile --configuration $Configuration --output $PublishDir
if ($LASTEXITCODE -ne 0) { throw "publish failed ($LASTEXITCODE)" }
Write-Host "  Publish OK" -ForegroundColor Green

# ── Step 2: Create bundle structure ──
Write-Host "`n=== Step 2: Create bundle ===" -ForegroundColor Cyan
if (Test-Path $BundleRoot) { Remove-Item -Path $BundleRoot -Recurse -Force }
New-Item -ItemType Directory -Path $ContentsDir | Out-Null

# ── Step 3: Copy files ──
Write-Host "`n=== Step 3: Copy files ===" -ForegroundColor Cyan

# DLL (仅复制插件本身，AutoCAD 依赖由 AutoCAD 安装提供)
Copy-Item (Join-Path $PublishDir "BS.CAD.Tools.dll") (Join-Path $ContentsDir "BS.CAD.Tools.dll")
Write-Host "  [OK] Contents/BS.CAD.Tools.dll"

# PackageContents.xml for ApplicationBundle autoloading
Copy-Item (Join-Path $PSScriptRoot "PackageContents.xml") (Join-Path $BundleRoot "PackageContents.xml")
Write-Host "  [OK] PackageContents.xml"

# Clean up publish temp
Remove-Item -Path $PublishDir -Recurse -Force

# ── Step 4: Summary ──
$BundleSize = (Get-ChildItem $BundleRoot -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1KB
$DllCount = (Get-ChildItem $ContentsDir -Filter "*.dll").Count

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Bundle created!" -ForegroundColor Green
Write-Host "  Path: $BundleRoot" -ForegroundColor White
Write-Host "  DLLs: $DllCount"
Write-Host "  Size: $($BundleSize.ToString('0.0')) KB"
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n── Next steps ──" -ForegroundColor Yellow
Write-Host "  1. Copy dist/BS-CAD-Tools.bundle to target computer"
Write-Host "  2. Place it under:"
Write-Host "     C:\ProgramData\Autodesk\ApplicationPlugins\"
Write-Host "  3. Restart AutoCAD"
Write-Host "  4. Run ShowPanel or LY"
