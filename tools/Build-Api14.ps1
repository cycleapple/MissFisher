param(
    [Parameter(Mandatory=$true)][string]$DalamudHome,
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$work = Join-Path $root '.api14/work'
$artifacts = Join-Path $root '.api14/artifacts'
New-Item -ItemType Directory -Force $work,$artifacts | Out-Null
$archive = Join-Path $work 'official.zip'
$url = 'https://raw.githubusercontent.com/QianChangUwU/MissFisher/b7686d625a2835e0caeaa6df6eceb6d1d2f4f799/latest.zip'
$expected = 'FE9A8CEC811818A4AFF8B9A167459A0025EA4F458811E36A74B435437A12903B'
if (!(Test-Path $archive)) { Invoke-WebRequest $url -OutFile $archive }
if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne $expected) { throw 'Official archive SHA256 mismatch.' }
$inputFolder = Join-Path $work 'official'
$package = Join-Path $work 'package'
Expand-Archive $archive $inputFolder -Force
New-Item -ItemType Directory -Force $package | Out-Null
Copy-Item (Join-Path $inputFolder '*') $package -Recurse -Force
$env:DALAMUD_HOME = (Resolve-Path $DalamudHome).Path
& $Dotnet run --project (Join-Path $PSScriptRoot 'MissFisherPatcher/MissFisherPatcher.csproj') -c Release -- (Join-Path $inputFolder 'MissFisher.dll') (Join-Path $package 'MissFisher.dll') (Join-Path $root 'lang-api14.json') '1.9.17.2'
if ($LASTEXITCODE -ne 0) { throw 'API14 localization/verification failed.' }
$manifestPath = Join-Path $package 'MissFisher.json'
$manifest = Get-Content $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$manifest.AssemblyVersion = '1.9.17.2'
$manifest.Name = 'MissFisher 繁體中文'
$manifest.Description = '釣魚、海釣及相關任務工具。官方 API14 版本的繁中資源移植；繁中服實機驗證尚待完成。'
$manifest.Punchline = 'MissFisher API14 繁體中文預備移植版'
$manifest.RepoUrl = 'https://github.com/cycleapple/MissFisher/tree/api14-tw'
$manifest | ConvertTo-Json -Depth 20 | Set-Content $manifestPath -Encoding UTF8
$verification = Join-Path $package 'MissFisher.dll.verification.json'
Copy-Item $verification (Join-Path $artifacts 'verification.json') -Force
Remove-Item -LiteralPath $verification
$zip = Join-Path $artifacts 'MissFisher-1.9.17.2.zip'
Compress-Archive (Join-Path $package '*') $zip -Force
Get-FileHash $zip -Algorithm SHA256
Write-Host 'API14 package built. TC runtime validation is still pending; this does not publish a feed entry.'
