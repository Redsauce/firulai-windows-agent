; Native startup probe: always cancels before installing or changing any service.
[Setup]
AppName=Installer startup tests
AppVersion=1
DefaultDirName={autopf}\RSAgent
CreateAppDir=no
Uninstallable=no
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64
OutputBaseFilename=InstallerStartupTests

[Code]
function IsServiceInstalled(): Boolean;
begin
  // Exercise the fresh installation/reinstallation branch even on an agent host.
  Result := False;
end;

#include "..\installer\LocalInstallation.iss"

function InitializeSetup(): Boolean;
var
  Status, Directory: string;
  Installed: Boolean;
begin
  Result := False;
  Status := 'PASS';
  try
    Directory := ExistingAgentDirectory();
    if Directory = '' then RaiseException('Empty installation directory');
    Installed := IsLocalAgentInstalled();
    if Installed <> (HasLocalAgentExecutableOrUninstaller() or
      FileExists(ExpandConstant('{commonappdata}\RSAgent\config.json'))) then
      RaiseException('Local installation detection is inconsistent');
  except
    Status := 'FAIL: ' + GetExceptionMessage;
  end;
  SaveStringToFile(ExpandConstant('{param:RESULT}'), Status, False);
end;
