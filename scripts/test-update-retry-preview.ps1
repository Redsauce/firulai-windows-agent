$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$testScript = Join-Path $PSScriptRoot 'test-upgrade.ps1'
$resultDirectory = Join-Path $repo 'Output\prueba-actualizacion'
New-Item -ItemType Directory -Force -Path $resultDirectory | Out-Null
$resultFile = Join-Path $resultDirectory ('simulacion-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.txt')

Write-Host 'PRUEBA AISLADA: no instala nada ni reinicia tu servicio.' -ForegroundColor Cyan
Write-Host 'Simula fallos de red, recuperacion, suspension y reintentos.'
Write-Host 'Usa un reloj simulado y un temporizador real acelerado; no hay que esperar cinco minutos.'

# Run in a separate process so compilation/test failures cannot be mistaken for success.
$previousPreference = $ErrorActionPreference
try {
    # The simulated failures deliberately emit WARN lines on stderr in Windows PowerShell.
    $ErrorActionPreference = 'Continue'
    $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $testScript 2>&1
    $testExitCode = $LASTEXITCODE
}
finally { $ErrorActionPreference = $previousPreference }
$output | Out-File -LiteralPath $resultFile -Encoding utf8
if ($testExitCode -ne 0) {
    Write-Host "La prueba ha fallado. Registro: $resultFile" -ForegroundColor Red
    $output | ForEach-Object { Write-Host $_ }
    exit 1
}
$text = $output -join "`n"
foreach ($required in @(
    'PASS: offline startup, DNS failure, download timeout, online recovery, cooldown and concurrent checks.',
    'PASS: hourly polling, 5/10/20/60-minute retries, host jitter, inventory timer preservation and stopped service.'
)) {
    if (-not $text.Contains($required)) { throw "Falta una comprobacion esperada. Consulta $resultFile" }
}
Write-Host ''
Write-Host 'SIMULACION CORRECTA' -ForegroundColor Green
Write-Host '- Consulta cada hora y reintenta tras fallos a los 5, 10, 20 y 60 minutos.'
Write-Host '- Los fallos repetidos mantienen reintentos cada hora, con variacion por equipo.'
Write-Host '- Al volver la conexion se permite lanzar la actualizacion.'
Write-Host '- Se conserva el horario del inventario y no se duplican instaladores.'
Write-Host '- Reanudar el equipo recupera la comprobacion pendiente.'
Write-Host "Registro de la simulacion: $resultFile"
Write-Host ''
$registryPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1'
$installedVersion = (Get-ItemProperty -LiteralPath $registryPath -ErrorAction SilentlyContinue).DisplayVersion
Write-Host "Tu agente instalado sigue en: $installedVersion"
Write-Host 'Esta prueba valida el codigo; no demuestra un arranque real sin red del servicio instalado.'
