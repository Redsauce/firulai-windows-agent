#define MyAppName "RSAgent"
#define MyAppDisplayName "Firulai Inventory Agent"
#define MyAppVersion "0.1.3"
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

[Setup]
AppId={{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}
AppName={#MyAppDisplayName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\RSAgent
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=..\Output
OutputBaseFilename=RSAgentSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#MyAppDisplayName}
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Agente de inventario para Firulai
VersionInfoProductName={#MyAppDisplayName}
LicenseFile=LICENSE-es.txt

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Messages]
WelcomeLabel1=Bienvenido al asistente de instalación de [name]
WelcomeLabel2=Este agente se instalará para recopilar el inventario de este equipo y mandar los datos a Firulai. Una vez instalado, enviará los datos iniciales a Firulai y repetirá el envío automáticamente cada noche.
ConfirmUninstall=Se va a desinstalar %1.%n%nEsta acción solo eliminará los archivos locales del agente junto al instalador. No se borrarán los datos de Firulai.%n%nEl sistema quedará como inactivo en Firulai. Desde Firulai podrás eliminar definitivamente sus datos o volver a instalar el agente más adelante enlazándolo al System y al inventario ya guardados.%n%n¿Quieres continuar?

[Files]
Source: "..\src\RsAgent\bin\Release\RsAgent.exe"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{commonappdata}\RSAgent"; Permissions: admins-full system-full
Name: "{commonappdata}\RSAgent\logs"; Permissions: admins-full system-full

[Registry]
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\Application\RSAgent"; ValueType: expandsz; ValueName: "EventMessageFile"; ValueData: "{win}\Microsoft.NET\Framework64\v4.0.30319\EventLogMessages.dll"
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\EventLog\Application\RSAgent"; ValueType: dword; ValueName: "TypesSupported"; ValueData: "7"

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create RSAgent binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""{#MyAppDisplayName}"""; Flags: runhidden waituntilterminated; StatusMsg: "Registrando servicio..."
Filename: "{sys}\sc.exe"; Parameters: "description RSAgent ""Recopila el inventario de software del sistema y lo envía a Firulai"""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "failure RSAgent reset= 86400 actions= restart/60000/restart/60000/""""/60000"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start RSAgent"; Flags: runhidden waituntilterminated; StatusMsg: "Arrancando servicio..."

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop RSAgent"; Flags: runhidden waituntilterminated; RunOnceId: "StopRSAgent"
Filename: "{sys}\sc.exe"; Parameters: "delete RSAgent"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteRSAgent"

[Code]
var
  ConfigPage: TInputQueryWizardPage;
  RsmSystemItemId: string;

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

function InitializeSetup(): Boolean;
begin
  Result := True;

  if WizardSilent() and not LicenseAcceptedFromCommandLine() then
  begin
    MsgBox(
      'Para realizar una instalación silenciosa debes leer y aceptar el Acuerdo de licencia y aviso de uso incluido con el instalador.' + #13#10#13#10 +
      'Si lo aceptas, vuelve a ejecutar el instalador añadiendo /ACCEPTLICENSE=yes.',
      mbError,
      MB_OK
    );
    Result := False;
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
    Result := 'Este sistema ya tiene un agente instalado con este UUID.';
  end
  else
  begin
    Result := 'Ya existe un agente instalado en este sistema.';
    if InstalledUuid <> '' then
    begin
      Result := Result + #13#10 + 'UUID instalado actualmente: ' + InstalledUuid;
    end;
    Result := Result + #13#10 + 'UUID solicitado: ' + EffectiveUuid();
  end;

  Result := Result + #13#10#13#10 +
    'Si necesitas reinstalar el agente, desinstala primero el agente actual desde Windows o ejecutando:' + #13#10 +
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
    Result := 'No se pudo validar el UUID en Firulai.' + #13#10 +
      'Comprueba la conexión a internet y que el Agent token sea correcto. La instalación no continuará sin confirmar que el UUID está disponible.';
    Exit;
  end;

  if (Http.Status <> 200) and (Http.Status <> 201) then
  begin
    Result := 'Firulai no permitió validar el UUID (HTTP ' + IntToStr(Http.Status) + ').' + #13#10 +
      'Comprueba que el token corresponde al UUID facilitado en Firulai. La instalación no continuará sin confirmar que el UUID está disponible.' + #13#10 +
      'Respuesta: ' + ResponseBody;
    Exit;
  end;

  if Pos(EffectiveUuid(), ResponseBody) = 0 then
  begin
    Result := 'UUID inválido: no existe en Firulai.' + #13#10 +
      'No se puede instalar el agente con un UUID que no haya sido generado desde Firulai.' + #13#10#13#10 +
      'UUID: ' + EffectiveUuid();
    Exit;
  end;

  RsmSystemItemId := JsonExtractFirstScalarKey(ResponseBody, 'ID');
  if RsmSystemItemId = '' then
  begin
    RsmSystemItemId := JsonExtractFirstScalarKey(ResponseBody, 'id');
  end;

  if RsmSystemItemId = '' then
  begin
    Result := 'No se pudo localizar el sistema de Firulai asociado al UUID.' + #13#10 +
      'Por seguridad, la instalación no continuará sin poder guardar el alias.';
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
    Result := 'Este sistema ya tiene un agente instalado en Firulai con este UUID.' + #13#10 +
      'No se puede realizar una nueva instalación con el mismo UUID.' + #13#10#13#10 +
      'UUID: ' + EffectiveUuid() + #13#10 +
      'Sistema en Firulai:' + #13#10 +
      '   - Hostname: ' + ExistingHostname + #13#10 +
      '   - FQDN:     ' + ExistingFqdn + #13#10 +
      'Equipo local:' + #13#10 +
      '   - Hostname: ' + LocalHostname() + #13#10 +
      '   - FQDN:     ' + LocalFqdn() + #13#10#13#10 +
      'Si necesitas reinstalar el agente, desinstala primero el agente actual.';
    Exit;
  end;

  Result := 'Este UUID ya pertenece a otro sistema en Firulai.' + #13#10 +
    'No se puede instalar este agente en el equipo local con ese UUID.';
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
    Result := 'No se pudo guardar el alias en Firulai porque no se encontró el sistema asociado al UUID.';
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
    Result := 'No se pudo guardar el alias en Firulai.' + #13#10 +
      'Comprueba la conexión y el token. La instalación no continuará sin confirmar el alias.';
    Exit;
  end;

  if (Http.Status <> 200) and (Http.Status <> 201) then
  begin
    Result := 'Firulai no permitió guardar el alias (HTTP ' + IntToStr(Http.Status) + ').' + #13#10 +
      'Comprueba que el token corresponde al UUID facilitado. La instalación no continuará sin confirmar el alias.' + #13#10 +
      'Respuesta: ' + ResponseBody;
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
    Result := 'No se pudo actualizar el estado en Firulai porque no se encontró el sistema asociado al UUID.';
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
    Result := 'No se pudo actualizar el estado en Firulai.' + #13#10 +
      'Comprueba la conexión y el token. La instalación no continuará sin activar el sistema.';
    Exit;
  end;

  if (Http.Status <> 200) and (Http.Status <> 201) then
  begin
    Result := 'Firulai no permitió actualizar el estado (HTTP ' + IntToStr(Http.Status) + ').' + #13#10 +
      'Comprueba que el token corresponde al UUID facilitado. La instalación no continuará sin activar el sistema.' + #13#10 +
      'Respuesta: ' + ResponseBody;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PageID = ConfigPage.ID) and IsSilentWithConfig() then Result := True;
end;

procedure InitializeWizard();
begin
  ConfigPage := CreateInputQueryPage(
    wpSelectDir,
    'Configuración de RSAgent',
    'Introduce los datos facilitados por Firulai',
    'Copia el UUID y el token que se te han facilitado en Firulai. Después escribe un alias para identificar este equipo. El alias se guardará en Firulai y podrás modificarlo más adelante.'
  );
  ConfigPage.Add('UUID:', False);
  ConfigPage.Add('Alias del sistema:', False);
  ConfigPage.Add('Agent token:', True);
  ConfigPage.Values[0] := CmdParam('UUID');
  ConfigPage.Values[1] := CmdParam('ALIAS');
  ConfigPage.Values[2] := CmdParam('TOKEN');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = ConfigPage.ID then
  begin
    if not IsUuid(ConfigPage.Values[0]) then
    begin
      MsgBox('Introduce un UUID válido con formato xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(ConfigPage.Values[1]) = '' then
    begin
      MsgBox('Introduce un alias para el sistema. Este campo es obligatorio para terminar la instalación y podrás modificarlo más adelante en Firulai.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Trim(ConfigPage.Values[2]) = '' then
    begin
      MsgBox('Introduce el Agent token facilitado junto al UUID. Es obligatorio para enlazar este agente con Firulai.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
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

  if not IsUuid(EffectiveUuid()) then
  begin
    Result := 'UUID obligatorio o no válido. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o rellena los campos del asistente.';
    Exit;
  end;

  if EffectiveAlias() = '' then
  begin
    Result := 'Alias obligatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o rellena los campos del asistente. Podrás modificarlo más adelante en Firulai.';
    Exit;
  end;

  if EffectiveToken() = '' then
  begin
    Result := 'Agent token obligatorio. Usa setup.exe /UUID=<UUID> /ALIAS=<ALIAS> /TOKEN=<TOKEN> o rellena los campos del asistente.';
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
      Result := 'No se pudo reemplazar ' + ConfigPath + '. Cierra el instalador, ejecútalo como Administrador o elimina el archivo manualmente.';
      Exit;
    end;
  end;

  ConfigJson :=
    '{' + #13#10 +
    '  "token": "' + JsonEscape(EffectiveToken()) + '",' + #13#10 +
    '  "uuid": "' + JsonEscape(EffectiveUuid()) + '",' + #13#10 +
    '  "api_url": "{#DefaultApiUrl}"' + #13#10 +
    '}';

  if not SaveStringToFile(ConfigPath, ConfigJson, False) then
  begin
    Result := 'No se pudo escribir ' + ConfigPath;
    Exit;
  end;

  Exec(ExpandConstant('{sys}\icacls.exe'), '"' + ConfigPath + '" /inheritance:r /grant:r *S-1-5-18:F *S-1-5-32-544:R', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function MarkSystemDisconnectedInRsm(): Boolean;
var
  ResultCode: Integer;
  AgentPath: string;
  ConfigPath: string;
begin
  Result := True;
  AgentPath := ExpandConstant('{app}\{#MyAppExeName}');
  ConfigPath := ExpandConstant('{commonappdata}\RSAgent\config.json');

  if (not FileExists(AgentPath)) or (not FileExists(ConfigPath)) then
  begin
    Exit;
  end;

  Exec(ExpandConstant('{sys}\sc.exe'), 'stop RSAgent', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if not Exec(AgentPath, '--mark-disconnected-on-uninstall', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('No se pudo contactar con el agente para marcar el sistema como inactivo en Firulai. La desinstalación se cancelará para que el estado remoto no quede desactualizado.', mbError, MB_OK);
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
      MsgBox('No se pudo marcar el sistema como inactivo en Firulai. Revisa la conectividad con Firulai y vuelve a desinstalar.', mbError, MB_OK);
      Result := False;
    end;
  end
  else if ResultCode = -1 then
  begin
    MsgBox('El sistema se ha marcado como inactivo en Firulai. No se borrarán los datos guardados; podrás eliminarlos desde Firulai o volver a instalar el agente más adelante enlazándolo a este mismo System.', mbInformation, MB_OK);
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
