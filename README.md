# Firulai Inventory Agent para Windows

Agente de inventario para equipos Windows. Recopila información técnica del sistema, software instalado y componentes relevantes para análisis de vulnerabilidades, y envía el inventario a Firulai para su correlación con CVEs.

Este repositorio se usa como punto de descarga y documentación pública del agente Windows. El instalador oficial se publica como archivo adjunto en la sección **Releases**.

---

## Descarga

Descarga siempre la última versión desde:

[Descargar RSAgentSetup.exe](../../releases/latest/download/RSAgentSetup.exe)

O entra en la página de Releases del repositorio y descarga el archivo:

```text
RSAgentSetup.exe
```

---

## Requisitos

- Windows 10, Windows 11 o Windows Server 2019 o superior.
- .NET Framework 4.8 o compatible instalado en el sistema.
- Permisos de Administrador para instalar o desinstalar.
- Conectividad HTTPS hacia:

```text
https://rsm1.redsauce.net
```

- UUID y token facilitados en Firulai para identificar y autorizar el equipo.
- Alias obligatorio para identificar el sistema. Se guarda en Firulai y se puede modificar posteriormente desde Firulai.

---

## Instalación gráfica

1. Descarga `RSAgentSetup.exe` desde el último Release.
2. Ejecuta el instalador con doble clic.
3. Acepta la solicitud de permisos de Administrador de Windows.
4. Introduce el UUID y el token que se te han facilitado en Firulai, y escribe un alias para el sistema cuando el asistente lo solicite.
5. Finaliza la instalación.

Al terminar, el instalador crea y arranca automáticamente el servicio Windows `RSAgent`. A partir de ese momento, los datos del equipo se enviarán a Firulai y el inventario se actualizará automáticamente cada noche.

Antes de instalar, el asistente valida que no exista ya un agente en el equipo, que el token permite consultar Firulai y que el UUID existe y está disponible. Si detecta una instalación previa, el proceso se cancela y se debe desinstalar primero el agente actual.

---

## Qué hace el instalador

El instalador `RSAgentSetup.exe` realiza estas acciones:

