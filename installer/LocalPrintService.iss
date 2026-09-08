[Setup]
AppName=Local Print Service
AppVersion=1.0.0
AppPublisher=LocalPrintService
DefaultDirName={autopf}\LocalPrintService
DefaultGroupName=Local Print Service
OutputDir=..\installer-output
OutputBaseFilename=LocalPrintService-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\LocalPrintService.Api.exe

[Files]
Source: "..\src\LocalPrintService.Api\bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\scripts\*"; DestDir: "{app}\scripts"; Flags: ignoreversion recursesubdirs

[Dirs]
Name: "{app}\certs"; Permissions: users-modify
Name: "{app}\scripts"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify

[Icons]
Name: "{group}\Local Print Service (HTTPS)"; Filename: "https://print.local:5201"
Name: "{group}\Local Print Service (HTTP)"; Filename: "http://localhost:5200"
Name: "{group}\Uninstall"; Filename: "{uninstallexe}"

[Run]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\setup-domain.ps1"""; StatusMsg: "Configuring domain and SSL..."; Flags: runhidden waituntilterminated
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\install-service.ps1"""; StatusMsg: "Installing service..."; Flags: runhidden waituntilterminated
Filename: "https://print.local:5201"; Description: "Open Local Print Service (HTTPS)"; Flags: postinstall shellexec nowait skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
