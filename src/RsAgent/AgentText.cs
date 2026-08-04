using System;
using System.Collections.Generic;

namespace RsAgent
{
    internal static class AgentText
    {
        public const string DefaultLocale = "en_US";
        private static readonly object Sync = new object();
        private static string _language = "en";

        private static readonly Dictionary<string, string> English = Build(new[]
        {
            "config.missing", "config.json does not exist",
            "config.tokenMissing", "Agent token is not configured in the agent.",
            "config.uuidInvalid", "config.json contains an invalid UUID.",
            "state.invalidLastSuccess", "state.json does not contain a valid last execution date. The execution will be considered pending.",
            "state.readFailed", "Could not read state.json. The execution will be considered pending",
            "logger.eventWriteFailed", "Could not write event {0} to the Windows Application log: {1}",
            "program.usageRunOnce", "Usage: RsAgent.exe --run-once",
            "program.usageService", "As a service, install it with the Inno Setup installer or sc.exe.",
            "program.fatal", "Fatal failure",
            "program.uninstallNoSystemLog", "Uninstall: no System exists in Firulai for UUID {0}. Local uninstall is allowed.",
            "program.uninstallNoSystemConsole", "No System is linked in Firulai for UUID {0}. Local uninstall will continue.",
            "program.uninstallDisconnectedLog", "Uninstall: System marked as Disconnected in Firulai for UUID {0}. Firulai response: {1}",
            "program.uninstallDisconnectedConsole", "System marked as inactive in Firulai for UUID {0}.",
            "rsm.httpStarted", "HTTP send started. Destination={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "HTTP response received. Status={0} {1}, durationMs={2}, responseCharacters={3}.",
            "rsm.httpFailed", "Firulai responded {0}: {1}",
            "rsm.invalidUrl", "Invalid URL",
            "rsm.noSystemForUuid", "No System exists in Firulai for UUID {0}. Remote update is skipped and local uninstall is allowed.",
            "rsm.winHttpInitFailed", "Could not initialize WinHTTP to query Firulai.",
            "rsm.uuidSearchFailed", "Firulai responded {0} while searching for the UUID: {1}",
            "rsm.statusUpdateFailed", "Firulai responded {0} while updating the system status: {1}",
            "service.triggerNone", "none",
            "service.triggerStartupRecovery", "startup-recovery",
            "service.triggerServiceStart", "service-start",
            "service.triggerResumeRecovery", "resume-recovery",
            "service.triggerDailyScheduled", "daily-scheduled",
            "service.triggerRetry", "retry",
            "service.phasePreparation", "preparation",
            "service.phaseConfigLoad", "configuration-load",
            "service.phaseInventoryCollection", "inventory-collection",
            "service.phaseHttpSend", "http-send",
            "service.phaseStateSave", "state-save",
            "service.unavailable", "unavailable",
            "service.started", "Service started. Version={0}, machine={1}, localTime={2}, timeZone={3}, log={4}.",
            "service.stopped", "Service stopped.",
            "service.shutdown", "Windows is shutting down; service stopped.",
            "service.powerEvent", "Power event received. Status={0}, localTime={1}, nextExecution={2}, type={3}.",
            "service.resumeMissedRun", "Windows resumed after missing the 03:00 execution. A recovery execution will start. Scheduled={0}, resumed={1}.",
            "service.noDailyPendingAfterResume", "There is no daily execution pending after resume.",
            "service.executionStarted", "Execution started. Id={0}, origin={1}.",
            "service.configLoaded", "Configuration loaded. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Collecting inventory. Id={0}.",
            "service.inventoryCollected", "Inventory collected. Id={0}, characters={1}, file={2}.",
            "service.executionCompleted", "Execution completed successfully. Id={0}, origin={1}, durationMs={2}, state={3}.",
            "service.executionFailed", "Execution failed. Id={0}, origin={1}, phase={2}, durationMs={3}.",
            "service.skippedStopping", "Execution skipped because the service is stopping. Origin={0}.",
            "service.skippedSatisfied", "Execution skipped because the request is already satisfied. Origin={0}, requiredSince={1}, lastSuccess={2}.",
            "service.scheduled", "Execution scheduled. Type={0}, scheduled={1}, maximumCheck={2}.",
            "service.timerElapsed", "Timer elapsed. Type={0}, scheduled={1}, actual={2}, delay={3}."
        });

