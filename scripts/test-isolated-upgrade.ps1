param([Parameter(Mandatory = $true)][string]$InstallerPath)
$ErrorActionPreference = 'Stop'
# Installs a real service. Never run on a developer or customer machine.
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'This test requires a disposable GitHub-hosted runner.'
}
$repo = Split-Path -Parent $PSScriptRoot
$appDir = Join-Path $env:ProgramFiles 'firulaiAgent'
$dataDir = Join-Path $env:ProgramData 'firulaiAgent'
$uninstallKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1'
$agentKey = 'HKLM:\SOFTWARE\Redsauce\firulaiAgent'
$journal = Join-Path $agentKey 'UpgradeRecovery'
foreach ($path in @($appDir, $dataDir, $uninstallKey, $agentKey)) {
    if (Test-Path -LiteralPath $path) { throw "Existing installation must not be touched: $path" }
}
if (Get-Service firulaiAgent -ErrorAction SilentlyContinue) { throw 'An agent service already exists.' }
$installer = (Resolve-Path -LiteralPath $InstallerPath).Path
$logs = Join-Path $repo 'upgrade-acceptance-logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$child = $null
$setup = $null
$binary = Join-Path $appDir 'FirulaiAgent.exe'
$config = Join-Path $dataDir 'config.json'
$agentLog = Join-Path $dataDir 'logs\rs_agent.log'
function Start-TestInstaller([string]$Name) {
    Start-Process -FilePath $installer -ArgumentList @('/SP-', '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/ACCEPTLICENSE=yes', "/LOG=`"$logs\$Name.log`"") -WindowStyle Hidden -PassThru
}
try {
    New-Item -ItemType Directory -Path $appDir, $dataDir | Out-Null
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    & $compiler /nologo /target:exe "/out:$binary" /r:System.ServiceProcess.dll "$repo\tests\UpgradeServiceFixture.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Fixture compilation failed.' }
    '{"uuid":"550e8400-e29b-41d4-a716-446655440000","token":"offline-fixture-only","locale":"en","auto_update":false}' |
        Set-Content -LiteralPath $config -Encoding UTF8
    # Future local success suppresses inventory collection. Pin any event to
    # loopback as an independent safeguard against production RSM requests.
    @{ last_success_utc = [DateTime]::UtcNow.AddDays(1).ToString('o') } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $dataDir 'state.json') -Encoding UTF8
    New-Item -Path $agentKey -Force | Out-Null
    New-ItemProperty -LiteralPath $agentKey -Name ApiBaseUrl -Value 'https://127.0.0.1:9/' | Out-Null
    New-Item -Path $uninstallKey -Force | Out-Null
    New-ItemProperty -LiteralPath $uninstallKey -Name DisplayVersion -Value '0.20.19' | Out-Null
    New-ItemProperty -LiteralPath $uninstallKey -Name InstallLocation -Value $appDir | Out-Null
    & sc.exe create firulaiAgent binPath= "`"$binary`"" start= demand
    if ($LASTEXITCODE -ne 0) { throw 'Fixture service creation failed.' }
    Start-Service firulaiAgent
    $oldHash = (Get-FileHash -LiteralPath $binary).Hash
    $configHash = (Get-FileHash -LiteralPath $config).Hash
    $start = New-Object Diagnostics.ProcessStartInfo($binary, '--hold-image')
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $child = [Diagnostics.Process]::Start($start)
    $ready = $child.StandardOutput.ReadLineAsync()
    if (-not $ready.Wait(10000) -or $ready.Result -ne 'ready') { throw 'Blocking process did not start.' }

    $setup = Start-TestInstaller 'blocked'
    if (-not $setup.WaitForExit(240000)) { throw 'Blocked preparation exceeded its bounded wait.' }
    if ($setup.ExitCode -ne 7) { throw "Expected preparation failure 7, got $($setup.ExitCode)." }
    if ($child.HasExited) { throw 'Installer terminated the manual process.' }
    if ((Get-FileHash -LiteralPath $binary).Hash -ne $oldHash) { throw 'Blocked preparation changed the executable.' }
    if ((Get-Service firulaiAgent).Status -ne 'Running') { throw 'Blocked preparation left the service stopped.' }
    if (Test-Path -LiteralPath $journal) { throw 'Blocked preparation left recovery pending.' }
    if ((Get-FileHash -LiteralPath $config).Hash -ne $configHash) { throw 'Blocked preparation changed configuration.' }
    Write-Host 'PASS: persistent blocker preserves old executable, service, configuration and manual process.'

    Clear-Content -LiteralPath $agentLog
    $setup = Start-TestInstaller 'success'
    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    $observed = $false
    while ([DateTime]::UtcNow -lt $deadline -and -not $setup.HasExited) {
        if ((Get-Content -LiteralPath $agentLog -Raw) -match "installed executable still running, PID=$($child.Id)") {
            $observed = $true
            break
        }
        Start-Sleep -Milliseconds 250
    }
    if (-not $observed) { throw 'Installer did not observe the manual blocker.' }
    $child.StandardInput.WriteLine()
    if (-not $child.WaitForExit(10000)) { throw 'Manual process did not finish.' }
    if (-not $setup.WaitForExit(120000) -or $setup.ExitCode -ne 0) { throw "Upgrade failed: $($setup.ExitCode)" }
    $source = Get-Content -LiteralPath "$repo\src\RsAgent\AgentConfig.cs" -Raw
    if ($source -notmatch 'AgentVersion = "([0-9.]+)"') { throw 'Version missing from source.' }
    $expected = $Matches[1]
    if ((Get-Item -LiteralPath $binary).VersionInfo.FileVersion -ne $expected -or
        (Get-ItemProperty -LiteralPath $uninstallKey).DisplayVersion -ne $expected) { throw 'Installed version mismatch.' }
    if ((Get-AuthenticodeSignature -LiteralPath $binary).Status -ne 'Valid') { throw 'Installed agent signature invalid.' }
    if ((Get-Service firulaiAgent).Status -ne 'Running') { throw 'Updated service is not running.' }
    if (Test-Path -LiteralPath $journal) { throw 'Successful upgrade left recovery pending.' }
    if ((Get-FileHash -LiteralPath $config).Hash -ne $configHash) { throw 'Successful upgrade changed configuration.' }
    Write-Host "PASS: signed installer upgraded to $expected after release; service and configuration preserved."
}
finally {
    if ($child -and -not $child.HasExited) { $child.StandardInput.WriteLine(); $child.WaitForExit(10000) | Out-Null }
    if ($setup -and -not $setup.HasExited) { $setup.WaitForExit(240000) | Out-Null }
    if (Test-Path -LiteralPath $agentLog) { Copy-Item -LiteralPath $agentLog -Destination (Join-Path $logs 'agent.log') }
    if (Get-Service firulaiAgent -ErrorAction SilentlyContinue) { Stop-Service firulaiAgent -ErrorAction Continue }
    # The GitHub-hosted VM is discarded. Leave files and registry intact for
    # failure analysis instead of recursively removing installation paths.
}
