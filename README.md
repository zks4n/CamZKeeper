# CamZKeeper

Utilitário para Windows que lê, ajusta e **mantém salvas** todas as propriedades UVC (foco, zoom, exposição, brilho, contraste, balanço de branco, ganho, nitidez, etc.) de webcams compatíveis — inclusive controles proprietários de fabricante, como o RightLight da Logitech.

<!-- Sugestão: coloque aqui um print ou GIF da interface em ação.
     Exemplo de sintaxe: ![Tela principal do CamZKeeper](docs/screenshot.png) -->

## O problema que ele resolve

Muitas webcams (a Logitech C920 é o caso clássico) perdem as configurações de foco, zoom, exposição e nitidez toda vez que o computador reinicia ou que certos aplicativos são abertos — obrigando a reconfigurar tudo manualmente. Softwares como Logitech G HUB ou OBS conseguem ajustar esses valores, mas não garantem que fiquem persistidos.

O **CamZKeeper** guarda um perfil permanente dessas configurações e reaplica tudo com um clique — sem depender de software proprietário do fabricante.

## Funcionalidades

- Detecta automaticamente as webcams conectadas (filtra câmeras virtuais como OBS Virtual Camera, que não implementam as interfaces UVC padrão).
- Lê e exibe todas as propriedades UVC suportadas pela câmera selecionada.
- Permite ajustar cada propriedade em tempo real, direto pelos sliders — sem precisar clicar em "Aplicar".
- Alterna entre modo Automático e Manual por propriedade (quando a câmera suporta), com o valor real da câmera refletido na tela.
- Restaura os valores de fábrica com um clique.
- Salva o perfil em disco (`CameraSettings.json`) e reaplica tudo automaticamente na próxima vez que a câmera for selecionada.
- Indica visualmente quais propriedades têm alterações ainda não salvas.
- Suporte a controles de Extension Unit (proprietários de fabricante), como o RightLight da Logitech, quando a câmera expõe essa interface.
- Observa em segundo plano quando algum aplicativo começa a usar a webcam (via Windows, sem polling — custo de CPU zero enquanto ocioso).

## Requisitos

- Windows 10/11
- Uma webcam com suporte UVC (USB Video Class) — a grande maioria das webcams USB modernas

Se você baixou o instalador da aba [Releases](../../releases), **não precisa instalar mais nada** — o .NET necessário já vem embutido.

## Instalação

### Opção 1 — Instalador (recomendado para a maioria dos usuários)

1. Vá em [Releases](../../releases) e baixe o instalador da versão mais recente (`CamZKeeper-Setup-x.x.x.exe`).
2. Execute o instalador e siga o assistente.
3. **Nota:** por não ser um app assinado digitalmente (certificado de assinatura de código é pago), o Windows SmartScreen pode exibir um aviso na primeira execução. Isso é esperado em projetos pequenos/independentes — clique em **"Mais informações" → "Executar assim mesmo"**.

### Opção 2 — Compilar a partir do código-fonte

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e Visual Studio 2022+ (ou `dotnet` CLI).

```bash
git clone https://github.com/<seu-usuario>/CamZKeeper.git
cd CamZKeeper
dotnet build
dotnet run --project CamZKeeper.Desktop
```

## Como usar

1. Abra o CamZKeeper e selecione sua webcam na lista.
2. Na primeira vez, ele lê todas as propriedades suportadas e cria o perfil automaticamente.
3. Ajuste os sliders — as mudanças são aplicadas na câmera imediatamente.
4. Clique em **Salvar** para persistir as alterações em disco.
5. Use **Restaurar Padrão** a qualquer momento para voltar aos valores de fábrica (não esqueça de salvar depois, se quiser manter).

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
```

**Princípios seguidos:**
- A interface (`Desktop`) não conhece DirectShow — só conversa com o `Core`.
- Toda a comunicação com a câmera passa exclusivamente pelo `CameraController`.
- O `Core` é reutilizável: no futuro pode virar base para uma interface diferente (CLI, API, MAUI) sem alterações.

## Tecnologias

- **C# / .NET 10**
- **WPF** para a interface
- **DirectShowLib** para comunicação com a câmera (`IAMCameraControl`, `IAMVideoProcAmp`, `IKsPropertySet`)

## Roadmap

- [x] Descoberta, leitura e ajuste de propriedades UVC em tempo real
- [x] Persistência de configurações (salvar/restaurar/restaurar padrão de fábrica)
- [x] Indicação visual de alterações não salvas
- [x] Suporte a Extension Unit (RightLight e afins)
- [ ] Aplicação automática das configurações ao iniciar o Windows
- [ ] Agrupamento/busca de propriedades na interface
- [ ] Suporte simultâneo a múltiplas webcams
- [ ] Perfis por dispositivo

## Contribuindo

Sugestões, issues e pull requests são bem-vindos. Se encontrar um bug ou tiver uma ideia, abra uma [issue](../../issues).

## Licença

Este projeto está licenciado sob os termos da [MIT License](LICENSE).
