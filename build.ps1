param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$framework = Split-Path -Parent $compiler
$wpf = Join-Path $framework 'WPF'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
if (-not (Test-Path (Join-Path $PSScriptRoot 'logo.ico'))) { & (Join-Path $PSScriptRoot 'MakeIcon.ps1') }
$exe = Join-Path $OutputDirectory 'JingDll.exe'
$references = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Xaml.dll') | ForEach-Object { '/r:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/r:' + (Join-Path $wpf $_) }
$sources = @('Core.cs','App.cs','SelfTest.cs','AssemblyInfo.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$exe" "/win32icon:$PSScriptRoot\logo.ico" "/win32manifest:$PSScriptRoot\app.manifest" "/resource:$PSScriptRoot\Main.xaml,Main.xaml" "/resource:$PSScriptRoot\logo.ico,logo.ico" "/resource:$PSScriptRoot\catalog.tsv,catalog.tsv" @references @sources
if ($LASTEXITCODE -ne 0) { throw '应用编译失败' }
$setup = Join-Path $OutputDirectory 'JingDll-Setup.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$setup" "/win32icon:$PSScriptRoot\logo.ico" "/win32manifest:$PSScriptRoot\app.manifest" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll "/resource:$exe,JingDll.exe" (Join-Path $PSScriptRoot 'Installer.cs')
if ($LASTEXITCODE -ne 0) { throw '安装器编译失败' }
Write-Host "生成完成：$exe 和 $setup"
