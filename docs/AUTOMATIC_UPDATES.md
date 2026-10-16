# Actualizaciones Windows

## Aviso ligero de version (incluido desde 0.20.4)

El servicio prepara un `changeSystemStatus` con `action=report_version`, UUID,
hostname, FQDN y version del ejecutable en uso antes de recopilar el inventario.
Durante una actualizacion espera a que se cierre el diario de recuperacion,
para no anunciar el binario antes de que termine el instalador. Si falla el
aviso, continua el inventario completo. Una respuesta vacia confirma solo
la recepcion en la cola; no se registra como confirmacion de actualizacion RSM.

El receptor debe aceptar esta accion antes de publicar los agentes. El aviso
esta incluido desde la release 0.20.4; no esta incluido en 0.20.3.

La release 0.20.3 incorpora actualizaciones firmadas. El instalador permite actualizar sobre una
instalacion valida con una version estrictamente superior. No desinstala ni
modifica UUID, token, idioma, `config.json`, estado o URL de API. El servicio
puede avanzar su estado de exito al enviar el siguiente inventario.

El helper extraido del instalador valida la configuracion y la version de la
instalacion, guarda el ejecutable anterior, registra una tarea de recuperacion
al arrancar Windows y espera a que el servicio se detenga. Si falla la instalacion,
restaura el ejecutable anterior y su version registrada. Si el equipo se reinicia
antes de completar la instalacion, la tarea hace la misma recuperacion. La tarea
y el diario se eliminan cuando el instalador confirma que el servicio arranca.
Esta recuperacion conserva el agente y sus datos; no es una restauracion completa
de todas las modificaciones internas de Inno Setup al desinstalador.

## Actualizacion automatica

Las configuraciones existentes tienen actualizacion automatica activada por
defecto. `"auto_update": false` permite desactivarla localmente como administrador.
El interruptor del detalle de equipo en Firulai queda pendiente para el final,
por decision del usuario: recordar esta opcion tras validar el funcionamiento.

El servicio comprueba inmediatamente al arrancar y cada hora mediante un temporizador
independiente del inventario. Si la consulta falla, los plazos entre intentos son
5, 10, 20 y despues 60 minutos; se mantiene el intervalo de 60 minutos mientras
persista el fallo. Una consulta correcta restablece el intervalo normal y la
secuencia de reintentos. Cada equipo agrega entre 0 y 60 segundos a los plazos,
calculados de forma estable a partir de su nombre, para repartir las consultas.
Al reanudarse Windows vuelve a comprobar si ha vencido ese plazo. Cada consulta
correcta registra la hora de la siguiente comprobacion. Este cambio esta pendiente
de publicacion; las versiones 0.20.6 y 0.20.7 mantienen la espera de 24 horas.
Un fallo de descarga no bloquea
el inventario. Solo se aceptan versiones estables superiores, destinos HTTPS
versionados del repositorio oficial y paquetes de hasta 100 MiB.

La version objetivo se lee de RSM (`items/get.php`, propiedad Windows `1990`)
con el token local del agente y la URL de API configurada, igual que la seleccion
de version del frontend. Ese token debe tener lectura de esta propiedad.
Se descarga `releases/download/{tag}/agent-update.json` y su firma; ya no se usa
`releases/latest`. El manifiesto firmado debe identificar exactamente la version
y el tag seleccionados por RSM. Una version igual o inferior no se instala.
Si RSM no responde, deniega acceso, devuelve una version invalida o versiones
contradictorias, no se recurre a `latest`: se registra el fallo y se reintenta
con los plazos progresivos anteriores. La firma, el hash y el limite de tamano siguen siendo obligatorios.

Un fallo de recopilacion o envio del inventario conserva su propio reintento a
los 30 minutos y no modifica la hora de la proxima consulta de actualizaciones.
El siguiente intento vuelve a recopilar y enviar el inventario. El estado de
ultimo envio correcto solo avanza cuando el envio finaliza sin error. El servidor
puede haber recibido un envio cuya respuesta se perdio; no se garantiza entrega
exactamente una vez. Si una recopilacion esta en curso, la comprobacion de
actualizaciones espera a que termine para no solapar una instalacion con ella.

Esta seleccion y el temporizador estan implementados y probados localmente;
queda pendiente validar la lectura con un token real de agente en produccion,
preparar una nueva release y publicarla. No cambia los binarios ya instalados.

La release debe contener:

- `FirulaiAgent.exe`
- `agent-update.json`
- `agent-update.json.sig`

La firma RSA de al menos 3072 bits con SHA-256 se verifica con la clave publica
incluida en el ejecutable. El manifiesto contiene version, URL concreta, tamano
y SHA-256 del instalador. El helper independiente vuelve a verificar firma y
paquete antes de ejecutarlo. Los archivos temporales ejecutables quedan
restringidos a administradores y SYSTEM.

