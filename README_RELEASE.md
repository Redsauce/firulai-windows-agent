# RSAgent Windows - Mantenimiento y publicación

Documento interno para generar, publicar y actualizar el instalador Windows.

---

## Qué se sube al repositorio

Subir código fuente y scripts:

```text
agent/windows/README.md
agent/windows/README_RELEASE.md
agent/windows/RsAgent.sln
agent/windows/src/RsAgent/*.cs
agent/windows/src/RsAgent/RsAgent.csproj
agent/windows/installer/RsAgent.iss
```

No subir artefactos generados:

```text
agent/windows/Output/
agent/windows/src/RsAgent/bin/
agent/windows/src/RsAgent/obj/
```

Estos directorios estan ignorados en `.gitignore`.

---

## Qué se publica para descarga

El usuario final no descarga el repo. Descarga el instalador publicado como asset de GitHub Releases:

```text
FirulaiAgent.exe
```

Firulai no descarga directamente este asset desde el navegador. La aplicacion sirve el mismo binario desde el endpoint autenticado:

```text
/api/agents/windows/installer
```

Ese endpoint lee el idioma guardado en las preferencias del App user (`preferences.locale`) y cambia solo el nombre de descarga, por ejemplo `FirulaiAgent-ca.exe` o `FirulaiAgent-en.exe`. El instalador detecta ese sufijo al arrancar y selecciona el idioma antes de mostrar el asistente. No se generan instaladores distintos por idioma.

Enlace estable para la aplicación:

```text
https://github.com/OWNER/REPO/releases/latest/download/FirulaiAgent.exe
```

Enlace a una versión concreta:

```text
https://github.com/OWNER/REPO/releases/download/v0.14.0/FirulaiAgent.exe
```

---

## Generar el instalador

Desde `agent/windows`:

```powershell
cd agent\windows
```

Compilar el agente:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /target:exe `
  /out:.\src\RsAgent\bin\Release\RsAgent.exe `
  /r:System.Management.dll `
  /r:System.Net.Http.dll `
  /r:System.ServiceProcess.dll `
  /r:System.Web.Extensions.dll `
  .\src\RsAgent\*.cs
```

Generar el instalador. Inno Setup puede estar instalado para todos los usuarios o solo para el usuario actual:

```powershell
$iscc = @(
  "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe"
  "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
  "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $iscc) {
  throw "No se ha encontrado ISCC.exe. Instala Inno Setup antes de continuar."
}

