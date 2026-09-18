# API: configuracion y pruebas Windows

Version preparada: 0.18.0.

El instalador escribe la cadena `ApiBaseUrl` en
`HKLM\SOFTWARE\Redsauce\RSAgent` (vista de 64 bits en Windows de 64 bits).
Su valor inicial es `https://rsm1.redsauce.net/AppController/`.
`config.json` guarda token, UUID e idioma; ya no se utiliza `api_url`.
Las instalaciones antiguas sin el valor de registro utilizan la base predeterminada.
No hay migracion de direcciones personalizadas antiguas: los campos api_url se ignoran.

`ApiEndpoint` construye base + `commands_RSM/api/api.php`. Las dos constantes
proceden de `src/RsAgent/ApiEndpoint.txt`, incrustado en el agente y leido por
Inno Setup al compilar. La desinstalacion llama al mismo agente.

301/308 seguido de 2xx guarda la base nueva; 302/307 conserva la anterior.
POST, cuerpo y token se mantienen. Location debe terminar en el endpoint y
puede ser relativa. Solo se acepta HTTPS sin credenciales, query ni fragmento.
Hay cinco saltos maximo. En cadenas mixtas solo se persiste el tramo permanente
inicial; los errores del destino no cambian la base. Durante la validacion del
instalador la base permanente se retiene y se escribe al proceder la instalacion.

## Evidencia de almacenamiento

En PowerShell como administrador, sin mostrar el token:

```powershell
$config = Get-Content "$env:ProgramData\RSAgent\config.json" -Raw -ErrorAction Stop | ConvertFrom-Json
$config.PSObject.Properties.Name -contains 'api_url'
reg.exe query "HKLM\SOFTWARE\Redsauce\RSAgent" /v ApiBaseUrl /reg:64
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
repositorio; el ejecutable va como asset de la release v0.18.0, no como archivo
versionado. Las pruebas no se incluyen en el instalador. Consultar
`Output/RELEASE_CHECKSUMS.txt` para el SHA256 generado. La nueva instalacion
completa y los receptores de RSM requieren comprobacion en un equipo de pruebas.
