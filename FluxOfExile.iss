; FluxOfExile Installer Script for Inno Setup
; https://jrsoftware.org/isinfo.php

#define MyAppName "FluxOfExile"
#define MyAppVersion "5.0"
#define MyAppPublisher "FluxOfExile"
#define MyAppURL "https://github.com/yourusername/FluxOfExile"
#define MyAppExeName "FluxOfExile.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=releases
OutputBaseFilename={#MyAppName}-v{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Run on Windows startup"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\icon.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Prevent downgrade: Check if newer version is already installed
function InitializeSetup(): Boolean;
var
  InstalledVersion: String;
  CurrentVersion: String;
begin
  Result := True;
  CurrentVersion := '{#MyAppVersion}';

  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}_is1',
                          'DisplayVersion', InstalledVersion) or
     RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}_is1',
                          'DisplayVersion', InstalledVersion) then
  begin
    // Simple version comparison (works for major.minor format)
    if InstalledVersion > CurrentVersion then
    begin
      MsgBox('A newer version (' + InstalledVersion + ') is already installed. Setup will now exit.',
             mbInformation, MB_OK);
      Result := False;
    end;
  end;
end;
