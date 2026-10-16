param(
    [Parameter(Mandatory = $true)][string]$PackageDirectory,
    [Parameter(Mandatory = $true)][string]$AgentPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Web.Extensions
$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path -LiteralPath $AgentPath).Path)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$keyStream = $assembly.GetManifestResourceStream('RsAgent.UpdatePublicKey.xml')
if ($null -eq $keyStream) { throw 'Built agent has no release verification key.' }
$reader = New-Object IO.StreamReader($keyStream)
try { $publicKey = $reader.ReadToEnd().Trim() } finally { $reader.Dispose() }
$manifestType = $assembly.GetType('RsAgent.UpdateManifest', $true)
$json = [IO.File]::ReadAllBytes((Join-Path $packageRoot 'agent-update.json'))
$signature = [IO.File]::ReadAllText((Join-Path $packageRoot 'agent-update.json.sig'))
$manifest = $manifestType.GetMethod('Verify', $flags).Invoke($null, @($json, $signature, $publicKey))
[string]$installerPath = [IO.Path]::Combine($packageRoot, 'FirulaiAgent.exe')
$manifestType.GetMethod('VerifyPackage', [Reflection.BindingFlags]'Instance,NonPublic').Invoke(
    $manifest, @($installerPath)) | Out-Null
$agentVersion = $assembly.GetType('RsAgent.AgentConfig', $true).GetField('AgentVersion').GetRawConstantValue()
if ($manifest.version -ne $agentVersion) { throw 'Manifest version differs from the built agent.' }
$installer = Get-Item -LiteralPath (Join-Path $packageRoot 'FirulaiAgent.exe')
if ($installer.VersionInfo.ProductVersion.Trim() -ne $agentVersion) { throw 'Installer version differs from the built agent.' }
Write-Host "PASS: actual agent verified release $agentVersion signature, installer hash, size and version."
