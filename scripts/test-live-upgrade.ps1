param(
    [Parameter(Mandatory = $true)][string]$InstallerPath,
    [Parameter(Mandatory = $true)][string]$ExpectedBinaryPath,
    [string]$ExpectedVersion = '0.20.2',
    [switch]$UseAutomaticWorker,
    [Parameter(Mandatory = $true)][string]$ResultPath
)
$ErrorActionPreference = 'Stop'
$report = [ordered]@{ success = $false; expectedVersion = $ExpectedVersion }
$backupDir = $null
try {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this test as administrator.' }
    $appDir = 'C:\Program Files\RSAgent'
    $dataDir = Join-Path $env:ProgramData 'RSAgent'
    $registryPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1'
    $configPath = Join-Path $dataDir 'config.json'
    $oldVersion = (Get-ItemProperty -LiteralPath $registryPath).DisplayVersion
    if ([version]$ExpectedVersion -le [version]$oldVersion) { throw 'This live test requires a strictly newer installer.' }
    $configHash = (Get-FileHash -LiteralPath $configPath).Hash
    $baseUrl = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Redsauce\RSAgent' -Name ApiBaseUrl -ErrorAction SilentlyContinue).ApiBaseUrl
    $backupDir = Join-Path $dataDir ('upgrade-test-backup-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $backupDir | Out-Null
    $acl = New-Object Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($sid in @('S-1-5-18', 'S-1-5-32-544')) {
        $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier($sid)), 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
    }
    Set-Acl -LiteralPath $backupDir -AclObject $acl
    foreach ($name in @('RsAgent.exe', 'unins000.exe', 'unins000.dat')) {
        $path = Join-Path $appDir $name
        if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $backupDir $name) }
    }
    Copy-Item -LiteralPath $configPath -Destination (Join-Path $backupDir 'config.json')
    if (Test-Path -LiteralPath (Join-Path $dataDir 'state.json')) {
        Copy-Item -LiteralPath (Join-Path $dataDir 'state.json') -Destination (Join-Path $backupDir 'state.json')
    }
    $report.backupDirectory = $backupDir
    $report.previousVersion = $oldVersion
    $installerLog = Join-Path $backupDir 'installer.log'
    $startedAt = [DateTime]::UtcNow
    if ($UseAutomaticWorker) {
        $packageDir = Split-Path -Parent ([IO.Path]::GetFullPath($InstallerPath))
        $stage = Join-Path $dataDir 'updates'
        New-Item -ItemType Directory -Force -Path $stage | Out-Null
        Set-Acl -LiteralPath $stage -AclObject $acl
        foreach ($name in @('FirulaiAgent.exe', 'agent-update.json', 'agent-update.json.sig')) {
            Copy-Item -LiteralPath (Join-Path $packageDir $name) -Destination (Join-Path $stage $name) -Force
        }
        $worker = Join-Path $stage 'update-worker.exe'
        Copy-Item -LiteralPath (Join-Path $appDir 'RsAgent.exe') -Destination $worker -Force
        $report.automaticWorker = $true
        $process = Start-Process -FilePath $worker -ArgumentList '--apply-update' -WindowStyle Hidden -PassThru
    }
    else {
        $process = Start-Process -FilePath ([IO.Path]::GetFullPath($InstallerPath)) -ArgumentList @(
            '/SP-', '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/ACCEPTLICENSE=yes', "/LOG=`"$installerLog`""
        ) -WindowStyle Hidden -PassThru
    }
    if (-not $process.WaitForExit(10 * 60 * 1000)) { throw 'Installer timed out; check its log and recovery journal before retrying.' }
    $report.installerExitCode = $process.ExitCode
    if ($process.ExitCode -ne 0) { throw ('Installer failed: ' + $process.ExitCode) }
    $service = Get-Service RSAgent
    $service.WaitForStatus('Running', [TimeSpan]::FromSeconds(60))
    $report.serviceRunning = $true
    $report.configurationPreserved = (Get-FileHash -LiteralPath $configPath).Hash -eq $configHash
    $report.binaryMatchesBuild = (Get-FileHash -LiteralPath (Join-Path $appDir 'RsAgent.exe')).Hash -eq (Get-FileHash -LiteralPath $ExpectedBinaryPath).Hash
    $report.installedVersion = (Get-ItemProperty -LiteralPath $registryPath).DisplayVersion
    $report.apiBasePreserved = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Redsauce\RSAgent' -Name ApiBaseUrl -ErrorAction SilentlyContinue).ApiBaseUrl -eq $baseUrl
    $report.recoveryJournalCleared = -not (Test-Path -LiteralPath 'HKLM:\SOFTWARE\Redsauce\RSAgent\UpgradeRecovery')
    if (-not $report.configurationPreserved -or -not $report.binaryMatchesBuild -or $report.installedVersion -ne $ExpectedVersion -or -not $report.apiBasePreserved -or -not $report.recoveryJournalCleared) {
        throw 'Upgrade verification failed. The complete original installation backup is retained.'
    }
    $inventoryPath = Join-Path $dataDir 'inventory.json'
    $deadline = [DateTime]::UtcNow.AddMinutes(3)
    $report.newInventoryGenerated = $false
    while ([DateTime]::UtcNow -lt $deadline) {
        if ((Test-Path -LiteralPath $inventoryPath) -and (Get-Item -LiteralPath $inventoryPath).LastWriteTimeUtc -gt $startedAt) {
            try {
                $inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
                if ($inventory.system.agent_version -eq $ExpectedVersion) { $report.newInventoryGenerated = $true; break }
            }
            catch { }
        }
        Start-Sleep -Seconds 2
    }
    $report.inventoryUploadConfirmed = $false
    while ($report.newInventoryGenerated -and [DateTime]::UtcNow -lt $deadline) {
        try {
            $agentState = Get-Content -LiteralPath (Join-Path $dataDir 'state.json') -Raw | ConvertFrom-Json
            if ([DateTimeOffset]::Parse($agentState.last_success_utc).UtcDateTime -ge $startedAt) {
                $report.inventoryUploadConfirmed = $true
                break
            }
        }
        catch { }
        Start-Sleep -Seconds 2
    }
    # Local package/service verification and remote inventory eligibility are separate results.
    $report.success = $true
}
catch { $report.error = $_.Exception.Message }
finally {
    $report | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $ResultPath -Encoding UTF8
}
