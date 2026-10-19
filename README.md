# 🚦 Cities: Skylines - City & Traffic AI Optimizer 🧠

[![Build & Tests](https://img.shields.io/badge/tests-35%20passed-brightgreen.svg)]()
[![Target Framework](https://img.shields.io/badge/.NET-3.5%20%7C%204.7.2-blue.svg)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Cities: Skylines](https://img.shields.io/badge/Cities%3A%20Skylines-Sunset%20Harbor-orange.svg)]()

Extensão profissional para o jogo **Cities: Skylines 1 (Sunset Harbor)** que integra múltiplos ecossistemas de Inteligência Artificial para diagnosticar e otimizar a malha viária, resolver alertas urbanos críticos em tempo real e auditar intervenções com soluções alternativas avançadas para problemas persistentes.

---

## 🌟 Principais Recursos

- **🧠 Multi-Provedor de IA**: Suporte nativo a 6 ecossistemas líderes:
  - 🇨🇳 **DeepSeek**: `deepseek-chat` (V3), `deepseek-reasoner` (R1)
  - 🇺🇸 **OpenAI**: `gpt-4o`, `gpt-4o-mini`, `o3-mini`, `o1`, `gpt-4.5-preview`
  - 🤖 **Anthropic**: `claude-3-7-sonnet-20250219`, `claude-3-5-sonnet-20241022`, `claude-3-5-haiku-20241022`
  - 🌐 **Google Gemini**: `gemini-2.0-flash`, `gemini-2.5-pro`, `gemini-2.5-flash`, `gemini-1.5-pro`
  - ⚡ **Grok (xAI)**: `grok-2-latest`, `grok-2`, `grok-beta`
  - 🦙 **Meta (Llama)**: `llama-3.3-70b-versatile`, `llama-3.1-8b-instant`, `meta-llama/llama-3.3-70b-instruct` (via Groq / OpenRouter)
- **🔄 Preparado para a Mudança Contínua de Modelos**:
  - **Busca em Tempo Real via API (`[ 🔄 Buscar Modelos ]`)**: Consulta o endpoint oficial `/models` com a chave do jogador e atualiza a lista suspensa na hora.
  - **Entrada Livre de Modelo Customizado (`[ ➕ Outro Modelo ]`)**: Digite qualquer identificador recém-lançado (ex: `gpt-5`, `claude-4`, `gemini-3.0-flash`) e passe a usá-lo imediatamente sem aguardar updates do mod.
- **🌐 Internacionalização Completa (i18n)**:
  - Sincronização automática com a linguagem definida no jogo:
    - 🇧🇷 Português (`pt-BR`)
    - 🇺🇸 Inglês (`en` - fallback padrão internacional)
    - 🇫🇷 Francês (`fr`)
    - 🇪🇸 Espanhol (`es`)
  - A interface gráfica, os alertas urbanos e as instruções do prompt de sistema da IA respondem no idioma do jogador.
- **🏢 Otimizador de Alertas Urbanos da Cidade**:
  - Varredura de edifícios com problemas críticos: *Cadáver aguardando transporte, Poucos clientes/compradores, Poucos trabalhadores, Falta de água, Falta de luz, Esgoto acumulado, Lixo acumulado, Incêndio e Criminalidade*.
  - Ações diretas executadas na thread de simulação nativa do jogo (`SimulationManager`).
- **📋 Log de Auditoria & Soluções Alternativas (>10 min)**:
  - Todas as intervenções aplicadas são registradas com cronômetro em tempo real.
  - Se um problema persistir por mais de 10 minutos, o mod consulta a IA para formular uma solução estrutural/macrourbana alternativa.
  - Verificação leve sob demanda (zero polling em background para proteger o framerate do jogo).
- **🔒 Segurança Estrita de API Keys (In-Memory Only)**:
  - As chaves de cada provedor são mantidas em arrays voláteis `char[]` na memória RAM.
  - Zero persistência em disco, PlayerPrefs, XML ou logs de console.
  - Limpeza explícita com sobrescrita de bytes nulos (`\0`) via botão ou saída do mapa.

---

## 🏛 Arquitetura & SOLID

O projeto foi construído sob rigorosos padrões de engenharia de software:
- **SRP (Single Responsibility)**: Módulos específicos para tráfego, alertas da cidade, log de correções, parsers de resposta, segurança de memória e interface.
- **OCP (Open/Closed)**: Extensões de provedores desacopladas através de contratos (`IPayloadBuilder`, `IResponseParser`, `ISecureKeyStorage`).
- **DIP (Dependency Inversion)**: Injeção de dependências e abstrações que viabilizam 100% de cobertura de testes fora do motor Unity.

---

## 🛠 Compilação e Testes

### Pré-requisitos
- .NET SDK (6.0, 7.0, 8.0 ou 9.0)
- Visual Studio 2022, Rider ou VS Code

### Compilando a Solução:
```bash
dotnet build TrafficAIOptimizer.sln
```
*Se as DLLs do jogo estiverem em um local customizado, passe o parâmetro:*
```bash
dotnet build TrafficAIOptimizer.sln /p:CitiesManagedDir="C:\Caminho\Para\Cities_Data\Managed"
```

### Executando os Testes Unitários:
```bash
dotnet test TrafficAIOptimizer.sln
```
*Status:* **35 testes aprovados, 0 falhas**.

---

## 🤝 Como Cooperar e Contribuir

Consulte o arquivo [CONTRIBUTING.md](CONTRIBUTING.md) para detalhes sobre fluxo de branches, convenções de código e regras de Pull Request.

---

## 📄 Licença

Distribuído sob a licença [MIT](LICENSE).
