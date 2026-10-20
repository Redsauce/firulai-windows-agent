[Setup]
AppName=Uninstall recovery tests
AppVersion=1
DefaultDirName={tmp}\recovery-test
CreateAppDir=no
Uninstallable=no
PrivilegesRequired=lowest
OutputBaseFilename=InstallerUninstallRecoveryTests

[Code]
#include "..\installer\UninstallRecovery.iss"

function CreateLockedFile(Name: string; Access, Share: Cardinal; Security: Integer;
  Creation, Attributes: Cardinal; Template: Integer): THandle;
  external 'CreateFileW@kernel32.dll stdcall';
function CloseLockedFile(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

procedure Check(Condition: Boolean; MessageText: string);
begin
  if not Condition then RaiseException(MessageText);
end;

function InitializeSetup(): Boolean;
var
  Root, AppDir, DataDir, BackupDir, Status: string;
  Lock: THandle;
  Contents: AnsiString;
begin
  Result := False;
  Root := ExpandConstant('{param:FIXTURE}');
  AppDir := Root + '\app'; DataDir := Root + '\data'; BackupDir := Root + '\backup';
  Status := 'PASS'; Lock := 0;
  try
    Check(ForceDirectories(AppDir) and ForceDirectories(DataDir), 'fixture directories');
    Check(SaveStringToFile(AppDir + '\RsAgent.exe', 'agent-fixture', False), 'agent fixture');
    Check(SaveStringToFile(AppDir + '\unins000.exe', 'uninstaller-fixture', False), 'uninstaller fixture');
    Check(SaveStringToFile(AppDir + '\unins000.dat', 'log-fixture', False), 'log fixture');
    Check(SaveStringToFile(DataDir + '\config.json', 'private-fixture-token', False), 'config fixture');
    Check(not PrepareUninstallRecovery(AppDir, DataDir, DataDir + '\unsafe'), 'nested backup accepted');
    Check(PrepareUninstallRecovery(AppDir, DataDir, BackupDir), 'prepare backup');
    // Simulate partial application deletion leaving a locked/surviving agent.
    Check(DeleteFile(AppDir + '\unins000.exe') and DeleteFile(AppDir + '\unins000.dat'), 'remove retry files');
    Check(FinalizeUninstallRecovery(AppDir, DataDir, BackupDir) = 1, 'surviving agent must restore retry');
    Check(FileExists(AppDir + '\unins000.exe') and FileExists(AppDir + '\unins000.dat'), 'uninstaller not restored');
    // A real Windows sharing violation makes DelTree fail after deleting config.
    Check(DelTree(AppDir, True, True, True), 'remove app fixture');
    Lock := CreateLockedFile(DataDir + '\locked.bin', $40000000, 0, 0, 2, $80, 0);
    Check(Lock <> -1, 'create locked file');
    Check(FinalizeUninstallRecovery(AppDir, DataDir, BackupDir) = 1, 'failed data deletion must restore retry');
    Check(LoadStringFromFile(DataDir + '\config.json', Contents), 'config not restored');
    Check(Contents = 'private-fixture-token', 'config changed');
    Check(FileExists(AppDir + '\RsAgent.exe'), 'agent not restored for confirmation retry');
    CloseLockedFile(Lock); Lock := 0;
    // Retry: the uninstall engine removes the restored application, then finalize.
    Check(DelTree(AppDir, True, True, True), 'retry app cleanup');
    Check(FinalizeUninstallRecovery(AppDir, DataDir, BackupDir) = 0, 'retry failed');
    Check(not DirExists(DataDir) and not DirExists(BackupDir), 'retry retained data or backup');
  except
    Status := 'FAIL: ' + GetExceptionMessage;
  end;
  if (Lock <> 0) and (Lock <> -1) then CloseLockedFile(Lock);
  SaveStringToFile(ExpandConstant('{param:RESULT}'), Status, False);
end;
