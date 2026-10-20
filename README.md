# Firulai Inventory Agent para Windows

## Aviso de recopilación (0.20.12)

El flujo común de ejecución manual (`--run-once`) y del servicio informa del
inicio, renueva cada 45 segundos y retira el aviso al terminar el envío o fallar.
La web muestra «Recopilando datos» mientras dura ese trabajo. Si el proceso se
interrumpe, el aviso caduca a los tres minutos sin heartbeat.

El reporte es opcional: un backend antiguo o sin conexión no bloquea el
inventario. Utiliza `https://firulai.redsauce.net/api/agents/collection-status`;
`RS_AGENT_COLLECTION_URL` permite un destino HTTPS de prueba, sin redirecciones
con credenciales. Necesita el backend de Firulai con los endpoints de recopilación. No requiere
cambios adicionales en scripts RSM.

Agente de inventario para equipos Windows. Recopila información técnica del sistema, software instalado y componentes relevantes para análisis de vulnerabilidades, y envía el inventario a Firulai para su correlación con CVEs.

Este repositorio se usa como punto de descarga y documentación pública del agente Windows. El instalador oficial se publica como archivo adjunto en la sección **Releases**.

---

## Descarga

Descarga siempre la última versión desde:

[Descargar FirulaiAgent.exe](../../releases/latest/download/FirulaiAgent.exe)

O entra en la página de Releases del repositorio y descarga el archivo:

```text
FirulaiAgent.exe
```

---

## Requisitos

El instalador consulta RSM de forma síncrona antes de crear configuración o
servicio: el UUID debe existir una sola vez, tener cobertura y corresponder a
Windows y a este equipo (o ser una reserva nueva). Una respuesta vacía de un
evento no confirma estas condiciones. Tras pedir la activación, espera su
confirmación en RSM antes de arrancar el servicio.

Cada ejecución vuelve a comprobar existencia, estado activo y cobertura antes
de recopilar y antes de enviar. Si falla alguna condición, no envía inventario
ni actualiza la fecha de éxito. La cuenta Arsys 6956 mantiene únicamente su
excepción temporal de cobertura.

- Windows 10, Windows 11 o Windows Server 2019 o superior.
- .NET Framework 4.8 o compatible instalado en el sistema.
- Permisos de Administrador para instalar o desinstalar.
- Conectividad HTTPS hacia:

```text
https://rsm1.redsauce.net
```

- UUID y token facilitados en Firulai para identificar y autorizar el equipo.

---

## Instalación gráfica

1. Descarga el instalador desde Firulai. Segun tu idioma de usuario, el mismo binario puede descargarse como `FirulaiAgent-ca.exe`, `FirulaiAgent-en.exe`, `FirulaiAgent_ca.exe`, etc. El instalador lee ese sufijo para elegir el idioma.
2. Ejecuta el instalador con doble clic.
3. Acepta la solicitud de permisos de Administrador de Windows.
4. Lee el Acuerdo de licencia y aviso de uso y marca la casilla de aceptación. No se puede continuar sin aceptarlo.
5. Introduce el UUID y el token que se te han facilitado en Firulai.
6. Finaliza la instalación.

Al terminar, el instalador crea y arranca automáticamente el servicio Windows `RSAgent`. A partir de ese momento, los datos del equipo se enviarán a Firulai y el inventario se actualizará automáticamente cada noche.

En una instalacion nueva, el asistente valida el token y el UUID en Firulai. Si
encuentra una instalacion valida y el instalador tiene una version superior,
actualiza el agente conservando UUID, token, idioma y configuracion, sin
desinstalar. No permite instalar una version inferior ni cambiar la identidad
del equipo durante una actualizacion.

Las actualizaciones automaticas y su validacion se describen en
[AUTOMATIC_UPDATES.md](docs/AUTOMATIC_UPDATES.md).

---

## Qué hace el instalador

El instalador `FirulaiAgent.exe`, tambien cuando se descarga con sufijo de idioma, realiza estas acciones:

