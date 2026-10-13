param([Parameter(Mandatory = $true)][string]$ResultPath)
$ErrorActionPreference = 'Stop'
$report = [ordered]@{ success = $false }
try {
    $release = Invoke-RestMethod 'https://api.github.com/repos/Redsauce/firulai-windows-agent/releases/latest'
    $expected = $release.tag_name.TrimStart('v')
    $report.expectedVersion = $expected
    $key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1'
    $report.previousVersion = (Get-ItemProperty -LiteralPath $key).DisplayVersion
    if ([version]$expected -le [version]$report.previousVersion) { throw 'A newer published release is required for this test.' }
    $configPath = Join-Path $env:ProgramData 'RSAgent\config.json'
    $configHash = (Get-FileHash -LiteralPath $configPath).Hash
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    if ($config.auto_update -eq $false) { throw 'Automatic updates are disabled in the existing configuration.' }
    $started = [DateTime]::UtcNow
    Restart-Service RSAgent
    $deadline = [DateTime]::UtcNow.AddMinutes(6)
    while ([DateTime]::UtcNow -lt $deadline) {
        $version = (Get-ItemProperty -LiteralPath $key).DisplayVersion
        if ($version -eq $expected -and (Get-Service RSAgent).Status -eq 'Running' -and
            -not (Test-Path -LiteralPath 'HKLM:\SOFTWARE\Redsauce\RSAgent\UpgradeRecovery')) {
            $state = Get-Content -LiteralPath (Join-Path $env:ProgramData 'RSAgent\state.json') -Raw | ConvertFrom-Json
            if ([DateTimeOffset]::Parse($state.last_success_utc).UtcDateTime -gt $started) {
                $report.installedVersion = $version
                $report.configurationPreserved = (Get-FileHash -LiteralPath $configPath).Hash -eq $configHash
                $report.inventoryUploadConfirmed = $true
                $report.downloadedInstaller = Test-Path -LiteralPath (Join-Path $env:ProgramData 'RSAgent\updates\FirulaiAgent.exe')
                if (-not $report.configurationPreserved) { throw 'Configuration changed unexpectedly.' }
                $report.success = $true
                break
            }
        }
        Start-Sleep -Seconds 3
    }
    if (-not $report.success) { throw 'Published update did not complete within six minutes; inspect the agent and installer logs.' }
}
catch { $report.error = $_.Exception.Message }
finally { $report | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8 }
