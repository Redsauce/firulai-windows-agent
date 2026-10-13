param([Parameter(Mandatory = $true)][string]$PrivateKeyPath)
$ErrorActionPreference = 'Stop'
$privatePath = [IO.Path]::GetFullPath($PrivateKeyPath)
$repo = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
if ($privatePath.StartsWith($repo + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The private signing key must be stored outside this repository.'
}
if (Test-Path -LiteralPath $privatePath) { throw 'Refusing to replace an existing signing key.' }
$publicPath = Join-Path $repo 'src\RsAgent\UpdatePublicKey.xml'
if (-not [string]::IsNullOrWhiteSpace([IO.File]::ReadAllText($publicPath))) {
    throw 'A release trust key already exists. Key rotation needs a migration plan.'
}
$provider = New-Object System.Security.Cryptography.CspParameters
$provider.ProviderType = 24
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider(3072, $provider)
$rsa.PersistKeyInCsp = $false
try {
    $parent = Split-Path -Parent $privatePath
    if (-not (Test-Path -LiteralPath $parent)) { throw 'Create a private key directory outside the repository first.' }
    # Establish restricted permissions before writing any private key material.
    $stream = [IO.File]::Open($privatePath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    $stream.Dispose()
    $acl = New-Object System.Security.AccessControl.FileSecurity
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($sid in @([Security.Principal.WindowsIdentity]::GetCurrent().User,
        (New-Object Security.Principal.SecurityIdentifier('S-1-5-18')))) {
        $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($sid, 'FullControl', 'Allow')))
    }
    Set-Acl -LiteralPath $privatePath -AclObject $acl
    [IO.File]::WriteAllText($privatePath, $rsa.ToXmlString($true), (New-Object Text.UTF8Encoding($false)))
    [IO.File]::WriteAllText($publicPath, $rsa.ToXmlString($false), (New-Object Text.UTF8Encoding($false)))
    Write-Host 'Release trust key created. Back up the private key securely; only the public key belongs in the repository.'
}
finally { $rsa.Dispose() }
