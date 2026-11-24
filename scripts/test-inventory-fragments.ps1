param([string]$Export)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path ([IO.Path]::GetTempPath()) ('firulai-fragment-tests-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    $sources = Get-ChildItem -LiteralPath (Join-Path $repo 'src\RsAgent') -Filter '*.cs' | ForEach-Object { $_.FullName }
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:RsAgent.InventoryFragmentTests "/out:$output" `
        "/resource:$repo\src\RsAgent\ApiEndpoint.txt,RsAgent.ApiEndpoint.txt" `
        /r:System.Management.dll /r:System.Net.Http.dll /r:System.ServiceProcess.dll /r:System.Web.Extensions.dll `
        $sources "$repo\tests\InventoryFragmentTests.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Fragment test compilation failed' }
    if ($Export) { & $output --export $Export } else { & $output }
    if ($LASTEXITCODE -ne 0) { throw 'Fragment tests failed' }
}
finally {
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output }
}
