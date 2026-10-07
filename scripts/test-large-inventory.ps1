$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path ([IO.Path]::GetTempPath()) ('rsagent-large-inventory-' + [Guid]::NewGuid().ToString('N') + '.exe')
$sources = @('RsmClient.cs', 'InventoryCollector.cs', 'AgentConfig.cs', 'AgentText.cs', 'AgentState.cs', 'SystemEligibility.cs') |
    ForEach-Object { Join-Path "$repo\src\RsAgent" $_ }
try {
    & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:RsAgent.LargeInventoryTests "/out:$output" `
        /r:System.Management.dll /r:System.Net.Http.dll /r:System.ServiceProcess.dll /r:System.Web.Extensions.dll `
        $sources "$repo\tests\LargeInventoryTests.cs"
    if ($LASTEXITCODE -ne 0) { throw 'Large inventory test compilation failed' }
    & $output
    if ($LASTEXITCODE -ne 0) { throw 'Large inventory tests failed' }
}
finally {
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output }
}
