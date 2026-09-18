param([switch]$Live)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-api-tests-' + [Guid]::NewGuid().ToString('N') + '.exe')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
try {
    & $compiler /nologo /target:exe "/out:$output" /r:System.Net.Http.dll `
        "/resource:$repo\src\RsAgent\ApiEndpoint.txt,RsAgent.ApiEndpoint.txt" `
        "$repo\src\RsAgent\ApiEndpoint.cs" "$repo\tests\ApiEndpointTests.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
    if ($Live) { & $output --live } else { & $output }
    if ($LASTEXITCODE -ne 0) { throw 'API redirect tests failed' }
}
finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