La version intentada se guarda en
`C:\ProgramData\RSAgent\updates\attempted-version.txt` antes de ejecutar el
instalador, para evitar bucles tras fallos o reinicios. Un administrador puede
eliminar ese archivo para reintentar una version despues de investigar su fallo.
La ejecucion silenciosa usa `/NORESTART`: el agente no reinicia Windows.

## Clave de publicacion

La clave privada se conserva fuera del repositorio. La clave publica esta en
`src/RsAgent/UpdatePublicKey.xml`. Nunca publicar ni adjuntar la clave privada.
Perderla impide firmar versiones que los agentes actuales acepten: conservar
una copia protegida. Cambiar la clave publica exige planificar una migracion.

Crear una clave una sola vez, indicando una carpeta privada ya existente:

```powershell
.\scripts\new-update-signing-key.ps1 -PrivateKeyPath '<carpeta privada>\windows-update-private.xml'
```

Compilar despues de incorporar la clave publica:

```powershell
.\scripts\build-agent.ps1
.\scripts\build-localized-installers.ps1
.\scripts\sign-update-manifest.ps1 -PrivateKeyPath '<carpeta privada>\windows-update-private.xml' -InstallerPath .\Output\FirulaiAgent.exe -Version '0.20.3' -OutputDir .\Output
```

Los tres archivos deben publicarse juntos en la release correspondiente. La
firma del manifiesto no sustituye Authenticode ni cambia el publicador mostrado
por Windows. Un binario sin clave publica no ejecuta actualizaciones automaticas.

## Validacion

```powershell
.\scripts\test-upgrade.ps1
```

Prueba comparacion numerica de versiones, versiones inferiores o de prueba,
firmas incorrectas, manifiestos y paquetes alterados, destinos inesperados y
conservacion de la preferencia local. No toca el servicio instalado.

`scripts/test-live-upgrade.ps1` requiere administrador y actualiza realmente el
equipo. Guarda una copia protegida de ejecutable, desinstalador, configuracion y
estado. Comprueba version, hash del binario, servicio, configuracion, URL de API,
limpieza del diario y nuevo inventario. `-UseAutomaticWorker` prueba la misma
verificacion y ejecucion del helper automatico con un paquete local firmado;
no descarga ni publica una release.

Pruebas en el equipo autorizado: actualizacion manual 0.19.2 -> 0.20.0 y mediante
helper automatico firmado 0.20.0 -> 0.20.1, ambas correctas, con configuracion
conservada y nuevo inventario. La comprobacion final 0.20.1 -> 0.20.2 mediante
helper automatico confirma tambien el envio correcto del inventario mediante
el avance de `last_success_utc`. Evidencia: `automatic-update-validation.json`.
La release 0.20.3 esta publicada. Se ha reiniciado el servicio del equipo
autorizado con version 0.20.2: ha descargado el manifiesto y el instalador
oficiales, actualizado automaticamente a 0.20.3, conservado la configuracion
y confirmado el envio del inventario. Evidencia: `published-update-validation.json`.
El guion reproducible es `scripts/test-published-update.ps1`; necesita una
release superior a la instalada y permisos de administrador.

El rollback forzado se ha probado en este mismo equipo: se detuvo el servicio,
se simulo un ejecutable de reemplazo invalido y el helper restauro el binario
original, con configuracion intacta, servicio arrancado y diario/tarea eliminados.
Evidencia: `recovery-validation.json`. El reinicio o corte de corriente durante
la instalacion quedan pendientes; no se ha reiniciado el equipo.

## Correccion de reintentos 0.20.5

Las versiones 0.20.3 y 0.20.4 aplican una espera de 24 horas incluso si falla
la descarga al arrancar. La correccion reintenta cada cinco minutos
mediante un temporizador independiente del inventario. Conserva los horarios
y reintentos del inventario, serializa las ejecuciones y cancela el temporizador
al detener el servicio. Una comprobacion correcta cancela los reintentos cortos
y aplica la espera habitual de 24 horas. Se conservan la firma y las protecciones
contra versiones inferiores y bucles de instalacion fallida.

Probado sin modificar el servicio instalado: fallo repetido, recuperacion,
activacion real del temporizador, conservacion del temporizador de inventario
y ausencia de reintentos tras detener el servicio. La correccion no protege
a un agente antiguo hasta que este haya instalado la nueva version.

Validacion adicional de 0.20.5: simulacion de DNS no disponible, timeout,
recuperacion posterior y dos comprobaciones concurrentes; activacion real
del temporizador y evento de reanudacion sin inventario pendiente; cancelacion
al detener el servicio; pruebas de firmas y paquetes alterados, 28 comprobaciones
de inventarios grandes, 19 de elegibilidad, redirecciones HTTP y URL del instalador.
Instalador compilado y manifiesto firmado localmente. No se ha instalado
0.20.5 en el servicio real ni probado un apagado/reinicio completo con esta version.
