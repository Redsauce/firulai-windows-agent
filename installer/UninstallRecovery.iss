// Retry essentials stay outside both directories being removed. Callers protect
// this directory before copying credentials; no remote state is changed here.
function UninstallRecoveryAttributes(Path: string): Cardinal;
  external 'GetFileAttributesW@kernel32.dll stdcall';

function SafeUninstallRecoveryDirectory(Path: string): Boolean;
var
  Attributes: Cardinal;
begin
  Attributes := UninstallRecoveryAttributes(Path);
  Result := (Attributes = $FFFFFFFF) or ((Attributes and $400) = 0);
end;

function CopyRecoveryFile(Source, Destination: string): Boolean;
begin
  Result := FileExists(Destination) or FileCopy(Source, Destination, False);
end;

function ConvertRecoveryDescriptor(Sddl: string; Revision: Cardinal;
  var Descriptor: THandle; DescriptorSize: THandle): Boolean;
  external 'ConvertStringSecurityDescriptorToSecurityDescriptorW@advapi32.dll stdcall';
function SetRecoveryDescriptor(Path: string; Information: Cardinal; Descriptor: THandle): Boolean;
  external 'SetFileSecurityW@advapi32.dll stdcall';
function FreeRecoveryDescriptor(Descriptor: THandle): THandle;
  external 'LocalFree@kernel32.dll stdcall';

function ProtectPrivateRecoveryTree(Path: string): Boolean;
var
  Descriptor: THandle;
  Entry: TFindRec;
begin
  Result := False;
  if not SafeUninstallRecoveryDirectory(Path) then Exit;
  Descriptor := 0;
  // Replace the entire DACL atomically; adding grants alone leaves Inno's
  // explicit logon-session ACE. Children inherit only SYSTEM/Administrators.
  if not ConvertRecoveryDescriptor('D:P(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)', 1, Descriptor, 0) then Exit;
  try
    Result := SetRecoveryDescriptor(Path, $80000004, Descriptor);
  finally
    FreeRecoveryDescriptor(Descriptor);
  end;
  if not Result then Exit;
  if DirExists(Path) and FindFirst(AddBackslash(Path) + '*', Entry) then
  begin
    try
      repeat
        if (Entry.Name <> '.') and (Entry.Name <> '..') then
          if not ProtectPrivateRecoveryTree(AddBackslash(Path) + Entry.Name) then
          begin Result := False; Exit; end;
      until not FindNext(Entry);
    finally
      FindClose(Entry);
    end;
  end;
end;

function ProtectUninstallRecoveryDirectory(Path: string): Boolean;
begin
  Result := SafeUninstallRecoveryDirectory(Path) and ForceDirectories(Path);
  if Result then Result := ProtectPrivateRecoveryTree(Path);
end;

function ProtectUninstallDataDirectory(Path: string): Boolean;
begin
  Result := ProtectPrivateRecoveryTree(Path);
end;

function PrepareUninstallRecovery(AppDir, DataDir, RecoveryDir: string): Boolean;
var
  MatchingLogBackup: Boolean;
  SavedHash: AnsiString;
begin
  Result := False;
  if not SafeUninstallRecoveryDirectory(RecoveryDir) then Exit;
  if (Pos(Lowercase(AddBackslash(AppDir)), Lowercase(AddBackslash(RecoveryDir))) = 1) or
     (Pos(Lowercase(AddBackslash(DataDir)), Lowercase(AddBackslash(RecoveryDir))) = 1) then Exit;
  if not ForceDirectories(RecoveryDir) then Exit;
  MatchingLogBackup := FileExists(AddBackslash(RecoveryDir) + 'unins000.dat') and
    FileExists(AddBackslash(RecoveryDir) + 'unins000.exe') and
    LoadStringFromFile(AddBackslash(RecoveryDir) + 'unins000.dat.sha256', SavedHash);
  if MatchingLogBackup then
    MatchingLogBackup := GetSHA256OfFile(AddBackslash(RecoveryDir) + 'unins000.exe') =
      GetSHA256OfFile(AddBackslash(AppDir) + 'unins000.exe');
  if MatchingLogBackup then
    MatchingLogBackup := SavedHash = GetSHA256OfFile(AddBackslash(RecoveryDir) + 'unins000.dat');
  if not FileCopy(AddBackslash(AppDir) + 'RsAgent.exe', AddBackslash(RecoveryDir) + 'RsAgent.exe', False) then
  begin Log('Cannot back up agent'); Exit; end;
  if not FileCopy(AddBackslash(AppDir) + 'unins000.exe', AddBackslash(RecoveryDir) + 'unins000.exe', False) then
  begin Log('Cannot back up uninstaller'); Exit; end;
  if FileCopy(AddBackslash(AppDir) + 'unins000.dat', AddBackslash(RecoveryDir) + 'unins000.dat.new', False) then
  begin
    if not FileCopy(AddBackslash(RecoveryDir) + 'unins000.dat.new', AddBackslash(RecoveryDir) + 'unins000.dat', False) then Exit;
    DeleteFile(AddBackslash(RecoveryDir) + 'unins000.dat.new');
    if not SaveStringToFile(AddBackslash(RecoveryDir) + 'unins000.dat.sha256',
      GetSHA256OfFile(AddBackslash(RecoveryDir) + 'unins000.dat'), False) then Exit;
  end
  else
  begin
    // The installer stages this exact log at ssDone, before the uninstaller
    // opens it exclusively. Reject a log from a different uninstaller build.
    if not MatchingLogBackup then
    begin Log('No matching staged uninstall log for recovery'); Exit; end;
  end;
  if not FileCopy(AddBackslash(DataDir) + 'config.json', AddBackslash(RecoveryDir) + 'config.json', False) then
  begin Log('Cannot back up configuration'); Exit; end;
  Result := True;
