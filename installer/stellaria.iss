#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef AppNumericVersion
  #define AppNumericVersion "0.0.0"
#endif
#ifndef Stage
  #define Stage "..\artifacts\stage"
#endif
#define AppName "Stellaria"
#define RepoUrl "https://github.com/SMIDR76/stellaria-windows"
#define ZebarMsiUrl "https://github.com/glzr-io/zebar/releases/download/v3.3.1/zebar-v3.3.1-opt1-x64.msi"
#define ZebarMsiSha256 "1d5584be8def58f828d55ed09cee52fe80fdd60ea118141f37963aa80e902c28"
#define ZebarUpgradeKey "SOFTWARE\Classes\Installer\UpgradeCodes\8DD3D075DBDFC5B55A91F23FB46AB033"
#define WebView2Key "SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
#define WebView2Url "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
#define PackDir "{%USERPROFILE}\.glzr\zebar\stellaria"
#define Helper "{app}\bin\stellaria-helper.exe"
#define Launcher "{app}\bin\stellaria-launcher.exe"

[Setup]
AppId={{07E2E875-6641-40F1-998A-41F55DCD99DD}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Stellaria contributors
AppPublisherURL={#RepoUrl}
AppSupportURL={#RepoUrl}/issues
AppUpdatesURL={#RepoUrl}/releases
VersionInfoVersion={#AppNumericVersion}
VersionInfoProductName={#AppName}
DefaultDirName={%USERPROFILE}\.stellaria
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableWelcomePage=yes
DisableReadyPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputBaseFilename=StellariaSetup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dynamic
SetupLogging=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={#Launcher}
SetupIconFile=..\launcher\icons\icon.ico
CloseApplications=force
RestartApplications=no
ShowLanguageDialog=auto

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "pl"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "ko"; MessagesFile: "compiler:Languages\Korean.isl"

[CustomMessages]
en.ChangesTitle=What Stellaria changes on your PC
en.ChangesDescription=Read this before installing. Everything is undone by the uninstaller.
en.ChangesSubCaption=Stellaria is free software under the GNU GPL 3.0 (see LICENSE in the install folder).
en.Requirements=Requirements
en.ZebarPresent=Zebar: installed
en.ZebarAbsent=Zebar: missing, it will be installed (Windows will ask for permission once)
en.WebView2Present=WebView2: installed
en.WebView2Absent=WebView2: missing, it will be installed for your account
en.Changes=Changes
en.ChangeFiles=Files in %USERPROFILE%\.stellaria and %USERPROFILE%\.glzr\zebar\stellaria, for your account only, no administrator rights.
en.ChangeStartup=Three tasks start the helper, Zebar and the switcher when you sign in.
en.ChangeTaskbar=The Windows taskbar is hidden while the theme runs and comes back when it stops.
en.ChangeZebar=Zebar's start-up list gets the Stellaria widgets (a backup of your settings is kept).
en.ChangeColors=Optional, off by default: the Windows accent colour can follow the theme (Settings > Appearance).
en.ChangePrivacy=No telemetry. Network use: weather (Open-Meteo) and lyrics (LRCLIB), both optional.
en.TaskSwitcher=Stellaria Alt+Tab switcher
en.TaskStartNow=Start Stellaria now
en.TaskOpenLauncher=Open the Stellaria app
en.IconRestore=Stellaria - Restore the taskbar
en.IconStart=Stellaria - Start
en.IconUninstall=Uninstall Stellaria
en.DownloadingWebView2=Downloading WebView2
en.DownloadingZebar=Downloading Zebar
en.InstallingZebar=Installing Zebar…
en.ZebarFailed=Zebar could not be installed. Install it with "winget install -e --id glzr-io.zebar", then run this setup again.
en.InstallFailed=Stellaria could not be registered (code %1). The log is in %LOCALAPPDATA%\Stellaria\logs\helper.log.
en.RemoveSettings=Also delete your Stellaria settings (colours, options, favourites)?
fr.ChangesTitle=Ce que Stellaria modifie sur votre PC
fr.ChangesDescription=À lire avant d'installer. Tout est annulé par la désinstallation.
fr.ChangesSubCaption=Stellaria est un logiciel libre sous licence GNU GPL 3.0 (voir LICENSE dans le dossier d'installation).
fr.Requirements=Prérequis
fr.ZebarPresent=Zebar : installé
fr.ZebarAbsent=Zebar : absent, il sera installé (Windows demandera une autorisation une fois)
fr.WebView2Present=WebView2 : installé
fr.WebView2Absent=WebView2 : absent, il sera installé pour votre compte
fr.Changes=Modifications
fr.ChangeFiles=Fichiers dans %USERPROFILE%\.stellaria et %USERPROFILE%\.glzr\zebar\stellaria, pour votre compte uniquement, sans droits administrateur.
fr.ChangeStartup=Trois tâches lancent le helper, Zebar et le sélecteur à l'ouverture de session.
fr.ChangeTaskbar=La barre des tâches Windows est masquée pendant que le thème tourne et revient quand il s'arrête.
fr.ChangeZebar=La liste de démarrage de Zebar reçoit les widgets Stellaria (une sauvegarde de vos réglages est conservée).
fr.ChangeColors=En option, désactivé par défaut : la couleur d'accent de Windows peut suivre le thème (Réglages > Apparence).
fr.ChangePrivacy=Aucune télémétrie. Réseau : météo (Open-Meteo) et paroles (LRCLIB), tous deux facultatifs.
fr.TaskSwitcher=Sélecteur Alt+Tab Stellaria
fr.TaskStartNow=Démarrer Stellaria maintenant
fr.TaskOpenLauncher=Ouvrir l'application Stellaria
fr.IconRestore=Stellaria - Rétablir la barre des tâches
fr.IconStart=Stellaria - Relancer
fr.IconUninstall=Désinstaller Stellaria
fr.DownloadingWebView2=Téléchargement de WebView2
fr.DownloadingZebar=Téléchargement de Zebar
fr.InstallingZebar=Installation de Zebar…
fr.ZebarFailed=Zebar n'a pas pu être installé. Installez-le avec « winget install -e --id glzr-io.zebar » puis relancez cette installation.
fr.InstallFailed=Stellaria n'a pas pu être enregistré (code %1). Le journal se trouve dans %LOCALAPPDATA%\Stellaria\logs\helper.log.
fr.RemoveSettings=Supprimer aussi vos réglages Stellaria (couleurs, options, favoris) ?
es.ChangesTitle=Qué cambia Stellaria en tu PC
es.ChangesDescription=Léelo antes de instalar. El desinstalador lo deshace todo.
es.ChangesSubCaption=Stellaria es software libre con licencia GNU GPL 3.0 (consulta LICENSE en la carpeta de instalación).
es.Requirements=Requisitos
es.ZebarPresent=Zebar: instalado
es.ZebarAbsent=Zebar: falta, se instalará (Windows pedirá permiso una vez)
es.WebView2Present=WebView2: instalado
es.WebView2Absent=WebView2: falta, se instalará para tu cuenta
es.Changes=Cambios
es.ChangeFiles=Archivos en %USERPROFILE%\.stellaria y %USERPROFILE%\.glzr\zebar\stellaria, solo para tu cuenta, sin derechos de administrador.
es.ChangeStartup=Tres tareas inician el servicio, Zebar y el selector al iniciar sesión.
es.ChangeTaskbar=La barra de tareas de Windows se oculta mientras el tema está activo y vuelve cuando se detiene.
es.ChangeZebar=La lista de inicio de Zebar recibe los widgets de Stellaria (se guarda una copia de tus ajustes).
es.ChangeColors=Opcional, desactivado por defecto: el color de énfasis de Windows puede seguir el tema (Ajustes > Apariencia).
es.ChangePrivacy=Sin telemetría. Red: tiempo (Open-Meteo) y letras (LRCLIB), ambos opcionales.
es.TaskSwitcher=Selector Alt+Tab de Stellaria
es.TaskStartNow=Iniciar Stellaria ahora
es.TaskOpenLauncher=Abrir la aplicación Stellaria
es.IconRestore=Stellaria - Restaurar la barra de tareas
es.IconStart=Stellaria - Iniciar
es.IconUninstall=Desinstalar Stellaria
es.DownloadingWebView2=Descargando WebView2
es.DownloadingZebar=Descargando Zebar
es.InstallingZebar=Instalando Zebar…
es.ZebarFailed=No se pudo instalar Zebar. Instálalo con "winget install -e --id glzr-io.zebar" y vuelve a ejecutar este instalador.
es.InstallFailed=No se pudo registrar Stellaria (código %1). El registro está en %LOCALAPPDATA%\Stellaria\logs\helper.log.
es.RemoveSettings=¿Eliminar también tus ajustes de Stellaria (colores, opciones, favoritos)?
de.ChangesTitle=Was Stellaria auf deinem PC ändert
de.ChangesDescription=Lies das vor der Installation. Die Deinstallation macht alles rückgängig.
de.ChangesSubCaption=Stellaria ist freie Software unter der GNU GPL 3.0 (siehe LICENSE im Installationsordner).
de.Requirements=Voraussetzungen
de.ZebarPresent=Zebar: installiert
de.ZebarAbsent=Zebar: fehlt, wird installiert (Windows fragt einmal nach einer Berechtigung)
de.WebView2Present=WebView2: installiert
de.WebView2Absent=WebView2: fehlt, wird für dein Konto installiert
de.Changes=Änderungen
de.ChangeFiles=Dateien in %USERPROFILE%\.stellaria und %USERPROFILE%\.glzr\zebar\stellaria, nur für dein Konto, ohne Administratorrechte.
de.ChangeStartup=Drei Aufgaben starten bei der Anmeldung den Dienst, Zebar und den Wechsler.
de.ChangeTaskbar=Die Windows-Taskleiste wird ausgeblendet, solange das Design läuft, und kehrt zurück, wenn es stoppt.
de.ChangeZebar=Die Zebar-Startliste erhält die Stellaria-Widgets (eine Sicherung deiner Einstellungen wird aufbewahrt).
de.ChangeColors=Optional, standardmäßig aus: Die Akzentfarbe von Windows kann dem Design folgen (Einstellungen > Darstellung).
de.ChangePrivacy=Keine Telemetrie. Netzwerk: Wetter (Open-Meteo) und Liedtexte (LRCLIB), beides optional.
de.TaskSwitcher=Stellaria-Alt+Tab-Wechsler
de.TaskStartNow=Stellaria jetzt starten
de.TaskOpenLauncher=Die Stellaria-App öffnen
de.IconRestore=Stellaria - Taskleiste wiederherstellen
de.IconStart=Stellaria - Starten
de.IconUninstall=Stellaria deinstallieren
de.DownloadingWebView2=WebView2 wird heruntergeladen
de.DownloadingZebar=Zebar wird heruntergeladen
de.InstallingZebar=Zebar wird installiert…
de.ZebarFailed=Zebar konnte nicht installiert werden. Installiere es mit "winget install -e --id glzr-io.zebar" und starte dieses Setup erneut.
de.InstallFailed=Stellaria konnte nicht registriert werden (Code %1). Das Protokoll liegt in %LOCALAPPDATA%\Stellaria\logs\helper.log.
de.RemoveSettings=Auch deine Stellaria-Einstellungen (Farben, Optionen, Favoriten) löschen?
ptbr.ChangesTitle=O que o Stellaria muda no seu PC
ptbr.ChangesDescription=Leia antes de instalar. O desinstalador desfaz tudo.
ptbr.ChangesSubCaption=O Stellaria é software livre sob a licença GNU GPL 3.0 (veja LICENSE na pasta de instalação).
ptbr.Requirements=Requisitos
ptbr.ZebarPresent=Zebar: instalado
ptbr.ZebarAbsent=Zebar: ausente, será instalado (o Windows pedirá permissão uma vez)
ptbr.WebView2Present=WebView2: instalado
ptbr.WebView2Absent=WebView2: ausente, será instalado para a sua conta
ptbr.Changes=Alterações
ptbr.ChangeFiles=Arquivos em %USERPROFILE%\.stellaria e %USERPROFILE%\.glzr\zebar\stellaria, só para a sua conta, sem direitos de administrador.
ptbr.ChangeStartup=Três tarefas iniciam o serviço, o Zebar e o seletor quando você entra.
ptbr.ChangeTaskbar=A barra de tarefas do Windows fica oculta enquanto o tema está ativo e volta quando ele para.
ptbr.ChangeZebar=A lista de inicialização do Zebar recebe os widgets do Stellaria (um backup das suas configurações é mantido).
ptbr.ChangeColors=Opcional, desligado por padrão: a cor de destaque do Windows pode seguir o tema (Configurações > Aparência).
ptbr.ChangePrivacy=Sem telemetria. Rede: clima (Open-Meteo) e letras (LRCLIB), ambos opcionais.
ptbr.TaskSwitcher=Seletor Alt+Tab do Stellaria
ptbr.TaskStartNow=Iniciar o Stellaria agora
ptbr.TaskOpenLauncher=Abrir o aplicativo Stellaria
ptbr.IconRestore=Stellaria - Restaurar a barra de tarefas
ptbr.IconStart=Stellaria - Iniciar
ptbr.IconUninstall=Desinstalar o Stellaria
ptbr.DownloadingWebView2=Baixando o WebView2
ptbr.DownloadingZebar=Baixando o Zebar
ptbr.InstallingZebar=Instalando o Zebar…
ptbr.ZebarFailed=Não foi possível instalar o Zebar. Instale-o com "winget install -e --id glzr-io.zebar" e execute esta instalação de novo.
ptbr.InstallFailed=Não foi possível registrar o Stellaria (código %1). O log está em %LOCALAPPDATA%\Stellaria\logs\helper.log.
ptbr.RemoveSettings=Excluir também suas configurações do Stellaria (cores, opções, favoritos)?
it.ChangesTitle=Cosa cambia Stellaria sul tuo PC
it.ChangesDescription=Leggi prima di installare. La disinstallazione annulla tutto.
it.ChangesSubCaption=Stellaria è software libero con licenza GNU GPL 3.0 (vedi LICENSE nella cartella di installazione).
it.Requirements=Requisiti
it.ZebarPresent=Zebar: installato
it.ZebarAbsent=Zebar: mancante, verrà installato (Windows chiederà un'autorizzazione una volta)
it.WebView2Present=WebView2: installato
it.WebView2Absent=WebView2: mancante, verrà installato per il tuo account
it.Changes=Modifiche
it.ChangeFiles=File in %USERPROFILE%\.stellaria e %USERPROFILE%\.glzr\zebar\stellaria, solo per il tuo account, senza diritti di amministratore.
it.ChangeStartup=Tre attività avviano il servizio, Zebar e il selettore all'accesso.
it.ChangeTaskbar=La barra delle applicazioni di Windows è nascosta mentre il tema è attivo e torna quando si ferma.
it.ChangeZebar=L'elenco di avvio di Zebar riceve i widget di Stellaria (viene conservato un backup delle tue impostazioni).
it.ChangeColors=Facoltativo, disattivato per impostazione predefinita: il colore principale di Windows può seguire il tema (Impostazioni > Aspetto).
it.ChangePrivacy=Nessuna telemetria. Rete: meteo (Open-Meteo) e testi (LRCLIB), entrambi facoltativi.
it.TaskSwitcher=Selettore Alt+Tab di Stellaria
it.TaskStartNow=Avvia Stellaria ora
it.TaskOpenLauncher=Apri l'app Stellaria
it.IconRestore=Stellaria - Ripristina la barra delle applicazioni
it.IconStart=Stellaria - Avvia
it.IconUninstall=Disinstalla Stellaria
it.DownloadingWebView2=Download di WebView2
it.DownloadingZebar=Download di Zebar
it.InstallingZebar=Installazione di Zebar…
it.ZebarFailed=Impossibile installare Zebar. Installalo con "winget install -e --id glzr-io.zebar", poi esegui di nuovo questa installazione.
it.InstallFailed=Impossibile registrare Stellaria (codice %1). Il log si trova in %LOCALAPPDATA%\Stellaria\logs\helper.log.
it.RemoveSettings=Eliminare anche le impostazioni di Stellaria (colori, opzioni, preferiti)?
pl.ChangesTitle=Co Stellaria zmienia na twoim komputerze
pl.ChangesDescription=Przeczytaj przed instalacją. Dezinstalator cofa wszystko.
pl.ChangesSubCaption=Stellaria to wolne oprogramowanie na licencji GNU GPL 3.0 (zobacz LICENSE w folderze instalacji).
pl.Requirements=Wymagania
pl.ZebarPresent=Zebar: zainstalowany
pl.ZebarAbsent=Zebar: brak, zostanie zainstalowany (Windows raz poprosi o zgodę)
pl.WebView2Present=WebView2: zainstalowany
pl.WebView2Absent=WebView2: brak, zostanie zainstalowany dla twojego konta
pl.Changes=Zmiany
pl.ChangeFiles=Pliki w %USERPROFILE%\.stellaria i %USERPROFILE%\.glzr\zebar\stellaria, tylko dla twojego konta, bez uprawnień administratora.
pl.ChangeStartup=Trzy zadania uruchamiają usługę, Zebar i przełącznik po zalogowaniu.
pl.ChangeTaskbar=Pasek zadań Windows jest ukryty, gdy motyw działa, i wraca, gdy się zatrzyma.
pl.ChangeZebar=Lista startowa Zebar otrzymuje widżety Stellaria (kopia twoich ustawień jest zachowywana).
pl.ChangeColors=Opcjonalnie, domyślnie wyłączone: kolor akcentu Windows może podążać za motywem (Ustawienia > Wygląd).
pl.ChangePrivacy=Brak telemetrii. Sieć: pogoda (Open-Meteo) i teksty (LRCLIB), obie opcjonalne.
pl.TaskSwitcher=Przełącznik Alt+Tab Stellaria
pl.TaskStartNow=Uruchom Stellaria teraz
pl.TaskOpenLauncher=Otwórz aplikację Stellaria
pl.IconRestore=Stellaria - Przywróć pasek zadań
pl.IconStart=Stellaria - Uruchom
pl.IconUninstall=Odinstaluj Stellaria
pl.DownloadingWebView2=Pobieranie WebView2
pl.DownloadingZebar=Pobieranie Zebar
pl.InstallingZebar=Instalowanie Zebar…
pl.ZebarFailed=Nie udało się zainstalować Zebar. Zainstaluj go poleceniem "winget install -e --id glzr-io.zebar", a potem uruchom ten instalator ponownie.
pl.InstallFailed=Nie udało się zarejestrować Stellaria (kod %1). Dziennik jest w %LOCALAPPDATA%\Stellaria\logs\helper.log.
pl.RemoveSettings=Usunąć też ustawienia Stellaria (kolory, opcje, ulubione)?
ru.ChangesTitle=Что Stellaria меняет на вашем ПК
ru.ChangesDescription=Прочитайте перед установкой. Удаление отменяет все изменения.
ru.ChangesSubCaption=Stellaria — свободное ПО под лицензией GNU GPL 3.0 (см. LICENSE в папке установки).
ru.Requirements=Требования
ru.ZebarPresent=Zebar: установлен
ru.ZebarAbsent=Zebar: отсутствует, будет установлен (Windows один раз запросит разрешение)
ru.WebView2Present=WebView2: установлен
ru.WebView2Absent=WebView2: отсутствует, будет установлен для вашей учётной записи
ru.Changes=Изменения
ru.ChangeFiles=Файлы в %USERPROFILE%\.stellaria и %USERPROFILE%\.glzr\zebar\stellaria, только для вашей учётной записи, без прав администратора.
ru.ChangeStartup=Три задачи запускают службу, Zebar и переключатель при входе в систему.
ru.ChangeTaskbar=Панель задач Windows скрыта, пока тема запущена, и возвращается после её остановки.
ru.ChangeZebar=В список автозапуска Zebar добавляются виджеты Stellaria (резервная копия ваших настроек сохраняется).
ru.ChangeColors=Необязательно, по умолчанию выключено: цвет акцента Windows может следовать теме (Настройки > Внешний вид).
ru.ChangePrivacy=Без телеметрии. Сеть: погода (Open-Meteo) и тексты песен (LRCLIB), оба необязательны.
ru.TaskSwitcher=Переключатель Alt+Tab Stellaria
ru.TaskStartNow=Запустить Stellaria сейчас
ru.TaskOpenLauncher=Открыть приложение Stellaria
ru.IconRestore=Stellaria - Вернуть панель задач
ru.IconStart=Stellaria - Запустить
ru.IconUninstall=Удалить Stellaria
ru.DownloadingWebView2=Загрузка WebView2
ru.DownloadingZebar=Загрузка Zebar
ru.InstallingZebar=Установка Zebar…
ru.ZebarFailed=Не удалось установить Zebar. Установите его командой "winget install -e --id glzr-io.zebar" и запустите установку снова.
ru.InstallFailed=Не удалось зарегистрировать Stellaria (код %1). Журнал находится в %LOCALAPPDATA%\Stellaria\logs\helper.log.
ru.RemoveSettings=Удалить также настройки Stellaria (цвета, параметры, избранное)?
tr.ChangesTitle=Stellaria bilgisayarınızda neleri değiştirir
tr.ChangesDescription=Yüklemeden önce okuyun. Kaldırıcı her şeyi geri alır.
tr.ChangesSubCaption=Stellaria, GNU GPL 3.0 lisanslı özgür yazılımdır (kurulum klasöründeki LICENSE dosyasına bakın).
tr.Requirements=Gereksinimler
tr.ZebarPresent=Zebar: yüklü
tr.ZebarAbsent=Zebar: eksik, yüklenecek (Windows bir kez izin isteyecek)
tr.WebView2Present=WebView2: yüklü
tr.WebView2Absent=WebView2: eksik, hesabınız için yüklenecek
tr.Changes=Değişiklikler
tr.ChangeFiles=%USERPROFILE%\.stellaria ve %USERPROFILE%\.glzr\zebar\stellaria içindeki dosyalar, yalnızca sizin hesabınız için, yönetici hakları olmadan.
tr.ChangeStartup=Üç görev, oturum açtığınızda hizmeti, Zebar'ı ve değiştiriciyi başlatır.
tr.ChangeTaskbar=Windows görev çubuğu tema çalışırken gizlenir ve durduğunda geri gelir.
tr.ChangeZebar=Zebar başlangıç listesine Stellaria widget'ları eklenir (ayarlarınızın bir yedeği tutulur).
tr.ChangeColors=İsteğe bağlı, varsayılan olarak kapalı: Windows vurgu rengi temayı izleyebilir (Ayarlar > Görünüm).
tr.ChangePrivacy=Telemetri yok. Ağ: hava durumu (Open-Meteo) ve şarkı sözleri (LRCLIB), ikisi de isteğe bağlı.
tr.TaskSwitcher=Stellaria Alt+Tab değiştiricisi
tr.TaskStartNow=Stellaria'yı şimdi başlat
tr.TaskOpenLauncher=Stellaria uygulamasını aç
tr.IconRestore=Stellaria - Görev çubuğunu geri yükle
tr.IconStart=Stellaria - Başlat
tr.IconUninstall=Stellaria'yı kaldır
tr.DownloadingWebView2=WebView2 indiriliyor
tr.DownloadingZebar=Zebar indiriliyor
tr.InstallingZebar=Zebar yükleniyor…
tr.ZebarFailed=Zebar yüklenemedi. "winget install -e --id glzr-io.zebar" ile yükleyin, ardından bu kurulumu yeniden çalıştırın.
tr.InstallFailed=Stellaria kaydedilemedi (kod %1). Günlük %LOCALAPPDATA%\Stellaria\logs\helper.log konumunda.
tr.RemoveSettings=Stellaria ayarlarınız da (renkler, seçenekler, sık kullanılanlar) silinsin mi?
ja.ChangesTitle=Stellaria が PC に加える変更
ja.ChangesDescription=インストール前にお読みください。アンインストールですべて元に戻ります。
ja.ChangesSubCaption=Stellaria は GNU GPL 3.0 ライセンスのフリー ソフトウェアです (インストール フォルダーの LICENSE を参照)。
ja.Requirements=必要条件
ja.ZebarPresent=Zebar: インストール済み
ja.ZebarAbsent=Zebar: 未インストール。インストールします (Windows が一度だけ許可を求めます)
ja.WebView2Present=WebView2: インストール済み
ja.WebView2Absent=WebView2: 未インストール。お使いのアカウントにインストールします
ja.Changes=変更内容
ja.ChangeFiles=%USERPROFILE%\.stellaria と %USERPROFILE%\.glzr\zebar\stellaria のファイル。お使いのアカウントのみで、管理者権限は不要です。
ja.ChangeStartup=3 つのタスクがサインイン時にサービス、Zebar、スイッチャーを起動します。
ja.ChangeTaskbar=テーマの実行中は Windows のタスク バーが非表示になり、停止すると元に戻ります。
ja.ChangeZebar=Zebar のスタートアップ リストに Stellaria のウィジェットが追加されます (設定のバックアップを保持します)。
ja.ChangeColors=オプション、既定ではオフ: Windows のアクセント カラーをテーマに合わせられます (設定 > 外観)。
ja.ChangePrivacy=テレメトリなし。ネットワーク: 天気 (Open-Meteo) と歌詞 (LRCLIB)、どちらも任意です。
ja.TaskSwitcher=Stellaria の Alt+Tab スイッチャー
ja.TaskStartNow=今すぐ Stellaria を起動する
ja.TaskOpenLauncher=Stellaria アプリを開く
ja.IconRestore=Stellaria - タスク バーを元に戻す
ja.IconStart=Stellaria - 起動
ja.IconUninstall=Stellaria をアンインストール
ja.DownloadingWebView2=WebView2 をダウンロードしています
ja.DownloadingZebar=Zebar をダウンロードしています
ja.InstallingZebar=Zebar をインストールしています…
ja.ZebarFailed=Zebar をインストールできませんでした。"winget install -e --id glzr-io.zebar" でインストールしてから、このセットアップをもう一度実行してください。
ja.InstallFailed=Stellaria を登録できませんでした (コード %1)。ログは %LOCALAPPDATA%\Stellaria\logs\helper.log にあります。
ja.RemoveSettings=Stellaria の設定 (色、オプション、お気に入り) も削除しますか?
ko.ChangesTitle=Stellaria가 PC에서 바꾸는 것
ko.ChangesDescription=설치하기 전에 읽어 주세요. 제거하면 모두 되돌려집니다.
ko.ChangesSubCaption=Stellaria는 GNU GPL 3.0 라이선스의 자유 소프트웨어입니다 (설치 폴더의 LICENSE 참조).
ko.Requirements=요구 사항
ko.ZebarPresent=Zebar: 설치됨
ko.ZebarAbsent=Zebar: 없음, 설치됩니다 (Windows에서 한 번 권한을 요청합니다)
ko.WebView2Present=WebView2: 설치됨
ko.WebView2Absent=WebView2: 없음, 내 계정에 설치됩니다
ko.Changes=변경 사항
ko.ChangeFiles=%USERPROFILE%\.stellaria 및 %USERPROFILE%\.glzr\zebar\stellaria의 파일. 내 계정에만 해당하며 관리자 권한이 필요 없습니다.
ko.ChangeStartup=로그인할 때 세 가지 작업이 서비스, Zebar, 전환기를 시작합니다.
ko.ChangeTaskbar=테마가 실행되는 동안 Windows 작업 표시줄이 숨겨지고, 중지하면 돌아옵니다.
ko.ChangeZebar=Zebar 시작 목록에 Stellaria 위젯이 추가됩니다 (설정 백업이 유지됩니다).
ko.ChangeColors=선택 사항, 기본값은 꺼짐: Windows 강조 색이 테마를 따를 수 있습니다 (설정 > 모양).
ko.ChangePrivacy=원격 분석 없음. 네트워크: 날씨 (Open-Meteo)와 가사 (LRCLIB), 모두 선택 사항입니다.
ko.TaskSwitcher=Stellaria Alt+Tab 전환기
ko.TaskStartNow=지금 Stellaria 시작
ko.TaskOpenLauncher=Stellaria 앱 열기
ko.IconRestore=Stellaria - 작업 표시줄 복원
ko.IconStart=Stellaria - 시작
ko.IconUninstall=Stellaria 제거
ko.DownloadingWebView2=WebView2 다운로드 중
ko.DownloadingZebar=Zebar 다운로드 중
ko.InstallingZebar=Zebar 설치 중…
ko.ZebarFailed=Zebar를 설치할 수 없습니다. "winget install -e --id glzr-io.zebar"로 설치한 뒤 이 설치 프로그램을 다시 실행하세요.
ko.InstallFailed=Stellaria를 등록할 수 없습니다 (코드 %1). 로그는 %LOCALAPPDATA%\Stellaria\logs\helper.log에 있습니다.
ko.RemoveSettings=Stellaria 설정 (색상, 옵션, 즐겨찾기)도 삭제할까요?

[Tasks]
Name: "switcher"; Description: "{cm:TaskSwitcher}"
Name: "startnow"; Description: "{cm:TaskStartNow}"
Name: "openlauncher"; Description: "{cm:TaskOpenLauncher}"

[InstallDelete]
Type: files; Name: "{userprograms}\Stellaria\*.lnk"
Type: files; Name: "{app}\restore-taskbar.ps1"
Type: filesandordirs; Name: "{#PackDir}"
Type: filesandordirs; Name: "{app}\bin"

[Files]
Source: "{#Stage}\bin\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "{#Stage}\pack\*"; DestDir: "{#PackDir}"; Excludes: "\native\StellariaSwitcher.exe"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Stage}\pack\native\StellariaSwitcher.exe"; DestDir: "{#PackDir}\native"; Tasks: switcher; Flags: ignoreversion
Source: "{#Stage}\registry\*"; DestDir: "{app}\registry"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#Stage}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Stage}\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Stage}\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Stellaria\Stellaria"; Filename: "{#Launcher}"; IconFilename: "{#Launcher}"; Check: LauncherShipped
Name: "{userprograms}\Stellaria\{cm:IconRestore}"; Filename: "{#Helper}"; Parameters: "--restore-shell"; IconFilename: "{sys}\imageres.dll"; IconIndex: 72
Name: "{userprograms}\Stellaria\{cm:IconStart}"; Filename: "{#Helper}"; Parameters: "--start"
Name: "{userprograms}\Stellaria\{cm:IconUninstall}"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Software\Classes\stellaria"; ValueType: string; ValueName: ""; ValueData: "URL:Stellaria"; Flags: uninsdeletekey; Check: LauncherShipped
Root: HKCU; Subkey: "Software\Classes\stellaria"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Check: LauncherShipped
Root: HKCU; Subkey: "Software\Classes\stellaria\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{#Launcher},0"; Check: LauncherShipped
Root: HKCU; Subkey: "Software\Classes\stellaria\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{#Launcher}"" ""%1"""; Check: LauncherShipped

[UninstallRun]
Filename: "{#Helper}"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "StellariaUninstall"

[UninstallDelete]
Type: filesandordirs; Name: "{userprograms}\Stellaria"
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

procedure StopStellaria;
var
  Code: Integer;
  Helper: String;
begin
  Helper := ExpandConstant('{#Helper}');
  if FileExists(Helper) then
  begin
    if not Exec(Helper, '--shutdown', '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
    begin
      Exec(Helper, '--quit', '', SW_HIDE, ewWaitUntilTerminated, Code);
      Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM zebar.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
    end;
  end;
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM StellariaSwitcher.exe /IM stellaria-helper.exe /IM stellaria-launcher.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

procedure ClearWidgetCache;
var
  Profile: String;
begin
  Profile := ExpandConstant('{userappdata}\zebar\webview-cache\stellaria\EBWebView\Default');
  DelTree(Profile + '\Cache', True, True, True);
  DelTree(Profile + '\Code Cache', True, True, True);
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
  StopStellaria;
  ClearWidgetCache;
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
  DelTree(ExpandConstant('{userappdata}\Stellaria'), True, True, True);
  DelTree(ExpandConstant('{localappdata}\Stellaria'), True, True, True);
  DelTree(ExpandConstant('{userappdata}\zebar\webview-cache\stellaria'), True, True, True);
end;