        private static readonly Dictionary<string, string> Spanish = Build(new[]
        {
            "config.missing", "No existe config.json",
            "config.tokenMissing", "Agent token no configurado en el agente.",
            "config.uuidInvalid", "UUID no valido en config.json.",
            "state.invalidLastSuccess", "state.json no contiene una fecha de ultima ejecucion valida. Se considerara que la ejecucion esta pendiente.",
            "state.readFailed", "No se pudo leer state.json. Se considerara que la ejecucion esta pendiente",
            "logger.eventWriteFailed", "No se pudo escribir el evento {0} en el registro Aplicacion de Windows: {1}",
            "program.usageRunOnce", "Uso: RsAgent.exe --run-once",
            "program.usageService", "Como servicio, instalalo con el instalador Inno Setup o sc.exe.",
            "program.fatal", "Fallo fatal",
            "program.uninstallNoSystemLog", "Desinstalacion: no existe ningun System en Firulai para UUID {0}. Se permite la desinstalacion local.",
            "program.uninstallNoSystemConsole", "No hay ningun System enlazado en Firulai para UUID {0}. La desinstalacion local continuara.",
            "program.uninstallDisconnectedLog", "Desinstalacion: System marcado como Disconnected en Firulai para UUID {0}. Respuesta Firulai: {1}",
            "program.uninstallDisconnectedConsole", "Sistema marcado como inactivo en Firulai para UUID {0}.",
            "rsm.httpStarted", "Envio HTTP iniciado. Destino={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "Respuesta HTTP recibida. Estado={0} {1}, duracionMs={2}, respuestaCaracteres={3}.",
            "rsm.httpFailed", "Firulai respondio {0}: {1}",
            "rsm.invalidUrl", "URL no valida",
            "rsm.noSystemForUuid", "No existe ningun System en Firulai para el UUID {0}. Se omite la actualizacion remota y se permite la desinstalacion local.",
            "rsm.winHttpInitFailed", "No se pudo inicializar WinHTTP para consultar Firulai.",
            "rsm.uuidSearchFailed", "Firulai respondio {0} al buscar el UUID: {1}",
            "rsm.statusUpdateFailed", "Firulai respondio {0} al actualizar el estado del sistema: {1}",
            "service.triggerNone", "ninguno",
            "service.triggerStartupRecovery", "recuperacion-arranque",
            "service.triggerServiceStart", "inicio-servicio",
            "service.triggerResumeRecovery", "recuperacion-reanudacion",
            "service.triggerDailyScheduled", "programada-diaria",
            "service.triggerRetry", "reintento",
            "service.phasePreparation", "preparacion",
            "service.phaseConfigLoad", "carga-configuracion",
            "service.phaseInventoryCollection", "recopilacion-inventario",
            "service.phaseHttpSend", "envio-http",
            "service.phaseStateSave", "guardado-estado",
            "service.unavailable", "no-disponible",
            "service.started", "Servicio iniciado. Version={0}, equipo={1}, horaLocal={2}, zonaHoraria={3}, log={4}.",
            "service.stopped", "Servicio detenido.",
            "service.shutdown", "Windows se esta apagando; servicio detenido.",
            "service.powerEvent", "Evento de energia recibido. Estado={0}, horaLocal={1}, proximaEjecucion={2}, tipo={3}.",
            "service.resumeMissedRun", "Windows se reanudo despues de perder la ejecucion de las 03:00. Se iniciara una ejecucion de recuperacion. Prevista={0}, reanudacion={1}.",
            "service.noDailyPendingAfterResume", "No hay una ejecucion diaria pendiente despues de la reanudacion.",
            "service.executionStarted", "Ejecucion iniciada. Id={0}, origen={1}.",
            "service.configLoaded", "Configuracion cargada. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Recopilando inventario. Id={0}.",
            "service.inventoryCollected", "Inventario recopilado. Id={0}, caracteres={1}, fichero={2}.",
            "service.executionCompleted", "Ejecucion completada correctamente. Id={0}, origen={1}, duracionMs={2}, estado={3}.",
            "service.executionFailed", "Ejecucion fallida. Id={0}, origen={1}, fase={2}, duracionMs={3}.",
            "service.skippedStopping", "Ejecucion omitida porque el servicio se esta deteniendo. Origen={0}.",
            "service.skippedSatisfied", "Ejecucion omitida porque la solicitud ya esta satisfecha. Origen={0}, requeridaDesde={1}, ultimaCorrecta={2}.",
            "service.scheduled", "Ejecucion programada. Tipo={0}, prevista={1}, comprobacionMaxima={2}.",
            "service.timerElapsed", "Temporizador vencido. Tipo={0}, previsto={1}, real={2}, retraso={3}."
        });

        private static readonly Dictionary<string, string> Catalan = Build(new[]
        {
            "config.missing", "No existeix config.json",
            "config.tokenMissing", "L'Agent token no esta configurat a l'agent.",
            "config.uuidInvalid", "config.json conte un UUID no valid.",
            "state.invalidLastSuccess", "state.json no conte una data d'ultima execucio valida. Es considerara que l'execucio esta pendent.",
            "state.readFailed", "No s'ha pogut llegir state.json. Es considerara que l'execucio esta pendent",
            "logger.eventWriteFailed", "No s'ha pogut escriure l'esdeveniment {0} al registre Aplicacio de Windows: {1}",
            "program.usageRunOnce", "Us: RsAgent.exe --run-once",
            "program.usageService", "Com a servei, instal.la'l amb l'instal.lador Inno Setup o sc.exe.",
            "program.fatal", "Error fatal",
            "program.uninstallNoSystemLog", "Desinstal.lacio: no existeix cap System a Firulai per a l'UUID {0}. Es permet la desinstal.lacio local.",
            "program.uninstallNoSystemConsole", "No hi ha cap System enllacat a Firulai per a l'UUID {0}. La desinstal.lacio local continuara.",
            "program.uninstallDisconnectedLog", "Desinstal.lacio: System marcat com a Disconnected a Firulai per a l'UUID {0}. Resposta de Firulai: {1}",
            "program.uninstallDisconnectedConsole", "Sistema marcat com a inactiu a Firulai per a l'UUID {0}.",
            "rsm.httpStarted", "Enviament HTTP iniciat. Desti={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "Resposta HTTP rebuda. Estat={0} {1}, duradaMs={2}, caractersResposta={3}.",
            "rsm.httpFailed", "Firulai ha respost {0}: {1}",
            "rsm.invalidUrl", "URL no valida",
            "rsm.noSystemForUuid", "No existeix cap System a Firulai per a l'UUID {0}. S'omet l'actualitzacio remota i es permet la desinstal.lacio local.",
            "rsm.winHttpInitFailed", "No s'ha pogut inicialitzar WinHTTP per consultar Firulai.",
            "rsm.uuidSearchFailed", "Firulai ha respost {0} en cercar l'UUID: {1}",
            "rsm.statusUpdateFailed", "Firulai ha respost {0} en actualitzar l'estat del sistema: {1}",
            "service.triggerNone", "cap",
            "service.triggerStartupRecovery", "recuperacio-arrencada",
            "service.triggerServiceStart", "inici-servei",
            "service.triggerResumeRecovery", "recuperacio-represa",
            "service.triggerDailyScheduled", "programada-diaria",
            "service.triggerRetry", "reintent",
            "service.phasePreparation", "preparacio",
            "service.phaseConfigLoad", "carrega-configuracio",
            "service.phaseInventoryCollection", "recollida-inventari",
            "service.phaseHttpSend", "enviament-http",
            "service.phaseStateSave", "desat-estat",
            "service.unavailable", "no-disponible",
            "service.started", "Servei iniciat. Versio={0}, equip={1}, horaLocal={2}, zonaHoraria={3}, log={4}.",
            "service.stopped", "Servei aturat.",
            "service.shutdown", "Windows s'esta apagant; servei aturat.",
            "service.powerEvent", "Esdeveniment d'energia rebut. Estat={0}, horaLocal={1}, properaExecucio={2}, tipus={3}.",
            "service.resumeMissedRun", "Windows s'ha repres despres de perdre l'execucio de les 03:00. S'iniciara una execucio de recuperacio. Prevista={0}, represa={1}.",
            "service.noDailyPendingAfterResume", "No hi ha cap execucio diaria pendent despres de la represa.",
            "service.executionStarted", "Execucio iniciada. Id={0}, origen={1}.",
            "service.configLoaded", "Configuracio carregada. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Recollint inventari. Id={0}.",
            "service.inventoryCollected", "Inventari recollit. Id={0}, caracters={1}, fitxer={2}.",
            "service.executionCompleted", "Execucio completada correctament. Id={0}, origen={1}, duradaMs={2}, estat={3}.",
            "service.executionFailed", "Execucio fallida. Id={0}, origen={1}, fase={2}, duradaMs={3}.",
            "service.skippedStopping", "Execucio omesa perque el servei s'esta aturant. Origen={0}.",
            "service.skippedSatisfied", "Execucio omesa perque la sol.licitud ja esta satisfeta. Origen={0}, requeridaDesDe={1}, ultimaCorrecta={2}.",
            "service.scheduled", "Execucio programada. Tipus={0}, prevista={1}, comprovacioMaxima={2}.",
            "service.timerElapsed", "Temporitzador vençut. Tipus={0}, previst={1}, real={2}, retard={3}."
        });