end;

function RestoreUninstallRecovery(AppDir, DataDir, RecoveryDir: string): Boolean;
begin
  Result := ForceDirectories(AppDir) and ForceDirectories(DataDir);
  if not Result then Exit;
  // A locked surviving executable must not be overwritten. rm does not modify
  // file contents; restore only files which the partial deletion removed.
  Result :=
    CopyRecoveryFile(AddBackslash(RecoveryDir) + 'RsAgent.exe', AddBackslash(AppDir) + 'RsAgent.exe') and
    CopyRecoveryFile(AddBackslash(RecoveryDir) + 'unins000.exe', AddBackslash(AppDir) + 'unins000.exe') and
    CopyRecoveryFile(AddBackslash(RecoveryDir) + 'unins000.dat', AddBackslash(AppDir) + 'unins000.dat') and
    CopyRecoveryFile(AddBackslash(RecoveryDir) + 'config.json', AddBackslash(DataDir) + 'config.json');
end;

function FinalizeUninstallRecovery(AppDir, DataDir, RecoveryDir: string): Integer;
begin
  // 0 = complete, 1 = retry essentials restored, 2 = external backup retained.
  Result := 0;
  if not FileExists(AddBackslash(AppDir) + 'RsAgent.exe') then
    if DelTree(DataDir, True, True, True) then
    begin
      if not DelTree(RecoveryDir, True, True, True) then
        Log('Private uninstall recovery directory could not be removed: ' + RecoveryDir);
      Exit;
    end;
  Result := 2;
  if RestoreUninstallRecovery(AppDir, DataDir, RecoveryDir) then Result := 1;
end;

function RestoreUninstallEntry(RootKey: Integer; Key, AppDir, DisplayName, Version: string): Boolean;
begin
  Result := RegWriteStringValue(RootKey, Key, 'DisplayName', DisplayName) and
    RegWriteStringValue(RootKey, Key, 'DisplayVersion', Version) and
    RegWriteStringValue(RootKey, Key, 'InstallLocation', AppDir) and
    RegWriteStringValue(RootKey, Key, 'UninstallString', '"' + AddBackslash(AppDir) + 'unins000.exe"');
end;

function PreparePrivateUninstallRecovery(AppDir, DataDir, RecoveryDir: string): Boolean;
begin
  Result := ProtectUninstallRecoveryDirectory(RecoveryDir) and
    ProtectUninstallDataDirectory(DataDir);
  if not Result then Exit;
  Result := PrepareUninstallRecovery(AppDir, DataDir, RecoveryDir);
  if not Result then Exit;
  Result := ProtectUninstallRecoveryDirectory(RecoveryDir) and
    ProtectUninstallDataDirectory(DataDir);
end;

function FinalizePrivateUninstallRecovery(AppDir, DataDir, RecoveryDir: string): Integer;
begin
  Result := 2;
  if not ProtectUninstallDataDirectory(DataDir) then Exit;
  Result := FinalizeUninstallRecovery(AppDir, DataDir, RecoveryDir);
  if Result = 1 then
    if not (ProtectUninstallDataDirectory(DataDir) and ProtectUninstallRecoveryDirectory(RecoveryDir)) then
      Result := 2;
end;
