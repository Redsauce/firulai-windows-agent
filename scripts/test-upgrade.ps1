$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-upgrade-tests-' + [Guid]::NewGuid().ToString('N') + '.exe')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
try {
    $sources = Get-ChildItem -LiteralPath (Join-Path $repo 'src\RsAgent') -Filter '*.cs' | ForEach-Object { $_.FullName }
    & $compiler /nologo /target:exe /main:RsAgent.UpgradeTests "/out:$output" `
        "/resource:$repo\src\RsAgent\ApiEndpoint.txt,RsAgent.ApiEndpoint.txt" `
        /r:System.Management.dll /r:System.Net.Http.dll /r:System.ServiceProcess.dll /r:System.Web.Extensions.dll `
        $sources "$repo\tests\UpgradeTests.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Upgrade test compilation failed' }
    & $output
    if ($LASTEXITCODE -ne 0) { throw 'Upgrade tests failed' }
}
finally {
    Remove-Item -LiteralPath $output -ErrorAction SilentlyContinue
}
