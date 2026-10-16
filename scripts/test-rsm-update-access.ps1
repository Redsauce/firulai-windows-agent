param([string]$AgentPath = (Join-Path $PSScriptRoot '..\src\RsAgent\bin\Release\RsAgent.exe'))
$ErrorActionPreference = 'Stop'
# Requires permission to read the existing config.json (normally administrator).
# Performs read-only RSM queries. Does not install, restart or send inventory.
Add-Type -AssemblyName System.Web.Extensions
$assembly = [Reflection.Assembly]::LoadFile((Resolve-Path -LiteralPath $AgentPath).Path)
$config = $assembly.GetType('RsAgent.AgentConfig', $true).GetMethod('Load').Invoke($null, @())
$client = $assembly.GetType('RsAgent.RsmClient', $true)
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$task = $client.GetMethod('FetchAgentVersionsAsync', $flags).Invoke($null, @($config, $null, $null))
$response = $task.GetAwaiter().GetResult()
$selection = $assembly.GetType('RsAgent.AgentReleaseSelection', $true)
$tag = $selection.GetMethod('ReadWindowsVersion', $flags).Invoke($null, @($response))
$task = $client.GetMethod('ValidateSystemUuidExistsAsync').Invoke($null, @($config, $false, $null))
$task.GetAwaiter().GetResult() | Out-Null
Write-Host "PASS: existing agent token can read Windows version $tag and validate this covered, active system."
