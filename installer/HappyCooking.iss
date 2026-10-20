; Happy Cooking - Windows installer (Inno Setup 6)
; Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios
;
; Built by installer/build-installer.ps1, which publishes the desktop app first.
; Manual build:  ISCC.exe /DAppVersion=1.0.0 /DPublishDir=..\artifacts\publish\win-x64 installer\HappyCooking.iss

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

#define AppName "Happy Cooking"
#define AppPublisher "Syubadubin Studios"
#define AppExe "HappyCooking.exe"
#define AppUrl "https://github.com/SyubadubinStudios/HappyCooking"

[Setup]
AppId={{5C3F7E2A-9B41-4D6E-A0C8-2F7B1D9E4A63}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppCopyright=Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} - 3D Cooking Adventure for Kids
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Per-user by default (no admin prompt for families); the dialog lets an adult install for everyone.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#OutputDir}
OutputBaseFilename=HappyCooking-Setup-{#AppVersion}-win-x64
SetupIconFile=..\src\HappyCooking.Desktop\happycooking.ico
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern
WizardImageFile=wizard-large.bmp
WizardSmallImageFile=wizard-small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "indonesian"; MessagesFile: "Languages\Indonesian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Messages]
english.WelcomeLabel2=This will install [name/ver] on your computer.%n%nA 3D cooking adventure for kids.%nDibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios.
indonesian.WelcomeLabel2=Program ini akan memasang [name/ver] di komputer Anda.%n%nPetualangan memasak 3D untuk anak-anak.%nDibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios.
