param([switch]$Live)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$candidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe",
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files (x86)\Inno Setup 7\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 7\ISCC.exe'
)
$compiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Inno Setup compiler not found' }
$probeDir = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-installer-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeDir | Out-Null
$probeExe = Join-Path $probeDir 'ApiEndpointTests.exe'
$resultFile = Join-Path $probeDir 'result.txt'
$logFile = Join-Path $probeDir 'probe.log'
try {
    & $compiler /Q "/O$probeDir" "$repo\tests\InstallerEndpointTests.iss"
    if ($LASTEXITCODE -ne 0) { throw 'Probe compilation failed' }
    $liveFlag = if ($Live) { '1' } else { '0' }
    $process = Start-Process -FilePath $probeExe -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', "/LIVE=$liveFlag", "/LOG=`"$logFile`"", "/RESULT=`"$resultFile`"") -WindowStyle Hidden -Wait -PassThru
    # InitializeSetup returns False intentionally, so inspect the test result.
    $result = Get-Content -LiteralPath $resultFile -Raw
    if ($Live) { Get-Content -LiteralPath $logFile | Select-String 'base for Registry' | ForEach-Object { Write-Host $_.Line } }
    if ($result -ne 'PASS') { throw $result }
    Write-Host 'PASS: native installer URL construction and redirect validation; no installation performed.'
}
finally {
    foreach ($file in @($probeExe, $resultFile, $logFile)) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    Remove-Item -LiteralPath $probeDir
}