        private static readonly Dictionary<string, string> Basque = Build(new[]
        {
            "config.missing", "config.json ez da existitzen",
            "config.tokenMissing", "Agent token ez dago agentean konfiguratuta.",
            "config.uuidInvalid", "config.json fitxategiak UUID baliogabea dauka.",
            "state.invalidLastSuccess", "state.json fitxategiak ez dauka azken exekuzio data baliozkorik. Exekuzioa zain dagoela hartuko da.",
            "state.readFailed", "Ezin izan da state.json irakurri. Exekuzioa zain dagoela hartuko da",
            "logger.eventWriteFailed", "Ezin izan da {0} gertaera Windows Aplikazio erregistroan idatzi: {1}",
            "program.usageRunOnce", "Erabilera: RsAgent.exe --run-once",
            "program.usageService", "Zerbitzu gisa, instalatu Inno Setup instalatzailearekin edo sc.exe erabiliz.",
            "program.fatal", "Errore larria",
            "program.uninstallNoSystemLog", "Desinstalazioa: ez dago Systemik Firulain {0} UUIDarentzat. Desinstalazio lokala baimentzen da.",
            "program.uninstallNoSystemConsole", "Ez dago Systemik Firulain {0} UUIDari lotuta. Desinstalazio lokalak jarraituko du.",
            "program.uninstallDisconnectedLog", "Desinstalazioa: System Disconnected gisa markatu da Firulain {0} UUIDarentzat. Firulairen erantzuna: {1}",
            "program.uninstallDisconnectedConsole", "Sistema inaktibo gisa markatu da Firulain {0} UUIDarentzat.",
            "rsm.httpStarted", "HTTP bidalketa hasi da. Helmuga={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "HTTP erantzuna jaso da. Egoera={0} {1}, iraupenaMs={2}, erantzunKaraktereak={3}.",
            "rsm.httpFailed", "Firulaik {0} erantzun du: {1}",
            "rsm.invalidUrl", "URL baliogabea",
            "rsm.noSystemForUuid", "Ez dago Systemik Firulain {0} UUIDarentzat. Urruneko eguneraketa saltatu eta desinstalazio lokala baimentzen da.",
            "rsm.winHttpInitFailed", "Ezin izan da WinHTTP hasieratu Firulai kontsultatzeko.",
            "rsm.uuidSearchFailed", "Firulaik {0} erantzun du UUIDa bilatzean: {1}",
            "rsm.statusUpdateFailed", "Firulaik {0} erantzun du sistemaren egoera eguneratzean: {1}",
            "service.triggerNone", "batere-ez",
            "service.triggerStartupRecovery", "abio-berreskuratzea",
            "service.triggerServiceStart", "zerbitzu-hasiera",
            "service.triggerResumeRecovery", "berrekite-berreskuratzea",
            "service.triggerDailyScheduled", "eguneroko-programatua",
            "service.triggerRetry", "berriro-saiatu",
            "service.phasePreparation", "prestaketa",
            "service.phaseConfigLoad", "konfigurazioa-kargatu",
            "service.phaseInventoryCollection", "inbentarioa-bildu",
            "service.phaseHttpSend", "http-bidalketa",
            "service.phaseStateSave", "egoera-gorde",
            "service.unavailable", "ez-erabilgarri",
            "service.started", "Zerbitzua hasi da. Bertsioa={0}, ekipoa={1}, orduLokala={2}, orduZona={3}, log={4}.",
            "service.stopped", "Zerbitzua gelditu da.",
            "service.shutdown", "Windows itzaltzen ari da; zerbitzua gelditu da.",
            "service.powerEvent", "Energia gertaera jaso da. Egoera={0}, orduLokala={1}, hurrengoExekuzioa={2}, mota={3}.",
            "service.resumeMissedRun", "Windows berriro hasi da 03:00etako exekuzioa galdu ondoren. Berreskuratze exekuzioa hasiko da. Aurreikusita={0}, berrabiarazita={1}.",
            "service.noDailyPendingAfterResume", "Ez dago eguneroko exekuziorik zain berrabiarazi ondoren.",
            "service.executionStarted", "Exekuzioa hasi da. Id={0}, jatorria={1}.",
            "service.configLoaded", "Konfigurazioa kargatu da. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Inbentarioa biltzen. Id={0}.",
            "service.inventoryCollected", "Inbentarioa bildu da. Id={0}, karaktereak={1}, fitxategia={2}.",
            "service.executionCompleted", "Exekuzioa ondo amaitu da. Id={0}, jatorria={1}, iraupenaMs={2}, egoera={3}.",
            "service.executionFailed", "Exekuzioak huts egin du. Id={0}, jatorria={1}, fasea={2}, iraupenaMs={3}.",
            "service.skippedStopping", "Exekuzioa saltatu da zerbitzua gelditzen ari delako. Jatorria={0}.",
            "service.skippedSatisfied", "Exekuzioa saltatu da eskaera dagoeneko beteta dagoelako. Jatorria={0}, beharrezkoaNoiztik={1}, azkenArrakasta={2}.",
            "service.scheduled", "Exekuzioa programatu da. Mota={0}, aurreikusita={1}, gehienezkoEgiaztapena={2}.",
            "service.timerElapsed", "Tenporizadorea iraungi da. Mota={0}, aurreikusita={1}, erreala={2}, atzerapena={3}."
        });

