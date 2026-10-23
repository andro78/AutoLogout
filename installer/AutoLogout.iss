#define AppVersion "0.2.0"
[Setup]
AppId={{7A046114-8198-4E50-84FB-4AC71E7112C4}
AppName=AutoLogout
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\AutoLogout
PrivilegesRequired=lowest
OutputDir=..\out\installer
OutputBaseFilename=AutoLogout-0.2.0-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
[Files]
Source: "..\out\app\*"; DestDir: "{app}\app"; Flags: ignoreversion recursesubdirs
Source: "..\out\host\*"; DestDir: "{app}\host"; Flags: ignoreversion recursesubdirs
Source: "..\extension\*"; DestDir: "{app}\extension"; Flags: ignoreversion recursesubdirs
Source: "..\scripts\install-native-host.ps1"; DestDir: "{app}\scripts"
Source: "..\README.md"; DestDir: "{app}"
Source: "..\README-v0.2.md"; DestDir: "{app}"
Source: "..\BUILD-STATUS.md"; DestDir: "{app}"
[Icons]
Name: "{autoprograms}\AutoLogout"; Filename: "{app}\app\AutoLogout.App.exe"
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "AutoLogout"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Google\Chrome\NativeMessagingHosts\com.autologout.bridge"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Edge\NativeMessagingHosts\com.autologout.bridge"; Flags: uninsdeletekey
[Run]
Filename: "{app}\app\AutoLogout.App.exe"; Description: "AutoLogout 열기"; Flags: nowait postinstall skipifsilent

