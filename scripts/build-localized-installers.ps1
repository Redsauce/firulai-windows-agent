param(
    [string]$OutputDir,
    [switch]$BuildLocalizedVariants
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$installerScript = Join-Path $repoRoot 'installer\RsAgent.iss'
$outputDir = if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    Join-Path $repoRoot 'Output'
}
else {
    [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDir))
}

$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 7\ISCC.exe',
    'C:\Program Files\Inno Setup 7\ISCC.exe',
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)

$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iscc) {
    throw 'ISCC.exe was not found. Install Inno Setup before building installers.'
}

$installers = @(
    @{ Language = 'english'; Suffix = 'en' },
    @{ Language = 'spanish'; Suffix = 'es' },
    @{ Language = 'catalan'; Suffix = 'ca' },
    @{ Language = 'basque'; Suffix = 'eu' },
    @{ Language = 'galician'; Suffix = 'gl' },
    @{ Language = 'french'; Suffix = 'fr' },
    @{ Language = 'german'; Suffix = 'de' },
    @{ Language = 'italian'; Suffix = 'it' },
    @{ Language = 'japanese'; Suffix = 'ja' },
    @{ Language = 'chinesesimplified'; Suffix = 'zh' }
)

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Get-ChildItem -LiteralPath $outputDir -Filter 'FirulaiAgent*.exe' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

Write-Host 'Building FirulaiAgent.exe (single multilingual build; locale is selected from the downloaded filename suffix)'
& $iscc "/O$outputDir" $installerScript
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($BuildLocalizedVariants) {
    foreach ($installer in $installers) {
        Write-Host ("Building FirulaiAgent-{0}.exe ({1})" -f $installer.Suffix, $installer.Language)
        & $iscc "/O$outputDir" "/DInstallerLanguage=$($installer.Language)" "/DInstallerSuffix=$($installer.Suffix)" $installerScript
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}

Get-ChildItem -LiteralPath $outputDir -Filter 'FirulaiAgent*.exe' -File |
    Sort-Object Name |
    Select-Object Name, Length, LastWriteTime
