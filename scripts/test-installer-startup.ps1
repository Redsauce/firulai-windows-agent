$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$candidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe",
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files (x86)\Inno Setup 7\ISCC.exe'
)
$compiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Inno Setup compiler not found' }
$probeDir = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-startup-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeDir | Out-Null
$probeExe = Join-Path $probeDir 'InstallerStartupTests.exe'
$resultFile = Join-Path $probeDir 'result.txt'
$logFile = Join-Path $probeDir 'probe.log'
try {
    & $compiler /Q "/O$probeDir" "$repo\tests\InstallerStartupTests.iss"
    if ($LASTEXITCODE -ne 0) { throw 'Startup probe compilation failed' }
    Start-Process -FilePath $probeExe -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', "/LOG=`"$logFile`"", "/RESULT=`"$resultFile`"") -WindowStyle Hidden -Wait | Out-Null
    # InitializeSetup intentionally returns False; the result file is authoritative.
    $result = Get-Content -LiteralPath $resultFile -Raw
    if ($result -ne 'PASS') { throw $result }
    Write-Host 'PASS: native installer detects local files before app initialization; no installation or service changes.'
}
finally {
    foreach ($file in @($probeExe, $resultFile, $logFile)) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    Remove-Item -LiteralPath $probeDir
}
