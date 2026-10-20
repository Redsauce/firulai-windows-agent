$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$compiler = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Inno compiler missing' }
$probeDir = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-recovery-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeDir | Out-Null
try {
    & $compiler /Q "/O$probeDir" "$repo\tests\InstallerUninstallRecoveryTests.iss"
    if ($LASTEXITCODE -ne 0) { throw 'Recovery probe compilation failed' }
    $resultFile = Join-Path $probeDir 'result.txt'
    Start-Process -FilePath (Join-Path $probeDir 'InstallerUninstallRecoveryTests.exe') `
        -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', "/FIXTURE=`"$probeDir\fixture`"", "/RESULT=`"$resultFile`"") `
        -WindowStyle Hidden -Wait | Out-Null
    $result = Get-Content -LiteralPath $resultFile -Raw
    if ($result -ne 'PASS') { throw $result }
    Write-Host 'PASS: native partial app removal, locked data file, restored credentials/uninstaller, and cleanup retry; no real service or installation touched.'
}
finally {
    $resolvedProbe = [IO.Path]::GetFullPath($probeDir)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedProbe.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe probe cleanup path' }
    Remove-Item -LiteralPath $resolvedProbe -Recurse -Force
}
