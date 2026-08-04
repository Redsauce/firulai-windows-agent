#define MyAppName "RSAgent"
#define MyAppDisplayName "Firulai Inventory Agent"
#define MyAppVersion "0.13.0"
#define MyAppPublisher "Redsauce"
#define MyAppExeName "RsAgent.exe"
#define DefaultApiUrl "https://rsm1.redsauce.net/AppController/commands_RSM/api/api.php"
#define RsmItemsGetUrl "https://rsm1.redsauce.net/AppController/commands_RSM/api/v2/items/get.php"
#define RsmItemsUpdateUrl "https://rsm1.redsauce.net/AppController/commands_RSM/api/v2/items/update.php"
#define RsmSystemHostnamePropertyId "1749"
#define RsmSystemFqdnPropertyId "1750"
#define RsmSystemUuidPropertyId "1780"
#define RsmSystemAliasPropertyId "1827"
#define RsmSystemHostnameStatusPropertyId "1751"
#define RsmSystemHostnameStatusActiveValue "Activo"
#define RsmAccountAgentTokenPropertyId "1790"
#define RsmAppUserAccountPropertyId "516"
#define RsmAppUserLocalePropertyId "1824"

[Setup]
AppId={{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}
AppName={#MyAppDisplayName}
AppVersion={#MyAppVersion}
AppVerName={cm:NameAndVersion,{#MyAppDisplayName},{#MyAppVersion}}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\RSAgent
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=..\Output
OutputBaseFilename=FirulaiAgent
Compression=zip
SolidCompression=no
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#MyAppDisplayName}
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Agente de inventario para Firulai
VersionInfoProductName={#MyAppDisplayName}
LicenseFile=LICENSE-es.txt
ShowLanguageDialog=no
UsePreviousLanguage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"; LicenseFile: "LICENSE-en.txt"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"; LicenseFile: "LICENSE-es.txt"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"; LicenseFile: "LICENSE-ca.txt"
Name: "basque"; MessagesFile: "Languages\Basque.isl"; LicenseFile: "LICENSE-eu.txt"
Name: "galician"; MessagesFile: "Languages\Galician.isl"; LicenseFile: "LICENSE-gl.txt"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"; LicenseFile: "LICENSE-fr.txt"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"; LicenseFile: "LICENSE-de.txt"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"; LicenseFile: "LICENSE-it.txt"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"; LicenseFile: "LICENSE-ja.txt"
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"; LicenseFile: "LICENSE-zh-CN.txt"

[CustomMessages]
english.RegisteringService=Registering service...
english.StartingService=Starting service...
english.ServiceDescription=Collects the system software inventory and sends it to Firulai
english.NameAndVersion=%1 version %2
spanish.RegisteringService=Registrando servicio...
spanish.StartingService=Arrancando servicio...
spanish.ServiceDescription=Recopila el inventario de software del sistema y lo envia a Firulai
spanish.NameAndVersion=%1 versión %2
catalan.RegisteringService=Registrant servei...
catalan.StartingService=Arrencant servei...
catalan.ServiceDescription=Recull l'inventari de programari del sistema i l'envia a Firulai
catalan.NameAndVersion=%1 versió %2
basque.RegisteringService=Zerbitzua erregistratzen...
basque.StartingService=Zerbitzua abiarazten...
basque.ServiceDescription=Sistemaren software inbentarioa bildu eta Firulaira bidaltzen du
basque.NameAndVersion=%1 %2 bertsioa
galician.RegisteringService=Rexistrando servizo...
galician.StartingService=Iniciando servizo...
galician.ServiceDescription=Recolle o inventario de software do sistema e envíao a Firulai
galician.NameAndVersion=%1 version %2
french.RegisteringService=Enregistrement du service...
french.StartingService=Demarrage du service...
french.ServiceDescription=Collecte l'inventaire logiciel du systeme et l'envoie a Firulai
french.NameAndVersion=%1 version %2
german.RegisteringService=Dienst wird registriert...
german.StartingService=Dienst wird gestartet...
german.ServiceDescription=Erfasst das Softwareinventar des Systems und sendet es an Firulai
german.NameAndVersion=%1 Version %2
italian.RegisteringService=Registrazione servizio...
italian.StartingService=Avvio servizio...
italian.ServiceDescription=Raccoglie l'inventario software del sistema e lo invia a Firulai
italian.NameAndVersion=%1 versione %2
japanese.RegisteringService=サービスを登録しています...
japanese.StartingService=サービスを開始しています...
japanese.ServiceDescription=システムのソフトウェア インベントリを収集して Firulai に送信します
japanese.NameAndVersion=%1 バージョン %2
chinesesimplified.RegisteringService=正在注册服务...
chinesesimplified.StartingService=正在启动服务...
chinesesimplified.ServiceDescription=收集系统软件清单并发送到 Firulai
chinesesimplified.NameAndVersion=%1 版本 %2

[Messages]
english.SetupAppTitle=Install
english.SetupWindowTitle=Install - %1
english.WelcomeLabel1=Welcome to the [name] Setup Wizard
english.WelcomeLabel2=This agent will collect this computer inventory and send the data to Firulai. Once installed, it will send the initial data to Firulai and repeat the send automatically every night.
english.ConfirmUninstall=%1 will be uninstalled.%n%nThis action will only remove the local agent files and the installer. Firulai data will not be deleted.%n%nThe system will be marked as inactive in Firulai. From Firulai you can permanently delete its data or reinstall the agent later by linking it to the already saved System and inventory.%n%nDo you want to continue?
spanish.SetupAppTitle=Instalar
spanish.SetupWindowTitle=Instalar - %1
spanish.WelcomeLabel1=Bienvenido al asistente de instalacion de [name]
spanish.WelcomeLabel2=Este agente se instalara para recopilar el inventario de este equipo y mandar los datos a Firulai. Una vez instalado, enviara los datos iniciales a Firulai y repetira el envio automaticamente cada noche.
spanish.ConfirmUninstall=Se va a desinstalar %1.%n%nEsta accion solo eliminara los archivos locales del agente junto al instalador. No se borraran los datos de Firulai.%n%nEl sistema quedara como inactivo en Firulai. Desde Firulai podras eliminar definitivamente sus datos o volver a instalar el agente mas adelante enlazandolo al System y al inventario ya guardados.%n%nQuieres continuar?
catalan.SetupAppTitle=Instal·lació
catalan.SetupWindowTitle=Instal·lació - %1
catalan.WelcomeLabel1=Benvingut a l'assistent d'instal.lacio de [name]
catalan.WelcomeLabel2=Aquest agent recollira l'inventari d'aquest equip i enviara les dades a Firulai. Un cop instal.lat, enviara les dades inicials a Firulai i repetira l'enviament automaticament cada nit.
catalan.ConfirmUninstall=Es desinstal.lara %1.%n%nAquesta accio nomes eliminara els fitxers locals de l'agent i l'instal.lador. Les dades de Firulai no s'eliminaran.%n%nEl sistema es marcara com a inactiu a Firulai. Des de Firulai podras eliminar definitivament les dades o reinstal.lar l'agent mes endavant enllacant-lo al System i a l'inventari ja desats.%n%nVols continuar?
basque.SetupAppTitle=Instalazioa
basque.SetupWindowTitle=Instalazioa - %1
basque.WelcomeLabel1=Ongi etorri [name] ezarpen morroira
basque.WelcomeLabel2=Agente honek ordenagailu honen inbentarioa bildu eta datuak Firulaira bidaliko ditu. Instalatu ondoren, hasierako datuak Firulaira bidaliko ditu eta bidalketa automatikoki errepikatuko du gauero.
basque.ConfirmUninstall=%1 desinstalatuko da.%n%nEkintza honek agentearen fitxategi lokalak eta instalatzailea bakarrik kenduko ditu. Firulaiko datuak ez dira ezabatuko.%n%nSistema inaktibo gisa markatuko da Firulain. Firulaitik datuak behin betiko ezabatu edo agentea gero berriro instalatu ahal izango duzu gordetako System eta inbentarioari lotuta.%n%nJarraitu nahi duzu?
galician.SetupAppTitle=Instalacion
galician.SetupWindowTitle=Instalacion - %1
galician.WelcomeLabel1=Benvido ao asistente de instalación de [name]
galician.WelcomeLabel2=Este axente recollera o inventario deste equipo e enviara os datos a Firulai. Unha vez instalado, enviara os datos iniciais a Firulai e repetira o envio automaticamente cada noite.
galician.ConfirmUninstall=Vaise desinstalar %1.%n%nEsta accion so eliminara os ficheiros locais do axente e o instalador. Non se borraran os datos de Firulai.%n%nO sistema quedara como inactivo en Firulai. Desde Firulai poderas eliminar definitivamente os seus datos ou volver instalar o axente mais adiante enlazandoo co System e co inventario xa gardados.%n%nQueres continuar?
french.SetupAppTitle=Installer
french.SetupWindowTitle=Installer - %1
french.WelcomeLabel1=Bienvenue dans l'assistant d'installation de [name]
french.WelcomeLabel2=Cet agent collectera l'inventaire de cet ordinateur et enverra les donnees a Firulai. Une fois installe, il enverra les donnees initiales a Firulai et repetera l'envoi automatiquement chaque nuit.
french.ConfirmUninstall=%1 va etre desinstalle.%n%nCette action supprimera uniquement les fichiers locaux de l'agent et l'installateur. Les donnees Firulai ne seront pas supprimees.%n%nLe systeme sera marque comme inactif dans Firulai. Depuis Firulai, vous pourrez supprimer definitivement ses donnees ou reinstaller l'agent plus tard en le liant au System et a l'inventaire deja enregistres.%n%nVoulez-vous continuer?
german.SetupAppTitle=Installieren
german.SetupWindowTitle=Installieren - %1
german.WelcomeLabel1=Willkommen beim Setup-Assistenten fuer [name]
german.WelcomeLabel2=Dieser Agent erfasst das Inventar dieses Computers und sendet die Daten an Firulai. Nach der Installation sendet er die ersten Daten an Firulai und wiederholt das Senden jede Nacht automatisch.
german.ConfirmUninstall=%1 wird deinstalliert.%n%nDiese Aktion entfernt nur die lokalen Agent-Dateien und das Installationsprogramm. Firulai-Daten werden nicht geloescht.%n%nDas System wird in Firulai als inaktiv markiert. In Firulai koennen Sie seine Daten endgueltig loeschen oder den Agenten spaeter erneut installieren, indem Sie ihn mit dem bereits gespeicherten System und Inventar verknuepfen.%n%nMoechten Sie fortfahren?
italian.SetupAppTitle=Installa
italian.SetupWindowTitle=Installa - %1
italian.WelcomeLabel1=Benvenuto nella procedura guidata di installazione di [name]
italian.WelcomeLabel2=Questo agente raccogliera l'inventario di questo computer e inviera i dati a Firulai. Una volta installato, inviera i dati iniziali a Firulai e ripetera l'invio automaticamente ogni notte.
italian.ConfirmUninstall=%1 verra disinstallato.%n%nQuesta azione rimuovera solo i file locali dell'agente e il programma di installazione. I dati Firulai non verranno eliminati.%n%nIl sistema verra marcato come inattivo in Firulai. Da Firulai potrai eliminare definitivamente i dati o reinstallare l'agente piu avanti collegandolo al System e all'inventario gia salvati.%n%nVuoi continuare?
japanese.SetupAppTitle=インストール
japanese.SetupWindowTitle=インストール - %1
japanese.WelcomeLabel1=[name] セットアップ ウィザードへようこそ
japanese.WelcomeLabel2=このエージェントは、このコンピューターのインベントリを収集して Firulai に送信します。インストール後、初回データを Firulai に送信し、その後は毎晩自動的に送信します。
japanese.ConfirmUninstall=%1 をアンインストールします。%n%nこの操作では、ローカルのエージェント ファイルとインストーラーのみを削除します。Firulai のデータは削除されません。%n%nシステムは Firulai で非アクティブとしてマークされます。Firulai からデータを完全に削除するか、あとで保存済みの System とインベントリにリンクしてエージェントを再インストールできます。%n%n続行しますか?
chinesesimplified.SetupAppTitle=安装
chinesesimplified.SetupWindowTitle=安装 - %1
chinesesimplified.WelcomeLabel1=欢迎使用 [name] 安装向导
chinesesimplified.WelcomeLabel2=此代理将收集此计算机的清单并将数据发送到 Firulai。安装后，它会向 Firulai 发送初始数据，并在之后每晚自动重复发送。
chinesesimplified.ConfirmUninstall=%1 将被卸载。%n%n此操作只会删除本地代理文件和安装程序。Firulai 数据不会被删除。%n%n系统将在 Firulai 中标记为非活动。你可以在 Firulai 中永久删除其数据，或稍后将代理重新链接到已保存的 System 和清单后再次安装。%n%n是否继续?

[Files]
Source: "..\src\RsAgent\bin\Release\RsAgent.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE-ca.txt"; Flags: dontcopy
Source: "LICENSE-de.txt"; Flags: dontcopy
Source: "LICENSE-en.txt"; Flags: dontcopy
Source: "LICENSE-es.txt"; Flags: dontcopy
Source: "LICENSE-eu.txt"; Flags: dontcopy
Source: "LICENSE-fr.txt"; Flags: dontcopy
Source: "LICENSE-gl.txt"; Flags: dontcopy
Source: "LICENSE-it.txt"; Flags: dontcopy
Source: "LICENSE-ja.txt"; Flags: dontcopy
Source: "LICENSE-zh-CN.txt"; Flags: dontcopy

[Dirs]
Name: "{commonappdata}\RSAgent"; Permissions: admins-full system-full
Name: "{commonappdata}\RSAgent\logs"; Permissions: admins-full system-full

[Registry]
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\Application\RSAgent"; ValueType: expandsz; ValueName: "EventMessageFile"; ValueData: "{win}\Microsoft.NET\Framework64\v4.0.30319\EventLogMessages.dll"
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\Application\RSAgent"; ValueType: dword; ValueName: "TypesSupported"; ValueData: "7"

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create RSAgent binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""{#MyAppDisplayName}"""; Flags: runhidden waituntilterminated; StatusMsg: "{cm:RegisteringService}"
Filename: "{sys}\sc.exe"; Parameters: "description RSAgent ""{cm:ServiceDescription}"""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "failure RSAgent reset= 86400 actions= restart/60000/restart/60000/""""/60000"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start RSAgent"; Flags: runhidden waituntilterminated; StatusMsg: "{cm:StartingService}"

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop RSAgent"; Flags: runhidden waituntilterminated; RunOnceId: "StopRSAgent"
Filename: "{sys}\sc.exe"; Parameters: "delete RSAgent"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteRSAgent"

[Code]
var
  ConfigPage: TInputQueryWizardPage;
  RsmSystemItemId: string;
  AgentLocale: string;

function IsUuid(Value: string): Boolean;
var
  I: Integer;
  C: Char;
begin
  Result := Length(Value) = 36;
  if not Result then Exit;

  for I := 1 to Length(Value) do
  begin
    C := Value[I];
    if (I = 9) or (I = 14) or (I = 19) or (I = 24) then
    begin
      if C <> '-' then
      begin
        Result := False;
        Exit;
      end;
    end
    else if not (((C >= '0') and (C <= '9')) or ((C >= 'a') and (C <= 'f')) or ((C >= 'A') and (C <= 'F'))) then
    begin
      Result := False;
      Exit;
    end;
  end;
end;

function JsonEscape(Value: string): string;
begin
  Result := Value;
  StringChangeEx(Result, '\', '\\', True);
  StringChangeEx(Result, '"', '\"', True);
end;

function CmdParam(Name: string): string;
begin
  Result := ExpandConstant('{param:' + Name + '|}');
end;

function NormalizeLocale(Value: string): string;
begin
  Value := Lowercase(Trim(Value));
  StringChangeEx(Value, '-', '_', True);
  if Pos('es', Value) = 1 then Result := 'es_ES'
  else if Pos('ca', Value) = 1 then Result := 'ca_ES'
  else if Pos('eu', Value) = 1 then Result := 'eu_ES'
  else if Pos('gl', Value) = 1 then Result := 'gl_ES'
  else if Pos('fr', Value) = 1 then Result := 'fr_FR'
  else if Pos('de', Value) = 1 then Result := 'de_DE'
  else if Pos('it', Value) = 1 then Result := 'it_IT'
  else if Pos('ja', Value) = 1 then Result := 'ja_JP'
  else if Pos('zh', Value) = 1 then Result := 'zh_CN'
  else Result := 'en_US';
end;

function LocaleFromInstallerFileName(): string;
var
  FileName: string;
  BaseName: string;
  Prefix: string;
  DotPos: Integer;
begin
  Result := '';
  FileName := Lowercase(ExtractFileName(ExpandConstant('{srcexe}')));
  DotPos := Pos('.', FileName);
  if DotPos > 0 then
    BaseName := Copy(FileName, 1, DotPos - 1)
  else
    BaseName := FileName;

  Prefix := 'firulaiagent-';
  if Pos(Prefix, BaseName) = 1 then
    Result := Copy(BaseName, Length(Prefix) + 1, Length(BaseName));

  if Result <> '' then Exit;

  Prefix := 'firulaiagent_';
  if Pos(Prefix, BaseName) = 1 then
    Result := Copy(BaseName, Length(Prefix) + 1, Length(BaseName));
end;

function InnoLanguageForLocale(Locale: string): string;
begin
  Locale := NormalizeLocale(Locale);
  if Locale = 'es_ES' then Result := 'spanish'
  else if Locale = 'ca_ES' then Result := 'catalan'
  else if Locale = 'eu_ES' then Result := 'basque'
  else if Locale = 'gl_ES' then Result := 'galician'
  else if Locale = 'fr_FR' then Result := 'french'
  else if Locale = 'de_DE' then Result := 'german'
  else if Locale = 'it_IT' then Result := 'italian'
  else if Locale = 'ja_JP' then Result := 'japanese'
  else if Locale = 'zh_CN' then Result := 'chinesesimplified'
  else Result := 'english';
end;

function QuotedParam(Name: string; Value: string): string;
begin
  Result := '';
  Value := Trim(Value);
  if Value = '' then Exit;
  StringChangeEx(Value, '"', '\"', True);
  Result := ' /' + Name + '="' + Value + '"';
end;

function RelaunchParameters(Language: string): string;
begin
  Result := '/LANG=' + Language + ' /LANGRELAUNCHED=yes' + QuotedParam('LOCALE', AgentLocale);
  Result := Result + QuotedParam('UUID', CmdParam('UUID'));
  Result := Result + QuotedParam('ALIAS', CmdParam('ALIAS'));
  Result := Result + QuotedParam('TOKEN', CmdParam('TOKEN'));
  Result := Result + QuotedParam('ACCEPTLICENSE', CmdParam('ACCEPTLICENSE'));
end;

function RelaunchWithResolvedLanguageIfNeeded(): Boolean;
var
  DesiredLanguage: string;
  ResultCode: Integer;
begin
  Result := False;
  if CompareText(Trim(CmdParam('LANGRELAUNCHED')), 'yes') = 0 then Exit;
  DesiredLanguage := InnoLanguageForLocale(AgentLocale);
  if CompareText(ActiveLanguage(), DesiredLanguage) = 0 then Exit;

  Result := Exec(
    ExpandConstant('{srcexe}'),
    RelaunchParameters(DesiredLanguage),
    '',
    SW_SHOWNORMAL,
    ewNoWait,
    ResultCode
  );
end;

function T(Key: string): string;
var
  Locale: string;
begin
  if AgentLocale = '' then
  begin
    Locale := Trim(CmdParam('LOCALE'));
    if Locale <> '' then AgentLocale := NormalizeLocale(Locale);
  end;
  if AgentLocale = '' then
  begin
    Locale := Trim(LocaleFromInstallerFileName());
    if Locale <> '' then AgentLocale := NormalizeLocale(Locale);
  end;
  if AgentLocale = '' then
    AgentLocale := NormalizeLocale(ActiveLanguage());

  if AgentLocale = 'es_ES' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'Para realizar una instalacion silenciosa debes leer y aceptar el Acuerdo de licencia y aviso de uso incluido con el instalador.' + #13#10#13#10 + 'Si lo aceptas, vuelve a ejecutar el instalador anadiendo /ACCEPTLICENSE=yes.'
    else if Key = 'configTitle' then Result := 'Configuracion de RSAgent'
    else if Key = 'configSubtitle' then Result := 'Introduce los datos facilitados por Firulai'
    else if Key = 'configDescription' then Result := 'Copia el UUID y el token que se te han facilitado en Firulai. Despues escribe un alias para identificar este equipo. El alias se guardara en Firulai y podras modificarlo mas adelante.'
    else if Key = 'aliasLabel' then Result := 'Alias del sistema:'
    else if Key = 'invalidUuid' then Result := 'Introduce un UUID valido con formato xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.'
    else if Key = 'aliasRequired' then Result := 'Introduce un alias para el sistema. Este campo es obligatorio para terminar la instalacion y podras modificarlo mas adelante en Firulai.'
    else if Key = 'tokenRequired' then Result := 'Introduce el Agent token facilitado junto al UUID. Es obligatorio para enlazar este agente con Firulai.'
    else if Key = 'missingUuidCli' then Result := 'UUID obligatorio o no valido. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o rellena los campos del asistente.'
    else if Key = 'missingAliasCli' then Result := 'Alias obligatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o rellena los campos del asistente. Podras modificarlo mas adelante en Firulai.'
    else if Key = 'missingTokenCli' then Result := 'Agent token obligatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o rellena los campos del asistente.'
    else if Key = 'replaceConfigFailed' then Result := 'No se pudo reemplazar '
    else if Key = 'replaceConfigAdvice' then Result := '. Cierra el instalador, ejecutalo como Administrador o elimina el archivo manualmente.'
    else if Key = 'writeConfigFailed' then Result := 'No se pudo escribir '
    else if Key = 'localInstalledSameUuid' then Result := 'Este sistema ya tiene un agente instalado con este UUID.'
    else if Key = 'localInstalledOther' then Result := 'Ya existe un agente instalado en este sistema.'
    else if Key = 'installedUuidLabel' then Result := 'UUID instalado actualmente: '
    else if Key = 'requestedUuidLabel' then Result := 'UUID solicitado: '
    else if Key = 'reinstallAdvice' then Result := 'Si necesitas reinstalar el agente, desinstala primero el agente actual desde Windows o ejecutando:'
    else if Key = 'uuidValidateFailed' then Result := 'No se pudo validar el UUID en Firulai. Comprueba la conexion a internet y que el Agent token sea correcto. La instalacion no continuara sin confirmar que el UUID esta disponible.'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai no permitio validar el UUID'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'Comprueba que el token corresponde al UUID facilitado en Firulai. La instalacion no continuara sin confirmar que el UUID esta disponible.'
    else if Key = 'responseLabel' then Result := 'Respuesta: '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID invalido: no existe en Firulai.'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'No se puede instalar el agente con un UUID que no haya sido generado desde Firulai.'
    else if Key = 'uuidLabel' then Result := 'UUID: '
    else if Key = 'systemLookupFailed' then Result := 'No se pudo localizar el sistema de Firulai asociado al UUID.'
    else if Key = 'systemLookupSaveAliasSafety' then Result := 'Por seguridad, la instalacion no continuara sin poder guardar el alias.'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'Este sistema ya tiene un agente instalado en Firulai con este UUID.'
    else if Key = 'duplicateUuidInstallForbidden' then Result := 'No se puede realizar una nueva instalacion con el mismo UUID.'
    else if Key = 'firulaiSystemLabel' then Result := 'Sistema en Firulai:'
    else if Key = 'localComputerLabel' then Result := 'Equipo local:'
    else if Key = 'uuidBelongsOther' then Result := 'Este UUID ya pertenece a otro sistema en Firulai.'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'No se puede instalar este agente en el equipo local con ese UUID.'
    else if Key = 'aliasSaveNoSystem' then Result := 'No se pudo guardar el alias en Firulai porque no se encontro el sistema asociado al UUID.'
    else if Key = 'aliasSaveFailed' then Result := 'No se pudo guardar el alias en Firulai. Comprueba la conexion y el token. La instalacion no continuara sin confirmar el alias.'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai no permitio guardar el alias'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'Comprueba que el token corresponde al UUID facilitado. La instalacion no continuara sin confirmar el alias.'
    else if Key = 'statusUpdateNoSystem' then Result := 'No se pudo actualizar el estado en Firulai porque no se encontro el sistema asociado al UUID.'
    else if Key = 'statusUpdateFailed' then Result := 'No se pudo actualizar el estado en Firulai. Comprueba la conexion y el token. La instalacion no continuara sin activar el sistema.'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai no permitio actualizar el estado'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'Comprueba que el token corresponde al UUID facilitado. La instalacion no continuara sin activar el sistema.'
    else if Key = 'uninstallContactFailed' then Result := 'No se pudo contactar con el agente para marcar el sistema como inactivo en Firulai. La desinstalacion se cancelara para que el estado remoto no quede desactualizado.'
    else if Key = 'uninstallUpdateFailed' then Result := 'No se pudo marcar el sistema como inactivo en Firulai. Revisa la conectividad con Firulai y vuelve a desinstalar.'
    else if Key = 'uninstallSuccess' then Result := 'El sistema se ha marcado como inactivo en Firulai. No se borraran los datos guardados; podras eliminarlos desde Firulai o volver a instalar el agente mas adelante enlazandolo a este mismo System.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'ca_ES' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'Per fer una instal.lacio silenciosa has de llegir i acceptar acord de llicencia i avis dus inclosos amb instal.lador.' + #13#10#13#10 + 'Si ho acceptes, torna a executar instal.lador afegint /ACCEPTLICENSE=yes.'
    else if Key = 'configTitle' then Result := 'Configuracio de RSAgent'
    else if Key = 'configSubtitle' then Result := 'Introdueix les dades facilitades per Firulai'
    else if Key = 'configDescription' then Result := 'Copia UUID i token facilitats per Firulai. Despres escriu un alias per identificar aquest equip. Alias es desara a Firulai i el podras modificar mes endavant.'
    else if Key = 'aliasLabel' then Result := 'Alias del sistema:'
    else if Key = 'invalidUuid' then Result := 'Introdueix un UUID valid amb format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.'
    else if Key = 'aliasRequired' then Result := 'Introdueix un alias per al sistema. Aquest camp es obligatori per acabar la instal.lacio i el podras modificar mes endavant a Firulai.'
    else if Key = 'tokenRequired' then Result := 'Introdueix Agent token facilitat amb UUID. Es obligatori per enllacar aquest agent amb Firulai.'
    else if Key = 'missingUuidCli' then Result := 'UUID obligatori o no valid. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o omple els camps de assistent.'
    else if Key = 'missingAliasCli' then Result := 'Alias obligatori. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o omple els camps de assistent. El podras modificar mes endavant a Firulai.'
    else if Key = 'missingTokenCli' then Result := 'Agent token obligatori. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o omple els camps de assistent.'
    else if Key = 'replaceConfigFailed' then Result := 'No es pot substituir '
    else if Key = 'replaceConfigAdvice' then Result := '. Tanca instal.lador, executa com a Administrador o elimina el fitxer manualment.'
    else if Key = 'writeConfigFailed' then Result := 'No es pot escriure '
    else if Key = 'localInstalledSameUuid' then Result := 'Aquest sistema ja te un agent instal.lat amb aquest UUID.'
    else if Key = 'localInstalledOther' then Result := 'Ja existeix un agent instal.lat en aquest sistema.'
    else if Key = 'installedUuidLabel' then Result := 'UUID instal.lat actualment: '
    else if Key = 'requestedUuidLabel' then Result := 'UUID sol.licitat: '
    else if Key = 'reinstallAdvice' then Result := 'Si necessites reinstal.lar agent, desinstal.la primer agent actual des de Windows o executant:'
    else if Key = 'uuidValidateFailed' then Result := 'No es pot validar UUID a Firulai. Comprova la connexio a internet i que Agent token sigui correcte. La instal.lacio no continuara sense confirmar que UUID esta disponible.'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai no ha permes validar UUID'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'Comprova que el token correspon a UUID facilitat a Firulai. La instal.lacio no continuara sense confirmar que UUID esta disponible.'
    else if Key = 'responseLabel' then Result := 'Resposta: '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID no valid: no existeix a Firulai.'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'No es pot instal.lar agent amb un UUID que no hagi estat generat des de Firulai.'
    else if Key = 'uuidLabel' then Result := 'UUID: '
    else if Key = 'systemLookupFailed' then Result := 'No es pot localitzar sistema de Firulai associat a UUID.'
    else if Key = 'systemLookupSaveAliasSafety' then Result := 'Per seguretat, la instal.lacio no continuara sense poder desar alias.'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'Aquest sistema ja te un agent instal.lat a Firulai amb aquest UUID.'
    else if Key = 'duplicateUuidInstallForbidden' then Result := 'No es pot fer una nova instal.lacio amb el mateix UUID.'
    else if Key = 'firulaiSystemLabel' then Result := 'Sistema a Firulai:'
    else if Key = 'localComputerLabel' then Result := 'Equip local:'
    else if Key = 'uuidBelongsOther' then Result := 'Aquest UUID ja pertany a un altre sistema a Firulai.'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'No es pot instal.lar aquest agent en equip local amb aquest UUID.'
    else if Key = 'aliasSaveNoSystem' then Result := 'No es pot desar alias a Firulai perque no s ha trobat sistema associat a UUID.'
    else if Key = 'aliasSaveFailed' then Result := 'No es pot desar alias a Firulai. Comprova la connexio i el token. La instal.lacio no continuara sense confirmar alias.'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai no ha permes desar alias'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'Comprova que el token correspon a UUID facilitat. La instal.lacio no continuara sense confirmar alias.'
    else if Key = 'statusUpdateNoSystem' then Result := 'No es pot actualitzar estat a Firulai perque no s ha trobat sistema associat a UUID.'
    else if Key = 'statusUpdateFailed' then Result := 'No es pot actualitzar estat a Firulai. Comprova la connexio i el token. La instal.lacio no continuara sense activar el sistema.'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai no ha permes actualitzar estat'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'Comprova que el token correspon a UUID facilitat. La instal.lacio no continuara sense activar el sistema.'
    else if Key = 'uninstallContactFailed' then Result := 'No es pot contactar amb agent per marcar sistema com a inactiu a Firulai. La desinstal.lacio es cancel.lara per evitar estat remot desactualitzat.'
    else if Key = 'uninstallUpdateFailed' then Result := 'No es pot marcar sistema com a inactiu a Firulai. Revisa la connectivitat amb Firulai i torna a desinstal.lar.'
    else if Key = 'uninstallSuccess' then Result := 'El sistema s ha marcat com a inactiu a Firulai. No s esborraran les dades desades; podras eliminar-les des de Firulai o tornar a instal.lar agent mes endavant enllacant-lo a aquest mateix System.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'eu_ES' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'Instalazio isila egiteko, instalatzailearekin datorren lizentzia-akordioa eta erabilera-oharra irakurri eta onartu behar dituzu.' + #13#10#13#10 + 'Onartzen baduzu, exekutatu berriro instalatzailea /ACCEPTLICENSE=yes gehituta.'
    else if Key = 'configTitle' then Result := 'RSAgent konfigurazioa'
    else if Key = 'configSubtitle' then Result := 'Sartu Firulaik emandako datuak'
    else if Key = 'configDescription' then Result := 'Kopiatu Firulaik emandako UUIDa eta tokena. Ondoren idatzi ekipo hau identifikatzeko aliasa. Aliasa Firulain gordeko da eta gero aldatu ahal izango duzu.'
    else if Key = 'aliasLabel' then Result := 'Sistemaren aliasa:'
    else if Key = 'invalidUuid' then Result := 'Sartu UUID balioduna xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx formatuarekin.'
    else if Key = 'aliasRequired' then Result := 'Sartu sistemaren aliasa. Eremu hau derrigorrezkoa da instalazioa amaitzeko eta gero Firulain aldatu ahal izango duzu.'
    else if Key = 'tokenRequired' then Result := 'Sartu UUIDarekin batera emandako Agent tokena. Derrigorrezkoa da agente hau Firulairekin lotzeko.'
    else if Key = 'missingUuidCli' then Result := 'UUIDa derrigorrezkoa da edo ez da baliozkoa. Erabili setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> edo bete morroiaren eremuak.'
    else if Key = 'missingAliasCli' then Result := 'Aliasa derrigorrezkoa da. Erabili setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> edo bete morroiaren eremuak. Gero Firulain aldatu ahal izango duzu.'
    else if Key = 'missingTokenCli' then Result := 'Agent tokena derrigorrezkoa da. Erabili setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> edo bete morroiaren eremuak.'
    else if Key = 'replaceConfigFailed' then Result := 'Ezin izan da ordezkatu '
    else if Key = 'replaceConfigAdvice' then Result := '. Itxi instalatzailea, exekutatu Administratzaile gisa edo ezabatu fitxategia eskuz.'
    else if Key = 'writeConfigFailed' then Result := 'Ezin izan da idatzi '
    else if Key = 'localInstalledSameUuid' then Result := 'Sistema honek dagoeneko agente bat dauka instalatuta UUID honekin.'
    else if Key = 'localInstalledOther' then Result := 'Sistema honetan agente bat instalatuta dago dagoeneko.'
    else if Key = 'installedUuidLabel' then Result := 'Une honetan instalatutako UUIDa: '
    else if Key = 'requestedUuidLabel' then Result := 'Eskatutako UUIDa: '
    else if Key = 'reinstallAdvice' then Result := 'Agentea berriro instalatu behar baduzu, desinstalatu lehenik uneko agentea Windowsetik edo hau exekutatuz:'
    else if Key = 'uuidValidateFailed' then Result := 'Ezin izan da UUIDa baliozkotu Firulain. Egiaztatu interneteko konexioa eta Agent tokena zuzena dela. Instalazioak ez du jarraituko UUIDa erabilgarri dagoela baieztatu arte.'
    else if Key = 'uuidValidateDenied' then Result := 'Firulaik ez du UUIDa baliozkotzen utzi'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'Egiaztatu tokena Firulaik emandako UUIDari dagokiola. Instalazioak ez du jarraituko UUIDa erabilgarri dagoela baieztatu arte.'
    else if Key = 'responseLabel' then Result := 'Erantzuna: '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID baliogabea: ez da Firulain existitzen.'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'Ezin da agentea instalatu Firulaitik sortu ez den UUID batekin.'
    else if Key = 'uuidLabel' then Result := 'UUIDa: '
    else if Key = 'systemLookupFailed' then Result := 'Ezin izan da UUIDari lotutako Firulai sistema aurkitu.'
    else if Key = 'systemLookupSaveAliasSafety' then Result := 'Segurtasunagatik, instalazioak ez du jarraituko aliasa gorde ezin bada.'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'Sistema honek dagoeneko agente bat dauka Firulain UUID honekin.'
    else if Key = 'duplicateUuidInstallForbidden' then Result := 'Ezin da instalazio berri bat egin UUID berarekin.'
    else if Key = 'firulaiSystemLabel' then Result := 'Firulaiko sistema:'
    else if Key = 'localComputerLabel' then Result := 'Ekipo lokala:'
    else if Key = 'uuidBelongsOther' then Result := 'UUID hau Firulaiko beste sistema batena da dagoeneko.'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'Ezin da agente hau ekipo lokalean instalatu UUID horrekin.'
    else if Key = 'aliasSaveNoSystem' then Result := 'Ezin izan da aliasa Firulain gorde, ez delako UUIDari lotutako sistema aurkitu.'
    else if Key = 'aliasSaveFailed' then Result := 'Ezin izan da aliasa Firulain gorde. Egiaztatu konexioa eta tokena. Instalazioak ez du jarraituko aliasa baieztatu arte.'
    else if Key = 'aliasSaveDenied' then Result := 'Firulaik ez du aliasa gordetzen utzi'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'Egiaztatu tokena emandako UUIDari dagokiola. Instalazioak ez du jarraituko aliasa baieztatu arte.'
    else if Key = 'statusUpdateNoSystem' then Result := 'Ezin izan da egoera Firulain eguneratu, ez delako UUIDari lotutako sistema aurkitu.'
    else if Key = 'statusUpdateFailed' then Result := 'Ezin izan da egoera Firulain eguneratu. Egiaztatu konexioa eta tokena. Instalazioak ez du jarraituko sistema aktibatu arte.'
    else if Key = 'statusUpdateDenied' then Result := 'Firulaik ez du egoera eguneratzen utzi'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'Egiaztatu tokena emandako UUIDari dagokiola. Instalazioak ez du jarraituko sistema aktibatu arte.'
    else if Key = 'uninstallContactFailed' then Result := 'Ezin izan da agentearekin kontaktatu sistema Firulain inaktibo markatzeko. Desinstalazioa bertan behera utziko da urruneko egoera zaharkituta gera ez dadin.'
    else if Key = 'uninstallUpdateFailed' then Result := 'Ezin izan da sistema Firulain inaktibo markatu. Egiaztatu Firulairekin konektagarritasuna eta desinstalatu berriro.'
    else if Key = 'uninstallSuccess' then Result := 'Sistema Firulain inaktibo markatu da. Gordetako datuak ez dira ezabatuko; Firulaitik ezabatu edo agentea gero berriro instalatu ahal izango duzu System berari lotuta.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'gl_ES' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'Para realizar unha instalacion silenciosa debes ler e aceptar o acordo de licenza e aviso de uso incluido co instalador.' + #13#10#13#10 + 'Se o aceptas, executa de novo o instalador engadindo /ACCEPTLICENSE=yes.'
    else if Key = 'configTitle' then Result := 'Configuracion de RSAgent'
    else if Key = 'configSubtitle' then Result := 'Introduce os datos facilitados por Firulai'
    else if Key = 'configDescription' then Result := 'Copia o UUID e o token facilitados por Firulai. Despois escribe un alias para identificar este equipo. O alias gardarase en Firulai e poderas modificalo mais adiante.'
    else if Key = 'aliasLabel' then Result := 'Alias do sistema:'
    else if Key = 'invalidUuid' then Result := 'Introduce un UUID valido con formato xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.'
    else if Key = 'aliasRequired' then Result := 'Introduce un alias para o sistema. Este campo e obrigatorio para rematar a instalacion e poderas modificalo mais adiante en Firulai.'
    else if Key = 'tokenRequired' then Result := 'Introduce o Agent token facilitado xunto co UUID. E obrigatorio para enlazar este axente con Firulai.'
    else if Key = 'missingUuidCli' then Result := 'UUID obrigatorio ou non valido. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> ou cobre os campos do asistente.'
    else if Key = 'missingAliasCli' then Result := 'Alias obrigatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> ou cobre os campos do asistente. Poderas modificalo mais adiante en Firulai.'
    else if Key = 'missingTokenCli' then Result := 'Agent token obrigatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> ou cobre os campos do asistente.'
    else if Key = 'replaceConfigFailed' then Result := 'Non se puido substituír '
    else if Key = 'replaceConfigAdvice' then Result := '. Pecha o instalador, executao como Administrador ou elimina o ficheiro manualmente.'
    else if Key = 'writeConfigFailed' then Result := 'Non se puido escribir '
    else if Key = 'localInstalledSameUuid' then Result := 'Este sistema xa ten un axente instalado con este UUID.'
    else if Key = 'localInstalledOther' then Result := 'Xa existe un axente instalado neste sistema.'
    else if Key = 'installedUuidLabel' then Result := 'UUID instalado actualmente: '
    else if Key = 'requestedUuidLabel' then Result := 'UUID solicitado: '
    else if Key = 'reinstallAdvice' then Result := 'Se precisas reinstalar o axente, desinstala primeiro o axente actual desde Windows ou executando:'
    else if Key = 'uuidValidateFailed' then Result := 'Non se puido validar o UUID en Firulai. Comproba a conexion a internet e que o Agent token sexa correcto. A instalacion non continuara sen confirmar que o UUID esta dispoñible.'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai non permitiu validar o UUID'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'Comproba que o token corresponde ao UUID facilitado en Firulai. A instalacion non continuara sen confirmar que o UUID esta dispoñible.'
    else if Key = 'responseLabel' then Result := 'Resposta: '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID non valido: non existe en Firulai.'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'Non se pode instalar o axente cun UUID que non fose xerado desde Firulai.'
    else if Key = 'uuidLabel' then Result := 'UUID: '
    else if Key = 'systemLookupFailed' then Result := 'Non se puido localizar o sistema de Firulai asociado ao UUID.'
    else if Key = 'systemLookupSaveAliasSafety' then Result := 'Por seguridade, a instalacion non continuara sen poder gardar o alias.'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'Este sistema xa ten un axente instalado en Firulai con este UUID.'
    else if Key = 'duplicateUuidInstallForbidden' then Result := 'Non se pode realizar unha nova instalacion co mesmo UUID.'
    else if Key = 'firulaiSystemLabel' then Result := 'Sistema en Firulai:'
    else if Key = 'localComputerLabel' then Result := 'Equipo local:'
    else if Key = 'uuidBelongsOther' then Result := 'Este UUID xa pertence a outro sistema en Firulai.'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'Non se pode instalar este axente no equipo local con ese UUID.'
    else if Key = 'aliasSaveNoSystem' then Result := 'Non se puido gardar o alias en Firulai porque non se atopou o sistema asociado ao UUID.'
    else if Key = 'aliasSaveFailed' then Result := 'Non se puido gardar o alias en Firulai. Comproba a conexion e o token. A instalacion non continuara sen confirmar o alias.'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai non permitiu gardar o alias'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'Comproba que o token corresponde ao UUID facilitado. A instalacion non continuara sen confirmar o alias.'
    else if Key = 'statusUpdateNoSystem' then Result := 'Non se puido actualizar o estado en Firulai porque non se atopou o sistema asociado ao UUID.'
    else if Key = 'statusUpdateFailed' then Result := 'Non se puido actualizar o estado en Firulai. Comproba a conexion e o token. A instalacion non continuara sen activar o sistema.'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai non permitiu actualizar o estado'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'Comproba que o token corresponde ao UUID facilitado. A instalacion non continuara sen activar o sistema.'
    else if Key = 'uninstallContactFailed' then Result := 'Non se puido contactar co axente para marcar o sistema como inactivo en Firulai. A desinstalacion cancelarase para que o estado remoto non quede desactualizado.'
    else if Key = 'uninstallUpdateFailed' then Result := 'Non se puido marcar o sistema como inactivo en Firulai. Revisa a conectividade con Firulai e volve desinstalar.'
    else if Key = 'uninstallSuccess' then Result := 'O sistema marcouse como inactivo en Firulai. Non se borraran os datos gardados; poderas eliminalos desde Firulai ou volver instalar o axente mais adiante enlazandoo a este mesmo System.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'fr_FR' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'Pour une installation silencieuse, vous devez lire et accepter le contrat de licence et la notice utilisation inclus avec le programme.' + #13#10#13#10 + 'Si vous acceptez, relancez le programme avec /ACCEPTLICENSE=yes.'
    else if Key = 'configTitle' then Result := 'Configuration de RSAgent'
    else if Key = 'configSubtitle' then Result := 'Saisissez les informations fournies par Firulai'
    else if Key = 'configDescription' then Result := 'Copiez le UUID et le token fournis par Firulai. Saisissez ensuite un alias pour identifier cet ordinateur. Cet alias sera enregistre dans Firulai et pourra etre modifie plus tard.'
    else if Key = 'aliasLabel' then Result := 'Alias du systeme:'
    else if Key = 'invalidUuid' then Result := 'Saisissez un UUID valide au format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.'
    else if Key = 'aliasRequired' then Result := 'Saisissez un alias pour le systeme. Ce champ est obligatoire pour terminer installation et pourra etre modifie plus tard dans Firulai.'
    else if Key = 'tokenRequired' then Result := 'Saisissez le Agent token fourni avec le UUID. Il est obligatoire pour lier cet agent a Firulai.'
    else if Key = 'missingUuidCli' then Result := 'UUID obligatoire ou non valide. Utilisez setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> ou remplissez les champs de assistant.'
    else if Key = 'missingAliasCli' then Result := 'Alias obligatoire. Utilisez setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> ou remplissez les champs de assistant. Vous pourrez le modifier plus tard dans Firulai.'
    else if Key = 'missingTokenCli' then Result := 'Agent token obligatoire. Utilisez setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> ou remplissez les champs de assistant.'
    else if Key = 'replaceConfigFailed' then Result := 'Impossible de remplacer '
    else if Key = 'replaceConfigAdvice' then Result := '. Fermez le programme, executez en tant que Administrateur ou supprimez le fichier manuellement.'
    else if Key = 'writeConfigFailed' then Result := 'Impossible decrire '
    else if Key = 'localInstalledSameUuid' then Result := 'Ce systeme a deja un agent installe avec cet UUID.'
    else if Key = 'localInstalledOther' then Result := 'Un agent est deja installe sur ce systeme.'
    else if Key = 'installedUuidLabel' then Result := 'UUID actuellement installe : '
    else if Key = 'requestedUuidLabel' then Result := 'UUID demande : '
    else if Key = 'reinstallAdvice' then Result := 'Si vous devez reinstaller agent, desinstallez d abord agent actuel depuis Windows ou en executant :'
    else if Key = 'uuidValidateFailed' then Result := 'Impossible de valider le UUID dans Firulai. Verifiez la connexion internet et le Agent token. Installation ne continuera pas sans confirmer que le UUID est disponible.'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai a refuse la validation du UUID'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'Verifiez que le token correspond au UUID fourni dans Firulai. Installation ne continuera pas sans confirmer que le UUID est disponible.'
    else if Key = 'responseLabel' then Result := 'Reponse : '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID non valide : il n existe pas dans Firulai.'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'Impossible installer agent avec un UUID qui n a pas ete genere depuis Firulai.'
    else if Key = 'uuidLabel' then Result := 'UUID : '
    else if Key = 'systemLookupFailed' then Result := 'Impossible de localiser le systeme Firulai associe au UUID.'
    else if Key = 'systemLookupSaveAliasSafety' then Result := 'Par securite, installation ne continuera pas sans pouvoir enregistrer alias.'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'Ce systeme a deja un agent installe dans Firulai avec cet UUID.'
    else if Key = 'duplicateUuidInstallForbidden' then Result := 'Impossible effectuer une nouvelle installation avec le meme UUID.'
    else if Key = 'firulaiSystemLabel' then Result := 'Systeme dans Firulai :'
    else if Key = 'localComputerLabel' then Result := 'Ordinateur local :'
    else if Key = 'uuidBelongsOther' then Result := 'Cet UUID appartient deja a un autre systeme dans Firulai.'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'Impossible installer cet agent sur ordinateur local avec cet UUID.'
    else if Key = 'aliasSaveNoSystem' then Result := 'Impossible enregistrer alias dans Firulai car le systeme associe au UUID est introuvable.'
    else if Key = 'aliasSaveFailed' then Result := 'Impossible enregistrer alias dans Firulai. Verifiez la connexion et le token. Installation ne continuera pas sans confirmer alias.'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai a refuse enregistrer alias'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'Verifiez que le token correspond au UUID fourni. Installation ne continuera pas sans confirmer alias.'
    else if Key = 'statusUpdateNoSystem' then Result := 'Impossible actualiser etat dans Firulai car le systeme associe au UUID est introuvable.'
    else if Key = 'statusUpdateFailed' then Result := 'Impossible actualiser etat dans Firulai. Verifiez la connexion et le token. Installation ne continuera pas sans activer le systeme.'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai a refuse actualiser etat'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'Verifiez que le token correspond au UUID fourni. Installation ne continuera pas sans activer le systeme.'
    else if Key = 'uninstallContactFailed' then Result := 'Impossible contacter agent pour marquer le systeme comme inactif dans Firulai. Desinstallation sera annulee pour eviter un etat distant obsolete.'
    else if Key = 'uninstallUpdateFailed' then Result := 'Impossible marquer le systeme comme inactif dans Firulai. Verifiez la connectivite avec Firulai puis desinstallez a nouveau.'
    else if Key = 'uninstallSuccess' then Result := 'Le systeme a ete marque comme inactif dans Firulai. Les donnees enregistrees ne seront pas supprimees; vous pourrez les supprimer depuis Firulai ou reinstaller agent plus tard en le liant a ce meme System.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'de_DE' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'Fuer eine unbeaufsichtigte Installation muessen Sie die Lizenzvereinbarung und den Nutzungshinweis im Installer lesen und akzeptieren.' + #13#10#13#10 + 'Wenn Sie akzeptieren, starten Sie den Installer erneut mit /ACCEPTLICENSE=yes.'
    else if Key = 'configTitle' then Result := 'RSAgent-Konfiguration'
    else if Key = 'configSubtitle' then Result := 'Geben Sie die von Firulai bereitgestellten Daten ein'
    else if Key = 'configDescription' then Result := 'Kopieren Sie UUID und Token von Firulai. Geben Sie danach einen Alias ein, um diesen Computer zu identifizieren. Der Alias wird in Firulai gespeichert und kann spaeter geaendert werden.'
    else if Key = 'aliasLabel' then Result := 'Systemalias:'
    else if Key = 'invalidUuid' then Result := 'Geben Sie eine gueltige UUID im Format xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx ein.'
    else if Key = 'aliasRequired' then Result := 'Geben Sie einen Systemalias ein. Dieses Feld ist erforderlich, um die Installation abzuschliessen, und kann spaeter in Firulai geaendert werden.'
    else if Key = 'tokenRequired' then Result := 'Geben Sie das mit der UUID bereitgestellte Agent token ein. Es ist erforderlich, um diesen Agenten mit Firulai zu verbinden.'
    else if Key = 'missingUuidCli' then Result := 'UUID ist erforderlich oder ungueltig. Verwenden Sie setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> oder fuellen Sie die Felder im Assistenten aus.'
    else if Key = 'missingAliasCli' then Result := 'Alias ist erforderlich. Verwenden Sie setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> oder fuellen Sie die Felder im Assistenten aus. Sie koennen ihn spaeter in Firulai aendern.'
    else if Key = 'missingTokenCli' then Result := 'Agent token ist erforderlich. Verwenden Sie setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> oder fuellen Sie die Felder im Assistenten aus.'
    else if Key = 'replaceConfigFailed' then Result := 'Konnte nicht ersetzen: '
    else if Key = 'replaceConfigAdvice' then Result := '. Schliessen Sie den Installer, fuehren Sie ihn als Administrator aus oder loeschen Sie die Datei manuell.'
    else if Key = 'writeConfigFailed' then Result := 'Konnte nicht schreiben: '
    else if Key = 'localInstalledSameUuid' then Result := 'Auf diesem System ist bereits ein Agent mit dieser UUID installiert.'
    else if Key = 'localInstalledOther' then Result := 'Auf diesem System ist bereits ein Agent installiert.'
    else if Key = 'installedUuidLabel' then Result := 'Aktuell installierte UUID: '
    else if Key = 'requestedUuidLabel' then Result := 'Angeforderte UUID: '
    else if Key = 'reinstallAdvice' then Result := 'Wenn Sie den Agenten erneut installieren muessen, deinstallieren Sie zuerst den aktuellen Agenten ueber Windows oder mit:'
    else if Key = 'uuidValidateFailed' then Result := 'Die UUID konnte in Firulai nicht validiert werden. Pruefen Sie die Internetverbindung und den Agent token. Die Installation wird nicht fortgesetzt, bis die UUID bestaetigt ist.'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai hat die UUID-Validierung verweigert'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'Pruefen Sie, ob der Token zur in Firulai bereitgestellten UUID gehoert. Die Installation wird nicht fortgesetzt, bis die UUID bestaetigt ist.'
    else if Key = 'responseLabel' then Result := 'Antwort: '
    else if Key = 'uuidInvalidNotFound' then Result := 'Ungueltige UUID: Sie existiert nicht in Firulai.'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'Der Agent kann nicht mit einer UUID installiert werden, die nicht in Firulai erzeugt wurde.'
    else if Key = 'uuidLabel' then Result := 'UUID: '
    else if Key = 'systemLookupFailed' then Result := 'Das mit der UUID verknuepfte Firulai-System konnte nicht gefunden werden.'
    else if Key = 'systemLookupSaveAliasSafety' then Result := 'Aus Sicherheitsgruenden wird die Installation nicht fortgesetzt, wenn der Alias nicht gespeichert werden kann.'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'Dieses System hat in Firulai bereits einen Agenten mit dieser UUID.'
    else if Key = 'duplicateUuidInstallForbidden' then Result := 'Eine neue Installation mit derselben UUID ist nicht moeglich.'
    else if Key = 'firulaiSystemLabel' then Result := 'System in Firulai:'
    else if Key = 'localComputerLabel' then Result := 'Lokaler Computer:'
    else if Key = 'uuidBelongsOther' then Result := 'Diese UUID gehoert bereits zu einem anderen System in Firulai.'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'Dieser Agent kann mit dieser UUID nicht auf dem lokalen Computer installiert werden.'
    else if Key = 'aliasSaveNoSystem' then Result := 'Der Alias konnte nicht in Firulai gespeichert werden, weil das mit der UUID verknuepfte System nicht gefunden wurde.'
    else if Key = 'aliasSaveFailed' then Result := 'Der Alias konnte nicht in Firulai gespeichert werden. Pruefen Sie Verbindung und Token. Die Installation wird nicht fortgesetzt, bis der Alias bestaetigt ist.'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai hat das Speichern des Alias verweigert'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'Pruefen Sie, ob der Token zur bereitgestellten UUID gehoert. Die Installation wird nicht fortgesetzt, bis der Alias bestaetigt ist.'
    else if Key = 'statusUpdateNoSystem' then Result := 'Der Status konnte in Firulai nicht aktualisiert werden, weil das mit der UUID verknuepfte System nicht gefunden wurde.'
    else if Key = 'statusUpdateFailed' then Result := 'Der Status konnte in Firulai nicht aktualisiert werden. Pruefen Sie Verbindung und Token. Die Installation wird nicht fortgesetzt, bis das System aktiviert ist.'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai hat die Statusaktualisierung verweigert'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'Pruefen Sie, ob der Token zur bereitgestellten UUID gehoert. Die Installation wird nicht fortgesetzt, bis das System aktiviert ist.'
    else if Key = 'uninstallContactFailed' then Result := 'Der Agent konnte nicht kontaktiert werden, um das System in Firulai als inaktiv zu markieren. Die Deinstallation wird abgebrochen, damit der Remote-Status aktuell bleibt.'
    else if Key = 'uninstallUpdateFailed' then Result := 'Das System konnte in Firulai nicht als inaktiv markiert werden. Pruefen Sie die Verbindung zu Firulai und deinstallieren Sie erneut.'
    else if Key = 'uninstallSuccess' then Result := 'Das System wurde in Firulai als inaktiv markiert. Gespeicherte Daten werden nicht geloescht; Sie koennen sie in Firulai loeschen oder den Agenten spaeter erneut mit demselben System verknuepfen.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'it_IT' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'Per eseguire una installazione silenziosa devi leggere e accettare il contratto di licenza e avviso di utilizzo incluso nel programma.' + #13#10#13#10 + 'Se accetti, esegui di nuovo il programma aggiungendo /ACCEPTLICENSE=yes.'
    else if Key = 'configTitle' then Result := 'Configurazione di RSAgent'
    else if Key = 'configSubtitle' then Result := 'Inserisci i dati forniti da Firulai'
    else if Key = 'configDescription' then Result := 'Copia UUID e token forniti da Firulai. Poi inserisci un alias per identificare questo computer. Alias verra salvato in Firulai e potrai modificarlo piu avanti.'
    else if Key = 'aliasLabel' then Result := 'Alias del sistema:'
    else if Key = 'invalidUuid' then Result := 'Inserisci un UUID valido nel formato xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.'
    else if Key = 'aliasRequired' then Result := 'Inserisci un alias per il sistema. Questo campo e obbligatorio per completare installazione e potrai modificarlo piu avanti in Firulai.'
    else if Key = 'tokenRequired' then Result := 'Inserisci Agent token fornito con UUID. E obbligatorio per collegare questo agente a Firulai.'
    else if Key = 'missingUuidCli' then Result := 'UUID obbligatorio o non valido. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o compila i campi della procedura guidata.'
    else if Key = 'missingAliasCli' then Result := 'Alias obbligatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o compila i campi della procedura guidata. Potrai modificarlo piu avanti in Firulai.'
    else if Key = 'missingTokenCli' then Result := 'Agent token obbligatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o compila i campi della procedura guidata.'
    else if Key = 'replaceConfigFailed' then Result := 'Impossibile sostituire '
    else if Key = 'replaceConfigAdvice' then Result := '. Chiudi il programma, eseguilo come Amministratore o elimina il file manualmente.'
    else if Key = 'writeConfigFailed' then Result := 'Impossibile scrivere '
    else if Key = 'localInstalledSameUuid' then Result := 'Questo sistema ha gia un agente installato con questo UUID.'
    else if Key = 'localInstalledOther' then Result := 'Esiste gia un agente installato in questo sistema.'
    else if Key = 'installedUuidLabel' then Result := 'UUID attualmente installato: '
    else if Key = 'requestedUuidLabel' then Result := 'UUID richiesto: '
    else if Key = 'reinstallAdvice' then Result := 'Se devi reinstallare agente, disinstalla prima agente attuale da Windows o eseguendo:'
    else if Key = 'uuidValidateFailed' then Result := 'Impossibile validare UUID in Firulai. Controlla connessione internet e Agent token. Installazione non continuera senza confermare che UUID sia disponibile.'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai non ha permesso di validare UUID'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'Controlla che il token corrisponda a UUID fornito in Firulai. Installazione non continuera senza confermare che UUID sia disponibile.'
    else if Key = 'responseLabel' then Result := 'Risposta: '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID non valido: non esiste in Firulai.'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'Non e possibile installare agente con un UUID non generato da Firulai.'
    else if Key = 'uuidLabel' then Result := 'UUID: '
    else if Key = 'systemLookupFailed' then Result := 'Impossibile trovare il sistema Firulai associato a UUID.'
    else if Key = 'systemLookupSaveAliasSafety' then Result := 'Per sicurezza, installazione non continuera senza poter salvare alias.'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'Questo sistema ha gia un agente installato in Firulai con questo UUID.'
    else if Key = 'duplicateUuidInstallForbidden' then Result := 'Non e possibile eseguire una nuova installazione con lo stesso UUID.'
    else if Key = 'firulaiSystemLabel' then Result := 'Sistema in Firulai:'
    else if Key = 'localComputerLabel' then Result := 'Computer locale:'
    else if Key = 'uuidBelongsOther' then Result := 'Questo UUID appartiene gia a un altro sistema in Firulai.'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'Non e possibile installare questo agente nel computer locale con questo UUID.'
    else if Key = 'aliasSaveNoSystem' then Result := 'Impossibile salvare alias in Firulai perche il sistema associato a UUID non e stato trovato.'
    else if Key = 'aliasSaveFailed' then Result := 'Impossibile salvare alias in Firulai. Controlla connessione e token. Installazione non continuera senza confermare alias.'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai non ha permesso di salvare alias'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'Controlla che il token corrisponda a UUID fornito. Installazione non continuera senza confermare alias.'
    else if Key = 'statusUpdateNoSystem' then Result := 'Impossibile aggiornare stato in Firulai perche il sistema associato a UUID non e stato trovato.'
    else if Key = 'statusUpdateFailed' then Result := 'Impossibile aggiornare stato in Firulai. Controlla connessione e token. Installazione non continuera senza attivare il sistema.'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai non ha permesso di aggiornare stato'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'Controlla che il token corrisponda a UUID fornito. Installazione non continuera senza attivare il sistema.'
    else if Key = 'uninstallContactFailed' then Result := 'Impossibile contattare agente per marcare il sistema come inattivo in Firulai. Disinstallazione sara annullata per evitare stato remoto non aggiornato.'
    else if Key = 'uninstallUpdateFailed' then Result := 'Impossibile marcare il sistema come inattivo in Firulai. Controlla connettivita con Firulai e ripeti disinstallazione.'
    else if Key = 'uninstallSuccess' then Result := 'Il sistema e stato marcato come inattivo in Firulai. I dati salvati non saranno eliminati; potrai eliminarli da Firulai o reinstallare agente piu avanti collegandolo a questo stesso System.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'ja_JP' then
  begin
    if Key = 'silentLicenseRequired' then Result := 'サイレント インストールを実行するには、インストーラーに含まれるライセンス契約と使用上の注意を読み、同意する必要があります。' + #13#10#13#10 + '同意する場合は /ACCEPTLICENSE=yes を追加してインストーラーを再実行してください。'
    else if Key = 'configTitle' then Result := 'RSAgent の設定'
    else if Key = 'configSubtitle' then Result := 'Firulai から提供された情報を入力してください'
    else if Key = 'configDescription' then Result := 'Firulai から提供された UUID と token をコピーしてください。その後、このコンピューターを識別する alias を入力してください。alias は Firulai に保存され、あとで変更できます。'
    else if Key = 'aliasLabel' then Result := 'システム alias:'
    else if Key = 'invalidUuid' then Result := 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 形式の有効な UUID を入力してください。'
    else if Key = 'aliasRequired' then Result := 'システム alias を入力してください。この項目はインストール完了に必須で、あとで Firulai で変更できます。'
    else if Key = 'tokenRequired' then Result := 'UUID と一緒に提供された Agent token を入力してください。このエージェントを Firulai にリンクするために必要です。'
    else if Key = 'missingUuidCli' then Result := 'UUID が必須または無効です。setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> を使うか、ウィザードの項目を入力してください。'
    else if Key = 'missingAliasCli' then Result := 'Alias が必須です。setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> を使うか、ウィザードの項目を入力してください。あとで Firulai で変更できます。'
    else if Key = 'missingTokenCli' then Result := 'Agent token が必須です。setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> を使うか、ウィザードの項目を入力してください。'
    else if Key = 'replaceConfigFailed' then Result := '置換できませんでした: '
    else if Key = 'replaceConfigAdvice' then Result := '。インストーラーを閉じて管理者として実行するか、ファイルを手動で削除してください。'
    else if Key = 'writeConfigFailed' then Result := '書き込めませんでした: '
    else if Key = 'localInstalledSameUuid' then Result := 'このシステムには、この UUID のエージェントがすでにインストールされています。'
    else if Key = 'localInstalledOther' then Result := 'このシステムにはエージェントがすでにインストールされています。'
    else if Key = 'installedUuidLabel' then Result := '現在インストールされている UUID: '
    else if Key = 'requestedUuidLabel' then Result := '要求された UUID: '
    else if Key = 'reinstallAdvice' then Result := 'エージェントを再インストールする必要がある場合は、まず Windows から現在のエージェントをアンインストールするか、次を実行してください:'
    else if Key = 'uuidValidateFailed' then Result := 'Firulai で UUID を検証できませんでした。インターネット接続と Agent token を確認してください。UUID が利用可能であることを確認できるまで、インストールは続行されません。'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai が UUID の検証を許可しませんでした'
    else if Key = 'uuidValidateDeniedAdvice' then Result := 'token が Firulai で提供された UUID に対応していることを確認してください。UUID が利用可能であることを確認できるまで、インストールは続行されません。'
    else if Key = 'responseLabel' then Result := '応答: '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID が無効です: Firulai に存在しません。'
    else if Key = 'uuidInvalidNoGenerated' then Result := 'Firulai で生成されていない UUID ではエージェントをインストールできません。'
    else if Key = 'uuidLabel' then Result := 'UUID: '
    else if Key = 'systemLookupFailed' then Result := 'UUID に関連付けられた Firulai のシステムを見つけられませんでした。'
    else if Key = 'systemLookupSaveAliasSafety' then Result := '安全のため、alias を保存できない場合はインストールを続行しません。'
    else if Key = 'firulaiInstalledSameUuid' then Result := 'このシステムには、Firulai でこの UUID のエージェントがすでにインストールされています。'
    else if Key = 'duplicateUuidInstallForbidden' then Result := '同じ UUID で新しいインストールはできません。'
    else if Key = 'firulaiSystemLabel' then Result := 'Firulai のシステム:'
    else if Key = 'localComputerLabel' then Result := 'ローカル コンピューター:'
    else if Key = 'uuidBelongsOther' then Result := 'この UUID は Firulai の別のシステムにすでに属しています。'
    else if Key = 'uuidBelongsOtherLocal' then Result := 'この UUID では、このエージェントをローカル コンピューターにインストールできません。'
    else if Key = 'aliasSaveNoSystem' then Result := 'UUID に関連付けられたシステムが見つからないため、Firulai に alias を保存できませんでした。'
    else if Key = 'aliasSaveFailed' then Result := 'Firulai に alias を保存できませんでした。接続と token を確認してください。alias を確認できるまで、インストールは続行されません。'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai が alias の保存を許可しませんでした'
    else if Key = 'aliasSaveDeniedAdvice' then Result := 'token が提供された UUID に対応していることを確認してください。alias を確認できるまで、インストールは続行されません。'
    else if Key = 'statusUpdateNoSystem' then Result := 'UUID に関連付けられたシステムが見つからないため、Firulai の状態を更新できませんでした。'
    else if Key = 'statusUpdateFailed' then Result := 'Firulai の状態を更新できませんでした。接続と token を確認してください。システムを有効化できるまで、インストールは続行されません。'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai が状態更新を許可しませんでした'
    else if Key = 'statusUpdateDeniedAdvice' then Result := 'token が提供された UUID に対応していることを確認してください。システムを有効化できるまで、インストールは続行されません。'
    else if Key = 'uninstallContactFailed' then Result := 'Firulai でシステムを非アクティブにするためにエージェントへ接続できませんでした。リモート状態が古くならないようにアンインストールを取り消します。'
    else if Key = 'uninstallUpdateFailed' then Result := 'Firulai でシステムを非アクティブにできませんでした。Firulai への接続を確認し、もう一度アンインストールしてください。'
    else if Key = 'uninstallSuccess' then Result := 'システムは Firulai で非アクティブとしてマークされました。保存済みデータは削除されません。Firulai から削除するか、あとで同じ System にリンクしてエージェントを再インストールできます。'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'zh_CN' then
  begin
    if Key = 'silentLicenseRequired' then Result := '要执行静默安装，必须阅读并接受安装程序包含的许可协议和使用说明。' + #13#10#13#10 + '如果接受，请添加 /ACCEPTLICENSE=yes 后重新运行安装程序。'
    else if Key = 'configTitle' then Result := 'RSAgent 配置'
    else if Key = 'configSubtitle' then Result := '输入 Firulai 提供的信息'
    else if Key = 'configDescription' then Result := '复制 Firulai 提供的 UUID 和 token。然后输入用于识别此计算机的 alias。alias 将保存在 Firulai 中，以后可以修改。'
    else if Key = 'aliasLabel' then Result := '系统 alias:'
    else if Key = 'invalidUuid' then Result := '请输入格式为 xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 的有效 UUID。'
    else if Key = 'aliasRequired' then Result := '请输入系统 alias。此字段是完成安装所必需的，以后可以在 Firulai 中修改。'
    else if Key = 'tokenRequired' then Result := '请输入与 UUID 一起提供的 Agent token。需要它来将此代理链接到 Firulai。'
    else if Key = 'missingUuidCli' then Result := 'UUID 必填或无效。请使用 setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN>，或填写向导字段。'
    else if Key = 'missingAliasCli' then Result := 'Alias 必填。请使用 setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN>，或填写向导字段。以后可以在 Firulai 中修改。'
    else if Key = 'missingTokenCli' then Result := 'Agent token 必填。请使用 setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN>，或填写向导字段。'
    else if Key = 'replaceConfigFailed' then Result := '无法替换 '
    else if Key = 'replaceConfigAdvice' then Result := '。请关闭安装程序，以管理员身份运行，或手动删除该文件。'
    else if Key = 'writeConfigFailed' then Result := '无法写入 '
    else if Key = 'localInstalledSameUuid' then Result := '此系统已安装使用此 UUID 的代理。'
    else if Key = 'localInstalledOther' then Result := '此系统已安装代理。'
    else if Key = 'installedUuidLabel' then Result := '当前安装的 UUID: '
    else if Key = 'requestedUuidLabel' then Result := '请求的 UUID: '
    else if Key = 'reinstallAdvice' then Result := '如果需要重新安装代理，请先从 Windows 卸载当前代理，或运行:'
    else if Key = 'uuidValidateFailed' then Result := '无法在 Firulai 中验证 UUID。请检查互联网连接和 Agent token。未确认 UUID 可用前，安装不会继续。'
    else if Key = 'uuidValidateDenied' then Result := 'Firulai 不允许验证 UUID'
    else if Key = 'uuidValidateDeniedAdvice' then Result := '请确认 token 对应 Firulai 提供的 UUID。未确认 UUID 可用前，安装不会继续。'
    else if Key = 'responseLabel' then Result := '响应: '
    else if Key = 'uuidInvalidNotFound' then Result := 'UUID 无效: Firulai 中不存在。'
    else if Key = 'uuidInvalidNoGenerated' then Result := '不能使用不是从 Firulai 生成的 UUID 安装代理。'
    else if Key = 'uuidLabel' then Result := 'UUID: '
    else if Key = 'systemLookupFailed' then Result := '无法找到与 UUID 关联的 Firulai 系统。'
    else if Key = 'systemLookupSaveAliasSafety' then Result := '出于安全原因，无法保存 alias 时安装不会继续。'
    else if Key = 'firulaiInstalledSameUuid' then Result := '此系统已在 Firulai 中安装使用此 UUID 的代理。'
    else if Key = 'duplicateUuidInstallForbidden' then Result := '不能使用同一 UUID 执行新的安装。'
    else if Key = 'firulaiSystemLabel' then Result := 'Firulai 中的系统:'
    else if Key = 'localComputerLabel' then Result := '本地计算机:'
    else if Key = 'uuidBelongsOther' then Result := '此 UUID 已属于 Firulai 中的另一个系统。'
    else if Key = 'uuidBelongsOtherLocal' then Result := '不能在本地计算机上使用该 UUID 安装此代理。'
    else if Key = 'aliasSaveNoSystem' then Result := '无法在 Firulai 中保存 alias，因为未找到与 UUID 关联的系统。'
    else if Key = 'aliasSaveFailed' then Result := '无法在 Firulai 中保存 alias。请检查连接和 token。未确认 alias 前，安装不会继续。'
    else if Key = 'aliasSaveDenied' then Result := 'Firulai 不允许保存 alias'
    else if Key = 'aliasSaveDeniedAdvice' then Result := '请确认 token 对应提供的 UUID。未确认 alias 前，安装不会继续。'
    else if Key = 'statusUpdateNoSystem' then Result := '无法在 Firulai 中更新状态，因为未找到与 UUID 关联的系统。'
    else if Key = 'statusUpdateFailed' then Result := '无法在 Firulai 中更新状态。请检查连接和 token。未激活系统前，安装不会继续。'
    else if Key = 'statusUpdateDenied' then Result := 'Firulai 不允许更新状态'
    else if Key = 'statusUpdateDeniedAdvice' then Result := '请确认 token 对应提供的 UUID。未激活系统前，安装不会继续。'
    else if Key = 'uninstallContactFailed' then Result := '无法联系代理以在 Firulai 中将系统标记为非活动。卸载将取消，以避免远程状态过期。'
    else if Key = 'uninstallUpdateFailed' then Result := '无法在 Firulai 中将系统标记为非活动。请检查与 Firulai 的连接并重新卸载。'
    else if Key = 'uninstallSuccess' then Result := '系统已在 Firulai 中标记为非活动。保存的数据不会删除；可以从 Firulai 删除，或以后重新安装代理并链接到同一个 System。'
    else Result := Key;
    Exit;
  end;

  if Key = 'silentLicenseRequired' then Result := 'To run a silent installation, you must read and accept the license agreement and usage notice included with the installer.' + #13#10#13#10 + 'If you accept it, run the installer again adding /ACCEPTLICENSE=yes.'
  else if Key = 'configTitle' then Result := 'RSAgent configuration'
  else if Key = 'configSubtitle' then Result := 'Enter the details provided by Firulai'
  else if Key = 'configDescription' then Result := 'Copy the UUID and token provided by Firulai. Then enter an alias to identify this computer. The alias will be saved in Firulai and can be changed later.'
  else if Key = 'aliasLabel' then Result := 'System alias:'
  else if Key = 'invalidUuid' then Result := 'Enter a valid UUID using the xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx format.'
  else if Key = 'aliasRequired' then Result := 'Enter a system alias. This field is required to finish the installation and can be changed later in Firulai.'
  else if Key = 'tokenRequired' then Result := 'Enter the Agent token provided with the UUID. It is required to link this agent with Firulai.'
  else if Key = 'missingUuidCli' then Result := 'UUID is required or invalid. Use setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> or fill in the wizard fields.'
  else if Key = 'missingAliasCli' then Result := 'Alias is required. Use setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> or fill in the wizard fields. You can change it later in Firulai.'
  else if Key = 'missingTokenCli' then Result := 'Agent token is required. Use setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> or fill in the wizard fields.'
  else if Key = 'replaceConfigFailed' then Result := 'Could not replace '
  else if Key = 'replaceConfigAdvice' then Result := '. Close the installer, run it as Administrator, or delete the file manually.'
  else if Key = 'writeConfigFailed' then Result := 'Could not write '
  else if Key = 'localInstalledSameUuid' then Result := 'This system already has an agent installed with this UUID.'
  else if Key = 'localInstalledOther' then Result := 'An agent is already installed on this system.'
  else if Key = 'installedUuidLabel' then Result := 'Currently installed UUID: '
  else if Key = 'requestedUuidLabel' then Result := 'Requested UUID: '
  else if Key = 'reinstallAdvice' then Result := 'If you need to reinstall the agent, first uninstall the current agent from Windows or by running:'
  else if Key = 'uuidValidateFailed' then Result := 'Could not validate the UUID in Firulai. Check the internet connection and Agent token. Installation will not continue until the UUID availability is confirmed.'
  else if Key = 'uuidValidateDenied' then Result := 'Firulai did not allow UUID validation'
  else if Key = 'uuidValidateDeniedAdvice' then Result := 'Check that the token matches the UUID provided in Firulai. Installation will not continue until the UUID availability is confirmed.'
  else if Key = 'responseLabel' then Result := 'Response: '
  else if Key = 'uuidInvalidNotFound' then Result := 'Invalid UUID: it does not exist in Firulai.'
  else if Key = 'uuidInvalidNoGenerated' then Result := 'The agent cannot be installed with a UUID that was not generated from Firulai.'
  else if Key = 'uuidLabel' then Result := 'UUID: '
  else if Key = 'systemLookupFailed' then Result := 'Could not locate the Firulai system associated with the UUID.'
  else if Key = 'systemLookupSaveAliasSafety' then Result := 'For safety, installation will not continue without being able to save the alias.'
  else if Key = 'firulaiInstalledSameUuid' then Result := 'This system already has an agent installed in Firulai with this UUID.'
  else if Key = 'duplicateUuidInstallForbidden' then Result := 'A new installation cannot be performed with the same UUID.'
  else if Key = 'firulaiSystemLabel' then Result := 'System in Firulai:'
  else if Key = 'localComputerLabel' then Result := 'Local computer:'
  else if Key = 'uuidBelongsOther' then Result := 'This UUID already belongs to another system in Firulai.'
  else if Key = 'uuidBelongsOtherLocal' then Result := 'This agent cannot be installed on the local computer with that UUID.'
  else if Key = 'aliasSaveNoSystem' then Result := 'Could not save the alias in Firulai because the system associated with the UUID was not found.'
  else if Key = 'aliasSaveFailed' then Result := 'Could not save the alias in Firulai. Check the connection and token. Installation will not continue until the alias is confirmed.'
  else if Key = 'aliasSaveDenied' then Result := 'Firulai did not allow saving the alias'
  else if Key = 'aliasSaveDeniedAdvice' then Result := 'Check that the token matches the provided UUID. Installation will not continue until the alias is confirmed.'
  else if Key = 'statusUpdateNoSystem' then Result := 'Could not update the status in Firulai because the system associated with the UUID was not found.'
  else if Key = 'statusUpdateFailed' then Result := 'Could not update the status in Firulai. Check the connection and token. Installation will not continue until the system is activated.'
  else if Key = 'statusUpdateDenied' then Result := 'Firulai did not allow updating the status'
  else if Key = 'statusUpdateDeniedAdvice' then Result := 'Check that the token matches the provided UUID. Installation will not continue until the system is activated.'
  else if Key = 'uninstallContactFailed' then Result := 'Could not contact the agent to mark the system as inactive in Firulai. Uninstall will be cancelled so the remote status does not become outdated.'
  else if Key = 'uninstallUpdateFailed' then Result := 'Could not mark the system as inactive in Firulai. Check connectivity with Firulai and uninstall again.'
  else if Key = 'uninstallSuccess' then Result := 'The system has been marked as inactive in Firulai. Saved data will not be deleted; you can delete it from Firulai or reinstall the agent later by linking it to this same System.'
  else Result := Key;
end;

function EffectiveUuid(): string;
begin
  Result := CmdParam('UUID');
  if Result = '' then Result := ConfigPage.Values[0];
  Result := Trim(Result);
end;

function EffectiveAlias(): string;
begin
  Result := CmdParam('ALIAS');
  if Result = '' then Result := ConfigPage.Values[1];
  Result := Trim(Result);
end;

function EffectiveToken(): string;
begin
  Result := CmdParam('TOKEN');
  if Result = '' then Result := ConfigPage.Values[2];
  Result := Trim(Result);
end;

function IsSilentWithConfig(): Boolean;
begin
  Result := (CmdParam('UUID') <> '') and (CmdParam('ALIAS') <> '') and (CmdParam('TOKEN') <> '');
end;

function LicenseAcceptedFromCommandLine(): Boolean;
begin
  Result := CompareText(Trim(CmdParam('ACCEPTLICENSE')), 'yes') = 0;
end;

procedure ResolveAgentLocale(Token: string); forward;

function InitializeSetup(): Boolean;
begin
  Result := True;
  ResolveAgentLocale(CmdParam('TOKEN'));

  if not WizardSilent() then
  begin
    if RelaunchWithResolvedLanguageIfNeeded() then
    begin
      Result := False;
      Exit;
    end;
  end;

  if WizardSilent() and not LicenseAcceptedFromCommandLine() then
  begin
    MsgBox(T('silentLicenseRequired'), mbError, MB_OK);
    Result := False;
    Exit;
  end;

end;

function IsServiceInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'query RSAgent', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := ResultCode = 0;
end;

function IsLocalAgentInstalled(): Boolean;
begin
  Result :=
    IsServiceInstalled() or
    FileExists(ExpandConstant('{app}\{#MyAppExeName}')) or
    FileExists(ExpandConstant('{app}\unins000.exe')) or
    FileExists(ExpandConstant('{commonappdata}\RSAgent\config.json'));
end;

function ExtractInstalledUuid(): string;
var
  ConfigJson: AnsiString;
  UuidPos: Integer;
  ColonPos: Integer;
  FirstQuotePos: Integer;
  SecondQuotePos: Integer;
begin
  Result := '';

  if not LoadStringFromFile(ExpandConstant('{commonappdata}\RSAgent\config.json'), ConfigJson) then
  begin
    Exit;
  end;

  UuidPos := Pos('"uuid"', Lowercase(ConfigJson));
  if UuidPos = 0 then
  begin
    Exit;
  end;

  ColonPos := Pos(':', Copy(ConfigJson, UuidPos, Length(ConfigJson)));
  if ColonPos = 0 then
  begin
    Exit;
  end;

  ColonPos := UuidPos + ColonPos - 1;
  FirstQuotePos := Pos('"', Copy(ConfigJson, ColonPos, Length(ConfigJson)));
  if FirstQuotePos = 0 then
  begin
    Exit;
  end;

  FirstQuotePos := ColonPos + FirstQuotePos - 1;
  SecondQuotePos := Pos('"', Copy(ConfigJson, FirstQuotePos + 1, Length(ConfigJson)));
  if SecondQuotePos = 0 then
  begin
    Exit;
  end;

  SecondQuotePos := FirstQuotePos + SecondQuotePos;
  Result := Copy(ConfigJson, FirstQuotePos + 1, SecondQuotePos - FirstQuotePos - 1);
end;

function ExtractInstalledLocale(): string;
var
  ConfigJson: AnsiString;
  LocalePos: Integer;
  ColonPos: Integer;
  FirstQuotePos: Integer;
  SecondQuotePos: Integer;
begin
  Result := '';

  if not LoadStringFromFile(ExpandConstant('{commonappdata}\RSAgent\config.json'), ConfigJson) then
  begin
    Exit;
  end;

  LocalePos := Pos('"locale"', Lowercase(ConfigJson));
  if LocalePos = 0 then
  begin
    Exit;
  end;

  ColonPos := Pos(':', Copy(ConfigJson, LocalePos, Length(ConfigJson)));
  if ColonPos = 0 then
  begin
    Exit;
  end;

  ColonPos := LocalePos + ColonPos - 1;
  FirstQuotePos := Pos('"', Copy(ConfigJson, ColonPos, Length(ConfigJson)));
  if FirstQuotePos = 0 then
  begin
    Exit;
  end;

  FirstQuotePos := ColonPos + FirstQuotePos - 1;
  SecondQuotePos := Pos('"', Copy(ConfigJson, FirstQuotePos + 1, Length(ConfigJson)));
  if SecondQuotePos = 0 then
  begin
    Exit;
  end;

  SecondQuotePos := FirstQuotePos + SecondQuotePos;
  Result := Copy(ConfigJson, FirstQuotePos + 1, SecondQuotePos - FirstQuotePos - 1);
end;

function CheckLocalAgentInstallation(): string;
var
  InstalledUuid: string;
begin
  Result := '';

  if not IsLocalAgentInstalled() then
  begin
    Exit;
  end;

  InstalledUuid := ExtractInstalledUuid();

  if (InstalledUuid <> '') and (CompareText(InstalledUuid, EffectiveUuid()) = 0) then
  begin
    Result := T('localInstalledSameUuid');
  end
  else
  begin
    Result := T('localInstalledOther');
    if InstalledUuid <> '' then
    begin
      Result := Result + #13#10 + T('installedUuidLabel') + InstalledUuid;
    end;
    Result := Result + #13#10 + T('requestedUuidLabel') + EffectiveUuid();
  end;

  Result := Result + #13#10#13#10 +
    T('reinstallAdvice') + #13#10 +
    ExpandConstant('{app}\unins000.exe');
end;

function LocalHostname(): string;
begin
  Result := GetEnv('COMPUTERNAME');
  if Result = '' then
  begin
    Result := GetComputerNameString();
  end;
end;

function LocalFqdn(): string;
var
  Domain: string;
begin
  Result := LocalHostname();
  Domain := GetEnv('USERDNSDOMAIN');
  if (Result <> '') and (Domain <> '') then
  begin
    Result := Result + '.' + Domain;
  end;
end;

function JsonExtractFirstStringKey(Json: string; Key: string): string;
var
  KeyPattern: string;
  KeyPos: Integer;
  ColonPos: Integer;
  FirstQuotePos: Integer;
  SecondQuotePos: Integer;
begin
  Result := '';
  KeyPattern := '"' + Key + '"';
  KeyPos := Pos(KeyPattern, Json);
  if KeyPos = 0 then
  begin
    Exit;
  end;

  ColonPos := Pos(':', Copy(Json, KeyPos, Length(Json)));
  if ColonPos = 0 then
  begin
    Exit;
  end;

  ColonPos := KeyPos + ColonPos - 1;
  FirstQuotePos := Pos('"', Copy(Json, ColonPos, Length(Json)));
  if FirstQuotePos = 0 then
  begin
    Exit;
  end;

  FirstQuotePos := ColonPos + FirstQuotePos - 1;
  SecondQuotePos := Pos('"', Copy(Json, FirstQuotePos + 1, Length(Json)));
  if SecondQuotePos = 0 then
  begin
    Exit;
  end;

  SecondQuotePos := FirstQuotePos + SecondQuotePos;
  Result := Copy(Json, FirstQuotePos + 1, SecondQuotePos - FirstQuotePos - 1);
end;

function JsonExtractFirstScalarKey(Json: string; Key: string): string;
var
  KeyPattern: string;
  KeyPos: Integer;
  ColonPos: Integer;
  ValueStart: Integer;
  ValueEnd: Integer;
  C: Char;
begin
  Result := '';
  KeyPattern := '"' + Key + '"';
  KeyPos := Pos(KeyPattern, Json);
  if KeyPos = 0 then
  begin
    Exit;
  end;

  ColonPos := Pos(':', Copy(Json, KeyPos, Length(Json)));
  if ColonPos = 0 then
  begin
    Exit;
  end;

  ColonPos := KeyPos + ColonPos - 1;
  ValueStart := ColonPos + 1;
  while (ValueStart <= Length(Json)) and ((Json[ValueStart] = ' ') or (Json[ValueStart] = #9) or (Json[ValueStart] = #13) or (Json[ValueStart] = #10)) do
  begin
    ValueStart := ValueStart + 1;
  end;

  if ValueStart > Length(Json) then
  begin
    Exit;
  end;

  if Json[ValueStart] = '"' then
  begin
    ValueEnd := Pos('"', Copy(Json, ValueStart + 1, Length(Json)));
    if ValueEnd = 0 then
    begin
      Exit;
    end;
    ValueEnd := ValueStart + ValueEnd;
    Result := Copy(Json, ValueStart + 1, ValueEnd - ValueStart - 1);
    Exit;
  end;

  ValueEnd := ValueStart;
  while ValueEnd <= Length(Json) do
  begin
    C := Json[ValueEnd];
    if (C = ',') or (C = '}') or (C = ']') or (C = ' ') or (C = #9) or (C = #13) or (C = #10) then
    begin
      Break;
    end;
    ValueEnd := ValueEnd + 1;
  end;

  Result := Copy(Json, ValueStart, ValueEnd - ValueStart);
end;

function JsonExtractRsmProperty(Json: string; PropertyId: string): string;
begin
  Result := JsonExtractFirstStringKey(Json, PropertyId);
  if Result = '' then
  begin
    Result := JsonExtractFirstStringKey(Json, PropertyId + 'trs');
  end;
end;

function ResolveLocaleFromRsm(Token: string): string;
var
  Http: Variant;
  Payload: string;
  ResponseBody: string;
  AccountId: string;
begin
  Result := '';
  Token := Trim(Token);
  if Token = '' then Exit;

  try
    Payload :=
      '{"propertyIDs":["{#RsmAccountAgentTokenPropertyId}"],' +
      '"translateIDs":true,' +
      '"filterRules":[{"propertyID":"{#RsmAccountAgentTokenPropertyId}","value":"' + JsonEscape(Token) + '","operation":"="}]}';
    Http := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    Http.Open('GET', '{#RsmItemsGetUrl}', False);
    Http.SetTimeouts(5000, 5000, 20000, 20000);
    Http.SetRequestHeader('Authorization', Token);
    Http.SetRequestHeader('Content-Type', 'application/json');
    Http.Send(Payload);
    if (Http.Status <> 200) and (Http.Status <> 201) then Exit;
    ResponseBody := Http.ResponseText;
    AccountId := JsonExtractFirstScalarKey(ResponseBody, 'ID');
    if AccountId = '' then AccountId := JsonExtractFirstScalarKey(ResponseBody, 'id');
    if AccountId = '' then Exit;

    Payload :=
      '{"propertyIDs":["{#RsmAppUserAccountPropertyId}","{#RsmAppUserLocalePropertyId}"],' +
      '"translateIDs":true,' +
      '"filterRules":[{"propertyID":"{#RsmAppUserAccountPropertyId}","value":"' + JsonEscape(AccountId) + '","operation":"="}]}';
    Http.Open('GET', '{#RsmItemsGetUrl}', False);
    Http.SetTimeouts(5000, 5000, 20000, 20000);
    Http.SetRequestHeader('Authorization', Token);
    Http.SetRequestHeader('Content-Type', 'application/json');
    Http.Send(Payload);
    if (Http.Status <> 200) and (Http.Status <> 201) then Exit;
    ResponseBody := Http.ResponseText;
    Result := JsonExtractRsmProperty(ResponseBody, '{#RsmAppUserLocalePropertyId}');
  except
    Result := '';
  end;
end;

procedure ResolveAgentLocale(Token: string);
var
  Locale: string;
begin
  Locale := Trim(CmdParam('LOCALE'));
  if Locale = '' then Locale := Trim(CmdParam('AGENTLOCALE'));
  if Locale = '' then Locale := LocaleFromInstallerFileName();
  if Locale = '' then Locale := ResolveLocaleFromRsm(Token);
  if Locale = '' then Locale := ActiveLanguage();
  AgentLocale := NormalizeLocale(Locale);
end;

function IdentityMatchesLocalSystem(ExistingHostname: string; ExistingFqdn: string): Boolean;
var
  CurrentHostname: string;
  CurrentFqdn: string;
begin
  CurrentHostname := LocalHostname();
  CurrentFqdn := LocalFqdn();

  Result :=
    ((ExistingHostname <> '') and (CompareText(ExistingHostname, CurrentHostname) = 0)) or
    ((ExistingFqdn <> '') and (CompareText(ExistingFqdn, CurrentFqdn) = 0)) or
    ((ExistingHostname <> '') and (CompareText(ExistingHostname, CurrentFqdn) = 0)) or
    ((ExistingFqdn <> '') and (CompareText(ExistingFqdn, CurrentHostname) = 0));
end;

function CheckUuidAvailable(): string;
var
  Http: Variant;
  Payload: string;
  ResponseBody: string;
  ExistingHostname: string;
  ExistingFqdn: string;
begin
  Result := '';
  RsmSystemItemId := '';
  Payload :=
    '{"propertyIDs":["{#RsmSystemHostnamePropertyId}","{#RsmSystemFqdnPropertyId}","{#RsmSystemUuidPropertyId}","{#RsmSystemAliasPropertyId}"],' +
    '"translateIDs":true,' +
    '"filterRules":[{"propertyID":"{#RsmSystemUuidPropertyId}","value":"' + JsonEscape(EffectiveUuid()) + '","operation":"="}]}';

  try
    Http := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    Http.Open('GET', '{#RsmItemsGetUrl}', False);
    Http.SetTimeouts(5000, 5000, 20000, 20000);
    Http.SetRequestHeader('Authorization', EffectiveToken());
    Http.SetRequestHeader('Content-Type', 'application/json');
    Http.Send(Payload);
    ResponseBody := Http.ResponseText;
  except
    Result := T('uuidValidateFailed');
    Exit;
  end;

  if (Http.Status <> 200) and (Http.Status <> 201) then
  begin
    Result := T('uuidValidateDenied') + ' (HTTP ' + IntToStr(Http.Status) + ').' + #13#10 +
      T('uuidValidateDeniedAdvice') + #13#10 +
      T('responseLabel') + ResponseBody;
    Exit;
  end;

  if Pos(EffectiveUuid(), ResponseBody) = 0 then
  begin
    Result := T('uuidInvalidNotFound') + #13#10 +
      T('uuidInvalidNoGenerated') + #13#10#13#10 +
      T('uuidLabel') + EffectiveUuid();
    Exit;
  end;

  RsmSystemItemId := JsonExtractFirstScalarKey(ResponseBody, 'ID');
  if RsmSystemItemId = '' then
  begin
    RsmSystemItemId := JsonExtractFirstScalarKey(ResponseBody, 'id');
  end;

  if RsmSystemItemId = '' then
  begin
    Result := T('systemLookupFailed') + #13#10 +
      T('systemLookupSaveAliasSafety');
    Exit;
  end;

  ExistingHostname := JsonExtractRsmProperty(ResponseBody, '{#RsmSystemHostnamePropertyId}');
  ExistingFqdn := JsonExtractRsmProperty(ResponseBody, '{#RsmSystemFqdnPropertyId}');

  if (ExistingHostname = '') and (ExistingFqdn = '') then
  begin
    Exit;
  end;

  if IdentityMatchesLocalSystem(ExistingHostname, ExistingFqdn) then
  begin
    Exit;
  end;

  if (RsmSystemItemId = '') and IdentityMatchesLocalSystem(ExistingHostname, ExistingFqdn) then
  begin
    Result := T('firulaiInstalledSameUuid') + #13#10 +
      T('duplicateUuidInstallForbidden') + #13#10#13#10 +
      T('uuidLabel') + EffectiveUuid() + #13#10 +
      T('firulaiSystemLabel') + #13#10 +
      '   - Hostname: ' + ExistingHostname + #13#10 +
      '   - FQDN:     ' + ExistingFqdn + #13#10 +
      T('localComputerLabel') + #13#10 +
      '   - Hostname: ' + LocalHostname() + #13#10 +
      '   - FQDN:     ' + LocalFqdn() + #13#10#13#10 +
      T('reinstallAdvice');
    Exit;
  end;

  Result := T('uuidBelongsOther') + #13#10 +
    T('uuidBelongsOtherLocal');
end;

function SaveAliasInRsm(): string;
var
  Http: Variant;
  Payload: string;
  ResponseBody: string;
begin
  Result := '';

  if RsmSystemItemId = '' then
  begin
    Result := T('aliasSaveNoSystem');
    Exit;
  end;

  Payload :=
    '[{"ID":"' + JsonEscape(RsmSystemItemId) + '","{#RsmSystemAliasPropertyId}":"' + JsonEscape(EffectiveAlias()) + '"}]';

  try
    Http := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    Http.Open('PATCH', '{#RsmItemsUpdateUrl}', False);
    Http.SetTimeouts(5000, 5000, 20000, 20000);
    Http.SetRequestHeader('Authorization', EffectiveToken());
    Http.SetRequestHeader('Content-Type', 'application/json');
    Http.Send(Payload);
    ResponseBody := Http.ResponseText;
  except
    Result := T('aliasSaveFailed');
    Exit;
  end;

  if (Http.Status <> 200) and (Http.Status <> 201) then
  begin
    Result := T('aliasSaveDenied') + ' (HTTP ' + IntToStr(Http.Status) + ').' + #13#10 +
      T('aliasSaveDeniedAdvice') + #13#10 +
      T('responseLabel') + ResponseBody;
  end;
end;

function SaveHostnameStatusInRsm(Value: string): string;
var
  Http: Variant;
  Payload: string;
  ResponseBody: string;
begin
  Result := '';

  if RsmSystemItemId = '' then
  begin
    Result := T('statusUpdateNoSystem');
    Exit;
  end;

  Payload :=
    '[{"ID":"' + JsonEscape(RsmSystemItemId) + '","{#RsmSystemHostnameStatusPropertyId}":"' + JsonEscape(Value) + '"}]';

  try
    Http := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    Http.Open('PATCH', '{#RsmItemsUpdateUrl}', False);
    Http.SetTimeouts(5000, 5000, 20000, 20000);
    Http.SetRequestHeader('Authorization', EffectiveToken());
    Http.SetRequestHeader('Content-Type', 'application/json');
    Http.Send(Payload);
    ResponseBody := Http.ResponseText;
  except
    Result := T('statusUpdateFailed');
    Exit;
  end;

  if (Http.Status <> 200) and (Http.Status <> 201) then
  begin
    Result := T('statusUpdateDenied') + ' (HTTP ' + IntToStr(Http.Status) + ').' + #13#10 +
      T('statusUpdateDeniedAdvice') + #13#10 +
      T('responseLabel') + ResponseBody;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PageID = ConfigPage.ID) and IsSilentWithConfig() then Result := True;
end;

function LocalizedWindowTitle(): string;
begin
  if AgentLocale = 'ca_ES' then
    Result := 'Instal·lació - {#MyAppDisplayName} versió {#MyAppVersion}'
  else if AgentLocale = 'eu_ES' then
    Result := 'Instalazioa - {#MyAppDisplayName} {#MyAppVersion} bertsioa'
  else if AgentLocale = 'gl_ES' then
    Result := 'Instalacion - {#MyAppDisplayName} version {#MyAppVersion}'
  else if AgentLocale = 'fr_FR' then
    Result := 'Installer - {#MyAppDisplayName} version {#MyAppVersion}'
  else if AgentLocale = 'de_DE' then
    Result := 'Installieren - {#MyAppDisplayName} Version {#MyAppVersion}'
  else if AgentLocale = 'it_IT' then
    Result := 'Installa - {#MyAppDisplayName} versione {#MyAppVersion}'
  else if AgentLocale = 'ja_JP' then
    Result := 'インストール - {#MyAppDisplayName} バージョン {#MyAppVersion}'
  else if AgentLocale = 'zh_CN' then
    Result := '安装 - {#MyAppDisplayName} 版本 {#MyAppVersion}'
  else if AgentLocale = 'es_ES' then
    Result := 'Instalar - {#MyAppDisplayName} versión {#MyAppVersion}'
  else
    Result := 'Install - {#MyAppDisplayName} version {#MyAppVersion}';
end;

procedure ApplyLocalizedWizardChrome();
begin
  WizardForm.Caption := LocalizedWindowTitle();
end;

procedure ApplyLocalizedLicensePage(); forward;

// AgentLocale is the only source of truth for visible wizard text; Inno can
// still fall back to another ActiveLanguage depending on how the EXE is opened.
function LocalizedInstallerText(Key: string): string;
begin
  if AgentLocale = 'ca_ES' then
  begin
    if Key = 'buttonBack' then Result := 'Enrere'
    else if Key = 'buttonNext' then Result := 'Següent'
    else if Key = 'buttonInstall' then Result := 'Instal·la'
    else if Key = 'buttonCancel' then Result := 'Cancel·la'
    else if Key = 'buttonFinish' then Result := 'Finalitza'
    else if Key = 'welcomeTitle' then Result := 'Benvingut a l''assistent d''instal·lació de {#MyAppDisplayName}'
    else if Key = 'welcomeDescription' then Result := 'Aquest agent recollirà l''inventari d''aquest equip i enviarà les dades a Firulai. Un cop instal·lat, enviarà les dades inicials a Firulai i repetirà l''enviament automàticament cada nit.'
    else if Key = 'readyTitle' then Result := 'A punt per instal·lar'
    else if Key = 'readyDescription' then Result := 'El programa ja està a punt per iniciar la instal·lació de {#MyAppDisplayName} al sistema.'
    else if Key = 'readyInstruction' then Result := 'Fes clic a Instal·la per continuar.'
    else if Key = 'installingTitle' then Result := 'Instal·lant'
    else if Key = 'installingDescription' then Result := 'Espera mentre s''instal·la {#MyAppDisplayName} al sistema.'
    else if Key = 'installingStatus' then Result := 'Instal·lant...'
    else if Key = 'finishedTitle' then Result := 'Instal·lació completada'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} s''ha instal·lat correctament al sistema.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'eu_ES' then
  begin
    if Key = 'buttonBack' then Result := 'Atzera'
    else if Key = 'buttonNext' then Result := 'Hurrengoa'
    else if Key = 'buttonInstall' then Result := 'Instalatu'
    else if Key = 'buttonCancel' then Result := 'Utzi'
    else if Key = 'buttonFinish' then Result := 'Amaitu'
    else if Key = 'welcomeTitle' then Result := 'Ongi etorri {#MyAppDisplayName} instalatzeko morroira'
    else if Key = 'welcomeDescription' then Result := 'Agente honek ordenagailu honen inbentarioa bildu eta datuak Firulaira bidaliko ditu. Instalatu ondoren, hasierako datuak Firulaira bidaliko ditu eta bidalketa automatikoki errepikatuko du gauero.'
    else if Key = 'readyTitle' then Result := 'Instalatzeko prest'
    else if Key = 'readyDescription' then Result := 'Programa prest dago {#MyAppDisplayName} sisteman instalatzen hasteko.'
    else if Key = 'readyInstruction' then Result := 'Egin klik Instalatu botoian jarraitzeko.'
    else if Key = 'installingTitle' then Result := 'Instalatzen'
    else if Key = 'installingDescription' then Result := 'Itxaron {#MyAppDisplayName} sisteman instalatzen den bitartean.'
    else if Key = 'installingStatus' then Result := 'Instalatzen...'
    else if Key = 'finishedTitle' then Result := 'Instalazioa osatu da'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} behar bezala instalatu da sisteman.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'gl_ES' then
  begin
    if Key = 'buttonBack' then Result := 'Atrás'
    else if Key = 'buttonNext' then Result := 'Seguinte'
    else if Key = 'buttonInstall' then Result := 'Instalar'
    else if Key = 'buttonCancel' then Result := 'Cancelar'
    else if Key = 'buttonFinish' then Result := 'Finalizar'
    else if Key = 'welcomeTitle' then Result := 'Benvido ao asistente de instalación de {#MyAppDisplayName}'
    else if Key = 'welcomeDescription' then Result := 'Este axente recollerá o inventario deste equipo e enviará os datos a Firulai. Unha vez instalado, enviará os datos iniciais a Firulai e repetirá o envío automaticamente cada noite.'
    else if Key = 'readyTitle' then Result := 'Listo para instalar'
    else if Key = 'readyDescription' then Result := 'O programa está listo para iniciar a instalación de {#MyAppDisplayName} no sistema.'
    else if Key = 'readyInstruction' then Result := 'Fai clic en Instalar para continuar.'
    else if Key = 'installingTitle' then Result := 'Instalando'
    else if Key = 'installingDescription' then Result := 'Agarda mentres se instala {#MyAppDisplayName} no sistema.'
    else if Key = 'installingStatus' then Result := 'Instalando...'
    else if Key = 'finishedTitle' then Result := 'Instalación completada'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} instalouse correctamente no sistema.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'fr_FR' then
  begin
    if Key = 'buttonBack' then Result := 'Retour'
    else if Key = 'buttonNext' then Result := 'Suivant'
    else if Key = 'buttonInstall' then Result := 'Installer'
    else if Key = 'buttonCancel' then Result := 'Annuler'
    else if Key = 'buttonFinish' then Result := 'Terminer'
    else if Key = 'welcomeTitle' then Result := 'Bienvenue dans l''assistant d''installation de {#MyAppDisplayName}'
    else if Key = 'welcomeDescription' then Result := 'Cet agent collectera l''inventaire de cet ordinateur et enverra les donnees a Firulai. Une fois installe, il enverra les donnees initiales a Firulai et repetera l''envoi automatiquement chaque nuit.'
    else if Key = 'readyTitle' then Result := 'Prêt à installer'
    else if Key = 'readyDescription' then Result := 'Le programme est prêt à commencer l''installation de {#MyAppDisplayName} sur le système.'
    else if Key = 'readyInstruction' then Result := 'Cliquez sur Installer pour continuer.'
    else if Key = 'installingTitle' then Result := 'Installation'
    else if Key = 'installingDescription' then Result := 'Veuillez patienter pendant l''installation de {#MyAppDisplayName} sur le système.'
    else if Key = 'installingStatus' then Result := 'Installation...'
    else if Key = 'finishedTitle' then Result := 'Installation terminée'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} a été installé correctement sur le système.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'de_DE' then
  begin
    if Key = 'buttonBack' then Result := 'Zurück'
    else if Key = 'buttonNext' then Result := 'Weiter'
    else if Key = 'buttonInstall' then Result := 'Installieren'
    else if Key = 'buttonCancel' then Result := 'Abbrechen'
    else if Key = 'buttonFinish' then Result := 'Fertigstellen'
    else if Key = 'welcomeTitle' then Result := 'Willkommen beim Installationsassistenten von {#MyAppDisplayName}'
    else if Key = 'welcomeDescription' then Result := 'Dieser Agent erfasst das Inventar dieses Computers und sendet die Daten an Firulai. Nach der Installation sendet er die ersten Daten an Firulai und wiederholt das Senden jede Nacht automatisch.'
    else if Key = 'readyTitle' then Result := 'Bereit zur Installation'
    else if Key = 'readyDescription' then Result := 'Das Programm ist bereit, {#MyAppDisplayName} auf dem System zu installieren.'
    else if Key = 'readyInstruction' then Result := 'Klicken Sie auf Installieren, um fortzufahren.'
    else if Key = 'installingTitle' then Result := 'Installation läuft'
    else if Key = 'installingDescription' then Result := 'Bitte warten Sie, während {#MyAppDisplayName} auf dem System installiert wird.'
    else if Key = 'installingStatus' then Result := 'Installation läuft...'
    else if Key = 'finishedTitle' then Result := 'Installation abgeschlossen'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} wurde erfolgreich auf dem System installiert.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'it_IT' then
  begin
    if Key = 'buttonBack' then Result := 'Indietro'
    else if Key = 'buttonNext' then Result := 'Avanti'
    else if Key = 'buttonInstall' then Result := 'Installa'
    else if Key = 'buttonCancel' then Result := 'Annulla'
    else if Key = 'buttonFinish' then Result := 'Fine'
    else if Key = 'welcomeTitle' then Result := 'Benvenuto nella procedura guidata di installazione di {#MyAppDisplayName}'
    else if Key = 'welcomeDescription' then Result := 'Questo agente raccogliera l''inventario di questo computer e inviera i dati a Firulai. Una volta installato, inviera i dati iniziali a Firulai e ripetera l''invio automaticamente ogni notte.'
    else if Key = 'readyTitle' then Result := 'Pronto per l''installazione'
    else if Key = 'readyDescription' then Result := 'Il programma è pronto per avviare l''installazione di {#MyAppDisplayName} nel sistema.'
    else if Key = 'readyInstruction' then Result := 'Fai clic su Installa per continuare.'
    else if Key = 'installingTitle' then Result := 'Installazione'
    else if Key = 'installingDescription' then Result := 'Attendi mentre {#MyAppDisplayName} viene installato nel sistema.'
    else if Key = 'installingStatus' then Result := 'Installazione...'
    else if Key = 'finishedTitle' then Result := 'Installazione completata'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} è stato installato correttamente nel sistema.'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'ja_JP' then
  begin
    if Key = 'buttonBack' then Result := '戻る'
    else if Key = 'buttonNext' then Result := '次へ'
    else if Key = 'buttonInstall' then Result := 'インストール'
    else if Key = 'buttonCancel' then Result := 'キャンセル'
    else if Key = 'buttonFinish' then Result := '完了'
    else if Key = 'welcomeTitle' then Result := '{#MyAppDisplayName} セットアップ ウィザードへようこそ'
    else if Key = 'welcomeDescription' then Result := 'このエージェントは、このコンピューターのインベントリを収集して Firulai に送信します。インストール後、初回データを Firulai に送信し、その後は毎晩自動的に送信します。'
    else if Key = 'readyTitle' then Result := 'インストールの準備完了'
    else if Key = 'readyDescription' then Result := '{#MyAppDisplayName} をこのシステムにインストールする準備ができました。'
    else if Key = 'readyInstruction' then Result := '続行するには、インストールをクリックしてください。'
    else if Key = 'installingTitle' then Result := 'インストール中'
    else if Key = 'installingDescription' then Result := '{#MyAppDisplayName} をシステムにインストールしています。しばらくお待ちください。'
    else if Key = 'installingStatus' then Result := 'インストール中...'
    else if Key = 'finishedTitle' then Result := 'インストール完了'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} はシステムに正常にインストールされました。'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'zh_CN' then
  begin
    if Key = 'buttonBack' then Result := '上一步'
    else if Key = 'buttonNext' then Result := '下一步'
    else if Key = 'buttonInstall' then Result := '安装'
    else if Key = 'buttonCancel' then Result := '取消'
    else if Key = 'buttonFinish' then Result := '完成'
    else if Key = 'welcomeTitle' then Result := '欢迎使用 {#MyAppDisplayName} 安装向导'
    else if Key = 'welcomeDescription' then Result := '此代理将收集此计算机的清单并将数据发送到 Firulai。安装后，它会向 Firulai 发送初始数据，并在之后每晚自动重复发送。'
    else if Key = 'readyTitle' then Result := '准备安装'
    else if Key = 'readyDescription' then Result := '程序已准备好开始在系统中安装 {#MyAppDisplayName}。'
    else if Key = 'readyInstruction' then Result := '单击“安装”继续。'
    else if Key = 'installingTitle' then Result := '正在安装'
    else if Key = 'installingDescription' then Result := '请稍候，正在系统中安装 {#MyAppDisplayName}。'
    else if Key = 'installingStatus' then Result := '正在安装...'
    else if Key = 'finishedTitle' then Result := '安装完成'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} 已成功安装到系统中。'
    else Result := Key;
    Exit;
  end;

  if AgentLocale = 'es_ES' then
  begin
    if Key = 'buttonBack' then Result := 'Atrás'
    else if Key = 'buttonNext' then Result := 'Siguiente'
    else if Key = 'buttonInstall' then Result := 'Instalar'
    else if Key = 'buttonCancel' then Result := 'Cancelar'
    else if Key = 'buttonFinish' then Result := 'Finalizar'
    else if Key = 'welcomeTitle' then Result := 'Bienvenido al asistente de instalación de {#MyAppDisplayName}'
    else if Key = 'welcomeDescription' then Result := 'Este agente recopilará el inventario de este equipo y enviará los datos a Firulai. Una vez instalado, enviará los datos iniciales a Firulai y repetirá el envío automáticamente cada noche.'
    else if Key = 'readyTitle' then Result := 'Listo para instalar'
    else if Key = 'readyDescription' then Result := 'El programa está listo para iniciar la instalación de {#MyAppDisplayName} en el sistema.'
    else if Key = 'readyInstruction' then Result := 'Haz clic en Instalar para continuar.'
    else if Key = 'installingTitle' then Result := 'Instalando'
    else if Key = 'installingDescription' then Result := 'Espera mientras se instala {#MyAppDisplayName} en el sistema.'
    else if Key = 'installingStatus' then Result := 'Instalando...'
    else if Key = 'finishedTitle' then Result := 'Instalación completada'
    else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} se ha instalado correctamente en el sistema.'
    else Result := Key;
    Exit;
  end;

  if Key = 'buttonBack' then Result := 'Back'
  else if Key = 'buttonNext' then Result := 'Next'
  else if Key = 'buttonInstall' then Result := 'Install'
  else if Key = 'buttonCancel' then Result := 'Cancel'
  else if Key = 'buttonFinish' then Result := 'Finish'
  else if Key = 'welcomeTitle' then Result := 'Welcome to the {#MyAppDisplayName} Setup Wizard'
  else if Key = 'welcomeDescription' then Result := 'This agent will collect this computer inventory and send the data to Firulai. Once installed, it will send the initial data to Firulai and repeat the send automatically every night.'
  else if Key = 'readyTitle' then Result := 'Ready to Install'
  else if Key = 'readyDescription' then Result := 'Setup is ready to begin installing {#MyAppDisplayName} on your system.'
  else if Key = 'readyInstruction' then Result := 'Click Install to continue.'
  else if Key = 'installingTitle' then Result := 'Installing'
  else if Key = 'installingDescription' then Result := 'Please wait while {#MyAppDisplayName} is installed on your system.'
  else if Key = 'installingStatus' then Result := 'Installing...'
  else if Key = 'finishedTitle' then Result := 'Installation Complete'
  else if Key = 'finishedDescription' then Result := '{#MyAppDisplayName} has been installed successfully on your system.'
  else Result := Key;
end;

procedure ApplyLocalizedButtons(CurPageID: Integer);
begin
  WizardForm.BackButton.Caption := LocalizedInstallerText('buttonBack');
  WizardForm.CancelButton.Caption := LocalizedInstallerText('buttonCancel');

  if CurPageID = wpReady then
    WizardForm.NextButton.Caption := LocalizedInstallerText('buttonInstall')
  else if CurPageID = wpFinished then
    WizardForm.NextButton.Caption := LocalizedInstallerText('buttonFinish')
  else
    WizardForm.NextButton.Caption := LocalizedInstallerText('buttonNext');
end;

procedure ApplyLocalizedStandardPage(CurPageID: Integer);
begin
  ApplyLocalizedWizardChrome();
  ApplyLocalizedButtons(CurPageID);

  if CurPageID = wpWelcome then
  begin
    WizardForm.WelcomeLabel1.Caption := LocalizedInstallerText('welcomeTitle');
    WizardForm.WelcomeLabel2.Caption := LocalizedInstallerText('welcomeDescription');
  end
  else if CurPageID = wpLicense then
    ApplyLocalizedLicensePage()
  else if CurPageID = ConfigPage.ID then
  begin
    WizardForm.PageNameLabel.Caption := T('configTitle');
    WizardForm.PageDescriptionLabel.Caption := T('configDescription');
  end
  else if CurPageID = wpReady then
  begin
    WizardForm.PageNameLabel.Caption := LocalizedInstallerText('readyTitle');
    WizardForm.PageDescriptionLabel.Caption := LocalizedInstallerText('readyDescription');
    WizardForm.ReadyLabel.Caption := LocalizedInstallerText('readyInstruction');
  end
  else if CurPageID = wpInstalling then
  begin
    WizardForm.PageNameLabel.Caption := LocalizedInstallerText('installingTitle');
    WizardForm.PageDescriptionLabel.Caption := LocalizedInstallerText('installingDescription');
    WizardForm.StatusLabel.Caption := LocalizedInstallerText('installingStatus');
  end
  else if CurPageID = wpFinished then
  begin
    WizardForm.FinishedHeadingLabel.Caption := LocalizedInstallerText('finishedTitle');
    WizardForm.FinishedLabel.Caption := LocalizedInstallerText('finishedDescription');
  end;
end;

function LocalizedExitMessage(): string;
begin
  if AgentLocale = 'ca_ES' then
    Result := 'La instal·lació no s''ha completat. Si surts ara, el programa no s''instal·larà.' + #13#10#13#10 +
      'Podràs tornar a executar el programa d''instal·lació més endavant per completar-la.' + #13#10#13#10 +
      'Vols sortir de la instal·lació?'
  else if AgentLocale = 'eu_ES' then
    Result := 'Instalazioa ez da oraindik osatu. Orain bertan behera uzten baduzu, programa ez da instalatuko.' + #13#10#13#10 +
      'Instalazio programa berriro exekutatu ahal izango duzu beste une batean osatzeko.' + #13#10#13#10 +
      'Instalaziotik irten nahi duzu?'
  else if AgentLocale = 'gl_ES' then
    Result := 'A instalacion ainda non se completou. Se cancelas agora, o programa non se instalara.' + #13#10#13#10 +
      'Poderas executar novamente o programa de instalacion noutra ocasion para completala.' + #13#10#13#10 +
      'Sair da instalacion?'
  else if AgentLocale = 'fr_FR' then
    Result := 'L''installation n''est pas encore terminee. Si vous annulez maintenant, le programme ne sera pas installe.' + #13#10#13#10 +
      'Vous pourrez relancer le programme d''installation plus tard pour la terminer.' + #13#10#13#10 +
      'Quitter l''installation ?'
  else if AgentLocale = 'de_DE' then
    Result := 'Die Installation ist noch nicht abgeschlossen. Wenn Sie jetzt abbrechen, wird das Programm nicht installiert.' + #13#10#13#10 +
      'Sie koennen das Installationsprogramm spaeter erneut ausfuehren, um die Installation abzuschliessen.' + #13#10#13#10 +
      'Installation beenden?'
  else if AgentLocale = 'it_IT' then
    Result := 'L''installazione non e ancora stata completata. Se annulli ora, il programma non verra installato.' + #13#10#13#10 +
      'Potrai eseguire di nuovo il programma di installazione in un altro momento per completarla.' + #13#10#13#10 +
      'Uscire dall''installazione?'
  else if AgentLocale = 'ja_JP' then
    Result := 'インストールはまだ完了していません。今キャンセルすると、プログラムはインストールされません。' + #13#10#13#10 +
      'あとでインストーラーを再実行して完了できます。' + #13#10#13#10 +
      'インストールを終了しますか?'
  else if AgentLocale = 'zh_CN' then
    Result := '安装尚未完成。如果现在取消，程序将不会安装。' + #13#10#13#10 +
      '你可以稍后重新运行安装程序以完成安装。' + #13#10#13#10 +
      '要退出安装吗？'
  else if AgentLocale = 'es_ES' then
    Result := 'La instalacion no se ha completado aun. Si cancelas ahora, el programa no se instalara.' + #13#10#13#10 +
      'Puedes ejecutar nuevamente el programa de instalacion en otra ocasion para completarla.' + #13#10#13#10 +
      'Salir de la instalacion?'
  else
    Result := 'Setup has not completed yet. If you cancel now, the program will not be installed.' + #13#10#13#10 +
      'You can run Setup again later to complete the installation.' + #13#10#13#10 +
      'Exit Setup?';
end;

function LocalizedYesButton(): string;
begin
  if AgentLocale = 'ca_ES' then Result := 'Sí'
  else if AgentLocale = 'eu_ES' then Result := 'Bai'
  else if AgentLocale = 'fr_FR' then Result := 'Oui'
  else if AgentLocale = 'de_DE' then Result := 'Ja'
  else if AgentLocale = 'ja_JP' then Result := 'はい'
  else if AgentLocale = 'zh_CN' then Result := '是'
  else if AgentLocale = 'es_ES' then Result := 'Sí'
  else Result := 'Yes';
end;

function LocalizedNoButton(): string;
begin
  if AgentLocale = 'eu_ES' then Result := 'Ez'
  else if AgentLocale = 'gl_ES' then Result := 'Non'
  else if AgentLocale = 'fr_FR' then Result := 'Non'
  else if AgentLocale = 'de_DE' then Result := 'Nein'
  else if AgentLocale = 'ja_JP' then Result := 'いいえ'
  else if AgentLocale = 'zh_CN' then Result := '否'
  else Result := 'No';
end;

function RuntimeLicenseFileName(): string;
begin
  if AgentLocale = 'ca_ES' then Result := 'LICENSE-ca.txt'
  else if AgentLocale = 'eu_ES' then Result := 'LICENSE-eu.txt'
  else if AgentLocale = 'gl_ES' then Result := 'LICENSE-gl.txt'
  else if AgentLocale = 'fr_FR' then Result := 'LICENSE-fr.txt'
  else if AgentLocale = 'de_DE' then Result := 'LICENSE-de.txt'
  else if AgentLocale = 'it_IT' then Result := 'LICENSE-it.txt'
  else if AgentLocale = 'ja_JP' then Result := 'LICENSE-ja.txt'
  else if AgentLocale = 'zh_CN' then Result := 'LICENSE-zh-CN.txt'
  else if AgentLocale = 'es_ES' then Result := 'LICENSE-es.txt'
  else Result := 'LICENSE-en.txt';
end;

function LocalizedLicenseTitle(): string;
begin
  if AgentLocale = 'ca_ES' then Result := 'Acord de llicència'
  else if AgentLocale = 'eu_ES' then Result := 'Lizentzia-akordioa'
  else if AgentLocale = 'gl_ES' then Result := 'Acordo de licenza'
  else if AgentLocale = 'fr_FR' then Result := 'Contrat de licence'
  else if AgentLocale = 'de_DE' then Result := 'Lizenzvereinbarung'
  else if AgentLocale = 'it_IT' then Result := 'Contratto di licenza'
  else if AgentLocale = 'ja_JP' then Result := 'ライセンス契約'
  else if AgentLocale = 'zh_CN' then Result := '许可协议'
  else if AgentLocale = 'es_ES' then Result := 'Acuerdo de licencia'
  else Result := 'License Agreement';
end;

function LocalizedLicenseDescription(): string;
begin
  if AgentLocale = 'ca_ES' then Result := 'És important que llegeixis la informació següent abans de continuar.'
  else if AgentLocale = 'eu_ES' then Result := 'Jarraitu aurretik informazio hau irakurtzea garrantzitsua da.'
  else if AgentLocale = 'gl_ES' then Result := 'É importante que leas a seguinte información antes de continuar.'
  else if AgentLocale = 'fr_FR' then Result := 'Veuillez lire les informations suivantes avant de continuer.'
  else if AgentLocale = 'de_DE' then Result := 'Bitte lesen Sie die folgenden Informationen, bevor Sie fortfahren.'
  else if AgentLocale = 'it_IT' then Result := 'Leggi le seguenti informazioni prima di continuare.'
  else if AgentLocale = 'ja_JP' then Result := '続行する前に、次の情報をお読みください。'
  else if AgentLocale = 'zh_CN' then Result := '继续之前，请阅读以下信息。'
  else if AgentLocale = 'es_ES' then Result := 'Es importante que lea la siguiente información antes de continuar.'
  else Result := 'Please read the following important information before continuing.';
end;

function LocalizedLicenseInstruction(): string;
begin
  if AgentLocale = 'ca_ES' then Result := 'Llegeix l''acord de llicència següent. Has d''acceptar-ne les clàusules abans de continuar amb la instal·lació.'
  else if AgentLocale = 'eu_ES' then Result := 'Irakurri lizentzia-akordio hau. Instalazioarekin jarraitu aurretik akordioaren baldintzak onartu behar dituzu.'
  else if AgentLocale = 'gl_ES' then Result := 'Le o seguinte acordo de licenza. Debes aceptar as súas cláusulas antes de continuar coa instalación.'
  else if AgentLocale = 'fr_FR' then Result := 'Veuillez lire le contrat de licence suivant. Vous devez accepter ses clauses avant de poursuivre l''installation.'
  else if AgentLocale = 'de_DE' then Result := 'Bitte lesen Sie die folgende Lizenzvereinbarung. Sie muessen die Bedingungen akzeptieren, bevor Sie mit der Installation fortfahren.'
  else if AgentLocale = 'it_IT' then Result := 'Leggi il seguente contratto di licenza. Devi accettarne le clausole prima di continuare l''installazione.'
  else if AgentLocale = 'ja_JP' then Result := '次のライセンス契約をお読みください。インストールを続行する前に、契約条項に同意する必要があります。'
  else if AgentLocale = 'zh_CN' then Result := '请阅读以下许可协议。继续安装前，你必须接受该协议的条款。'
  else if AgentLocale = 'es_ES' then Result := 'Por favor, lea el siguiente acuerdo de licencia. Debe aceptar las cláusulas de este acuerdo antes de continuar con la instalación.'
  else Result := 'Please read the following license agreement. You must accept its terms before continuing with the installation.';
end;

function LocalizedAcceptLicense(): string;
begin
  if AgentLocale = 'ca_ES' then Result := 'Accepto l''acord'
  else if AgentLocale = 'eu_ES' then Result := 'Akordioa onartzen dut'
  else if AgentLocale = 'gl_ES' then Result := 'Acepto o acordo'
  else if AgentLocale = 'fr_FR' then Result := 'J''accepte le contrat'
  else if AgentLocale = 'de_DE' then Result := 'Ich akzeptiere die Vereinbarung'
  else if AgentLocale = 'it_IT' then Result := 'Accetto il contratto'
  else if AgentLocale = 'ja_JP' then Result := '契約に同意します'
  else if AgentLocale = 'zh_CN' then Result := '我接受协议'
  else if AgentLocale = 'es_ES' then Result := 'Acepto el acuerdo'
  else Result := 'I accept the agreement';
end;

function LocalizedRejectLicense(): string;
begin
  if AgentLocale = 'ca_ES' then Result := 'No accepto l''acord'
  else if AgentLocale = 'eu_ES' then Result := 'Ez dut akordioa onartzen'
  else if AgentLocale = 'gl_ES' then Result := 'Non acepto o acordo'
  else if AgentLocale = 'fr_FR' then Result := 'Je n''accepte pas le contrat'
  else if AgentLocale = 'de_DE' then Result := 'Ich akzeptiere die Vereinbarung nicht'
  else if AgentLocale = 'it_IT' then Result := 'Non accetto il contratto'
  else if AgentLocale = 'ja_JP' then Result := '契約に同意しません'
  else if AgentLocale = 'zh_CN' then Result := '我不接受协议'
  else if AgentLocale = 'es_ES' then Result := 'No acepto el acuerdo'
  else Result := 'I do not accept the agreement';
end;

procedure ApplyLocalizedLicensePage();
var
  LicenseText: AnsiString;
  LicenseFileName: string;
begin
  LicenseFileName := RuntimeLicenseFileName();
  ExtractTemporaryFile(LicenseFileName);
  if LoadStringFromFile(ExpandConstant('{tmp}\' + LicenseFileName), LicenseText) then
    WizardForm.LicenseMemo.Text := LicenseText;

  WizardForm.PageNameLabel.Caption := LocalizedLicenseTitle();
  WizardForm.PageDescriptionLabel.Caption := LocalizedLicenseDescription();
  WizardForm.LicenseLabel1.Caption := LocalizedLicenseInstruction();
  WizardForm.LicenseAcceptedRadio.Caption := LocalizedAcceptLicense();
  WizardForm.LicenseNotAcceptedRadio.Caption := LocalizedRejectLicense();
end;

procedure InitializeWizard();
begin
  ResolveAgentLocale(CmdParam('TOKEN'));
  ApplyLocalizedWizardChrome();
  ConfigPage := CreateInputQueryPage(
    wpSelectDir,
    T('configTitle'),
    T('configSubtitle'),
    T('configDescription')
  );
  ConfigPage.Caption := T('configTitle');
  ConfigPage.Description := T('configDescription');
  ConfigPage.Add('UUID:', False);
  ConfigPage.Add(T('aliasLabel'), False);
  ConfigPage.Add('Agent token:', True);
  ConfigPage.Values[0] := CmdParam('UUID');
  ConfigPage.Values[1] := CmdParam('ALIAS');
  ConfigPage.Values[2] := CmdParam('TOKEN');
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  ApplyLocalizedStandardPage(CurPageID);
end;

procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
var
  ButtonLabels: TArrayOfString;
begin
  if CurPageID = wpFinished then Exit;

  Confirm := False;
  SetArrayLength(ButtonLabels, 2);
  ButtonLabels[0] := LocalizedYesButton();
  ButtonLabels[1] := LocalizedNoButton();
  Cancel := TaskDialogMsgBox('', LocalizedExitMessage(), mbConfirmation, MB_YESNO, ButtonLabels, 0) = IDYES;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = ConfigPage.ID then
  begin
    ResolveAgentLocale(ConfigPage.Values[2]);
    if not IsUuid(ConfigPage.Values[0]) then
    begin
      MsgBox(T('invalidUuid'), mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(ConfigPage.Values[1]) = '' then
    begin
      MsgBox(T('aliasRequired'), mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(ConfigPage.Values[2]) = '' then
    begin
      MsgBox(T('tokenRequired'), mbError, MB_OK);
      Result := False;
      Exit;
    end;

    Exit;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ConfigPath: string;
  ConfigJson: string;
  ResultCode: Integer;
  ValidationError: string;
begin
  Result := '';
  ResolveAgentLocale(EffectiveToken());

  if not IsUuid(EffectiveUuid()) then
  begin
    Result := T('missingUuidCli');
    Exit;
  end;

  if EffectiveAlias() = '' then
  begin
    Result := T('missingAliasCli');
    Exit;
  end;

  if EffectiveToken() = '' then
  begin
    Result := T('missingTokenCli');
    Exit;
  end;

  ValidationError := CheckLocalAgentInstallation();
  if ValidationError <> '' then
  begin
    Result := ValidationError;
    Exit;
  end;

  ValidationError := CheckUuidAvailable();
  if ValidationError <> '' then
  begin
    Result := ValidationError;
    Exit;
  end;

  ValidationError := SaveAliasInRsm();
  if ValidationError <> '' then
  begin
    Result := ValidationError;
    Exit;
  end;

  ValidationError := SaveHostnameStatusInRsm('{#RsmSystemHostnameStatusActiveValue}');
  if ValidationError <> '' then
  begin
    Result := ValidationError;
    Exit;
  end;

  ForceDirectories(ExpandConstant('{commonappdata}\RSAgent'));
  ForceDirectories(ExpandConstant('{commonappdata}\RSAgent\logs'));

  ConfigPath := ExpandConstant('{commonappdata}\RSAgent\config.json');

  if FileExists(ConfigPath) then
  begin
    Exec(ExpandConstant('{sys}\icacls.exe'), '"' + ConfigPath + '" /grant:r *S-1-5-32-544:F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if not DeleteFile(ConfigPath) then
    begin
      Result := T('replaceConfigFailed') + ConfigPath + T('replaceConfigAdvice');
      Exit;
    end;
  end;

  ConfigJson :=
    '{' + #13#10 +
    '  "token": "' + JsonEscape(EffectiveToken()) + '",' + #13#10 +
    '  "uuid": "' + JsonEscape(EffectiveUuid()) + '",' + #13#10 +
    '  "api_url": "{#DefaultApiUrl}",' + #13#10 +
    '  "locale": "' + JsonEscape(AgentLocale) + '"' + #13#10 +
    '}';

  if not SaveStringToFile(ConfigPath, ConfigJson, False) then
  begin
    Result := T('writeConfigFailed') + ConfigPath;
    Exit;
  end;

  Exec(ExpandConstant('{sys}\icacls.exe'), '"' + ConfigPath + '" /inheritance:r /grant:r *S-1-5-18:F *S-1-5-32-544:R', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function MarkSystemDisconnectedInRsm(): Boolean;
var
  ResultCode: Integer;
  AgentPath: string;
  ConfigPath: string;
  InstalledLocale: string;
begin
  Result := True;
  AgentPath := ExpandConstant('{app}\{#MyAppExeName}');
  ConfigPath := ExpandConstant('{commonappdata}\RSAgent\config.json');
  InstalledLocale := ExtractInstalledLocale();
  if InstalledLocale <> '' then
  begin
    AgentLocale := NormalizeLocale(InstalledLocale);
  end;

  if (not FileExists(AgentPath)) or (not FileExists(ConfigPath)) then
  begin
    Exit;
  end;

  Exec(ExpandConstant('{sys}\sc.exe'), 'stop RSAgent', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if not Exec(AgentPath, '--mark-disconnected-on-uninstall', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox(T('uninstallContactFailed'), mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if ResultCode <> 0 then
  begin
    if ResultCode = 2 then
    begin
      Result := True;
    end
    else
    begin
      MsgBox(T('uninstallUpdateFailed'), mbError, MB_OK);
      Result := False;
    end;
  end
  else if ResultCode = -1 then
  begin
    MsgBox(T('uninstallSuccess'), mbInformation, MB_OK);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    if not MarkSystemDisconnectedInRsm() then
    begin
      Abort;
    end;
  end;

  if CurUninstallStep = usPostUninstall then
  begin
    Exec(ExpandConstant('{sys}\icacls.exe'), '"' + ExpandConstant('{commonappdata}\RSAgent') + '" /grant:r *S-1-5-32-544:F /T /C', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    DelTree(ExpandConstant('{commonappdata}\RSAgent'), True, True, True);
  end;
end;
