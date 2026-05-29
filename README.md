# Redsauce Inventory Agent para Windows

Agente de inventario para equipos Windows. Recopila informacion tecnica del sistema, software instalado y componentes relevantes para analisis de vulnerabilidades, y envia el inventario a la plataforma Redsauce para su correlacion con CVEs.

Este repositorio se usa solo como punto de descarga y documentacion publica del agente Windows. El codigo fuente no se distribuye aqui; el instalador oficial se publica como archivo adjunto en la seccion **Releases**.

---

## Descarga

Descarga siempre la ultima version desde:

[Descargar RSAgentSetup.exe](../../releases/latest/download/RSAgentSetup.exe)

O entra en la pagina de Releases del repositorio y descarga el archivo:

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

- UUID asignado por RSM para identificar el equipo.

---

## Instalacion grafica

1. Descarga `RSAgentSetup.exe` desde el ultimo Release.
2. Ejecuta el instalador con doble clic.
3. Acepta la solicitud de permisos de Administrador de Windows.
4. Introduce el UUID asignado por RSM cuando el asistente lo solicite.
5. Finaliza la instalacion.

Al terminar, el instalador crea y arranca automaticamente el servicio Windows `RSAgent`.

---

## Que hace el instalador

El instalador `RSAgentSetup.exe` realiza estas acciones:

1. Solicita privilegios de Administrador mediante UAC.
2. Instala `RsAgent.exe` en `C:\Program Files\RSAgent\`.
3. Crea los directorios de datos en `C:\ProgramData\RSAgent\`.
4. Genera `config.json` con el UUID, token embebido y URL de la API.
5. Restringe los permisos de `config.json` a `SYSTEM` y `Administrators`.
6. Registra el servicio Windows `RSAgent` con inicio automatico.
7. Configura recuperacion del servicio ante fallos.
8. Arranca el servicio y ejecuta el primer inventario.
9. Registra el desinstalador en "Agregar o quitar programas".

Si ya existia una version previa del servicio, el instalador la detiene, la elimina y registra la nueva version.

---

## Ejecucion automatica

El agente funciona como servicio Windows:

- Nombre del servicio: `RSAgent`.
- Nombre visible: `Redsauce Inventory Agent`.
- Inicio: automatico con Windows.
- Primera ejecucion: al arrancar el servicio.
- Ejecucion programada: una vez al dia a las `03:00`, hora local del equipo.
- Reintentos: si falla la recopilacion o el envio, reintenta cada 30 minutos.

---

## Que informacion recopila

El agente genera un inventario JSON con cuatro bloques principales.

### `system`

Informacion basica del host:

- Hostname y FQDN.
- UUID asignado por RSM.
- Nombre, version, build y edicion de Windows.
- Arquitectura del sistema.
- Zona horaria.
- Fecha de recopilacion.
- Version del agente.

### `hardware`

Informacion de hardware necesaria para correlacion tecnica:

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

Cada paquete incluye nombre, version y origen.

### `core_software`

Versiones de componentes relevantes para analisis de vulnerabilidades:

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

Cuando un componente no esta instalado o no esta disponible en el `PATH`, simplemente no se incluye en el inventario.

---

## Archivos y rutas

| Ruta | Descripcion |
| --- | --- |
| `C:\Program Files\RSAgent\RsAgent.exe` | Ejecutable principal del agente |
| `C:\Program Files\RSAgent\unins000.exe` | Desinstalador |
| `C:\ProgramData\RSAgent\config.json` | Configuracion local del agente |
| `C:\ProgramData\RSAgent\inventory.json` | Ultimo inventario generado |
| `C:\ProgramData\RSAgent\logs\rs_agent.log` | Log principal |
| `C:\ProgramData\RSAgent\logs\rs_agent.log.1` | Logs rotados |

El log rota automaticamente al superar 5 MB y conserva hasta tres ficheros rotados.

---

## Comprobacion de instalacion

### Desde la interfaz grafica

1. Abre `services.msc`.
2. Busca el servicio `RSAgent`.
3. Comprueba que esta iniciado y configurado con inicio automatico.

### Desde PowerShell

Ver estado del servicio:

```powershell
Get-Service RSAgent
```

Ver las ultimas lineas del log:

```powershell
Get-Content "C:\ProgramData\RSAgent\logs\rs_agent.log" -Tail 50
```

Comprobar que existe el ultimo inventario:

```powershell
Get-Item "C:\ProgramData\RSAgent\inventory.json"
```

Una ejecucion correcta deja en el log una linea similar a:

```text
Inventario enviado correctamente a RSM.
```

---

## Ejecucion manual

Aunque normalmente se ejecuta como servicio, tambien puede lanzarse una ejecucion puntual desde PowerShell con permisos de Administrador:

```powershell
& "C:\Program Files\RSAgent\RsAgent.exe" --run-once
```

Esto genera `inventory.json`, intenta enviarlo a RSM y escribe el resultado en el log.

---

## Desinstalacion

Desde Windows:

```text
Configuracion -> Apps -> Redsauce Inventory Agent -> Desinstalar
```

Desde PowerShell:

```powershell
& "C:\Program Files\RSAgent\unins000.exe"
```

El desinstalador detiene y elimina el servicio `RSAgent`, borra `C:\Program Files\RSAgent\` y elimina el ejecutable principal.

En modo grafico pregunta si tambien debe eliminar la configuracion, el inventario y los logs de:

```text
C:\ProgramData\RSAgent\
```

---

## Resolucion de problemas

### El servicio no aparece

Ejecuta:

```powershell
Get-Service RSAgent
```

Si no existe, reinstala `RSAgentSetup.exe` como Administrador.

### El servicio existe pero no envia inventario

Revisa el log:

```powershell
Get-Content "C:\ProgramData\RSAgent\logs\rs_agent.log" -Tail 100
```

Comprueba especialmente:

- Que el UUID usado durante la instalacion es valido.
- Que el equipo tiene salida HTTPS hacia `rsm1.redsauce.net`.
- Que no hay proxy, firewall o inspeccion TLS bloqueando la conexion.

### El inventario no contiene algunos paquetes

El agente solo puede listar herramientas disponibles en el equipo. Por ejemplo, los paquetes de `winget`, `choco`, `pip` o `npm` solo se incluyen si esas herramientas estan instaladas y accesibles desde el entorno del servicio.

---

## Version actual

Version del agente: `0.1.0`

Nombre del instalador publicado:

```text
RSAgentSetup.exe
```
