{ API URL construction and redirect handling for installation. The uninstaller
  calls RsAgent.exe, which reads the same HKLM ApiBaseUrl value. }
var
  PendingApiBaseUrl: string;

function InternetCombineUrl(BaseUrl, RelativeUrl, Buffer: string;
  var BufferLength: Cardinal; Flags: Cardinal): Boolean;
  external 'InternetCombineUrlW@wininet.dll stdcall';

function ApiBaseFromUrl(Url: string): string;
begin
  if (Copy(Url, 1, 8) <> 'https://') or (Pos('@', Url) > 0) or
     (Pos('?', Url) > 0) or (Pos('#', Url) > 0) or (Pos(' ', Url) > 0) or
     (Pos(#13, Url) > 0) or (Pos(#10, Url) > 0) or
     (Copy(Url, Length(Url) - Length('{#ApiPath}') + 1, Length('{#ApiPath}')) <> '{#ApiPath}') then
    RaiseException('Invalid API endpoint URL');
  Result := Copy(Url, 1, Length(Url) - Length('{#ApiPath}'));
  if (Length(Result) <= 9) or (Copy(Result, Length(Result), 1) <> '/') then
    RaiseException('Invalid API base URL');
end;

function ApiBaseUrlValue(Param: string): string;
begin
  Result := PendingApiBaseUrl;
  if Result = '' then
    if not RegQueryStringValue(HKEY_LOCAL_MACHINE, AppRegistryKey(), 'ApiBaseUrl', Result) then
      Result := '{#DefaultApiBaseUrl}';
  if Copy(Result, Length(Result), 1) <> '/' then Result := Result + '/';
  Result := ApiBaseFromUrl(Result + '{#ApiPath}');
end;

function ApiRedirectUrl(CurrentUrl, Location: string): string;
var
  BufferLength: Cardinal;
begin
  if Trim(Location) = '' then RaiseException('Missing API redirect Location');
  BufferLength := 8192;
  Result := StringOfChar(#0, BufferLength);
  if not InternetCombineUrl(CurrentUrl, Location, Result, BufferLength, 0) then
    RaiseException('Invalid API redirect Location');
  SetLength(Result, BufferLength);
  Result := ApiBaseFromUrl(Result) + '{#ApiPath}';
end;

function PostApiBody(Body, Token, Boundary: string;
  var StatusCode: Integer; var ResponseBody: string): Boolean;
var
  Http: Variant;
  Url, PermanentBase: string;
  Hop: Integer;
  PermanentPrefix: Boolean;
begin
  Result := False;
  StatusCode := 0;
  ResponseBody := '';
  try
    Url := ApiBaseUrlValue('') + '{#ApiPath}';
    PermanentBase := '';
    PermanentPrefix := True;
    for Hop := 0 to 5 do
    begin
      Http := CreateOleObject('WinHttp.WinHttpRequest.5.1');
      Http.Option(6) := False;
#ifdef ApiEndpointTest
      Http.Open('POST', TestTransportUrl(Url), False);
      Http.SetRequestHeader('User-Agent', 'RsAgent-Installer-Test/1.0');
#else
      Http.Open('POST', Url, False);
#endif
      Http.SetTimeouts(5000, 5000, 20000, 20000);
      Http.SetRequestHeader('Authorization', Token);
      Http.SetRequestHeader('Content-Type', 'multipart/form-data; boundary=' + Boundary);
      Http.Send(Body);
      StatusCode := Http.Status;
      if (StatusCode = 301) or (StatusCode = 302) or (StatusCode = 307) or (StatusCode = 308) then
      begin
        if Hop = 5 then RaiseException('Too many API redirects');
        Url := ApiRedirectUrl(Url, Http.GetResponseHeader('Location'));
        PermanentPrefix := PermanentPrefix and ((StatusCode = 301) or (StatusCode = 308));
        if PermanentPrefix then PermanentBase := ApiBaseFromUrl(Url);
      end
      else
      begin
        ResponseBody := Http.ResponseText;
        if (StatusCode >= 200) and (StatusCode < 300) and (PermanentBase <> '') then
          { Written by [Registry] only once installation proceeds. }
          PendingApiBaseUrl := PermanentBase;
        Result := True;
        Exit;
      end;
    end;
  except
    Log('API request failed: ' + GetExceptionMessage());
  end;
end;
