#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef Stage
  #define Stage "..\artifacts\stage"
#endif
#define AppName "Caelestia for Windows"
#define RepoUrl "https://github.com/SMIDR76/caelestia-windows"
#define ZebarMsiUrl "https://github.com/glzr-io/zebar/releases/download/v3.3.1/zebar-v3.3.1-opt1-x64.msi"
#define ZebarMsiSha256 "1d5584be8def58f828d55ed09cee52fe80fdd60ea118141f37963aa80e902c28"
#define ZebarUpgradeKey "SOFTWARE\Classes\Installer\UpgradeCodes\8DD3D075DBDFC5B55A91F23FB46AB033"
#define WebView2Key "SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
#define WebView2Url "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
#define PackDir "{%USERPROFILE}\.glzr\zebar\caelestia"
#define Helper "{app}\bin\caelestia-helper.exe"
#define Launcher "{app}\bin\caelestia-launcher.exe"

[Setup]
AppId={{07E2E875-6641-40F1-998A-41F55DCD99DD}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Caelestia for Windows contributors
AppPublisherURL={#RepoUrl}
AppSupportURL={#RepoUrl}/issues
AppUpdatesURL={#RepoUrl}/releases
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
DefaultDirName={%USERPROFILE}\.caelestia
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableWelcomePage=yes
DisableReadyPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputBaseFilename=CaelestiaSetup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dynamic
SetupLogging=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={#Launcher}
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=auto

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"

[CustomMessages]
en.ChangesTitle=What Caelestia changes on your PC
en.ChangesDescription=Read this before installing. Everything is undone by the uninstaller.
en.ChangesSubCaption=Caelestia is free software under the GNU GPL 3.0 (see LICENSE in the install folder).
en.Requirements=Requirements
en.ZebarPresent=Zebar: installed
en.ZebarAbsent=Zebar: missing, it will be installed (Windows will ask for permission once)
en.WebView2Present=WebView2: installed
en.WebView2Absent=WebView2: missing, it will be installed for your account
en.Changes=Changes
en.ChangeFiles=Files in %USERPROFILE%\.caelestia and %USERPROFILE%\.glzr\zebar\caelestia, for your account only, no administrator rights.
en.ChangeStartup=Three tasks start the helper, Zebar and the switcher when you sign in.
en.ChangeTaskbar=The Windows taskbar is hidden while the theme runs and comes back when it stops.
en.ChangeZebar=Zebar's start-up list gets the Caelestia widgets (a backup of your settings is kept).
en.ChangeColors=Optional, off by default: the Windows accent colour can follow the theme (Settings > Appearance).
en.ChangePrivacy=No telemetry. Network use: weather (Open-Meteo) and lyrics (LRCLIB), both optional.
en.TaskSwitcher=Caelestia Alt+Tab switcher
en.TaskStartNow=Start Caelestia now
en.TaskOpenLauncher=Open the Caelestia app
en.IconRestore=Caelestia - Restore the taskbar
en.IconStart=Caelestia - Start
en.IconUninstall=Uninstall Caelestia
en.DownloadingWebView2=Downloading WebView2
en.DownloadingZebar=Downloading Zebar
en.InstallingZebar=Installing Zebar…
en.ZebarFailed=Zebar could not be installed. Install it with "winget install -e --id glzr-io.zebar", then run this setup again.
en.InstallFailed=Caelestia could not be registered (code %1). The log is in %LOCALAPPDATA%\Caelestia\logs\helper.log.
en.RemoveSettings=Also delete your Caelestia settings (colours, options, favourites)?
fr.ChangesTitle=Ce que Caelestia modifie sur votre PC
fr.ChangesDescription=À lire avant d'installer. Tout est annulé par la désinstallation.
fr.ChangesSubCaption=Caelestia est un logiciel libre sous licence GNU GPL 3.0 (voir LICENSE dans le dossier d'installation).
fr.Requirements=Prérequis
fr.ZebarPresent=Zebar : installé
fr.ZebarAbsent=Zebar : absent, il sera installé (Windows demandera une autorisation une fois)
fr.WebView2Present=WebView2 : installé
fr.WebView2Absent=WebView2 : absent, il sera installé pour votre compte
fr.Changes=Modifications
fr.ChangeFiles=Fichiers dans %USERPROFILE%\.caelestia et %USERPROFILE%\.glzr\zebar\caelestia, pour votre compte uniquement, sans droits administrateur.
fr.ChangeStartup=Trois tâches lancent le helper, Zebar et le sélecteur à l'ouverture de session.
fr.ChangeTaskbar=La barre des tâches Windows est masquée pendant que le thème tourne et revient quand il s'arrête.
fr.ChangeZebar=La liste de démarrage de Zebar reçoit les widgets Caelestia (une sauvegarde de vos réglages est conservée).
fr.ChangeColors=En option, désactivé par défaut : la couleur d'accent de Windows peut suivre le thème (Réglages > Apparence).
fr.ChangePrivacy=Aucune télémétrie. Réseau : météo (Open-Meteo) et paroles (LRCLIB), tous deux facultatifs.
fr.TaskSwitcher=Sélecteur Alt+Tab Caelestia
fr.TaskStartNow=Démarrer Caelestia maintenant
fr.TaskOpenLauncher=Ouvrir l'application Caelestia
fr.IconRestore=Caelestia - Rétablir la barre des tâches
fr.IconStart=Caelestia - Relancer
fr.IconUninstall=Désinstaller Caelestia
fr.DownloadingWebView2=Téléchargement de WebView2
fr.DownloadingZebar=Téléchargement de Zebar
fr.InstallingZebar=Installation de Zebar…
fr.ZebarFailed=Zebar n'a pas pu être installé. Installez-le avec « winget install -e --id glzr-io.zebar » puis relancez cette installation.
fr.InstallFailed=Caelestia n'a pas pu être enregistré (code %1). Le journal se trouve dans %LOCALAPPDATA%\Caelestia\logs\helper.log.
fr.RemoveSettings=Supprimer aussi vos réglages Caelestia (couleurs, options, favoris) ?

[Tasks]
Name: "switcher"; Description: "{cm:TaskSwitcher}"
Name: "startnow"; Description: "{cm:TaskStartNow}"
Name: "openlauncher"; Description: "{cm:TaskOpenLauncher}"

[InstallDelete]
Type: files; Name: "{userprograms}\Caelestia\*.lnk"
Type: files; Name: "{app}\restore-taskbar.ps1"
Type: filesandordirs; Name: "{#PackDir}"
Type: filesandordirs; Name: "{app}\bin"

[Files]
Source: "{#Stage}\bin\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "{#Stage}\pack\*"; DestDir: "{#PackDir}"; Excludes: "\native\CaelestiaSwitcher.exe"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Stage}\pack\native\CaelestiaSwitcher.exe"; DestDir: "{#PackDir}\native"; Tasks: switcher; Flags: ignoreversion
Source: "{#Stage}\registry\*"; DestDir: "{app}\registry"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#Stage}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Stage}\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Stage}\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Caelestia\Caelestia"; Filename: "{#Launcher}"; Check: LauncherShipped
Name: "{userprograms}\Caelestia\{cm:IconRestore}"; Filename: "{#Helper}"; Parameters: "--restore-shell"; IconFilename: "{sys}\imageres.dll"; IconIndex: 72
Name: "{userprograms}\Caelestia\{cm:IconStart}"; Filename: "{#Helper}"; Parameters: "--start"
Name: "{userprograms}\Caelestia\{cm:IconUninstall}"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Classes\caelestia"; ValueType: string; ValueName: ""; ValueData: "URL:Caelestia"; Flags: uninsdeletekey; Check: LauncherShipped
Root: HKCU; Subkey: "Software\Classes\caelestia"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Check: LauncherShipped
Root: HKCU; Subkey: "Software\Classes\caelestia\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{#Launcher},0"; Check: LauncherShipped
Root: HKCU; Subkey: "Software\Classes\caelestia\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{#Launcher}"" ""%1"""; Check: LauncherShipped

[UninstallRun]
Filename: "{#Helper}"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "CaelestiaUninstall"

[UninstallDelete]
Type: filesandordirs; Name: "{userprograms}\Caelestia"
Type: filesandordirs; Name: "{#PackDir}"
Type: filesandordirs; Name: "{app}"

[Code]
function LauncherShipped: Boolean;
begin
  Result := FileExists(ExpandConstant('{#Launcher}'));
end;

var
  ChangesPage: TOutputMsgMemoWizardPage;
  DownloadPage: TDownloadWizardPage;

function HasVersion(const Root: Integer; const Key: String): Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(Root, Key, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0');
end;

function WebView2Installed: Boolean;
begin
  Result := HasVersion(HKLM32, '{#WebView2Key}') or HasVersion(HKCU, '{#WebView2Key}');
end;

function ZebarInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM, '{#ZebarUpgradeKey}')
    or FileExists(ExpandConstant('{commonpf64}\glzr.io\Zebar\zebar.exe'))
    or FileExists(ExpandConstant('{%USERPROFILE}\scoop\apps\zebar\current\zebar.exe'));
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall);
end;

function Line(const Name: String): String;
begin
  Result := '  • ' + CustomMessage(Name) + #13#10;
end;

function ChangesText: String;
var
  Zebar, WebView2: String;
begin
  if ZebarInstalled then Zebar := 'ZebarPresent' else Zebar := 'ZebarAbsent';
  if WebView2Installed then WebView2 := 'WebView2Present' else WebView2 := 'WebView2Absent';
  Result := CustomMessage('Requirements') + #13#10 + Line(Zebar) + Line(WebView2) + #13#10 +
    CustomMessage('Changes') + #13#10 + Line('ChangeFiles') + Line('ChangeStartup') + Line('ChangeTaskbar') +
    Line('ChangeZebar') + Line('ChangeColors') + Line('ChangePrivacy');
end;

procedure InitializeWizard;
begin
  ChangesPage := CreateOutputMsgMemoPage(wpWelcome, CustomMessage('ChangesTitle'), CustomMessage('ChangesDescription'),
    CustomMessage('ChangesSubCaption'), ChangesText);
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

function Download(const Url, FileName, Sha256, Title: String): String;
begin
  Result := '';
  DownloadPage.Clear;
  DownloadPage.SetText(CustomMessage(Title), '');
  DownloadPage.Add(Url, FileName, Sha256);
  if not WizardSilent then DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      Result := ExpandConstant('{tmp}\' + FileName);
    except
      Log('Download failed: ' + GetExceptionMessage);
    end;
  finally
    DownloadPage.Hide;
  end;
end;

procedure StopCaelestia;
var
  Code: Integer;
  Helper: String;
begin
  Helper := ExpandConstant('{#Helper}');
  if not FileExists(Helper) then Exit;
  if Exec(Helper, '--shutdown', '', SW_HIDE, ewWaitUntilTerminated, Code) and (Code = 0) then Exit;
  Exec(Helper, '--quit', '', SW_HIDE, ewWaitUntilTerminated, Code);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM zebar.exe /IM CaelestiaSwitcher.exe /IM caelestia-helper.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

procedure InstallWebView2;
var
  Code: Integer;
  Setup: String;
begin
  if WebView2Installed then Exit;
  Setup := Download('{#WebView2Url}', 'MicrosoftEdgeWebview2Setup.exe', '', 'DownloadingWebView2');
  if Setup <> '' then Exec(Setup, '/silent /install', '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

function InstallZebar: String;
var
  Code: Integer;
  Winget, Msi: String;
begin
  Result := '';
  if ZebarInstalled then Exit;
  Winget := ExpandConstant('{localappdata}\Microsoft\WindowsApps\winget.exe');
  if FileExists(Winget) then
  begin
    WizardForm.StatusLabel.Caption := CustomMessage('InstallingZebar');
    Exec(Winget, 'install --id glzr-io.zebar -e --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity',
      '', SW_HIDE, ewWaitUntilTerminated, Code);
    Log(Format('winget returned %d', [Code]));
  end;
  if not ZebarInstalled then
  begin
    Msi := Download('{#ZebarMsiUrl}', 'zebar.msi', '{#ZebarMsiSha256}', 'DownloadingZebar');
    if Msi <> '' then
      ShellExec('runas', ExpandConstant('{sys}\msiexec.exe'), '/i "' + Msi + '" /qn /norestart', '', SW_SHOW, ewWaitUntilTerminated, Code);
  end;
  if not ZebarInstalled then Result := CustomMessage('ZebarFailed');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopCaelestia;
  InstallWebView2;
  Result := InstallZebar;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
  Helper: String;
begin
  if CurStep <> ssPostInstall then Exit;
  Helper := ExpandConstant('{#Helper}');
  if not Exec(Helper, '--install', '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
  begin
    SuppressibleMsgBox(FmtMessage(CustomMessage('InstallFailed'), [IntToStr(Code)]), mbError, MB_OK, IDOK);
    Exit;
  end;
  if WizardIsTaskSelected('startnow') then
    Exec(Helper, '--start', '', SW_HIDE, ewNoWait, Code);
  if WizardIsTaskSelected('openlauncher') and LauncherShipped then
    ExecAsOriginalUser(ExpandConstant('{#Launcher}'), '', '', SW_SHOWNORMAL, ewNoWait, Code);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep <> usPostUninstall then Exit;
  if UninstallSilent then Exit;
  if MsgBox(CustomMessage('RemoveSettings'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) <> IDYES then Exit;
  DelTree(ExpandConstant('{userappdata}\Caelestia'), True, True, True);
  DelTree(ExpandConstant('{localappdata}\Caelestia'), True, True, True);
  DelTree(ExpandConstant('{userappdata}\zebar\webview-cache\caelestia'), True, True, True);
end;