& $iscc .\installer\RsAgent.iss
```

Resultado:

```text
agent/windows/Output/FirulaiAgent.exe
```

El instalador contiene el ejecutable compilado en `src/RsAgent/bin/Release/RsAgent.exe`. Si cambian el código C# o el script `installer/RsAgent.iss`, hay que repetir los dos pasos: compilar el agente y después generar el instalador. Para desplegar la descarga desde Firulai, publica este `Output/FirulaiAgent.exe` como asset de la release Windows.

---

## Validaciones previas de instalación

El instalador Windows replica el criterio del instalador Linux antes de crear archivos:

1. Valida el formato del UUID y que el token obligatorio no esté vacío.
2. Comprueba si ya existe una instalación local.
3. Valida en Firulai que el UUID existe y está disponible usando el token introducido por el usuario.
4. Envía `changeSystemStatus` con el UUID y `action=activate`; el receptor localiza el System y aplica el estado activo mediante su configuración interna.

La instalación local se considera existente si aparece cualquiera de estas señales:

- Servicio Windows `RSAgent`.
- Ejecutable `RsAgent.exe` en la ruta de instalación.
- Desinstalador `unins000.exe`.
- Configuración local `C:\ProgramData\RSAgent\config.json`.

Si existe instalación local, el instalador cancela el proceso y pide desinstalar primero el agente actual. No reinstala encima, igual que `install.sh`.

En modo gráfico, el acuerdo de licencia aparece antes de la página de UUID y token, y el botón para continuar permanece deshabilitado hasta marcar su aceptación. En modo silencioso, el instalador requiere `/ACCEPTLICENSE=yes /UUID=<UUID> /TOKEN=<TOKEN>`; si falta la aceptación explícita o alguna credencial, termina con error y no debe crear ni reemplazar archivos.

La validación remota envía `RStrigger=validateSystemInstallation` al endpoint del
agente. `RSdata` contiene `uuid`, `hostname`, `fqdn` y `RStoken`; ningún ID de
propiedad RSM forma parte del instalador. El receptor devuelve `available`,
`same_system`, `different_system` o `not_found` y mantiene internamente el
mapping de propiedades.

La validación es informativa y no bloquea la instalación. Si el UUID no existe,
pertenece a otro equipo o la validación falla, el script receptor debe preparar
la notificación al usuario y el instalador continúa con normalidad.

Orden equivalente al instalador Linux:

1. Validar permisos de Administrador mediante UAC.
2. Validar UUID y token obligatorios.
3. Bloquear si ya existe instalación local.
4. Validar UUID en Firulai usando el token introducido.
5. Enviar `changeSystemStatus` con UUID y `action=activate` para que el receptor active el System.
6. Crear directorios y configuración.
7. Instalar ejecutable y registrar una única ejecución automática mediante el servicio `RSAgent`.

---

## Desinstalación y estado inactivo en Firulai

El desinstalador de Windows no borra datos de Firulai. En su lugar, usa el propio `RsAgent.exe` con el modo interno `--mark-disconnected-on-uninstall`.

La notificación envía `RStrigger=changeSystemStatus` al endpoint configurado del
agente. `RSdata` contiene únicamente el UUID y el receptor localiza el System y
aplica el estado desconectado mediante su mapping interno.

Si Events Handler acepta el evento con HTTP 2xx pero no reenvía la salida del
script, el cuerpo vacío se considera aceptado y la desinstalación continúa.

Flujo interno:

1. `unins000.exe` muestra un aviso en modo gráfico.
2. Se detiene el servicio `RSAgent`.
3. `RsAgent.exe --mark-disconnected-on-uninstall` carga `C:\ProgramData\RSAgent\config.json`.
4. Envía una petición autenticada con `RStrigger=changeSystemStatus` y `action=disconnect`.
5. El cuerpo semántico enviado es:

```json
{
  "uuid": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "action": "disconnect"
}
```

6. Si Firulai responde correctamente y el UUID existe, el desinstalador marca el System como `Disconnected`, informa de que los datos no se borrarán y elimina el servicio, `C:\Program Files\RSAgent\` y `C:\ProgramData\RSAgent\`.
7. Si Firulai responde correctamente pero el UUID ya no existe, el desinstalador informa de que no hay ningún System enlazado en Firulai y continúa con la desinstalación local.

El aviso de desinstalación indica que solo se borrarán archivos locales junto al instalador, que no se borrarán datos de Firulai, y que el sistema quedará inactivo en Firulai. Desde Firulai se podrán eliminar definitivamente sus datos o volver a instalar el agente más adelante enlazándolo al System y al inventario ya guardados. Si Firulai no confirma la actualización de estado, el desinstalador cancela el proceso para que se pueda reintentar.

El aviso de permisos de Windows puede seguir mostrando `unins000.exe` porque es el ejecutable generado por Inno Setup y se muestra antes de que el desinstalador pueda ejecutar código propio. Para hacerlo más reconocible, el instalador define el nombre visible como `Firulai Inventory Agent` y la información de versión como agente de inventario para Firulai. Para que Windows muestre un publicador verificado en UAC será necesario firmar el instalador/desinstalador.

---

## Recuperación de ejecuciones y eventos de Windows

La planificación sigue dentro del servicio `RSAgent`; no se crea una tarea en el Programador de tareas. El agente no despierta el equipo. Si Windows estaba suspendido a las 03:00, `OnPowerEvent` detecta la reanudación y compara la hora prevista con `C:\ProgramData\RSAgent\state.json`.

`state.json` contiene `last_success_utc` y se reemplaza de forma atómica únicamente después de recibir una respuesta HTTP correcta de Firulai. El mismo control se ejecuta al arrancar el servicio. Un `SemaphoreSlim` serializa las solicitudes para impedir que arranque, reanudación, reintento y temporizador ejecuten inventarios a la vez.

El temporizador se arma en intervalos máximos de cinco minutos y vuelve a comparar el reloj local con la hora objetivo. Esto evita depender de un único temporizador largo, que puede retrasarse mientras Windows está suspendido, y actúa como respaldo si no se recibe el evento de energía.

El instalador crea el origen `RSAgent` bajo el log Aplicación de Windows. IDs reservados:

- `1000`: servicio iniciado.
- `1001`: servicio detenido.
- `1100`: inventario iniciado.
- `1101`: inventario enviado y estado guardado.
- `1102`: inventario fallido.
- `1200`: recuperación iniciada después de una suspensión.

La clave del origen se conserva al desinstalar para que Windows pueda seguir mostrando correctamente los eventos históricos.

---

## Publicar una nueva versión

1. Cambiar el código necesario.
2. Actualizar versión si procede:
   - `AgentConfig.AgentVersion` en `src/RsAgent/AgentConfig.cs`.
   - `MyAppVersion` en `installer/RsAgent.iss`.
3. Compilar el agente.
4. Generar `Output/FirulaiAgent.exe`.
5. Probar instalación en una máquina Windows limpia.
6. Probar que una segunda instalación sobre el mismo equipo se cancela y pide desinstalar primero.
7. Probar instalación con UUID inexistente o ya asignado y comprobar que se cancela antes de crear archivos.
8. Probar desinstalación y comprobar en Firulai que el receptor deja el System asociado al UUID como desconectado.
9. Crear tag en GitHub, por ejemplo `v0.14.0`.
10. Crear GitHub Release para ese tag.
11. Adjuntar `FirulaiAgent.exe` como asset.
12. Comprobar que descarga desde:

```text
https://github.com/OWNER/REPO/releases/latest/download/FirulaiAgent.exe
```

Para esta versión, los endpoints de Firulai del instalador Windows apuntan a `https://rsm1.redsauce.net/AppController/commands_RSM/api/...`. Como cambia la URL configurada en el instalador y en el `config.json` generado, hay que compilar de nuevo `RsAgent.exe` y generar un nuevo `FirulaiAgent.exe`.