        private static readonly Dictionary<string, string> Galician = Build(new[]
        {
            "config.missing", "Non existe config.json",
            "config.tokenMissing", "Token do axente non configurado no axente.",
            "config.uuidInvalid", "UUID non valido en config.json.",
            "state.invalidLastSuccess", "state.json non conten unha data de ultima execucion valida. Considerarase que a execucion esta pendente.",
            "state.readFailed", "Non se puido ler state.json. Considerarase que a execucion esta pendente",
            "logger.eventWriteFailed", "Non se puido escribir o evento {0} no rexistro Aplicacion de Windows: {1}",
            "program.usageRunOnce", "Uso: RsAgent.exe --run-once",
            "program.usageService", "Como servizo, instalao co instalador Inno Setup ou sc.exe.",
            "program.fatal", "Fallo fatal",
            "program.uninstallNoSystemLog", "Desinstalacion: non existe ningun System en Firulai para o UUID {0}. Permitese a desinstalacion local.",
            "program.uninstallNoSystemConsole", "Non hai ningun System enlazado en Firulai para o UUID {0}. A desinstalacion local continuara.",
            "program.uninstallDisconnectedLog", "Desinstalacion: System marcado como Disconnected en Firulai para o UUID {0}. Resposta de Firulai: {1}",
            "program.uninstallDisconnectedConsole", "Sistema marcado como inactivo en Firulai para o UUID {0}.",
            "rsm.httpStarted", "Envio HTTP iniciado. Destino={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "Resposta HTTP recibida. Estado={0} {1}, duracionMs={2}, caracteresResposta={3}.",
            "rsm.httpFailed", "Firulai respondeu {0}: {1}",
            "rsm.invalidUrl", "URL non valida",
            "rsm.noSystemForUuid", "Non existe ningun System en Firulai para o UUID {0}. Omitese a actualizacion remota e permitese a desinstalacion local.",
            "rsm.winHttpInitFailed", "Non se puido inicializar WinHTTP para consultar Firulai.",
            "rsm.uuidSearchFailed", "Firulai respondeu {0} ao buscar o UUID: {1}",
            "rsm.statusUpdateFailed", "Firulai respondeu {0} ao actualizar o estado do sistema: {1}",
            "service.triggerNone", "ningun",
            "service.triggerStartupRecovery", "recuperacion-arranque",
            "service.triggerServiceStart", "inicio-servizo",
            "service.triggerResumeRecovery", "recuperacion-reanudacion",
            "service.triggerDailyScheduled", "programada-diaria",
            "service.triggerRetry", "reintento",
            "service.phasePreparation", "preparacion",
            "service.phaseConfigLoad", "carga-configuracion",
            "service.phaseInventoryCollection", "recollida-inventario",
            "service.phaseHttpSend", "envio-http",
            "service.phaseStateSave", "gardado-estado",
            "service.unavailable", "non-disponible",
            "service.started", "Servizo iniciado. Version={0}, equipo={1}, horaLocal={2}, zonaHoraria={3}, log={4}.",
            "service.stopped", "Servizo detido.",
            "service.shutdown", "Windows estase apagando; servizo detido.",
            "service.powerEvent", "Evento de enerxia recibido. Estado={0}, horaLocal={1}, proximaExecucion={2}, tipo={3}.",
            "service.resumeMissedRun", "Windows reanudouse despois de perder a execucion das 03:00. Iniciarase unha execucion de recuperacion. Prevista={0}, reanudacion={1}.",
            "service.noDailyPendingAfterResume", "Non hai unha execucion diaria pendente despois da reanudacion.",
            "service.executionStarted", "Execucion iniciada. Id={0}, orixe={1}.",
            "service.configLoaded", "Configuracion cargada. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Recollendo inventario. Id={0}.",
            "service.inventoryCollected", "Inventario recollido. Id={0}, caracteres={1}, ficheiro={2}.",
            "service.executionCompleted", "Execucion completada correctamente. Id={0}, orixe={1}, duracionMs={2}, estado={3}.",
            "service.executionFailed", "Execucion fallida. Id={0}, orixe={1}, fase={2}, duracionMs={3}.",
            "service.skippedStopping", "Execucion omitida porque o servizo estase detendo. Orixe={0}.",
            "service.skippedSatisfied", "Execucion omitida porque a solicitude xa esta satisfeita. Orixe={0}, requiridaDesde={1}, ultimaCorrecta={2}.",
            "service.scheduled", "Execucion programada. Tipo={0}, prevista={1}, comprobacionMaxima={2}.",
            "service.timerElapsed", "Temporizador vencido. Tipo={0}, previsto={1}, real={2}, atraso={3}."
        });