1. Solicita privilegios de Administrador mediante UAC.
2. Muestra el Acuerdo de licencia y aviso de uso y exige su aceptación antes de solicitar credenciales.
3. Valida el formato del UUID introducido y que el token no esté vacío.
4. Detecta una instalacion existente y comprueba si corresponde actualizarla.
5. Valida en Firulai que el UUID existe y está disponible para este equipo.
6. Solicita al receptor que active el System asociado al UUID.
7. Instala `RsAgent.exe` en `C:\Program Files\RSAgent\`.
8. Crea los directorios de datos en `C:\ProgramData\RSAgent\`.
9. Genera `config.json` con la configuración local del agente, incluyendo el token facilitado por Firulai.
10. Restringe los permisos de `config.json` a `SYSTEM` y `Administrators`.
11. Registra el servicio Windows `RSAgent` con inicio automático.
12. Registra el origen `RSAgent` en el log Aplicación de Windows.
13. Configura la recuperación del servicio ante fallos.
14. Arranca el servicio y ejecuta el primer inventario.
15. Registra el desinstalador en "Aplicaciones instaladas" de Windows.

Si ya existe una instalacion valida y la nueva version es superior, el instalador
actualiza los archivos conservando la identidad y la configuracion. Si se desea
cambiar el UUID del equipo, se mantiene el procedimiento de desinstalar e instalar.

La instalación se considera existente si aparece cualquiera de estas señales: servicio `RSAgent`, `RsAgent.exe`, `unins000.exe` o `C:\ProgramData\RSAgent\config.json`.

---

## Ejecución automática

El agente funciona como servicio Windows:

- Nombre del servicio: `RSAgent`.
- Nombre visible: `Firulai Inventory Agent`.
- Inicio: automático con Windows.
- Primera ejecución: al arrancar el servicio.
- Ejecución programada: una vez al día, por la noche, a las `03:00` hora local del equipo.
- Recuperación: si el equipo estaba suspendido a las `03:00`, ejecuta el inventario al reanudarse Windows.
- Comprobación de respaldo: mientras el equipo está activo, contrasta el reloj real como máximo cada cinco minutos para detectar ejecuciones vencidas aunque Windows no entregue el evento de reanudación.
- Reintentos: si falla la recopilación o el envío, reintenta cada 30 minutos.

El agente no despierta el equipo. Cuando pierde la ejecución de las `03:00`, realiza una sola ejecución al volver a estar disponible, aunque se hayan perdido varias noches. La última ejecución correcta se guarda en `state.json`; solo se actualiza después de que Firulai confirme el envío. Un bloqueo interno evita que el temporizador, el arranque y la reanudación lancen inventarios simultáneos.

---

## Qué información recopila

El agente genera un inventario JSON con cinco bloques principales.

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

### `components`

Librerías y componentes instalados detectados desde varias fuentes:

| Manager | Fuente |
| --- | --- |
| `registry` | Componentes internos detectados en las claves `Uninstall` globales y de usuario |
| `file` | DLL con versión ubicadas bajo la raíz concreta de una aplicación descubierta |
| `appx` | Frameworks y recursos AppX/MSIX obtenidos con PowerShell nativo |

Cada componente incluye nombre, versión y origen. Cuando Windows ofrece
evidencias suficientes para reconocer el software padre, también incluye
`source_package`, `source_version` y `upstream_version`. Firulai usa esos campos
para enlazar el Componente con su Paquete mediante la propiedad RSM `1866`.

El agente considera evidencias nativas `ParentKeyName`, `ParentDisplayName`,
`SystemComponent`, `ProductName`, `Publisher`, la instalación registrada y
expresiones explícitas como `installed by`. Además, una vez identificada la
raíz concreta de una aplicación, recorre sus DLL, conserva el nombre de archivo
y la versión nativa y las enlaza a ese paquete. La misma librería puede aparecer
varias veces si pertenece a aplicaciones diferentes.

### `packages`

Software principal instalado. Se obtiene mediante fuentes incluidas en Windows:
Registro global y de usuarios, `Program Files`, `Program Files (x86)`, App Paths,
servicios, accesos directos y AppX/MSIX. También se siguen esas evidencias hacia
rutas de usuario o ubicaciones personalizadas. Cada Paquete contiene `name` y
`version`, obtenidos del registro o de los recursos de versión del ejecutable.
Los identificadores técnicos se presentan con espacios y palabras separadas:
`Microsoft.YourPhone` se guarda como `Microsoft Your Phone`, sin mantener un
catálogo de fabricantes o productos.

Ejemplo simplificado:

```json
{
  "components": [
    {
      "name": "Bitvise SSH Client - FlowSshNet (x64)",
      "version": "9.63.0.0",
      "manager": "registry",
      "source_package": "Bitvise SSH Client",
      "source_version": "9.63",
      "upstream_version": "9.63"
    }
  ],
  "packages": [
    {
      "name": "Bitvise SSH Client",
      "version": "9.63"
    }
  ]
}
```

## Archivos y rutas

| Ruta | Descripción |
| --- | --- |
| `C:\Program Files\RSAgent\RsAgent.exe` | Ejecutable principal del agente |
| `C:\Program Files\RSAgent\unins000.exe` | Desinstalador |
| `C:\ProgramData\RSAgent\config.json` | Configuración local del agente |
| `C:\ProgramData\RSAgent\state.json` | Fecha UTC de la última ejecución enviada correctamente |
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

Comprobar la última ejecución correcta guardada:

```powershell
Get-Content "C:\ProgramData\RSAgent\state.json"
```

### Registro de eventos de Windows

El instalador registra el origen `RSAgent` en **Visor de eventos > Registros de Windows > Aplicación**. El agente publica estos eventos:

| ID | Nivel | Significado |
| --- | --- | --- |
| `1000` | Información | Servicio iniciado |
| `1001` | Información | Servicio detenido o Windows apagándose |
| `1100` | Información | Ejecución de inventario iniciada |
| `1101` | Información | Ejecución y envío completados correctamente |
| `1102` | Error | Ejecución fallida, incluyendo fase y excepción |
| `1200` | Advertencia | Ejecución de las 03:00 perdida por suspensión; comienza la recuperación |

Consultar los últimos eventos desde PowerShell:

```powershell
Get-WinEvent -FilterHashtable @{
  LogName = 'Application'
  ProviderName = 'RSAgent'
  StartTime = (Get-Date).AddDays(-7)
} | Select-Object TimeCreated, Id, LevelDisplayName, Message
```

Seguir el fichero de log detallado en tiempo real:

```powershell
Get-Content "C:\ProgramData\RSAgent\logs\rs_agent.log" -Tail 100 -Wait
```

Una ejecución correcta deja en el log una línea similar a:

```text
Ejecución completada correctamente.
```

---

## Ejecución manual

Aunque normalmente se ejecuta como servicio, también puede lanzarse una ejecución puntual desde PowerShell con permisos de Administrador:

```powershell
& "C:\Program Files\RSAgent\RsAgent.exe" --run-once
```

Esto genera `inventory.json`, intenta enviarlo a Firulai, actualiza `state.json` si el envío termina correctamente y escribe el resultado tanto en el fichero de log como en el registro Aplicación de Windows.

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
3. Envía `changeSystemStatus` con el UUID y `action=disconnect` al receptor de Firulai.
4. El receptor localiza el System y aplica el estado desconectado mediante su configuración interna.
5. Elimina el servicio y los archivos locales del agente tanto si Firulai confirma la actualización como si falla la comunicación.
6. Si el UUID ya no existe en Firulai, informa de que no hay ningún System enlazado y desinstala igualmente la aplicación local.

Si no se puede contactar con Firulai o la actualización de estado falla, la desinstalación local continúa. El System puede conservar su estado anterior en Firulai.

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

Si no existe, reinstala `FirulaiAgent.exe` como Administrador.

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

La recopilación no requiere Winget, Chocolatey, pip, npm ni software adicional.
Comprueba que la cuenta del servicio pueda leer la ruta donde reside la aplicación.

---

## Versión actual

Versión del agente: `0.19.1`

Nombre del instalador publicado:

```text
FirulaiAgent.exe
```

## Base de la API y redirecciones

Consulta [la guia de configuracion y pruebas](docs/API_REDIRECTS.md).
