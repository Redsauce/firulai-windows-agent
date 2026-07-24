# RSAgent Windows - Mantenimiento y publicación

Documento interno para generar, publicar y actualizar el instalador Windows.

---

## Qué se sube al repositorio

Subir código fuente y scripts:

```text
README.md
README_RELEASE.md
RsAgent.sln
src/RsAgent/*.cs
src/RsAgent/RsAgent.csproj
installer/LICENSE-es.txt
installer/RsAgent.iss
```

No subir artefactos generados:

```text
Output/
src/RsAgent/bin/
src/RsAgent/obj/
```

Estos directorios estan ignorados en `.gitignore`.

---

## Qué se publica para descarga

El usuario final no descarga el repo. Descarga el instalador publicado como asset de GitHub Releases:

```text
RSAgentSetup.exe
```

Enlace estable para la aplicación:

```text
https://github.com/Redsauce/firulai-windows-agent/releases/latest/download/RSAgentSetup.exe
```

Enlace a una versión concreta:

```text
https://github.com/Redsauce/firulai-windows-agent/releases/download/v0.1.3/RSAgentSetup.exe
```

---

## Generar el instalador

Desde la raíz de `firulai-windows-agent`:

```powershell
cd firulai-windows-agent
```

Compilar el agente con MSBuild y el Developer Pack de .NET Framework 4.8:

```powershell
msbuild .\RsAgent.sln /t:Build /p:Configuration=Release
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
Output/RSAgentSetup.exe
```

El instalador contiene el ejecutable compilado en `src/RsAgent/bin/Release/RsAgent.exe`. Si cambian el código C# o el script `installer/RsAgent.iss`, hay que repetir los dos pasos: compilar el agente y después generar el instalador.

---

## Validaciones previas de instalación

El instalador Windows replica el criterio del instalador Linux antes de crear archivos:

1. Valida el formato del UUID y que el alias y el token obligatorios no estén vacíos.
2. Comprueba si ya existe una instalación local.
3. Valida en Firulai que el UUID existe y está disponible usando el token introducido por el usuario.
4. Guarda el alias en la propiedad `Alias` (`1827`) del item System asociado al UUID mediante `PATCH` a `items/update.php`, enviando el `ID` como string.

La instalación local se considera existente si aparece cualquiera de estas señales:

- Servicio Windows `RSAgent`.
- Ejecutable `RsAgent.exe` en la ruta de instalación.
- Desinstalador `unins000.exe`.
- Configuración local `C:\ProgramData\RSAgent\config.json`.

Si existe instalación local, el instalador cancela el proceso y pide desinstalar primero el agente actual. No reinstala encima, igual que `install.sh`.

En modo gráfico, el acuerdo de licencia aparece antes de la página de UUID, alias y token, y el botón para continuar permanece deshabilitado hasta marcar su aceptación. En modo silencioso, el instalador requiere `/ACCEPTLICENSE=yes /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN>`; si falta la aceptación explícita o alguna credencial, termina con error y no debe crear ni reemplazar archivos.

La validación remota usa el endpoint `api/v2/items/get.php` con los mismos IDs de propiedades que Linux:

- Hostname: `1749`.
- FQDN: `1750`.
- UUID: `1780`.
- Alias: `1827`.

Si el UUID no existe en Firulai, la instalación se cancela. Si el UUID ya tiene hostname o FQDN asignados, se compara contra el equipo local; si corresponde al mismo equipo o a otro sistema, también se cancela y se indica que debe desinstalarse primero o generarse un UUID nuevo desde Firulai.

Orden equivalente al instalador Linux:

1. Validar permisos de Administrador mediante UAC.
2. Validar UUID, alias y token obligatorios.
3. Bloquear si ya existe instalación local.
4. Validar UUID en Firulai usando el token introducido.
5. Guardar el alias en Firulai usando el item encontrado por UUID.
6. Crear directorios y configuración.
7. Instalar ejecutable y registrar una única ejecución automática mediante el servicio `RSAgent`.

---

## Desinstalación y estado inactivo en Firulai

El desinstalador de Windows no borra datos de Firulai. En su lugar, usa el propio `RsAgent.exe` con el modo interno `--mark-disconnected-on-uninstall`.

La notificación a Firulai usa API v2: primero consulta el System por UUID (`1780`) en `items/get.php` y después actualiza la propiedad `Hostnamestatus` (`1751`) a `Disconnected` mediante `PATCH` a `items/update.php`.

Flujo interno:

1. `unins000.exe` muestra un aviso en modo gráfico.
2. Se detiene el servicio `RSAgent`.
3. `RsAgent.exe --mark-disconnected-on-uninstall` carga `C:\ProgramData\RSAgent\config.json`.
4. Envía a Firulai una consulta `GET` contra `api/v2/items/get.php` filtrando por UUID.
5. Con el `ID` devuelto, envía a Firulai un `PATCH` contra `api/v2/items/update.php`:

```json
[
  {
    "ID": "157",
    "1751": "Disconnected"
  }
]
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
4. Generar `Output/RSAgentSetup.exe`.
5. Probar instalación en una máquina Windows limpia.
6. Probar que una segunda instalación sobre el mismo equipo se cancela y pide desinstalar primero.
7. Probar instalación con UUID inexistente o ya asignado y comprobar que se cancela antes de crear archivos.
8. Probar desinstalación y comprobar en Firulai que el System asociado al UUID queda con `Hostnamestatus` (`1751`) = `Disconnected`.
9. Crear tag en GitHub, por ejemplo `v0.1.3`.
10. Crear GitHub Release para ese tag.
11. Adjuntar `RSAgentSetup.exe` como asset.
12. Comprobar que descarga desde:

```text
https://github.com/Redsauce/firulai-windows-agent/releases/latest/download/RSAgentSetup.exe
```

Para esta versión, los endpoints de Firulai del instalador Windows apuntan a `https://rsm1.redsauce.net/AppController/commands_RSM/api/...`. Como cambia la URL configurada en el instalador y en el `config.json` generado, hay que compilar de nuevo `RsAgent.exe` y generar un nuevo `RSAgentSetup.exe`.

---

## Cuando hay que generar otro EXE

Generar un nuevo `RSAgentSetup.exe` si cambia cualquier cosa que deba llegar a los usuarios:

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
<a href="https://github.com/Redsauce/firulai-windows-agent/releases/latest/download/RSAgentSetup.exe">
  Descargar agente Windows
</a>
```

Flujo para el usuario:

1. Pulsa el boton Windows.
2. Descarga `RSAgentSetup.exe`.
3. Ejecuta el instalador como Administrador.
4. Introduce el UUID, el token facilitado en Firulai y un alias para el sistema.
5. Si ya existe un agente instalado, el instalador cancela y pide desinstalar primero.
6. Si el UUID no está disponible en Firulai, el instalador cancela sin crear la instalación.
7. Si no se puede guardar el alias en Firulai, el instalador cancela sin crear la instalación.
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

En Firulai, el System asociado al UUID debe quedar con `Hostnamestatus` (`1751`) = `Disconnected`. No deben borrarse inventario, vulnerabilidades ni el propio System durante la desinstalación.
