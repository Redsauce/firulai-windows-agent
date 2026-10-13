param(
    [Parameter(Mandatory = $true)][string]$PrivateKeyPath,
    [Parameter(Mandatory = $true)][string]$InstallerPath,
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$OutputDir
)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a stable three-component version.' }
$parsedVersion = [version]$Version
$repo = Split-Path -Parent $PSScriptRoot
$installer = Get-Item -LiteralPath $InstallerPath
if ($installer.Length -le 0 -or $installer.Length -gt 100MB) { throw 'Invalid installer size.' }
if ($installer.VersionInfo.ProductVersion.Trim() -ne $Version) { throw 'Installer product version does not match the manifest.' }
$provider = New-Object Security.Cryptography.CspParameters
$provider.ProviderType = 24
$rsa = New-Object Security.Cryptography.RSACryptoServiceProvider($provider)
$rsa.PersistKeyInCsp = $false
try {
    $rsa.FromXmlString([IO.File]::ReadAllText([IO.Path]::GetFullPath($PrivateKeyPath)))
    if ($rsa.KeySize -lt 3072) { throw 'Signing key must be at least 3072 bits.' }
    if ($rsa.ToXmlString($false) -ne [IO.File]::ReadAllText((Join-Path $repo 'src\RsAgent\UpdatePublicKey.xml')).Trim()) {
        throw 'Signing key does not match the public key embedded in the agent source.'
    }
    $manifest = [ordered]@{
        version = $Version
        url = "https://github.com/Redsauce/firulai-windows-agent/releases/download/$Version/FirulaiAgent.exe"
        sha256 = (Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        size = $installer.Length
    }
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes(($manifest | ConvertTo-Json -Compress))
    $signature = $rsa.SignData($bytes, [Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'))
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
    [IO.File]::WriteAllBytes((Join-Path ([IO.Path]::GetFullPath($OutputDir)) 'agent-update.json'), $bytes)
    [IO.File]::WriteAllText((Join-Path ([IO.Path]::GetFullPath($OutputDir)) 'agent-update.json.sig'), [Convert]::ToBase64String($signature))
    $checksumLines = foreach ($asset in @($installer.FullName, (Join-Path ([IO.Path]::GetFullPath($OutputDir)) 'agent-update.json'), (Join-Path ([IO.Path]::GetFullPath($OutputDir)) 'agent-update.json.sig'))) {
        (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($asset)
    }
    [IO.File]::WriteAllLines((Join-Path ([IO.Path]::GetFullPath($OutputDir)) 'RELEASE_CHECKSUMS.txt'), $checksumLines)
    Write-Host 'Signed manifest generated locally. Publish it and its signature with the exact installer that was hashed.'
}
finally { $rsa.Dispose() }
