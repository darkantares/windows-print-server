[Setup]
AppId={{5B9E6A21-3F4C-4E4B-9E6D-1A2B3C4D5E6F}
AppName=Local Print Service
AppVersion=1.0.0
AppPublisher=LocalPrintService
AppSupportURL=http://localhost:5200
VersionInfoVersion=1.0.0.0
VersionInfoProductName=Local Print Service
VersionInfoProductTextVersion=1.0.0
DefaultDirName={autopf}\LocalPrintService
DefaultGroupName=Local Print Service
DisableProgramGroupPage=yes
OutputDir=..\installer-output
OutputBaseFilename=LocalPrintService-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
SetupIconFile=printer.ico
UninstallDisplayIcon={app}\LocalPrintService.Api.exe
UninstallDisplayName=Local Print Service
CloseApplications=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un icono en el escritorio"; GroupDescription: "Iconos adicionales:"

[Files]
Source: "..\src\LocalPrintService.Api\bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\scripts\*"; DestDir: "{app}\scripts"; Flags: ignoreversion recursesubdirs
Source: "printer.ico"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{app}\certs"; Permissions: users-modify
Name: "{app}\scripts"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify

[Icons]
Name: "{group}\Local Print Service (HTTPS)"; Filename: "https://print.local:5201"; IconFilename: "{app}\printer.ico"; IconIndex: 0
Name: "{group}\Local Print Service (HTTP)"; Filename: "http://localhost:5200"; IconFilename: "{app}\printer.ico"; IconIndex: 0
Name: "{group}\Local Print Service"; Filename: "{app}\LocalPrintService.Api.exe"; IconFilename: "{app}\printer.ico"; IconIndex: 0
Name: "{group}\Desinstalar Local Print Service"; Filename: "{uninstallexe}"; IconFilename: "{app}\printer.ico"; IconIndex: 0
Name: "{autodesktop}\Local Print Service"; Filename: "https://print.local:5201"; IconFilename: "{app}\printer.ico"; IconIndex: 0; Tasks: desktopicon

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\setup-domain.ps1"""; StatusMsg: "Configurando dominio y SSL..."; Flags: runhidden waituntilterminated
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\install-service.ps1"""; StatusMsg: "Instalando servicio..."; Flags: runhidden waituntilterminated
Filename: "https://print.local:5201"; Description: "Abrir Local Print Service (HTTPS)"; Flags: postinstall shellexec nowait skipifsilent

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\uninstall-service.ps1"""; RunOnceId: "StopService"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\certs"
Type: filesandordirs; Name: "{app}"
