// These helpers are also called during InitializeWizard, before {app} exists.
function AppUninstallRegistryKey(): string;
begin
  Result := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1';
end;

function ExistingAgentDirectory(): string;
var
  RegisteredDirectory: string;
begin
  Result := ExpandConstant('{autopf}\RSAgent');
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, AppUninstallRegistryKey(),
    'InstallLocation', RegisteredDirectory) then
  begin
    if Trim(RegisteredDirectory) <> '' then
      Result := RemoveBackslashUnlessRoot(Trim(RegisteredDirectory));
  end;
end;

function HasLocalAgentExecutableOrUninstaller(): Boolean;
begin
  Result :=
    FileExists(AddBackslash(ExistingAgentDirectory()) + 'RsAgent.exe') or
    FileExists(AddBackslash(ExistingAgentDirectory()) + 'unins000.exe');
end;

function IsLocalAgentInstalled(): Boolean;
begin
  Result :=
    IsServiceInstalled() or
    HasLocalAgentExecutableOrUninstaller() or
    FileExists(ExpandConstant('{commonappdata}\RSAgent\config.json'));
end;
