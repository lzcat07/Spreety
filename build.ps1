$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path $compiler)) { throw 'Enable .NET Framework 4.8 on Windows before building.' }
New-Item -ItemType Directory -Force (Join-Path $root 'bin') | Out-Null
$refs = @('System.dll','System.Core.dll','System.Runtime.Serialization.dll','System.Windows.Forms.dll','System.Drawing.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ ('/win32manifest:' + (Join-Path $root 'app.manifest')) ('/out:' + (Join-Path $root 'bin/Spreety.exe')) @refs (Join-Path $root 'src/Spreety.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
New-Item -ItemType Directory -Force (Join-Path $root 'bin/assets') | Out-Null
Copy-Item (Join-Path $root 'assets/*') (Join-Path $root 'bin/assets') -Recurse -Force
Write-Host 'Built bin/Spreety.exe'

