param(
    [string]$GameManaged,
    [string]$InstallTo
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'FusionPower.csproj'
$arguments = @('build', $project, '-c', 'Release', '-v:q')
if ($GameManaged) { $arguments += "-p:GameManaged=$GameManaged" }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'C# build failed.' }

$package = Join-Path $root 'dist/BaiyeFusionPower'
New-Item -ItemType Directory -Force $package | Out-Null
New-Item -ItemType Directory -Force (Join-Path $package 'elem') | Out-Null
Copy-Item (Join-Path $root 'bin/Release/netstandard2.1/BaiyeFusionPower.dll') $package -Force
Copy-Item (Join-Path $root 'mod.yaml') $package -Force
Copy-Item (Join-Path $root 'mod_info.yaml') $package -Force
Copy-Item (Join-Path $root 'elem/elements.yaml') (Join-Path $package 'elem/elements.yaml') -Force
Get-ChildItem (Join-Path $root 'anim/assets') -Directory | ForEach-Object {
    $animationDest = Join-Path $package "anim/assets/$($_.Name)"
    New-Item -ItemType Directory -Force $animationDest | Out-Null
    Copy-Item (Join-Path $_.FullName '*') $animationDest -Force
}

if ($InstallTo) {
    New-Item -ItemType Directory -Force $InstallTo | Out-Null
    Get-ChildItem $package -File | ForEach-Object { Copy-Item $_.FullName $InstallTo -Force }
    $elementDest = Join-Path $InstallTo 'elem'
    New-Item -ItemType Directory -Force $elementDest | Out-Null
    Copy-Item (Join-Path $package 'elem/elements.yaml') $elementDest -Force
    Get-ChildItem (Join-Path $package 'anim/assets') -Directory | ForEach-Object {
        $animationDest = Join-Path $InstallTo "anim/assets/$($_.Name)"
        New-Item -ItemType Directory -Force $animationDest | Out-Null
        Copy-Item (Join-Path $_.FullName '*') $animationDest -Force
    }
    Write-Host "Installed to $InstallTo"
}
Write-Host "Package: $package"
