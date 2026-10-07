param([string]$AgentPath = "$env:ProgramFiles\RSAgent\RsAgent.exe")
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $AgentPath -PathType Leaf)) { throw "Agent executable not found: $AgentPath" }
$probeDir = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-inventory-limit-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeDir | Out-Null
$copy = Join-Path $probeDir 'RsAgent.exe'
$probe = Join-Path $probeDir 'InstalledInventoryLimitTests.exe'
try {
    Copy-Item -LiteralPath $AgentPath -Destination $copy
    $originalHash = (Get-FileHash -LiteralPath $AgentPath -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -ne $originalHash) { throw 'Binary copy mismatch' }
    Write-Host "Source binary (not recompiled): $AgentPath"
    Write-Host "SHA256: $originalHash"
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe "/out:$probe" /r:System.Web.Extensions.dll "$repo\tests\InstalledInventoryLimitTests.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Probe compilation failed' }
    & $probe $copy
    $probeExit = $LASTEXITCODE
    if ($probeExit -notin @(0, 2)) { throw "Unknown binary or probe error: $probeExit" }
    Write-Host 'No agent source was compiled; no service, configuration, state or RSM data was changed.'
}
finally {
    foreach ($file in @($copy, $probe)) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    Remove-Item -LiteralPath $probeDir
}
