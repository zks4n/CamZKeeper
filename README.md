# CamZKeeper

Utilitário para Windows que lê, ajusta e **mantém salvas** todas as propriedades UVC (foco, zoom, exposição, brilho, contraste, balanço de branco, ganho, nitidez, etc.) de webcams compatíveis — inclusive controles proprietários de fabricante, como o RightLight da Logitech.

Disponível em **Português (BR)** e **Inglês**.

## O problema que ele resolve

Muitas webcams (a Logitech C920 é o caso clássico) perdem as configurações de foco, zoom, exposição e nitidez toda vez que o computador reinicia ou que certos aplicativos são abertos — obrigando a reconfigurar tudo manualmente. Softwares como Logitech G HUB ou OBS conseguem ajustar esses valores, mas não garantem que fiquem persistidos.

O **CamZKeeper** guarda um perfil permanente dessas configurações, reaplica tudo automaticamente assim que o Windows liga, e detecta quando outro programa (OBS, Teams, Discord, navegador, etc.) começa a usar a câmera — reforçando as configurações certas na hora, sem precisar de nenhuma ação manual.

## Funcionalidades

- Detecta automaticamente as webcams conectadas (filtra câmeras virtuais como OBS Virtual Camera, que não implementam as interfaces UVC padrão).
- Lê e exibe todas as propriedades UVC suportadas pela câmera selecionada, com nomes traduzidos e consistentes com o painel nativo do Windows.
- Permite ajustar cada propriedade em tempo real, direto pelos sliders — sem precisar clicar em "Aplicar".
- Alterna entre modo Automático e Manual por propriedade (quando a câmera suporta), com o valor real da câmera refletido na tela.
- Restaura os valores de fábrica com um clique.
- Salva o perfil em disco (`CameraSettings.json`) e reaplica tudo automaticamente na próxima vez que a câmera for selecionada.
- Indica visualmente quais propriedades têm alterações ainda não salvas.
- Suporte a controles de Extension Unit (proprietários de fabricante), como o RightLight da Logitech, quando a câmera expõe essa interface.
- **Inicialização automática:** pode iniciar junto com o Windows (com a opção de abrir minimizado direto na bandeja do sistema) e já reaplica as configurações salvas antes mesmo de qualquer outro programa acessar a câmera.
- **Reforço automático:** observa em segundo plano quando algum aplicativo começa a usar a webcam (via um mecanismo nativo do Windows, sem polling — custo de CPU praticamente zero enquanto ocioso) e reaplica as configurações na hora, cobrindo o caso de câmeras que resetam seus próprios valores ao iniciar o stream de vídeo.
- **Troca de idioma:** um clique alterna toda a interface entre Português (BR) e Inglês, com a escolha lembrada nas próximas aberturas.
- **Reportar um problema:** botão "Problemas?" abre um formulário simples (modelo da câmera + descrição) que envia o relatório diretamente para o desenvolvedor, sem precisar configurar e-mail nem sair do app.
- Botão de apoio ao desenvolvedor (Twitch / Pix), pra quem quiser retribuir o projeto.

## Requisitos

- Windows 10/11
- Uma webcam com suporte UVC (USB Video Class) — a grande maioria das webcams USB modernas

Se você baixou o instalador da aba [Releases](../../releases), **não precisa instalar mais nada** — o .NET necessário já vem embutido.

## Instalação

### Opção 1 — Instalador (recomendado para a maioria dos usuários)

1. Vá em [Releases](../../releases) e baixe o instalador da versão mais recente (`CamZKeeper-Setup-x.x.x.exe`).
2. Execute o instalador e siga o assistente.
3. **Nota sobre avisos do Windows:** como o CamZKeeper não é um app assinado digitalmente (certificado de assinatura de código é pago), o Windows pode bloquear a execução na primeira vez. Isso é esperado em projetos pequenos/independentes e não significa que o app tenha algo de errado. Duas coisas diferentes podem acontecer, dependendo do seu Windows:

   - **Windows SmartScreen** (mais comum): aparece uma tela azul "O Windows protegeu seu computador". Clique em **"Mais informações" → "Executar assim mesmo"**.
   - **Controle de Aplicativos Inteligente / Smart App Control** (Windows 11, instalações limpas recentes): aparece uma mensagem dizendo que o app foi bloqueado, **sem nenhum botão pra liberar**. Se isso acontecer:
     1. Vá em **Configurações → Privacidade e segurança → Segurança do Windows → Controle de aplicativos e navegador → Configurações do Controle de Aplicativos Inteligente**.
     2. Mude temporariamente para **Desativado**.
     3. Rode o instalador normalmente.
     4. Depois, se quiser, reative a proteção (no Windows atualizado isso já não exige reinstalar o sistema).

