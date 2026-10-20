param([switch]$Elevated, [string]$ReportPath, [string]$CompilerPath)
$ErrorActionPreference = 'Stop'
$isAdmin = (New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($Elevated -and -not $isAdmin) { throw 'Elevated probe requires an administrator token' }
$report = [ordered]@{ success = $false; elevated = [bool]$Elevated; administrator = $isAdmin }
$repo = Split-Path -Parent $PSScriptRoot
$compiler = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if ($CompilerPath) { $compiler = $CompilerPath }
$probeDir = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-engine-tests-' + [Guid]::NewGuid().ToString('N'))
$appRoot = $probeDir
$dataRoot = $probeDir
$registry = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FirulaiIsolatedRecoveryProbe_is1'
if ($Elevated) {
    $fixtureName = 'FirulaiRecoveryProbe-' + [Guid]::NewGuid().ToString('N')
    $appRoot = Join-Path $env:ProgramFiles $fixtureName
    $dataRoot = Join-Path $env:ProgramData $fixtureName
    $registry = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FirulaiIsolatedRecoveryProbeAdmin_is1'
}
$appDir = Join-Path $appRoot 'app'
$dataDir = Join-Path $dataRoot 'data'
$backupDir = Join-Path $dataRoot 'backup'
$directoryArguments = @("/DATADIR=`"$dataDir`"", "/BACKUPDIR=`"$backupDir`"")
if (Test-Path -LiteralPath $registry) { throw 'An isolated recovery probe is already registered' }
New-Item -ItemType Directory -Path $probeDir | Out-Null
try {
    $compilerArguments = @('/Q', "/O$probeDir")
    if ($Elevated) { $compilerArguments += '/DElevatedProbe' }
    & $compiler @compilerArguments "$repo\tests\InstallerUninstallEngineTests.iss"
    if ($LASTEXITCODE -ne 0) { throw 'Engine probe compilation failed' }
    Start-Process -FilePath (Join-Path $probeDir 'InstallerUninstallEngineTests.exe') `
        -ArgumentList (@('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=`"$appDir`"") + $directoryArguments) -WindowStyle Hidden -Wait | Out-Null
    $uninstaller = Join-Path $appDir 'unins000.exe'
    $result = Join-Path $probeDir 'result.txt'
    # Reinstall the fixture to cover appended Inno logs and refreshed recovery.
    Start-Process -FilePath (Join-Path $probeDir 'InstallerUninstallEngineTests.exe') `
        -ArgumentList (@('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=`"$appDir`"") + $directoryArguments) -WindowStyle Hidden -Wait | Out-Null
    $hashFile = Join-Path $backupDir 'unins000.dat.sha256'
    $savedHash = [IO.File]::ReadAllText($hashFile)
    [IO.File]::WriteAllText($hashFile, 'corrupt-log-digest')
    Start-Process -FilePath $uninstaller -ArgumentList (@('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/RESULT=`"$result`"") + $directoryArguments) `
        -WindowStyle Hidden -Wait | Out-Null
    if ((Get-Content -LiteralPath $result -Raw) -ne 'backup-blocked') { throw 'Untrusted recovery log was accepted' }
    if (-not (Test-Path -LiteralPath (Join-Path $appDir 'RsAgent.exe'))) { throw 'Backup failure removed original agent' }
    [IO.File]::WriteAllText($hashFile, $savedHash)
    Start-Process -FilePath $uninstaller -ArgumentList (@('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/FAIL=1', "/LOG=`"$probeDir\uninstall.log`"", "/RESULT=`"$result`"") + $directoryArguments) `
        -WindowStyle Hidden -Wait | Out-Null
    if ((Get-Content -LiteralPath $result -Raw) -ne '1') { throw 'Actual Inno partial removal did not restore retry essentials' }
    foreach ($name in @('RsAgent.exe', 'unins000.exe', 'unins000.dat')) {
        if (-not (Test-Path -LiteralPath (Join-Path $appDir $name))) { throw "Inno removed restored retry file: $name" }
    }
    if ((Get-ItemProperty -LiteralPath $registry).UninstallString -ne ('"' + $uninstaller + '"')) { throw 'Retry Apps entry missing or incorrect' }
    if ((Get-Content -LiteralPath (Join-Path $dataDir 'config.json') -Raw) -ne 'isolated-token') { throw 'Recovered configuration differs' }
    if ($Elevated) {
        $report.configOwners = @()
        foreach ($directory in @($dataDir, $backupDir)) {
            if (-not (Get-Acl -LiteralPath $directory).AreAccessRulesProtected) { throw 'Private directory inherits unrelated permissions' }
        }
        foreach ($privateFile in @((Join-Path $dataDir 'config.json'), (Join-Path $backupDir 'config.json'))) {
            $privateAcl = Get-Acl -LiteralPath $privateFile
            $owner = $privateAcl.GetOwner([Security.Principal.SecurityIdentifier]).Value
            $report.configOwners += $owner
            if ($owner -notin @('S-1-5-18', 'S-1-5-32-544')) { throw "Private retry configuration has an unrelated owner: $owner" }
            foreach ($rule in $privateAcl.Access) {
                $sid = $rule.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value
                if ($rule.AccessControlType -eq 'Allow' -and $sid -notin @('S-1-5-18', 'S-1-5-32-544')) {
                    throw "Private retry configuration grants access to unrelated SID: $sid"
                }
            }
        }
        $report.privateAclsVerified = $true
    }
    Start-Process -FilePath $uninstaller -ArgumentList (@('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/RESULT=`"$result`"") + $directoryArguments) `
        -WindowStyle Hidden -Wait | Out-Null
    if ((Get-Content -LiteralPath $result -Raw) -ne '0') { throw 'Actual Inno retry failed' }
    if ((Test-Path -LiteralPath $dataDir) -or (Test-Path -LiteralPath $backupDir)) { throw 'Retry retained private data' }
    $report.success = $true
    $report.installationRoot = $appRoot
    $report.dataRoot = $dataRoot
    $report.registry = $registry
    Write-Host 'PASS: actual isolated Inno install/update/uninstall, corrupt backup rejection, locked agent failure, restored Apps entry/uninstaller/log, and successful retry; no RSAgent service touched.'
}
catch {
    $report.error = $_.Exception.Message
    if (Test-Path -LiteralPath (Join-Path $probeDir 'uninstall.log')) {
        Get-Content -LiteralPath (Join-Path $probeDir 'uninstall.log') | Select-Object -Last 35 | Write-Host
    }
    throw
}
finally {
    $resolvedProbe = [IO.Path]::GetFullPath($probeDir)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedProbe.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe probe cleanup path' }
    Remove-Item -LiteralPath $resolvedProbe -Recurse -Force
    if (Test-Path -LiteralPath $registry) { Remove-Item -LiteralPath $registry -Recurse -Force }
    if ($Elevated) {
        foreach ($target in @(@{ Path = $appRoot; Parent = $env:ProgramFiles }, @{ Path = $dataRoot; Parent = $env:ProgramData })) {
            $resolvedTarget = [IO.Path]::GetFullPath($target.Path)
            if ([IO.Path]::GetDirectoryName($resolvedTarget) -ne [IO.Path]::GetFullPath($target.Parent) -or
                [IO.Path]::GetFileName($resolvedTarget) -notmatch '^FirulaiRecoveryProbe-[a-f0-9]{32}$') { throw 'Unsafe elevated probe cleanup path' }
            if (Test-Path -LiteralPath $resolvedTarget) { Remove-Item -LiteralPath $resolvedTarget -Recurse -Force }
        }
    }
    $report.cleanedUp = $true
    if ($ReportPath) { $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
}
