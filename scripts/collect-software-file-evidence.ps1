<#
Read-only diagnostic. Reads registry/file metadata and writes one local JSON.
Does not execute inspected programs, upload data, or change inventory/service state.
PackagePattern discovers installation roots; explicit InstallRoot is also supported.
No product aliases or vulnerability decisions are made by this script.
#>
[CmdletBinding()]
param(
    [string]$PackagePattern,
    [string[]]$InstallRoot = @(),
    [string]$OriginalFilename,
    [string]$ProductVersion,
    [string[]]$RegistryContextPath = @(),
    [string[]]$RegistryContextValue = @(),
    [ValidateRange(1, 300)][int]$MaxSeconds = 60,
    [ValidateRange(1, 1000000)][int]$MaxFiles = 100000,
    [Parameter(Mandatory = $true)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
if (-not $PackagePattern -and -not $InstallRoot.Count) {
    throw 'Specify PackagePattern or InstallRoot.'
}
$watch = [Diagnostics.Stopwatch]::StartNew()
$packages = New-Object 'Collections.Generic.List[object]'
$files = New-Object 'Collections.Generic.List[object]'
$problems = New-Object 'Collections.Generic.List[object]'
$roots = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($root in $InstallRoot) { [void]$roots.Add([IO.Path]::GetFullPath($root)) }
if ($PackagePattern) {
    foreach ($base in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
                         'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
                         'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall')) {
        foreach ($key in @(Get-ChildItem -LiteralPath $base -ErrorAction SilentlyContinue)) {
            try {
                $entry = Get-ItemProperty -LiteralPath $key.PSPath
                if ($entry.DisplayName -notlike $PackagePattern) { continue }
                $packages.Add([ordered]@{name=$entry.DisplayName; version=$entry.DisplayVersion;
                    publisher=$entry.Publisher; registry_path=$key.Name; install_location=$entry.InstallLocation})
                $root = [string]$entry.InstallLocation
                if (-not $root) {
                    $icon = [string]$entry.DisplayIcon
                    if ($icon -match '^"([^\"]+\.exe)"|^([^\"]+\.exe)(?:,|$)') {
                        $executable = if ($Matches[1]) { $Matches[1] } else { $Matches[2] }
                        $root = [IO.Path]::GetDirectoryName($executable)
                    }
                }
                if ($root) { [void]$roots.Add([IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($root.Trim('"')))) }
            } catch { $problems.Add([ordered]@{path=$key.Name; reason='registry_read_failed'}) }
        }
    }
}
$pending = New-Object 'Collections.Generic.Stack[string]'
foreach ($root in $roots) { $pending.Push($root) }
$visited = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$examined = 0
$limitReached = $false
while ($pending.Count) {
    if ($watch.Elapsed.TotalSeconds -ge $MaxSeconds -or $examined -ge $MaxFiles) { $limitReached=$true; break }
    $directory = $pending.Pop()
    if (-not $visited.Add($directory)) { continue }
    try {
        $item = Get-Item -LiteralPath $directory
        if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { continue }
        foreach ($child in Get-ChildItem -LiteralPath $directory -Force) {
            if ($child.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
            if ($child.PSIsContainer) { $pending.Push($child.FullName); continue }
            if ($child.Extension -notin @('.dll','.exe')) { continue }
            if ($watch.Elapsed.TotalSeconds -ge $MaxSeconds -or $examined -ge $MaxFiles) { $limitReached=$true; break }
            $examined++
            try {
                $info = [Diagnostics.FileVersionInfo]::GetVersionInfo($child.FullName)
                if ($OriginalFilename -and $info.OriginalFilename -ine $OriginalFilename -and $child.Name -ine $OriginalFilename) { continue }
                if ($ProductVersion -and $info.ProductVersion -ne $ProductVersion -and $info.FileVersion -ne $ProductVersion) { continue }
                $files.Add([ordered]@{path=$child.FullName; original_filename=$info.OriginalFilename;
                    product_name=$info.ProductName; company_name=$info.CompanyName; file_description=$info.FileDescription;
                    product_version=$info.ProductVersion; file_version=$info.FileVersion;
                    version_source=$(if ([string]::IsNullOrWhiteSpace($info.ProductVersion)) {'FileVersion'} else {'ProductVersion'});
                    size=$child.Length; modified_utc=$child.LastWriteTimeUtc.ToString('o')})
            } catch { $problems.Add([ordered]@{path=$child.FullName; reason='metadata_read_failed'}) }
        }
    } catch { $problems.Add([ordered]@{path=$directory; reason='directory_read_failed'}) }
}
$context = @()
foreach ($path in $RegistryContextPath) {
    try {
        $key = Get-ItemProperty -LiteralPath $path
        $values = [ordered]@{}
        foreach ($name in $RegistryContextValue) {
            if ($key.PSObject.Properties[$name]) { $values[$name] = $key.$name }
        }
        $context += [ordered]@{path=$path; values=$values}
    } catch { $problems.Add([ordered]@{path=$path; reason='registry_context_unavailable'}) }
}
$result = [ordered]@{mode='local_read_only'; computer=$env:COMPUTERNAME; captured_at=[DateTime]::UtcNow.ToString('o');
    package_pattern=$PackagePattern; target_original_filename=$OriginalFilename; target_version=$ProductVersion;
    roots=@($roots); packages=@($packages.ToArray()); files=@($files.ToArray()); registry_context=$context;
    files_examined=$examined; limit_reached=$limitReached; problems=@($problems.ToArray());
    scan_complete=($roots.Count -gt 0 -and -not $limitReached -and $problems.Count -eq 0)}
$result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Host "Evidence saved: $OutputPath; matched files: $($files.Count); complete: $($result.scan_complete)"
