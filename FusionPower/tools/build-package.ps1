param(
    [string]$GameManaged,
    [string]$InstallTo
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'FusionPower.csproj'
$arguments = @('build', $project, '-c', 'Release', '-v:q', '-clp:ErrorsOnly')
if ($GameManaged) { $arguments += "-p:GameManaged=$GameManaged" }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'C# build failed.' }

$package = Join-Path $root 'dist/BaiyeFusionPower'
New-Item -ItemType Directory -Force $package | Out-Null
New-Item -ItemType Directory -Force (Join-Path $package 'elem') | Out-Null
Copy-Item (Join-Path $root 'bin/Release/netstandard2.1/BaiyeFusionPower.dll') $package -Force
Copy-Item (Join-Path $root 'mod.yaml') $package -Force
Copy-Item (Join-Path $root 'mod_info.yaml') $package -Force
Copy-Item -LiteralPath (Join-Path $root '../README.md') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $root '../CHANGELOG.md') -Destination $package -Force
Copy-Item (Join-Path $root 'elem/elements.yaml') (Join-Path $package 'elem/elements.yaml') -Force
Get-ChildItem (Join-Path $root 'anim/assets') -Directory | ForEach-Object {
    $animationDest = Join-Path $package "anim/assets/$($_.Name)"
    New-Item -ItemType Directory -Force $animationDest | Out-Null
    Copy-Item (Join-Path $_.FullName '*') $animationDest -Force
}

$dlls=Get-ChildItem -LiteralPath $package -Recurse -Filter '*.dll'
if($dlls.Count -ne 1 -or $dlls[0].Name -ne 'BaiyeFusionPower.dll'){throw 'Unexpected dependency in package'}
$versionLine=Get-Content -LiteralPath (Join-Path $root 'mod_info.yaml') | Where-Object {$_ -match '^version:'} | Select-Object -First 1
$version=($versionLine -replace '^version:\s*','').Trim()
if($version -notmatch '^\d+\.\d+\.\d+$'){throw 'Invalid package version'}
Compress-Archive -LiteralPath $package -DestinationPath (Join-Path $root "dist/BaiyeFusionPower-$version.zip") -Force
if ($InstallTo) {
    if(Get-Process -Name OxygenNotIncluded -ErrorAction SilentlyContinue){throw 'Exit Oxygen Not Included before installation'}
    if(Test-Path -LiteralPath $InstallTo){
        $backupRoot=Join-Path $root 'dist/install-backups'
        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
        $backup=Join-Path $backupRoot ((Split-Path -Leaf $InstallTo)+'-'+(Get-Date -Format yyyyMMdd-HHmmss))
        Copy-Item -LiteralPath $InstallTo -Destination $backup -Recurse
        Write-Output "Backup $backup"
    }
    New-Item -ItemType Directory -Path $InstallTo -Force | Out-Null
    Get-ChildItem -LiteralPath $package | ForEach-Object{Copy-Item -LiteralPath $_.FullName -Destination $InstallTo -Recurse -Force}
    $prefix=$package.TrimEnd([char[]]'\/')+[IO.Path]::DirectorySeparatorChar
    foreach($file in Get-ChildItem -LiteralPath $package -Recurse -File){
        $relative=$file.FullName.Substring($prefix.Length)
        if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $InstallTo $relative)).Hash){throw "Installation hash mismatch: $relative"}
    }
    Write-Output "Installed $InstallTo"
}
Write-Host "Package: $package"
