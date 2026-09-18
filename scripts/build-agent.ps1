$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$sourceDir = Join-Path $repo 'src\RsAgent'
$outputDir = Join-Path $sourceDir 'bin\Release'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$sources = Get-ChildItem -LiteralPath $sourceDir -Filter '*.cs' | ForEach-Object { $_.FullName }
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /optimize+ `
    "/out:$outputDir\RsAgent.exe" "/resource:$sourceDir\ApiEndpoint.txt,RsAgent.ApiEndpoint.txt" `
    /r:System.Management.dll /r:System.Net.Http.dll /r:System.ServiceProcess.dll /r:System.Web.Extensions.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Agent compilation failed' }
Write-Host "Agent generated: $outputDir\RsAgent.exe"
