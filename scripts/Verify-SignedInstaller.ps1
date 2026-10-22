[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Path,
    [Parameter(Mandatory = $true)][string]$ExpectedPublisher,
    [string]$ExpectedSha256,
    [switch]$InstallTest,
    [string]$TestUuid,
    [string]$TestToken
)
$ErrorActionPreference = 'Stop'

function Assert-Signature([string]$File, [string]$Publisher) {
    $signature = Get-AuthenticodeSignature -LiteralPath $File
    if ($signature.Status -ne 'Valid') { throw "Invalid Authenticode signature on $File`: $($signature.Status)" }
    $names = [regex]::Matches($signature.SignerCertificate.Subject, '(?:^|,\s*)(?:CN|O)=(?:"([^"]+)"|([^,]+))') |
        ForEach-Object { if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value.Trim() } }
    if ($Publisher -notin $names) {
        throw "Unexpected publisher on $File`: $($signature.SignerCertificate.Subject)"
    }
    if ($null -eq $signature.TimeStamperCertificate) { throw "Missing timestamp on $File" }
}

$installer = (Resolve-Path -LiteralPath $Path).Path
Assert-Signature $installer $ExpectedPublisher
if ($ExpectedSha256 -and (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash -ne $ExpectedSha256) {
    throw 'Installer SHA-256 does not match.'
}
if ($InstallTest) {
    if (-not $TestUuid -or -not $TestToken) { throw 'Installation test needs TestUuid and TestToken.' }
    $installDir = Join-Path $env:TEMP ('firulai-sign-verify-' + [guid]::NewGuid().ToString('N'))
    $installLog = Join-Path $env:TEMP ('firulai-install-' + [guid]::NewGuid().ToString('N') + '.log')
    $arguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', '/ACCEPTLICENSE=yes',
        "/UUID=$TestUuid", "/TOKEN=$TestToken", "/DIR=`"$installDir`"", "/LOG=`"$installLog`"")
    $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Silent installation failed: $($process.ExitCode). Log: $installLog" }
    $uninstaller = Join-Path $installDir 'unins000.exe'
    if (-not (Test-Path -LiteralPath $uninstaller)) { throw 'Uninstaller was not created.' }
    Assert-Signature $uninstaller $ExpectedPublisher
    $agent = Join-Path $installDir 'RsAgent.exe'
    if (-not (Test-Path -LiteralPath $agent)) { throw 'Agent was not installed.' }
    Assert-Signature $agent $ExpectedPublisher
    $uninstallLog = Join-Path $env:TEMP ('firulai-uninstall-' + [guid]::NewGuid().ToString('N') + '.log')
    $process = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$uninstallLog`"") -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw "Silent uninstallation failed: $($process.ExitCode). Log: $uninstallLog" }
    if (Test-Path -LiteralPath (Join-Path $installDir 'RsAgent.exe')) { throw 'Agent remains after uninstallation.' }
}
Write-Host 'Signed installer verification passed.'
