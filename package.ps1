$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
$stage = Join-Path $PSScriptRoot 'work/package'
New-Item -ItemType Directory -Force (Join-Path $stage 'assets') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'bin/Spreety.exe') $stage -Force
Copy-Item (Join-Path $PSScriptRoot 'assets/characters.png') (Join-Path $stage 'assets') -Force
Copy-Item (Join-Path $PSScriptRoot 'README.md') $stage -Force
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $dist 'Spreety-v1-win-x64.zip') -Force
Write-Host 'Packaged dist/Spreety-v1-win-x64.zip'