        private static readonly Dictionary<string, string> French = Build(new[]
        {
            "config.missing", "config.json n'existe pas",
            "config.tokenMissing", "Le token de l'agent n'est pas configure dans l'agent.",
            "config.uuidInvalid", "config.json contient un UUID non valide.",
            "state.invalidLastSuccess", "state.json ne contient pas de date de derniere execution valide. L'execution sera consideree comme en attente.",
            "state.readFailed", "Impossible de lire state.json. L'execution sera consideree comme en attente",
            "logger.eventWriteFailed", "Impossible d'ecrire l'evenement {0} dans le journal Application de Windows : {1}",
            "program.usageRunOnce", "Utilisation : RsAgent.exe --run-once",
            "program.usageService", "Comme service, installez-le avec l'installateur Inno Setup ou sc.exe.",
            "program.fatal", "Echec fatal",
            "program.uninstallNoSystemLog", "Desinstallation : aucun System n'existe dans Firulai pour l'UUID {0}. La desinstallation locale est autorisee.",
            "program.uninstallNoSystemConsole", "Aucun System n'est lie dans Firulai pour l'UUID {0}. La desinstallation locale va continuer.",
            "program.uninstallDisconnectedLog", "Desinstallation : System marque comme Disconnected dans Firulai pour l'UUID {0}. Reponse Firulai : {1}",
            "program.uninstallDisconnectedConsole", "Systeme marque comme inactif dans Firulai pour l'UUID {0}.",
            "rsm.httpStarted", "Envoi HTTP demarre. Destination={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "Reponse HTTP recue. Etat={0} {1}, dureeMs={2}, caracteresReponse={3}.",
            "rsm.httpFailed", "Firulai a repondu {0} : {1}",
            "rsm.invalidUrl", "URL non valide",
            "rsm.noSystemForUuid", "Aucun System n'existe dans Firulai pour l'UUID {0}. La mise a jour distante est ignoree et la desinstallation locale est autorisee.",
            "rsm.winHttpInitFailed", "Impossible d'initialiser WinHTTP pour interroger Firulai.",
            "rsm.uuidSearchFailed", "Firulai a repondu {0} lors de la recherche de l'UUID : {1}",
            "rsm.statusUpdateFailed", "Firulai a repondu {0} lors de la mise a jour de l'etat du systeme : {1}",
            "service.triggerNone", "aucun",
            "service.triggerStartupRecovery", "recuperation-demarrage",
            "service.triggerServiceStart", "demarrage-service",
            "service.triggerResumeRecovery", "recuperation-reprise",
            "service.triggerDailyScheduled", "planifiee-quotidienne",
            "service.triggerRetry", "nouvelle-tentative",
            "service.phasePreparation", "preparation",
            "service.phaseConfigLoad", "chargement-configuration",
            "service.phaseInventoryCollection", "collecte-inventaire",
            "service.phaseHttpSend", "envoi-http",
            "service.phaseStateSave", "enregistrement-etat",
            "service.unavailable", "indisponible",
            "service.started", "Service demarre. Version={0}, machine={1}, heureLocale={2}, fuseauHoraire={3}, log={4}.",
            "service.stopped", "Service arrete.",
            "service.shutdown", "Windows s'arrete; service arrete.",
            "service.powerEvent", "Evenement d'alimentation recu. Etat={0}, heureLocale={1}, prochaineExecution={2}, type={3}.",
            "service.resumeMissedRun", "Windows a repris apres avoir manque l'execution de 03:00. Une execution de recuperation va demarrer. Prevue={0}, reprise={1}.",
            "service.noDailyPendingAfterResume", "Aucune execution quotidienne n'est en attente apres la reprise.",
            "service.executionStarted", "Execution demarree. Id={0}, origine={1}.",
            "service.configLoaded", "Configuration chargee. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Collecte de l'inventaire. Id={0}.",
            "service.inventoryCollected", "Inventaire collecte. Id={0}, caracteres={1}, fichier={2}.",
            "service.executionCompleted", "Execution terminee correctement. Id={0}, origine={1}, dureeMs={2}, etat={3}.",
            "service.executionFailed", "Execution echouee. Id={0}, origine={1}, phase={2}, dureeMs={3}.",
            "service.skippedStopping", "Execution ignoree car le service s'arrete. Origine={0}.",
            "service.skippedSatisfied", "Execution ignoree car la demande est deja satisfaite. Origine={0}, requiseDepuis={1}, derniereReussite={2}.",
            "service.scheduled", "Execution planifiee. Type={0}, prevue={1}, verificationMaximale={2}.",
            "service.timerElapsed", "Minuteur expire. Type={0}, prevu={1}, reel={2}, retard={3}."
        });