1. Solicita privilegios de Administrador mediante UAC.
2. Valida el formato del UUID introducido y que el alias y el token no estén vacíos.
3. Comprueba que no exista una instalación local previa.
4. Valida en Firulai que el UUID existe y está disponible para este equipo.
5. Guarda el alias en Firulai sobre el item System asociado al UUID.
6. Instala `RsAgent.exe` en `C:\Program Files\RSAgent\`.
7. Crea los directorios de datos en `C:\ProgramData\RSAgent\`.
8. Genera `config.json` con la configuración local del agente, incluyendo el token facilitado por Firulai.
9. Restringe los permisos de `config.json` a `SYSTEM` y `Administrators`.
10. Registra el servicio Windows `RSAgent` con inicio automático.
11. Configura la recuperación del servicio ante fallos.
12. Arranca el servicio y ejecuta el primer inventario.
13. Registra el desinstalador en "Aplicaciones instaladas" de Windows.

Si ya existe una instalación previa o el servicio está ejecutándose, el instalador lo detecta al inicio y cancela la instalación. Para reinstalar, primero hay que desinstalar el agente actual.

La instalación se considera existente si aparece cualquiera de estas señales: servicio `RSAgent`, `RsAgent.exe`, `unins000.exe` o `C:\ProgramData\RSAgent\config.json`.

---

## Ejecución automática

El agente funciona como servicio Windows:

- Nombre del servicio: `RSAgent`.
- Nombre visible: `Firulai Inventory Agent`.
- Inicio: automático con Windows.
- Primera ejecución: al arrancar el servicio.
- Ejecución programada: una vez al día, por la noche, a las `03:00` hora local del equipo.
- Reintentos: si falla la recopilación o el envío, reintenta cada 30 minutos.

---

## Qué información recopila

El agente genera un inventario JSON con cuatro bloques principales.

### `system`

Información básica del host:

- Hostname y FQDN.
- UUID asignado por Firulai.
- Nombre, versión, build y edición de Windows.
- Arquitectura del sistema.
- Zona horaria.
- Fecha de recopilación.
- Versión del agente.

### `hardware`

Información de hardware necesaria para correlación técnica:

- Modelo de CPU.
- Discos detectados mediante WMI, incluyendo dispositivo y modelo.

### `packages`

Software instalado detectado desde varias fuentes:

| Manager | Fuente |
| --- | --- |
| `registry` | Registro de Windows, claves `Uninstall` de 64 y 32 bits |
| `winget` | Resultado de `winget list` |
| `choco` | Paquetes locales de Chocolatey |
| `pip` | Paquetes Python detectados con `pip` o `pip3` |
| `npm` | Paquetes globales de Node.js detectados con `npm` |

Cada paquete incluye nombre, versión y origen.

### `core_software`

Versiones de componentes relevantes para análisis de vulnerabilidades:

- IIS
- Apache/httpd
- nginx
- MySQL
- SQL Server
- PostgreSQL
- PHP
- Node.js
- Python
- Java
- Docker
- Git
- OpenSSH
- OpenSSL
- PowerShell
- .NET runtimes
- .NET SDKs

Cuando un componente no está instalado o no está disponible en el `PATH`, simplemente no se incluye en el inventario.

---

## Archivos y rutas

| Ruta | Descripción |
| --- | --- |
| `C:\Program Files\RSAgent\RsAgent.exe` | Ejecutable principal del agente |
| `C:\Program Files\RSAgent\unins000.exe` | Desinstalador |
| `C:\ProgramData\RSAgent\config.json` | Configuración local del agente |
| `C:\ProgramData\RSAgent\inventory.json` | Último inventario generado |
| `C:\ProgramData\RSAgent\logs\rs_agent.log` | Log principal |
| `C:\ProgramData\RSAgent\logs\rs_agent.log.1` | Logs rotados |

El log rota automáticamente al superar 5 MB y conserva hasta tres ficheros rotados.

---

## Comprobación de instalación

### Desde la interfaz gráfica

1. Abre `services.msc`.
2. Busca el servicio `RSAgent`.
3. Comprueba que está iniciado y configurado con inicio automático.

### Desde PowerShell

Ver estado del servicio:

```powershell
Get-Service RSAgent
```

Ver las últimas líneas del log:

```powershell
Get-Content "C:\ProgramData\RSAgent\logs\rs_agent.log" -Tail 50
```

Comprobar que existe el último inventario:

```powershell
Get-Item "C:\ProgramData\RSAgent\inventory.json"
```

Una ejecución correcta deja en el log una línea similar a:

```text
Inventario enviado correctamente a Firulai.
```

---

## Ejecución manual

Aunque normalmente se ejecuta como servicio, también puede lanzarse una ejecución puntual desde PowerShell con permisos de Administrador:

```powershell
& "C:\Program Files\RSAgent\RsAgent.exe" --run-once
```

Esto genera `inventory.json`, intenta enviarlo a Firulai y escribe el resultado en el log.

---

## Desinstalación gráfica

La desinstalación puede iniciarse desde Windows:

```text
Configuración -> Apps -> Aplicaciones instaladas -> Firulai Inventory Agent -> Desinstalar
```

También puede iniciarse desde PowerShell con permisos de Administrador:

```powershell
& "C:\Program Files\RSAgent\unins000.exe"
```

Durante la desinstalación aparecerá una única confirmación indicando que solo se eliminarán los archivos locales del agente junto al instalador. No se borrarán los datos de Firulai. El sistema quedará como inactivo en Firulai y, desde Firulai, se podrán eliminar definitivamente sus datos o volver a instalar el agente más adelante enlazándolo al System y al inventario ya guardados.

Si confirmas la operación, el desinstalador:

1. Lee el UUID configurado para este equipo.
2. Detiene el servicio `RSAgent`.
3. Busca en Firulai el item System asociado al UUID.
4. Actualiza la propiedad `Hostnamestatus` (`1751`) con el valor `Disconnected`.
5. Si Firulai confirma la actualización, informa de que los datos no se borrarán y elimina el servicio y los archivos locales del agente.
6. Si el UUID ya no existe en Firulai, informa de que no hay ningún System enlazado y desinstala igualmente la aplicación local.

Por seguridad, si no se puede contactar con Firulai o la actualización de estado no se confirma correctamente, la desinstalación se cancela. En ese caso, los archivos locales se conservan para que puedas revisar la conectividad y volver a intentarlo.

Una desinstalación completada elimina:

```text
C:\Program Files\RSAgent\
C:\ProgramData\RSAgent\
```

---

## Resolución de problemas

### El servicio no aparece

Ejecuta:

```powershell
Get-Service RSAgent
```

Si no existe, reinstala `RSAgentSetup.exe` como Administrador.

### El servicio existe pero no envía inventario

Revisa el log:

```powershell
Get-Content "C:\ProgramData\RSAgent\logs\rs_agent.log" -Tail 100
```

Comprueba especialmente:

- Que el UUID usado durante la instalación es válido.
- Que el token usado durante la instalación es válido y corresponde al UUID.
- Que el equipo tiene salida HTTPS hacia `rsm1.redsauce.net`.
- Que no hay proxy, firewall o inspección TLS bloqueando la conexión.

### La desinstalación se cancela

Si la desinstalación se cancela durante la actualización de estado en Firulai, revisa:

- Que el equipo tiene conexión a Internet.
- Que puede acceder por HTTPS a `https://rsm1.redsauce.net`.
- Que el UUID sigue asociado al sistema en Firulai.

Después de corregir el problema, vuelve a ejecutar la desinstalación.

### El inventario no contiene algunos paquetes

El agente solo puede listar herramientas disponibles en el equipo. Por ejemplo, los paquetes de `winget`, `choco`, `pip` o `npm` solo se incluyen si esas herramientas están instaladas y accesibles desde el entorno del servicio.

---

## Versión actual

Versión del agente: `0.1.2`

Nombre del instalador publicado:

```text
RSAgentSetup.exe
```
