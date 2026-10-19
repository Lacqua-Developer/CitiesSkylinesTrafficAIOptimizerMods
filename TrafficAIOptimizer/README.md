# City & Traffic AI Optimizer (DeepSeek) - Cities: Skylines Sunset Harbor

O **City & Traffic AI Optimizer** é um mod desenvolvido em C# (.NET / Unity Mono) para o jogo *Cities: Skylines (Sunset Harbor)*. Ele detecta em tempo real gargalos viários e alertas urbanos críticos (Cadáver aguardando transporte, Poucos compradores, Poucos trabalhadores, Falta de água, Luz, Esgoto, Lixo acumulado, Incêndio, Criminalidade), consulta a inteligência artificial da DeepSeek para obter causas raízes e diagnósticos especializados, executa ações automáticas de mitigação e audita todas as intervenções com monitoramento contínuo de 10 minutos (disparando pesquisa de soluções alternativas caso o problema persista).

---

## 🏛 Arquitetura e Princípios SOLID & Clean Code

O projeto foi construído adotando os mais altos padrões de arquitetura e engenharia de software:

1. **S - Single Responsibility Principle (SRP)**:
   - `SecureKeyStorage`: Custódia volátil e higienização segura da chave em RAM.
   - `BottleneckCalculator`: Fórmulas matemáticas de gravidade viária e velocidade.
   - `TrafficAnalyzer`: Varredura das entidades viárias (`NetManager`, `VehicleManager`).
   - `CityAlertAnalyzer`: Varredura de edifícios (`BuildingManager`) e detecção de alertas de serviços.
   - `CorrectionLogManager`: Auditoria cronológica de intervenções e monitoramento de 10 minutos.
   - `DeepSeekPayloadBuilder`: Construtor de payloads JSON sanitizados para tráfego, alertas da cidade e alternativas.
   - `DeepSeekResponseParser`: Parser resiliente com fallbacks defensivos garantidos.
   - `TrafficActionExecutor` / `CityActionExecutor`: Executores desacoplados de intervenções viárias e municipais.
   - `TrafficOverlayRenderer`: Renderizador exclusivo de destaques visuais 3D em nós e edifícios.
   - `TrafficAIPanel`: Camada de apresentação gráfica com alto contraste e navegação por abas.

2. **O - Open/Closed Principle (OCP)**:
   - O sistema de IA e provedores é desacoplado via contratos (`IPayloadBuilder`, `IResponseParser`, `IBottleneckCalculator`, `ITimeProvider`), permitindo extensões modulares sem alterar a lógica de domínio.

3. **L - Liskov Substitution Principle (LSP)**:
   - Todas as implementações concretas satisfazem integralmente seus contratos base sem efeitos colaterais.

4. **I - Interface Segregation Principle (ISP)**:
   - Interfaces pequenas e coesas em `Core/Contracts/`: `ISecureKeyStorage`, `IBottleneckCalculator`, `IPayloadBuilder`, `IResponseParser`, `ITimeProvider`.

5. **D - Dependency Inversion Principle (DIP)**:
   - Módulos de alto nível dependem de abstrações injetáveis, viabilizando testes unitários sem dependência estática do motor Unity.

---

## 🔒 Segurança Estrita da API Key (In-Memory Only)

- **Zero Persistência em Disco**: A chave da DeepSeek é capturada em tempo de execução através de campo mascarado (`isPasswordField = true`) com fundo de alto contraste e mantida puramente em array volátil `char[]` na memória RAM.
- **Prevenção de Vazamento em Logs**: Nenhum log de console (`Debug.Log`) ou arquivo de diagnóstico do Unity (`output_log.txt`) recebe a chave em texto plano.
- **Higienização de Memória (`SecureKeyStorage.Clear()`)**:
  - Sobrescrita explícita de cada caractere com `\0` antes da coleta de lixo.
  - Botão na UI para descarte manual a qualquer momento.
  - Purga automática na saída do mapa (`LoadingExtension.OnLevelUnloading()`).

---

## 🌐 Suporte Multilíngue Dinâmico (i18n)

O mod detecta automaticamente o idioma configurado no jogo via `ColossalFramework.Globalization.LocaleManager` e atualiza toda a interface em tempo de execução:
- 🇧🇷 **Português (pt-BR)**: Padrão brasileiro completo.
- 🇺🇸 **Inglês (en)**: Idioma global padrão e fallback para idiomas não mapeados (alemão, russo, chinês, etc.).
- 🇫🇷 **Francês (fr)**: Suporte completo a interfaces e alertas.
- 🇪🇸 **Espanhol (es)**: Suporte completo a interfaces e alertas.

