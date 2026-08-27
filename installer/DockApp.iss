#define MyAppName "DockApp"
#define MyAppPublisher "lorik440"
#define MyAppExeName "DockApp.Avalonia.exe"

[Setup]
AppId={{8B7F5E5A-5C4D-4B7E-9F24-D0B5A2E5D9C1}
AppName={#MyAppName}
AppVersion={#Version}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\DockApp
DefaultGroupName=DockApp

OutputDir=installer-output
OutputBaseFilename=DockApp-Setup-v{#Version}

Compression=lzma
SolidCompression=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

PrivilegesRequired=admin

UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DockApp"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\DockApp"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch DockApp"; Flags: nowait postinstall skipifsilent