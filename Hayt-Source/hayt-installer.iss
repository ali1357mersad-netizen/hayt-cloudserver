; Hayt Installer Script
#define MyAppName "Hayt - اندیشکده حیات طیبه"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "اندیشکده حیات طیبه"
#define MyAppExeName "Hayt.exe"

[Setup]
AppId={{8F4E2B1A-3C5D-4E6F-9A7B-1D2E3F4A5B6C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Hayt
DefaultGroupName=Hayt
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=G:\1\1\car\نسخه مادر\Hayt\installer
OutputBaseFilename=Hayt-Setup-1.0.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "G:\1\1\car\نسخه مادر\Hayt\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create desktop icon"; GroupDescription: "Icons:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Run {#MyAppName}"; Flags: nowait postinstall skipifsilent