        private static readonly Dictionary<string, string> German = Build(new[]
        {
            "config.missing", "config.json existiert nicht",
            "config.tokenMissing", "Agent token ist im Agenten nicht konfiguriert.",
            "config.uuidInvalid", "config.json enthaelt eine ungueltige UUID.",
            "state.invalidLastSuccess", "state.json enthaelt kein gueltiges Datum der letzten Ausfuehrung. Die Ausfuehrung gilt als ausstehend.",
            "state.readFailed", "state.json konnte nicht gelesen werden. Die Ausfuehrung gilt als ausstehend",
            "logger.eventWriteFailed", "Ereignis {0} konnte nicht in das Windows-Anwendungsprotokoll geschrieben werden: {1}",
            "program.usageRunOnce", "Verwendung: RsAgent.exe --run-once",
            "program.usageService", "Als Dienst mit dem Inno Setup Installer oder sc.exe installieren.",
            "program.fatal", "Schwerer Fehler",
            "program.uninstallNoSystemLog", "Deinstallation: In Firulai existiert kein System fuer UUID {0}. Lokale Deinstallation ist erlaubt.",
            "program.uninstallNoSystemConsole", "In Firulai ist kein System fuer UUID {0} verknuepft. Die lokale Deinstallation wird fortgesetzt.",
            "program.uninstallDisconnectedLog", "Deinstallation: System in Firulai fuer UUID {0} als Disconnected markiert. Firulai-Antwort: {1}",
            "program.uninstallDisconnectedConsole", "System in Firulai fuer UUID {0} als inaktiv markiert.",
            "rsm.httpStarted", "HTTP-Senden gestartet. Ziel={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "HTTP-Antwort empfangen. Status={0} {1}, dauerMs={2}, antwortZeichen={3}.",
            "rsm.httpFailed", "Firulai antwortete {0}: {1}",
            "rsm.invalidUrl", "Ungueltige URL",
            "rsm.noSystemForUuid", "In Firulai existiert kein System fuer UUID {0}. Remote-Aktualisierung wird uebersprungen und lokale Deinstallation ist erlaubt.",
            "rsm.winHttpInitFailed", "WinHTTP konnte fuer die Firulai-Abfrage nicht initialisiert werden.",
            "rsm.uuidSearchFailed", "Firulai antwortete {0} bei der Suche nach der UUID: {1}",
            "rsm.statusUpdateFailed", "Firulai antwortete {0} beim Aktualisieren des Systemstatus: {1}",
            "service.triggerNone", "keiner",
            "service.triggerStartupRecovery", "start-wiederherstellung",
            "service.triggerServiceStart", "dienststart",
            "service.triggerResumeRecovery", "fortsetzen-wiederherstellung",
            "service.triggerDailyScheduled", "taeglich-geplant",
            "service.triggerRetry", "wiederholung",
            "service.phasePreparation", "vorbereitung",
            "service.phaseConfigLoad", "konfiguration-laden",
            "service.phaseInventoryCollection", "inventar-sammeln",
            "service.phaseHttpSend", "http-senden",
            "service.phaseStateSave", "status-speichern",
            "service.unavailable", "nicht-verfuegbar",
            "service.started", "Dienst gestartet. Version={0}, Computer={1}, lokaleZeit={2}, zeitzone={3}, log={4}.",
            "service.stopped", "Dienst gestoppt.",
            "service.shutdown", "Windows wird heruntergefahren; Dienst gestoppt.",
            "service.powerEvent", "Energieereignis empfangen. Status={0}, lokaleZeit={1}, naechsteAusfuehrung={2}, typ={3}.",
            "service.resumeMissedRun", "Windows wurde fortgesetzt, nachdem die Ausfuehrung um 03:00 verpasst wurde. Eine Wiederherstellungsausfuehrung wird gestartet. Geplant={0}, fortgesetzt={1}.",
            "service.noDailyPendingAfterResume", "Nach dem Fortsetzen ist keine taegliche Ausfuehrung ausstehend.",
            "service.executionStarted", "Ausfuehrung gestartet. Id={0}, ursprung={1}.",
            "service.configLoaded", "Konfiguration geladen. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Inventar wird gesammelt. Id={0}.",
            "service.inventoryCollected", "Inventar gesammelt. Id={0}, zeichen={1}, datei={2}.",
            "service.executionCompleted", "Ausfuehrung erfolgreich abgeschlossen. Id={0}, ursprung={1}, dauerMs={2}, status={3}.",
            "service.executionFailed", "Ausfuehrung fehlgeschlagen. Id={0}, ursprung={1}, phase={2}, dauerMs={3}.",
            "service.skippedStopping", "Ausfuehrung uebersprungen, weil der Dienst gestoppt wird. Ursprung={0}.",
            "service.skippedSatisfied", "Ausfuehrung uebersprungen, weil die Anforderung bereits erfuellt ist. Ursprung={0}, erforderlichSeit={1}, letzterErfolg={2}.",
            "service.scheduled", "Ausfuehrung geplant. Typ={0}, geplant={1}, maximalePruefung={2}.",
            "service.timerElapsed", "Timer abgelaufen. Typ={0}, geplant={1}, tatsaechlich={2}, verzoegerung={3}."
        });

        private static readonly Dictionary<string, string> Italian = Build(new[]
        {
            "config.missing", "config.json non esiste",
            "config.tokenMissing", "Agent token non configurato nell'agente.",
            "config.uuidInvalid", "config.json contiene un UUID non valido.",
            "state.invalidLastSuccess", "state.json non contiene una data valida dell'ultima esecuzione. L'esecuzione sara considerata in sospeso.",
            "state.readFailed", "Impossibile leggere state.json. L'esecuzione sara considerata in sospeso",
            "logger.eventWriteFailed", "Impossibile scrivere l'evento {0} nel registro Applicazione di Windows: {1}",
            "program.usageRunOnce", "Uso: RsAgent.exe --run-once",
            "program.usageService", "Come servizio, installalo con il programma di installazione Inno Setup o sc.exe.",
            "program.fatal", "Errore fatale",
            "program.uninstallNoSystemLog", "Disinstallazione: non esiste alcun System in Firulai per UUID {0}. La disinstallazione locale e consentita.",
            "program.uninstallNoSystemConsole", "Nessun System e collegato in Firulai per UUID {0}. La disinstallazione locale continuera.",
            "program.uninstallDisconnectedLog", "Disinstallazione: System marcato come Disconnected in Firulai per UUID {0}. Risposta Firulai: {1}",
            "program.uninstallDisconnectedConsole", "Sistema marcato come inattivo in Firulai per UUID {0}.",
            "rsm.httpStarted", "Invio HTTP avviato. Destinazione={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "Risposta HTTP ricevuta. Stato={0} {1}, durataMs={2}, caratteriRisposta={3}.",
            "rsm.httpFailed", "Firulai ha risposto {0}: {1}",
            "rsm.invalidUrl", "URL non valido",
            "rsm.noSystemForUuid", "Non esiste alcun System in Firulai per UUID {0}. L'aggiornamento remoto viene ignorato e la disinstallazione locale e consentita.",
            "rsm.winHttpInitFailed", "Impossibile inizializzare WinHTTP per interrogare Firulai.",
            "rsm.uuidSearchFailed", "Firulai ha risposto {0} durante la ricerca dell'UUID: {1}",
            "rsm.statusUpdateFailed", "Firulai ha risposto {0} durante l'aggiornamento dello stato del sistema: {1}",
            "service.triggerNone", "nessuno",
            "service.triggerStartupRecovery", "recupero-avvio",
            "service.triggerServiceStart", "avvio-servizio",
            "service.triggerResumeRecovery", "recupero-ripresa",
            "service.triggerDailyScheduled", "giornaliera-programmata",
            "service.triggerRetry", "nuovo-tentativo",
            "service.phasePreparation", "preparazione",
            "service.phaseConfigLoad", "caricamento-configurazione",
            "service.phaseInventoryCollection", "raccolta-inventario",
            "service.phaseHttpSend", "invio-http",
            "service.phaseStateSave", "salvataggio-stato",
            "service.unavailable", "non-disponibile",
            "service.started", "Servizio avviato. Versione={0}, computer={1}, oraLocale={2}, fusoOrario={3}, log={4}.",
            "service.stopped", "Servizio arrestato.",
            "service.shutdown", "Windows si sta arrestando; servizio arrestato.",
            "service.powerEvent", "Evento di alimentazione ricevuto. Stato={0}, oraLocale={1}, prossimaEsecuzione={2}, tipo={3}.",
            "service.resumeMissedRun", "Windows e ripreso dopo aver perso l'esecuzione delle 03:00. Verra avviata un'esecuzione di recupero. Prevista={0}, ripresa={1}.",
            "service.noDailyPendingAfterResume", "Non ci sono esecuzioni giornaliere in sospeso dopo la ripresa.",
            "service.executionStarted", "Esecuzione avviata. Id={0}, origine={1}.",
            "service.configLoaded", "Configurazione caricata. Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "Raccolta inventario. Id={0}.",
            "service.inventoryCollected", "Inventario raccolto. Id={0}, caratteri={1}, file={2}.",
            "service.executionCompleted", "Esecuzione completata correttamente. Id={0}, origine={1}, durataMs={2}, stato={3}.",
            "service.executionFailed", "Esecuzione non riuscita. Id={0}, origine={1}, fase={2}, durataMs={3}.",
            "service.skippedStopping", "Esecuzione saltata perche il servizio si sta arrestando. Origine={0}.",
            "service.skippedSatisfied", "Esecuzione saltata perche la richiesta e gia soddisfatta. Origine={0}, richiestaDa={1}, ultimoSuccesso={2}.",
            "service.scheduled", "Esecuzione programmata. Tipo={0}, prevista={1}, controlloMassimo={2}.",
            "service.timerElapsed", "Timer scaduto. Tipo={0}, previsto={1}, reale={2}, ritardo={3}."
        });