### Opção 2 — Compilar a partir do código-fonte

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e Visual Studio 2022+ (ou `dotnet` CLI).

```bash
git clone https://github.com/<seu-usuario>/CamZKeeper.git
cd CamZKeeper
dotnet build
dotnet run --project CamZKeeper.Desktop
```

> Se for compilar do zero, crie o arquivo `CamZKeeper.Desktop/Support/DiscordWebhookConfig.cs` a partir do `DiscordWebhookConfig.example.cs` (o botão "Problemas?" não funciona sem isso, mas o restante do app funciona normalmente).

## Como usar

1. **Selecione sua webcam** na lista suspensa no topo da janela. Na primeira vez, o CamZKeeper lê todas as propriedades suportadas e cria o perfil automaticamente.
2. **Ajuste os sliders** — as mudanças são aplicadas na câmera imediatamente, sem precisar clicar em nada.
3. Clique em **Salvar** para persistir as alterações em disco (elas serão reaplicadas automaticamente depois).
4. Use **Restaurar Padrão** a qualquer momento para voltar aos valores de fábrica (não esqueça de salvar depois, se quiser manter).
5. Marque **"Iniciar automaticamente com o Windows"** para o CamZKeeper aplicar suas configurações assim que o computador ligar, sem precisar abrir o app manualmente.
6. Marque **"Minimizar para a bandeja do sistema"** para o app rodar discretamente em segundo plano (ícone perto do relógio) em vez de ficar aberto na área de trabalho ou na barra de tarefas.
7. Use o botão de **idioma** (bandeira 🇧🇷/🇺🇸) no topo pra alternar entre Português e Inglês a qualquer momento — a escolha fica salva para a próxima vez que abrir o app.
8. Se encontrar algum problema, clique em **"Problemas?"**, descreva o que aconteceu (o modelo da câmera já vem preenchido, se houver uma selecionada) e clique em **Enviar** — o relatório chega diretamente para o desenvolvedor.
9. Se quiser apoiar o projeto, use o botão **☕ Cafezinho**, que leva à Twitch e ao Pix do desenvolvedor.

## Arquitetura

O projeto é dividido em duas camadas, para manter a lógica de negócio independente da interface:

```
CamZKeeper
│
├── CamZKeeper.Core          → biblioteca de lógica pura, sem nenhuma dependência de UI
│   ├── Camera/               → descoberta, controle e observação de uso da webcam (DirectShow)
│   ├── Configuration/        → configuração da aplicação
│   ├── Core/                 → orquestração (ConfigurationManager)
│   ├── Models/                → modelos de dados (CameraSettings, UvcSetting, etc.)
│   └── Services/               → persistência em disco
│
└── CamZKeeper.Desktop        → interface WPF, consome o Core sem conhecer DirectShow
    ├── Controls/              → controles reutilizáveis (PropertyControl)
    ├── Localization/          → gerenciamento de idioma (PT-BR / EN-US)
    ├── Resources/             → dicionários de strings traduzidas
    ├── Support/               → envio de relatórios de problema (Discord Webhook)
    └── Assets/                → ícone e imagens da interface
```

**Princípios seguidos:**
- A interface (`Desktop`) não conhece DirectShow — só conversa com o `Core`.
- Toda a comunicação com a câmera passa exclusivamente pelo `CameraController`.
- O `Core` é reutilizável: no futuro pode virar base para uma interface diferente (CLI, API, MAUI) sem alterações.

## Tecnologias

- **C# / .NET 10**
- **WPF** para a interface
- **DirectShowLib** para comunicação com a câmera (`IAMCameraControl`, `IAMVideoProcAmp`, `IKsPropertySet`)
- **Discord Webhook** para o envio de relatórios de problema (sem credenciais de e-mail embutidas no app)

## Status do projeto

O CamZKeeper está funcionalmente completo para o seu propósito principal. A partir daqui, o foco passa a ser:

- Correção de bugs.
- Suporte a modelos de webcam adicionais (cada driver/fabricante pode se comportar de forma um pouco diferente).

Não há novas funcionalidades grandes planejadas no momento — sugestões continuam bem-vindas, mas o escopo do projeto é intencionalmente enxuto.

## Contribuindo

Sugestões, issues e pull requests são bem-vindos. Se encontrar um bug ou tiver uma ideia, abra uma [issue](../../issues) — ou use o botão **"Problemas?"** direto dentro do próprio app.

## Licença
Este projeto está licenciado sob os termos da [MIT License](LICENSE).
