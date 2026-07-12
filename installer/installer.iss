; ============================================================================
; Script do Inno Setup para o CamZKeeper.
;
; COMO USAR:
; 1. Instale o Inno Setup (gratuito): https://jrsoftware.org/isinfo.php
; 2. Gere a publicação self-contained do app (veja o comando abaixo).
; 3. Abra este arquivo no Inno Setup Compiler (ou clique com o botão direito
;    nele > "Compile") e ele gera o instalador em installer\Output\.
;
; COMANDO PARA GERAR A PUBLICAÇÃO (rode na pasta raiz da solução):
;
;   dotnet publish CamZKeeper.Desktop -c Release -r win-x64 --self-contained true ^
;     -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
;     -o publish
;
; Isso gera um único CamZKeeper.exe (grande, ~100-150MB, porque embute o .NET)
; na pasta "publish" na raiz da solução. Ajuste o caminho abaixo (#SourcePath)
; se você salvar em outro lugar.
; ============================================================================

#define MyAppName "CamZKeeper"
#define MyAppVersion "1.0.2"
#define MyAppPublisher "zks4n"
#define MyAppURL "https://github.com/zks4n/CamZKeeper"
#define MyAppExeName "CamZKeeper.exe"

; Pasta onde está a publicação gerada pelo "dotnet publish".
; Caminho relativo à pasta onde este .iss está salvo.
#define SourcePath "..\publish"

[Setup]
; Identificador único e fixo do app - NÃO mude entre versões, senão o Windows
; trata como um programa diferente e não substitui a instalação anterior.
AppId={{7A405474-C20E-4946-A9B6-545D85115C55}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Nome do instalador gerado (fica em installer\Output\)
OutputDir=Output
OutputBaseFilename=CamZKeeper-Setup-{#MyAppVersion}
; Compressão - lzma2/ultra reduz bem o tamanho do instalador self-contained
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
; Instalador de 64 bits (compatível com o publish win-x64)
ArchitecturesInstallIn64BitMode=x64compatible
; Ícone do instalador em si (opcional - descomente e ajuste o caminho se tiver um .ico)
SetupIconFile=..\CamZKeeper.Desktop\Assets\icon_webcam.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"

[Files]
; Copia TUDO que estiver na pasta de publicação (o .exe e eventuais dependências)
Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Oferece abrir o app assim que o instalador terminar
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName} agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Garante que arquivos gerados em runtime (config, perfil da câmera) sejam
; removidos na desinstalação também, já que ficam na pasta de instalação.
Type: filesandordirs; Name: "{app}"
