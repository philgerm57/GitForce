; ---------------------------------------------------------------------------
; GitForce - script d'installation Inno Setup 6
;
; 1. Publier l'application en un seul exécutable :
;      dotnet publish src\GitForce.csproj -p:PublishProfile=FolderProfile
; 2. Compiler ce script avec Inno Setup 6 (ISCC.exe ou l'éditeur Inno Setup) :
;      "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\GitForce.iss
;    L'installeur est créé dans installer\Output\GitForce-Setup-<version>.exe
;
; GitForce enregistre ses réglages dans %LOCALAPPDATA%\GitForce et ses données
; (outils, espaces de travail) dans %APPDATA%\GitForce : le dossier d'installation
; reste en lecture seule, l'application peut donc aller dans Program Files.
; ---------------------------------------------------------------------------

#define AppName      "GitForce"
#define AppPublisher "Goran Devic"
#define AppURL       "https://github.com/gdevic/GitForce"
#define AppExeName   "GitForce.exe"
#define PublishDir   "..\src\bin\Release\net10.0-windows\publish\win-x64"

; Version lue dans l'exécutable publié (ressource de version de GitForce.exe)
#define AppVersion GetVersionNumbersString(AddBackslash(SourcePath) + PublishDir + "\" + AppExeName)
#if AppVersion == ""
  #define AppVersion "1.0.0"
#endif

[Setup]
; AppId identifie l'application pour les mises à jour et la désinstallation : ne pas le changer
AppId={{6E0B2C8A-3F4D-4B7E-9A51-2C7D9E4F1A36}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
SetupIconFile=..\src\git.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}
OutputDir=Output
OutputBaseFilename=GitForce-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Application 64 bits (publiée pour win-x64)
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
; Installation pour tous les utilisateurs (administrateur) ou pour l'utilisateur courant seulement
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
; Fermer GitForce s'il est ouvert pendant une mise à jour
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppVersion}
VersionInfoDescription={#AppName} - interface graphique pour git

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Fichier de configuration généré à côté de l'exécutable (facultatif)
Source: "{#PublishDir}\GitForce.dll.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[CustomMessages]
french.GitMissing=Git ne semble pas installé sur cet ordinateur.%n%nGitForce est une interface pour git : installez Git pour Windows (https://git-scm.com/download/win) avant de l'utiliser.%n%nContinuer l'installation ?
english.GitMissing=Git does not seem to be installed on this computer.%n%nGitForce is a front end for git: install Git for Windows (https://git-scm.com/download/win) before using it.%n%nContinue the installation?

[Code]
// Git for Windows s'enregistre dans le registre ; sinon, chercher git.exe dans le PATH
function GitIsInstalled(): Boolean;
var
  Path, Dir: String;
  P: Integer;
begin
  Result := RegKeyExists(HKLM, 'SOFTWARE\GitForWindows') or
            RegKeyExists(HKCU, 'SOFTWARE\GitForWindows') or
            RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\GitForWindows');
  if Result then
    Exit;
  Path := GetEnv('PATH');
  while Path <> '' do
  begin
    P := Pos(';', Path);
    if P = 0 then
    begin
      Dir := Path;
      Path := '';
    end
    else
    begin
      Dir := Copy(Path, 1, P - 1);
      Delete(Path, 1, P);
    end;
    if (Dir <> '') and FileExists(AddBackslash(RemoveQuotes(Dir)) + 'git.exe') then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not GitIsInstalled() then
    Result := (SuppressibleMsgBox(CustomMessage('GitMissing'), mbConfirmation, MB_YESNO, IDYES) = IDYES);
end;