        private static readonly Dictionary<string, string> Japanese = Build(new[]
        {
            "config.missing", "config.json が存在しません",
            "config.tokenMissing", "Agent token がエージェントに設定されていません。",
            "config.uuidInvalid", "config.json に無効な UUID が含まれています。",
            "state.invalidLastSuccess", "state.json に有効な最終実行日時がありません。この実行は保留中として扱われます。",
            "state.readFailed", "state.json を読み取れませんでした。この実行は保留中として扱われます",
            "logger.eventWriteFailed", "イベント {0} を Windows アプリケーション ログに書き込めませんでした: {1}",
            "program.usageRunOnce", "使用方法: RsAgent.exe --run-once",
            "program.usageService", "サービスとして使用するには、Inno Setup インストーラーまたは sc.exe でインストールしてください。",
            "program.fatal", "致命的なエラー",
            "program.uninstallNoSystemLog", "アンインストール: UUID {0} の System は Firulai に存在しません。ローカル アンインストールを許可します。",
            "program.uninstallNoSystemConsole", "UUID {0} にリンクされた System は Firulai にありません。ローカル アンインストールを続行します。",
            "program.uninstallDisconnectedLog", "アンインストール: UUID {0} の System を Firulai で Disconnected にしました。Firulai 応答: {1}",
            "program.uninstallDisconnectedConsole", "UUID {0} のシステムを Firulai で非アクティブにしました。",
            "rsm.httpStarted", "HTTP 送信を開始しました。送信先={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "HTTP 応答を受信しました。状態={0} {1}, durationMs={2}, responseCharacters={3}.",
            "rsm.httpFailed", "Firulai が {0} を返しました: {1}",
            "rsm.invalidUrl", "無効な URL",
            "rsm.noSystemForUuid", "UUID {0} の System は Firulai に存在しません。リモート更新をスキップし、ローカル アンインストールを許可します。",
            "rsm.winHttpInitFailed", "Firulai を照会するための WinHTTP を初期化できませんでした。",
            "rsm.uuidSearchFailed", "UUID 検索中に Firulai が {0} を返しました: {1}",
            "rsm.statusUpdateFailed", "システム状態の更新中に Firulai が {0} を返しました: {1}",
            "service.triggerNone", "なし",
            "service.triggerStartupRecovery", "起動時リカバリ",
            "service.triggerServiceStart", "サービス開始",
            "service.triggerResumeRecovery", "再開時リカバリ",
            "service.triggerDailyScheduled", "日次スケジュール",
            "service.triggerRetry", "再試行",
            "service.phasePreparation", "準備",
            "service.phaseConfigLoad", "設定読み込み",
            "service.phaseInventoryCollection", "インベントリ収集",
            "service.phaseHttpSend", "http送信",
            "service.phaseStateSave", "状態保存",
            "service.unavailable", "利用不可",
            "service.started", "サービスを開始しました。Version={0}, machine={1}, localTime={2}, timeZone={3}, log={4}.",
            "service.stopped", "サービスを停止しました。",
            "service.shutdown", "Windows がシャットダウン中です。サービスを停止しました。",
            "service.powerEvent", "電源イベントを受信しました。Status={0}, localTime={1}, nextExecution={2}, type={3}.",
            "service.resumeMissedRun", "03:00 の実行を逃した後に Windows が再開しました。リカバリ実行を開始します。Scheduled={0}, resumed={1}.",
            "service.noDailyPendingAfterResume", "再開後に保留中の日次実行はありません。",
            "service.executionStarted", "実行を開始しました。Id={0}, origin={1}.",
            "service.configLoaded", "設定を読み込みました。Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "インベントリを収集中です。Id={0}.",
            "service.inventoryCollected", "インベントリを収集しました。Id={0}, characters={1}, file={2}.",
            "service.executionCompleted", "実行が正常に完了しました。Id={0}, origin={1}, durationMs={2}, state={3}.",
            "service.executionFailed", "実行に失敗しました。Id={0}, origin={1}, phase={2}, durationMs={3}.",
            "service.skippedStopping", "サービス停止中のため実行をスキップしました。Origin={0}.",
            "service.skippedSatisfied", "要求は既に満たされているため実行をスキップしました。Origin={0}, requiredSince={1}, lastSuccess={2}.",
            "service.scheduled", "実行をスケジュールしました。Type={0}, scheduled={1}, maximumCheck={2}.",
            "service.timerElapsed", "タイマーが期限に達しました。Type={0}, scheduled={1}, actual={2}, delay={3}."
        });

