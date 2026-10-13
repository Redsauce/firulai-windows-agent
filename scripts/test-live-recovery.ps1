param([Parameter(Mandatory = $true)][string]$ResultPath)
$ErrorActionPreference = 'Stop'
$report = [ordered]@{ success = $false }
$binary = 'C:\Program Files\RSAgent\RsAgent.exe'
$original = $binary + '.pre-update'
$helper = $null
$prepared = $false
function Run-Helper([string[]]$Arguments) {
    $process = Start-Process -FilePath $helper -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(180000)) { throw 'Recovery helper timed out.' }
    if ($process.ExitCode -ne 0) { throw ('Recovery helper failed: ' + $process.ExitCode) }
}
try {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Administrator required.' }
    if ((Test-Path -LiteralPath 'HKLM:\SOFTWARE\Redsauce\RSAgent\UpgradeRecovery') -or (Test-Path -LiteralPath $original)) {
        throw 'An existing recovery journal or backup must be resolved before this test.'
    }
    $version = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1').DisplayVersion
    $binaryHash = (Get-FileHash -LiteralPath $binary).Hash
    $config = Join-Path $env:ProgramData 'RSAgent\config.json'
    $configHash = (Get-FileHash -LiteralPath $config).Hash
    $probeDir = Join-Path $env:ProgramData ('RSAgent\recovery-probe-' + [Guid]::NewGuid().ToString('N'))
    $acl = New-Object Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($sid in @('S-1-5-18', 'S-1-5-32-544')) {
        $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier($sid)), 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
    }
    [IO.Directory]::CreateDirectory($probeDir, $acl) | Out-Null
    $helper = Join-Path $probeDir 'recovery-probe.exe'
    Copy-Item -LiteralPath $binary -Destination $helper
    Copy-Item -LiteralPath $binary -Destination $original
    $prepared = $true
    Run-Helper @('--prepare-upgrade', '"C:\Program Files\RSAgent"', $version)
    $report.startupTaskRegistered = $null -ne (Get-ScheduledTask -TaskName 'FirulaiAgentUpgradeRecovery' -ErrorAction Stop)
    if ((Get-Service RSAgent).Status -ne 'Stopped') { throw 'Service did not stop.' }
    # Simulate a failed replacement. Both the independent helper and original are intact.
    for ($attempt = 0; ; $attempt++) {
        try { [IO.File]::WriteAllText($binary, 'invalid executable fixture'); break }
        catch { if ($attempt -ge 30) { throw }; Start-Sleep -Seconds 1 }
    }
    Run-Helper @('--recover-upgrade')
    $report.originalBinaryRestored = (Get-FileHash -LiteralPath $binary).Hash -eq $binaryHash
    $report.configurationPreserved = (Get-FileHash -LiteralPath $config).Hash -eq $configHash
    $report.serviceRunning = (Get-Service RSAgent).Status -eq 'Running'
    $report.journalCleared = -not (Test-Path -LiteralPath 'HKLM:\SOFTWARE\Redsauce\RSAgent\UpgradeRecovery')
    $report.startupTaskRemoved = $null -eq (Get-ScheduledTask -TaskName 'FirulaiAgentUpgradeRecovery' -ErrorAction SilentlyContinue)
    if (-not $report.originalBinaryRestored -or -not $report.configurationPreserved -or -not $report.serviceRunning -or -not $report.journalCleared -or -not $report.startupTaskRemoved) {
        throw 'Recovery verification failed.'
    }
    Remove-Item -LiteralPath $original
    $report.success = $true
}
catch {
    $report.error = $_.Exception.Message
    # A second independent fallback ensures a failed probe does not leave the service broken.
    if ($prepared -and (Test-Path -LiteralPath $original)) {
        try {
            Stop-Service RSAgent -ErrorAction SilentlyContinue
            (Get-Service RSAgent).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(90))
            Copy-Item -LiteralPath $original -Destination $binary -Force
            Start-Service RSAgent
            Run-Helper @('--complete-upgrade')
            $report.fallbackServiceRestored = $true
        }
        catch { $report.fallbackError = $_.Exception.Message }
    }
}
finally { $report | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8 }
