# API: configuracion y pruebas Windows

Version preparada: 0.19.0.

El instalador escribe la cadena `ApiBaseUrl` en
`HKLM\SOFTWARE\Redsauce\firulaiAgent` (vista de 64 bits en Windows de 64 bits).
Su valor inicial es `https://rsm1.redsauce.net/AppController/`.
`config.json` guarda token, UUID e idioma; ya no se utiliza `api_url`.
Las instalaciones antiguas sin el valor de registro utilizan la base predeterminada.
No hay migracion de direcciones personalizadas antiguas: los campos api_url se ignoran.

`ApiEndpoint` construye base + `commands_RSM/api/api.php`. Las dos constantes
proceden de `src/RsAgent/ApiEndpoint.txt`, incrustado en el agente y leido por
Inno Setup al compilar. La desinstalacion llama al mismo agente.

## Inventarios comprimidos (desde 0.20.19)

El receptor `Upload JSON Server data` debe actualizarse antes de distribuir
el agente con este cambio. La ruta y el trigger `newServerData` se conservan.
Las consultas de sistemas siguen utilizando v2; no se crean eventos por v2.

Los inventarios de al menos 1 MiB se comprimen con gzip y se envuelven en JSON
con `inventory_transport=gzip-base64-v1`, `uncompressed_bytes`, `sha256` y
`data` en base64. El token del inventario permanece dentro del contenido
comprimido y la autenticacion HTTP/formulario se mantiene. Si la envoltura
resulta mayor, se conserva el JSON original. Los inventarios pequenos y los
agentes anteriores mantienen su contrato habitual.

El agente limita el inventario sin comprimir a 64 MiB y comprueba que el
formulario completo no supere 7 MiB antes de enviar cada peticion, dejando
margen bajo el corte observado de 8 MiB. El receptor limita la descompresion
al tamano declarado, con maximo 64 MiB, y verifica SHA256, gzip completo y
JSON de inventario antes de ejecutar la sincronizacion existente. Estos
limites son cotas de esta primera implementacion, no valores leidos de PHP.

La fragmentacion todavia no esta implementada. Si el cuerpo completo sigue
superando 7 MiB, el agente falla con un error explicito antes de enviarlo.
No envia ni procesa inventarios parciales y no reintenta el mismo contenido
sin comprimir como alternativa a un error del servidor.

Validacion local: `scripts/test-large-inventory.ps1` y
`scripts/test-api-redirects.ps1`, sin `-Live`. El primero admite
`-ExportTransport <archivo>` para exportar una muestra sintetica comprimida
para comprobarla con el receptor Python. Compilar no publica ni instala
el agente; la respuesta HTTP de eventos sigue indicando encolado, no que
se haya completado el procesamiento del inventario.

301/308 seguido de 2xx guarda la base nueva; 302/307 conserva la anterior.
POST, cuerpo y token se mantienen. Location debe terminar en el endpoint y
puede ser relativa. Solo se acepta HTTPS sin credenciales, query ni fragmento.
Hay cinco saltos maximo. En cadenas mixtas solo se persiste el tramo permanente
inicial; los errores del destino no cambian la base. Durante la validacion del
instalador la base permanente se retiene y se escribe al proceder la instalacion.

## Evidencia de almacenamiento

En PowerShell como administrador, sin mostrar el token:

```powershell
$config = Get-Content "$env:ProgramData\firulaiAgent\config.json" -Raw -ErrorAction Stop | ConvertFrom-Json
$config.PSObject.Properties.Name -contains 'api_url'
reg.exe query "HKLM\SOFTWARE\Redsauce\firulaiAgent" /v ApiBaseUrl /reg:64
```

Antigua: True y valor de registro ausente. Nueva: False y base en registro.

## Pruebas aisladas

Desde el repositorio, en PowerShell:

```powershell
.\scripts\test-api-redirects.ps1
.\scripts\test-api-redirects.ps1 -Live
.\scripts\test-installed-api.ps1
.\scripts\test-installer-endpoint.ps1 -Live
```

La primera prueba compila el modulo del repositorio y usa una clave HKCU temporal.
La segunda usa HTTPBingo. Un segundo proceso vuelve a leer la base persistida.
`test-installed-api` carga una copia identica del ejecutable instalado, sin
recompilar su codigo; no ejecuta el servicio ni recoge inventario.
`test-installer-endpoint` compila las funciones del instalador del repositorio,
independientemente de la version instalada, y cancela antes de instalar.
Comprueba la base que se entregaria al registro, no una escritura real en HKLM.
HTTPBingo representa origen y destino. Todos los datos son ficticios, y las claves
y ejecutables temporales se eliminan. No modificar la base real del servicio
para apuntarla a HTTPBingo: enviaria el inventario real.

## Entrega

Ejecutar `scripts/build-agent.ps1` y despues `scripts/build-localized-installers.ps1`.
Resultado: `Output/FirulaiAgent.exe`. El codigo se publica en una rama del
repositorio; el ejecutable va como asset de la release 0.19.0, no como archivo
versionado. Las pruebas no se incluyen en el instalador. Consultar
`Output/RELEASE_CHECKSUMS.txt` para el SHA256 generado. La nueva instalacion
completa y los receptores de RSM requieren comprobacion en un equipo de pruebas.