        private static readonly Dictionary<string, string> Chinese = Build(new[]
        {
            "config.missing", "config.json 不存在",
            "config.tokenMissing", "Agent token 未在代理中配置。",
            "config.uuidInvalid", "config.json 包含无效的 UUID。",
            "state.invalidLastSuccess", "state.json 不包含有效的上次执行日期。将视为执行待处理。",
            "state.readFailed", "无法读取 state.json。将视为执行待处理",
            "logger.eventWriteFailed", "无法将事件 {0} 写入 Windows 应用程序日志: {1}",
            "program.usageRunOnce", "用法: RsAgent.exe --run-once",
            "program.usageService", "作为服务运行时，请使用 Inno Setup 安装程序或 sc.exe 安装。",
            "program.fatal", "致命错误",
            "program.uninstallNoSystemLog", "卸载: Firulai 中不存在 UUID {0} 对应的 System。允许本地卸载。",
            "program.uninstallNoSystemConsole", "Firulai 中没有链接到 UUID {0} 的 System。本地卸载将继续。",
            "program.uninstallDisconnectedLog", "卸载: 已在 Firulai 中将 UUID {0} 的 System 标记为 Disconnected。Firulai 响应: {1}",
            "program.uninstallDisconnectedConsole", "已在 Firulai 中将 UUID {0} 的系统标记为非活动。",
            "rsm.httpStarted", "HTTP 发送已开始。Destination={0}, timeout=30s, payloadBytes={1}.",
            "rsm.httpResponse", "已收到 HTTP 响应。Status={0} {1}, durationMs={2}, responseCharacters={3}.",
            "rsm.httpFailed", "Firulai 返回 {0}: {1}",
            "rsm.invalidUrl", "无效 URL",
            "rsm.noSystemForUuid", "Firulai 中不存在 UUID {0} 对应的 System。将跳过远程更新并允许本地卸载。",
            "rsm.winHttpInitFailed", "无法初始化 WinHTTP 以查询 Firulai。",
            "rsm.uuidSearchFailed", "搜索 UUID 时 Firulai 返回 {0}: {1}",
            "rsm.statusUpdateFailed", "更新系统状态时 Firulai 返回 {0}: {1}",
            "service.triggerNone", "无",
            "service.triggerStartupRecovery", "启动恢复",
            "service.triggerServiceStart", "服务启动",
            "service.triggerResumeRecovery", "恢复后补偿",
            "service.triggerDailyScheduled", "每日计划",
            "service.triggerRetry", "重试",
            "service.phasePreparation", "准备",
            "service.phaseConfigLoad", "加载配置",
            "service.phaseInventoryCollection", "收集清单",
            "service.phaseHttpSend", "http发送",
            "service.phaseStateSave", "保存状态",
            "service.unavailable", "不可用",
            "service.started", "服务已启动。Version={0}, machine={1}, localTime={2}, timeZone={3}, log={4}.",
            "service.stopped", "服务已停止。",
            "service.shutdown", "Windows 正在关机；服务已停止。",
            "service.powerEvent", "收到电源事件。Status={0}, localTime={1}, nextExecution={2}, type={3}.",
            "service.resumeMissedRun", "Windows 在错过 03:00 执行后恢复。将启动恢复执行。Scheduled={0}, resumed={1}.",
            "service.noDailyPendingAfterResume", "恢复后没有待处理的每日执行。",
            "service.executionStarted", "执行已开始。Id={0}, origin={1}.",
            "service.configLoaded", "配置已加载。Id={0}, uuid={1}, api={2}, locale={3}.",
            "service.collectingInventory", "正在收集清单。Id={0}.",
            "service.inventoryCollected", "清单已收集。Id={0}, characters={1}, file={2}.",
            "service.executionCompleted", "执行已成功完成。Id={0}, origin={1}, durationMs={2}, state={3}.",
            "service.executionFailed", "执行失败。Id={0}, origin={1}, phase={2}, durationMs={3}.",
            "service.skippedStopping", "由于服务正在停止，已跳过执行。Origin={0}.",
            "service.skippedSatisfied", "由于请求已满足，已跳过执行。Origin={0}, requiredSince={1}, lastSuccess={2}.",
            "service.scheduled", "执行已计划。Type={0}, scheduled={1}, maximumCheck={2}.",
            "service.timerElapsed", "计时器已到期。Type={0}, scheduled={1}, actual={2}, delay={3}."
        });

        public static string CurrentLocale { get { return LocaleForLanguage(_language); } }

        public static void SetLocale(string locale)
        {
            lock (Sync)
            {
                _language = NormalizeLanguage(locale);
            }
        }

        public static string NormalizeLocale(string locale)
        {
            return LocaleForLanguage(NormalizeLanguage(locale));
        }

        public static string T(string key, params object[] args)
        {
            var dictionary = GetDictionary();
            var text = dictionary.ContainsKey(key) ? dictionary[key] : English[key];
            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }

        private static Dictionary<string, string> GetDictionary()
        {
            switch (_language)
            {
                case "es": return Spanish;
                case "ca": return Catalan;
                case "eu": return Basque;
                case "gl": return Galician;
                case "fr": return French;
                case "de": return German;
                case "it": return Italian;
                case "ja": return Japanese;
                case "zh": return Chinese;
                default: return English;
            }
        }

        private static string NormalizeLanguage(string locale)
        {
            var value = (locale ?? "").Trim().ToLowerInvariant().Replace('_', '-');
            var language = value.Split('-')[0];
            switch (language)
            {
                case "en":
                case "es":
                case "ca":
                case "eu":
                case "gl":
                case "fr":
                case "de":
                case "it":
                case "ja":
                case "zh":
                    return language;
                default:
                    return "en";
            }
        }

        private static string LocaleForLanguage(string language)
        {
            switch (language)
            {
                case "es": return "es_ES";
                case "ca": return "ca_ES";
                case "eu": return "eu_ES";
                case "gl": return "gl_ES";
                case "fr": return "fr_FR";
                case "de": return "de_DE";
                case "it": return "it_IT";
                case "ja": return "ja_JP";
                case "zh": return "zh_CN";
                default: return "en_US";
            }
        }

        private static Dictionary<string, string> Build(string[] pairs)
        {
            var result = new Dictionary<string, string>();
            for (var i = 0; i < pairs.Length; i += 2)
            {
                result[pairs[i]] = pairs[i + 1];
            }

            return result;
        }

    }
}