---

## Cuando hay que generar otro EXE

Generar un nuevo `FirulaiAgent.exe` si cambia cualquier cosa que deba llegar a los usuarios:

- Codigo C# del agente.
- Validación o almacenamiento del Agent token.
- URL de Firulai.
- Hora de ejecución programada.
- Reintentos, logging o formato del inventario.
- Instalador o desinstalador.
- Version o metadatos del instalador.

No hace falta generar otro EXE si solo cambia:

- Documentacion.
- Textos de la web.
- Botón/enlace de descarga en la aplicación.

---

## Integración en la aplicación

En la aplicación, el botón Windows debe apuntar al asset del último Release:

```html
<a href="https://github.com/OWNER/REPO/releases/latest/download/FirulaiAgent.exe">
  Descargar agente Windows
</a>
```

Flujo para el usuario:

1. Pulsa el boton Windows.
2. Descarga `FirulaiAgent.exe`.
3. Ejecuta el instalador como Administrador.
4. Introduce el UUID y el token facilitados en Firulai.
5. Si ya existe un agente instalado, el instalador cancela y pide desinstalar primero.
6. Si el UUID no está disponible en Firulai, el instalador cancela sin crear la instalación.
7. Si no se puede activar el System en Firulai, el instalador cancela sin crear la instalación.
8. Si las validaciones son correctas, el servicio `RSAgent` queda instalado.
9. El agente envía inventario al arrancar y luego diariamente a las 03:00.
10. Si el equipo está suspendido a las 03:00, realiza una única ejecución pendiente al reanudarse; `state.json` evita duplicados y solo se actualiza tras un envío confirmado.

---

## Verificación tras publicar

En una máquina de prueba:

```powershell
Get-Service RSAgent
Get-Content "C:\ProgramData\RSAgent\logs\rs_agent.log" -Tail 100
Get-Item "C:\ProgramData\RSAgent\inventory.json"
Get-Content "C:\ProgramData\RSAgent\state.json"
Get-WinEvent -FilterHashtable @{ LogName='Application'; ProviderName='RSAgent'; StartTime=(Get-Date).AddDays(-1) } |
  Select-Object TimeCreated, Id, LevelDisplayName, Message
```

Debe aparecer el evento `1101` y una línea similar en el fichero de log:

```text
Ejecución completada correctamente.
```

Para validar la recuperación, suspender una máquina de prueba antes de la hora programada y reanudarla después. Deben aparecer el evento de advertencia `1200`, el inicio `1100` con origen `recuperación-reanudación` y el éxito `1101`. Reanudar de nuevo el equipo el mismo día no debe iniciar otro inventario pendiente.

Para validar la desinstalación:

```powershell
& "C:\Program Files\RSAgent\unins000.exe"
```

En Firulai, el System asociado al UUID debe quedar desconectado. No deben borrarse inventario, vulnerabilidades ni el propio System durante la desinstalación.
