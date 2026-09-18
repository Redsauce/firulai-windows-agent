; A probe executable, not an alternative product installer. InitializeSetup
; always cancels before installation; only URL helpers are exercised.
#define EndpointFile FileOpen(AddBackslash(SourcePath) + "..\src\RsAgent\ApiEndpoint.txt")
#define DefaultApiBaseUrl Trim(FileRead(EndpointFile))
#define ApiPath Trim(FileRead(EndpointFile))
#expr FileClose(EndpointFile)
#define ApiEndpointTest
[Setup]
AppName=API endpoint tests
AppVersion=1
DefaultDirName={tmp}\ApiEndpointTests
CreateAppDir=no
Uninstallable=no
PrivilegesRequired=lowest
OutputBaseFilename=ApiEndpointTests
[Code]
var
  TestRedirectCode: Integer;
  TestCalls: Integer;

function TestTransportUrl(Url: string): string;
begin
  TestCalls := TestCalls + 1;
  if Url = 'https://rsm1.invalid/AppController/{#ApiPath}' then
    Result := 'https://httpbingo.org/redirect-to?url=https://httpbingo.org/anything/rsm2/AppController/{#ApiPath}&status_code=' + IntToStr(TestRedirectCode)
  else if Url = 'https://httpbingo.org/anything/rsm2/AppController/{#ApiPath}' then
    Result := Url
  else RaiseException('Unexpected test destination');
end;

function AppRegistryKey(): string;
begin
  Result := 'SOFTWARE\Redsauce\RSAgentTests\Unused';
end;

#include "..\installer\ApiEndpoint.iss"

procedure AssertEqual(Actual, Expected: string);
begin
  if Actual <> Expected then RaiseException('Expected ' + Expected + ', received ' + Actual);
end;

function InitializeSetup(): Boolean;
var
  Status, Base, Endpoint, Target, Body, ResponseBody, Boundary, Expected: string;
  Code, CaseIndex, RunIndex: Integer;
  Rejected: Boolean;
begin
  Result := False;
  Status := 'PASS';
  Base := 'https://rsm1.invalid/AppController/';
  Endpoint := Base + '{#ApiPath}';
  Target := 'https://rsm2.invalid/AppController/{#ApiPath}';
  try
    PendingApiBaseUrl := Base;
    AssertEqual(ApiBaseUrlValue(''), Base);
    AssertEqual(ApiRedirectUrl(Endpoint, Target), Target);
    AssertEqual(ApiRedirectUrl(Endpoint, '/Other/{#ApiPath}'), 'https://rsm1.invalid/Other/{#ApiPath}');
    AssertEqual(ApiRedirectUrl(Endpoint, 'api.php'), Endpoint);
    AssertEqual(ApiBaseFromUrl(Target), 'https://rsm2.invalid/AppController/');
    Rejected := False;
    try ApiRedirectUrl(Endpoint, 'http://rsm2.invalid/{#ApiPath}'); except Rejected := True; end;
    if not Rejected then RaiseException('HTTP downgrade accepted');
    Rejected := False;
    try ApiRedirectUrl(Endpoint, '/wrong.php'); except Rejected := True; end;
    if not Rejected then RaiseException('Unexpected endpoint accepted');
    Rejected := False;
    try ApiRedirectUrl(Endpoint, ''); except Rejected := True; end;
    if not Rejected then RaiseException('Missing Location accepted');
    if ExpandConstant('{param:LIVE|0}') = '1' then
    begin
      Boundary := 'FirulaiInstallerTestBoundary';
      Body := '--' + Boundary + #13#10 +
        'Content-Disposition: form-data; name="prueba"' + #13#10#13#10 +
        'test-payload' + #13#10 + '--' + Boundary + '--' + #13#10;
      for CaseIndex := 0 to 1 do
      begin
        TestRedirectCode := 301 + CaseIndex;
        PendingApiBaseUrl := Base;
        if TestRedirectCode = 301 then
          Expected := 'https://httpbingo.org/anything/rsm2/AppController/'
        else Expected := Base;
        for RunIndex := 1 to 2 do
        begin
          TestCalls := 0;
          if not PostApiBody(Body, 'fake-token', Boundary, Code, ResponseBody) then
            RaiseException('Installer POST failed');
          if (Code <> 200) or (Pos('POST', ResponseBody) = 0) or
             (Pos('test-payload', ResponseBody) = 0) or (Pos('fake-token', ResponseBody) = 0) then
            RaiseException('Unexpected destination response: HTTP ' + IntToStr(Code));
          AssertEqual(ApiBaseUrlValue(''), Expected);
          if (RunIndex = 1) or (TestRedirectCode = 302) then
          begin
            if TestCalls <> 2 then RaiseException('Expected two requests');
          end
          else if TestCalls <> 1 then RaiseException('Expected direct request to new base');
          Log('HTTP ' + IntToStr(TestRedirectCode) + ', run ' + IntToStr(RunIndex) +
            ': base for Registry = ' + ApiBaseUrlValue('') + '; requests = ' + IntToStr(TestCalls));
        end;
      end;
    end;
  except
    Status := 'FAIL: ' + GetExceptionMessage();
  end;
  SaveStringToFile(ExpandConstant('{param:RESULT}'), Status, False);
end;
