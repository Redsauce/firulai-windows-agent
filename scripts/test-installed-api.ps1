param([string]$AgentPath = "$env:ProgramFiles\RSAgent\RsAgent.exe")
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $AgentPath)) { throw "Agent executable not found: $AgentPath" }
$probeDir = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-installed-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeDir | Out-Null
$copy = Join-Path $probeDir 'RsAgent.exe'
$probe = Join-Path $probeDir 'InstalledApiTests.exe'
try {
    Copy-Item -LiteralPath $AgentPath -Destination $copy
    $originalHash = (Get-FileHash -LiteralPath $AgentPath -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -ne $originalHash) { throw 'Binary copy mismatch' }
    Write-Host "Source: $AgentPath"
    Write-Host "SHA256: $originalHash"
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe "/out:$probe" /r:System.Net.Http.dll "$repo\tests\InstalledApiTests.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Probe compilation failed' }
    & $probe $copy
    if ($LASTEXITCODE -ne 0) { throw 'Installed API tests failed' }
}
finally {
    foreach ($file in @($copy, $probe)) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    Remove-Item -LiteralPath $probeDir
}