O `LocalizationManager` integra-se ao evento `eventLocaleChanged` do motor de localização do jogo, traduzindo instantaneamente botões, títulos, alertas da cidade, status de auditoria e ajustando o prompt de sistema enviado à IA do DeepSeek para que as respostas sejam geradas no mesmo idioma do jogador.

---

## 🤖 Provedores de IA Suportados & Preparação para Mudança Contínua de Modelos

O mod suporta **6 ecossistemas líderes de IA**, desenhado com arquitetura aberta para acompanhar a evolução acelerada da inteligência artificial sem necessidade de recompilação:

| Provedor | Modelos Nativos Homologados | Mecanismo de Integração |
| :--- | :--- | :--- |
| **DeepSeek** | `deepseek-chat` (V3), `deepseek-reasoner` (R1) | OpenAI Chat Format (`api.deepseek.com`) |
| **OpenAI** | `gpt-4o`, `gpt-4o-mini`, `o3-mini`, `o1`, `gpt-4.5-preview` | OpenAI Chat Format (`api.openai.com`) |
| **Anthropic** | `claude-3-7-sonnet-20250219`, `claude-3-5-sonnet-20241022`, `claude-3-5-haiku-20241022` | Anthropic Messages API (`api.anthropic.com`) |
| **Google Gemini** | `gemini-2.0-flash`, `gemini-2.5-pro`, `gemini-2.5-flash`, `gemini-1.5-pro` | Gemini GenerateContent API (`generativelanguage.googleapis.com`) |
| **Grok (xAI)** | `grok-2-latest`, `grok-2`, `grok-beta` | OpenAI-Compatible API (`api.x.ai`) |
| **Meta (Llama)** | `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `meta-llama/llama-3.3-70b-instruct` | Groq LPU / OpenRouter Gateway (`api.groq.com` / `openrouter.ai`) |

### 🚀 Como o mod está preparado para a constante mudança de modelos:
1. **Busca de Modelos em Tempo Real via API (`[ 🔄 Buscar Modelos ]`)**:
   - Ao clicar no botão de busca, o mod consulta o endpoint `/models` oficial do provedor ativo com a sua chave de API, filtrando e adicionando instantaneamente à lista todos os modelos ativos que a sua conta possui acesso.
2. **Entrada Livre de Modelo Customizado (`[ ➕ Outro Modelo ]`)**:
   - Se a OpenAI, Anthropic, Google ou Meta lançar um novo modelo amanhã (ex: `gpt-5`, `claude-4`, `gemini-3.0-flash`, `llama-4`), você não precisa esperar atualização do mod: basta digitar o nome do modelo no campo da interface e clicar em **"➕ Outro Modelo"**. O mod passa a utilizá-lo imediatamente!
3. **Gestão Segura Multi-Chaves em RAM**:
   - Cada provedor mantém sua própria chave isolada estritamente na memória volátil (`SecureKeyStorage`). Trocar de provedor na interface preserva as chaves registradas em RAM até o fechamento da sessão ou comando explícito de limpeza.

---

## 🧪 Testes Unitários (NUnit)

A suíte de testes em `Source/TrafficAIOptimizer.Tests/` cobre:
- **Segurança da Memória RAM**: Armazenamento temporário isolado por provedor, mascaramento seguro e limpeza completa.
- **Cálculo de Congestionamento**: Pesos de densidade, veículos travados e semáforos.
- **Sanitização de Payloads**: Escape de caracteres JSON e envelopes para OpenAI, Anthropic e Gemini.
- **Resiliência do Parser de IA**: Extração de texto de OpenAI (`choices`), Anthropic (`content/text`) e Gemini (`candidates/parts/text`).
- **Auditoria de 10 Minutos**: Regras de persistência temporal e formatação de cronômetro.
- **Suporte Multilíngue**: Mapeamento de códigos de idioma (pt-BR, en, fr, es) com fallback para inglês.
- **Catálogo Dinâmico de Provedores**: Resolução de endpoints, filtros de modelos via API e suporte a modelos customizados.

### Executando os Testes:
```bash
dotnet test Source/TrafficAIOptimizer.sln
```
*Resultado:* **35 testes aprovados, 0 falhas**.

---

## 🚀 Como Compilar e Instalar

```bash
dotnet build Source/TrafficAIOptimizer.sln
```
A compilação copia automaticamente a DLL final para:
`Files/Mods/TrafficAIOptimizer/TrafficAIOptimizer.dll`
