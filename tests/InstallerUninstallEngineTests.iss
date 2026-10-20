[Setup]
#ifdef ElevatedProbe
AppId=FirulaiIsolatedRecoveryProbeAdmin
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
#define ProbeRoot "HKEY_LOCAL_MACHINE"
#define ProbeKey "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FirulaiIsolatedRecoveryProbeAdmin_is1"
#else
AppId=FirulaiIsolatedRecoveryProbe
PrivilegesRequired=lowest
#define ProbeRoot "HKEY_CURRENT_USER"
#define ProbeKey "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FirulaiIsolatedRecoveryProbe_is1"
#endif
AppName=Firulai isolated recovery probe
AppVersion=1
DefaultDirName={tmp}\FirulaiIsolatedRecoveryProbe
OutputBaseFilename=InstallerUninstallEngineTests
DisableProgramGroupPage=yes
UninstallRestartComputer=no

[Files]
Source: "RecoveryAgent.fixture"; DestDir: "{app}"; DestName: "RsAgent.exe"

[Code]
var
  Lock: THandle;

#include "..\installer\UninstallRecovery.iss"

function CreateLockedFile(Name: string; Access, Share: Cardinal; Security: Integer;
  Creation, Attributes: Cardinal; Template: Integer): THandle;
  external 'CreateFileW@kernel32.dll stdcall';
function CloseLockedFile(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

function ProbeDataDir(): string;
begin
  Result := ExpandConstant('{param:DATADIR}');
  if Result = '' then Result := ExtractFileDir(ExpandConstant('{app}')) + '\data';
end;

function ProbeBackupDir(): string;
begin
  Result := ExpandConstant('{param:BACKUPDIR}');
  if Result = '' then Result := ExtractFileDir(ExpandConstant('{app}')) + '\backup';
end;

procedure CurStepChanged(Step: TSetupStep);
begin
  if Step = ssPostInstall then
  begin
    ForceDirectories(ProbeDataDir());
#ifdef ElevatedProbe
    if not ProtectUninstallDataDirectory(ProbeDataDir()) then RaiseException('Cannot protect fixture state');
#endif
    SaveStringToFile(ProbeDataDir() + '\config.json', 'isolated-token', False);
  end;
  if Step = ssDone then
  begin
#ifdef ElevatedProbe
    if not PreparePrivateUninstallRecovery(ExpandConstant('{app}'),
#else
    if not PrepareUninstallRecovery(ExpandConstant('{app}'),
#endif
      ProbeDataDir(), ProbeBackupDir()) then
      RaiseException('Cannot stage recovery after installation');
  end;
end;

procedure CurUninstallStepChanged(Step: TUninstallStep);
var
  AppDir, DataDir, BackupDir, Status: string;
  Code: Integer;
begin
  AppDir := ExpandConstant('{app}');
  DataDir := ProbeDataDir();
  BackupDir := ProbeBackupDir();
  if Step = usUninstall then
  begin
#ifdef ElevatedProbe
    if not PreparePrivateUninstallRecovery(AppDir, DataDir, BackupDir) then
#else
    if not PrepareUninstallRecovery(AppDir, DataDir, BackupDir) then
#endif
    begin
      SaveStringToFile(ExpandConstant('{param:RESULT}'), 'backup-blocked', False);
      Abort;
    end;
    if ExpandConstant('{param:FAIL|0}') = '1' then
    begin
      Lock := CreateLockedFile(AppDir + '\RsAgent.exe', $80000000, 1, 0, 3, $80, 0);
      if Lock = -1 then RaiseException('Cannot lock fixture');
    end;
  end;
  if Step = usPostUninstall then
  begin
    if (Lock <> 0) and (Lock <> -1) then CloseLockedFile(Lock);
    Lock := 0;
#ifdef ElevatedProbe
    Code := FinalizePrivateUninstallRecovery(AppDir, DataDir, BackupDir);
#else
    Code := FinalizeUninstallRecovery(AppDir, DataDir, BackupDir);
#endif
    if Code = 1 then
      if not RestoreUninstallEntry({#ProbeRoot}, '{#ProbeKey}',
        AppDir, 'Firulai isolated recovery probe', '1') then
        RaiseException('Could not restore isolated retry entry');
    Status := IntToStr(Code);
    SaveStringToFile(ExpandConstant('{param:RESULT}'), Status, False);
  end;
end;
