[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'

function Test-CurrentProcessIsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$identity
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-CurrentProcessIsAdmin)) {
    $argumentList = @(
        '-NoExit',
        '-NoProfile',
        '-ExecutionPolicy',
        'Bypass',
        '-File',
        "`"$PSCommandPath`""
    )
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argumentList
    exit
}

$logPath = Join-Path $env:TEMP 'firulai-rsagent-force-uninstall.log'
try {
    Start-Transcript -Path $logPath -Force | Out-Null
}
catch {
    Write-Warning "Could not start transcript: $($_.Exception.Message)"
}

$installDir = 'C:\Program Files\RSAgent'
$dataDir = 'C:\ProgramData\RSAgent'
$allowedDeleteRoots = @($installDir, $dataDir) | ForEach-Object {
    [System.IO.Path]::GetFullPath($_).TrimEnd('\')
}

function Assert-AllowedDeleteRoot {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')
    if ($allowedDeleteRoots -notcontains $fullPath) {
        throw "Refusing recursive delete outside RSAgent directories: $Path"
    }
}

function Enable-PendingDeleteOnReboot {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not ('Firulai.NativeMethods' -as [type])) {
        Add-Type -Namespace Firulai -Name NativeMethods -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
public static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);
'@
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Get-ChildItem -LiteralPath $Path -Force -Recurse -ErrorAction SilentlyContinue |
        Sort-Object { $_.FullName.Length } -Descending |
        ForEach-Object {
            [Firulai.NativeMethods]::MoveFileEx($_.FullName, $null, 4) | Out-Null
        }

    [Firulai.NativeMethods]::MoveFileEx($Path, $null, 4) | Out-Null
}

function Grant-RSAgentDeletePermissions {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    takeown.exe /F $Path /A /R /D Y | Out-Null
    icacls.exe $Path /inheritance:e /grant:r '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-18:(OI)(CI)F' /T /C | Out-Null

    Get-ChildItem -LiteralPath $Path -Force -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            try {
                $_.Attributes = 'Normal'
            }
            catch {
                Write-Warning "Could not reset attributes for $($_.FullName): $($_.Exception.Message)"
            }
        }
}

function Remove-RSAgentDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)

    Assert-AllowedDeleteRoot -Path $Path

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    Grant-RSAgentDeletePermissions -Path $Path

    try {
        Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
        return $false
    }
    catch {
        Write-Warning "Could not delete $Path now: $($_.Exception.Message)"
        Enable-PendingDeleteOnReboot -Path $Path
        return $true
    }
}

Write-Host '== Closing RSAgent processes and broken uninstallers =='
Stop-Service -Name 'RSAgent' -Force -ErrorAction SilentlyContinue

Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object {
        ($_.Name -ieq 'RsAgent.exe') -or
        ($_.Name -ieq '_unins.tmp') -or
        ($_.CommandLine -and $_.CommandLine.IndexOf('unins000', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -or
        ($_.ExecutablePath -and $_.ExecutablePath.StartsWith($installDir, [System.StringComparison]::OrdinalIgnoreCase))
    } |
    ForEach-Object {
        try {
            Stop-Process -Id $_.ProcessId -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not stop process $($_.ProcessId): $($_.Exception.Message)"
        }
    }

Start-Sleep -Seconds 2

Write-Host '== Removing service =='
sc.exe delete RSAgent | Out-Null

Write-Host '== Removing files =='
$needsReboot = $false
$needsReboot = (Remove-RSAgentDirectory -Path $installDir) -or $needsReboot
$needsReboot = (Remove-RSAgentDirectory -Path $dataDir) -or $needsReboot

Write-Host '== Removing registry entries =='
$registryKeys = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1',
    'HKLM:\SOFTWARE\Redsauce\RSAgent',
    'HKLM:\SOFTWARE\WOW6432Node\Redsauce\RSAgent'
)

foreach ($registryKey in $registryKeys) {
    Remove-Item -LiteralPath $registryKey -Recurse -Force -ErrorAction SilentlyContinue
}

foreach ($registryRoot in @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
)) {
    Get-ChildItem -LiteralPath $registryRoot -ErrorAction SilentlyContinue |
        Where-Object {
            $properties = Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue
            $properties.DisplayName -eq 'Firulai Inventory Agent'
        } |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host '== Check =='
sc.exe query RSAgent
Write-Host "Program Files exists: $(Test-Path -LiteralPath $installDir)"
Write-Host "ProgramData exists: $(Test-Path -LiteralPath $dataDir)"
Write-Host "Log: $logPath"

if ($needsReboot) {
    Write-Warning 'Some files were locked. They have been scheduled for deletion on reboot. Restart Windows before installing again.'
    try { Stop-Transcript | Out-Null } catch {}
    exit 3010
}

Write-Host 'RSAgent local cleanup completed.'
try { Stop-Transcript | Out-Null } catch {}
