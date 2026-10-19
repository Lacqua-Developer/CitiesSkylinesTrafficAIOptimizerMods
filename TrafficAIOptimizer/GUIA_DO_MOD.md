# Guia Completo: City & Traffic AI Optimizer (DeepSeek)
**Cities: Skylines - Sunset Harbor**

---

## 📋 Sumário
1. [Visão Geral do Mod](#1-visão-geral-do-mod)
2. [Arquitetura e Segurança da API Key em RAM](#2-arquitetura-e-segurança-da-api-key-em-ram)
3. [Guia de Utilização no Jogo (Gameplay & UI)](#3-guia-de-utilização-no-jogo-gameplay--ui)
   - [3.1 Estrutura em Abas e Layout de Alto Contraste](#31-estrutura-em-abas-e-layout-de-alto-contraste)
   - [3.2 Módulo de Tráfego](#32-módulo-de-tráfego)
   - [3.3 Módulo de Alertas da Cidade (Cadáver, Compradores, Trabalhadores, Serviços)](#33-módulo-de-alertas-da-cidade)
   - [3.4 Log de Correções, Monitoramento de 10 Minutos e Soluções Alternativas](#34-log-de-correções-monitoramento-de-10-minutos-e-soluções-alternativas)
4. [Instalação e Ativação no Cities: Skylines](#4-instalação-e-ativação-no-cities-skylines)
5. [Compilação, Testes e Pipeline de Build](#5-compilação-testes-e-pipeline-de-build)
6. [Resolução de Problemas (Troubleshooting)](#6-resolução-de-problemas-troubleshooting)

---

## 1. Visão Geral do Mod

O **City & Traffic AI Optimizer** é uma ferramenta de gestão municipal inteligente e engenharia de tráfego baseada em Inteligência Artificial para o *Cities: Skylines (Sunset Harbor)*.

### Principais Capacidades:
- **Detecção em Tempo Real de Gargalos de Trânsito**: Varre nós e segmentos da malha viária calculando gravidade matemática com base em velocidade, densidade, semáforos e veículos emperrados.
- **Detecção Avançada de Alertas Urbanos**:
  - ⚰️ **Aguardando Cadáver (`Death`)**: Diagnostica problemas na rede funerária/crematórios e atrasos de acesso.
  - 🛍️ **Poucos Compradores (`NoCustomers`)**: Identifica falta de pedestres residenciais ou isolamento comercial.
  - 👷 **Poucos Trabalhadores (`NoWorkers`)**: Avalia descompasso de escolaridade e mobilidade urbana.
  - 💧 **Falta de Água / Esgoto (`Water` / `Sewage`)**: Diagnostica rompimentos na tubulação ou déficit de estações de tratamento.
  - ⚡ **Falta de Luz (`Electricity`)**: Detecta quarteirões desconectados da malha ou subdimensionamento elétrico.
  - 🗑️ **Lixo Acumulado (`Garbage`)**: Avalia frota de coleta, incineradores e aterros sanitários.
  - 🔥 **Incêndios e 🚨 Criminalidade**: Identifica ocorrências críticas para resposta emergencial.
- **Auditoria de Correções e Monitoramento de 10 Minutos**:
  - Cada intervenção municipal ou viária é registrada com carimbo de hora e cronômetro em tempo real.
  - Se o problema persistir por **mais de 10 minutos (600 segundos)**, o mod sinaliza persistência e aciona a DeepSeek para formular uma **Solução Alternativa Avançada** (reestruturação macro, transporte de massa ou políticas públicas).
- **Suporte Multilíngue Dinâmico (i18n)**:
  - Detecta automaticamente o idioma ativo do Cities: Skylines via `LocaleManager`.
  - Idiomas suportados com tradução integral: **Português (`pt-BR`)**, **Inglês (`en`)**, **Francês (`fr`)** e **Espanhol (`es`)**.
  - Fallback automático para **Inglês** caso o jogo esteja em outro idioma (Alemão, Russo, Chinês, Japonês, etc.).
  - A DeepSeek é instruída a responder no mesmo idioma configurado no jogo.
- **Raciocínio Especializado com DeepSeek**:
  - `deepseek-chat` (DeepSeek-V3): Diagnósticos ágeis em segundos.
  - `deepseek-reasoner` (DeepSeek-R1): Raciocínio aprofundado (*chain-of-thought*) para intervenções estruturais.
- **Foco Preciso de Câmera 3D e Overlays Visuais**: Translada suavemente a câmera até o nó viário ou edifício afetado e projeta um anel luminoso pulsante tridimensional.

---

## 2. Arquitetura e Segurança da API Key em RAM

O mod foi construído sob uma premissa inegociável de **Segurança Estrita de Dados**:

> [!IMPORTANT]
> **Armazenamento 100% Volátil (In-Memory Only)**
> - A sua API Key da DeepSeek **NUNCA** é gravada no disco rígido (`PlayerPrefs`, arquivos `.xml`, `.json` ou registro do Windows).
> - Em memória, a chave é armazenada em uma estrutura volátil desacoplada (`SecureKeyStorage`).
> - **Prevenção de Vazamento em Logs**: Nenhum log do console (`Debug.Log`) ou arquivo de despejo do Unity (`output_log.txt`) recebe a chave em texto puro.
> - **Higienização de Memória**: Ao ser descartada, cada caractere na RAM é explicitamente sobrescrito com `\0` antes da coleta de lixo.
> - **Descarte Automático**: A chave é permanentemente expurgada da RAM ao sair do mapa ou fechar o jogo (`LoadingExtension.OnLevelUnloading`).

---

## 3. Guia de Utilização no Jogo (Gameplay & UI)

### 3.1 Estrutura em Abas e Layout de Alto Contraste
1. **Abrindo o Painel**: Pressione **`Alt + T`** ou clique no botão dourado flutuante **`AI`** no topo da tela.
2. **Entrada de Chave com Alto Contraste**: O campo de texto possui fundo grafite azulado com borda iluminada ao focar, garantindo visibilidade clara contra o fundo da janela.
3. **Navegação por Abas**:
   - `[ 🚦 Tráfego ]`: Diagnósticos e resolução de fluxo viário.
   - `[ 🏢 Alertas da Cidade ]`: Varredura e mitigação de serviços municipais.
   - `[ 📋 Log & Alternativas (10m) ]`: Histórico de ações, cronômetro de 10 min e pesquisa de soluções alternativas.

### 3.2 Módulo de Tráfego
1. Clique em **`⚡ Analisar Tráfego`** para escanear a cidade.
2. Clique no nó desejado na lista para mover a câmera e visualizar a recomendação.
3. Clique em **`🛠 Aplicar Ajustes Automáticos`** (ativação/remoção de semáforos e desobstrução de veículos travados).

### 3.3 Módulo de Alertas da Cidade
1. Na aba **🏢 Alertas da Cidade**, clique em **`⚡ Analisar Alertas Urbanos`**.
2. O mod listará os edifícios com problemas críticos (Cadáver, Compradores, Trabalhadores, Água, Luz, Esgoto, Lixo).
3. Ao clicar em um edifício, a câmera translada até o local com um anel âmbar destacado.
4. Clique em **`🛠 Aplicar Resolução Municipal`** para realizar a intervenção imediata no local.

### 3.4 Log de Correções, Monitoramento de 10 Minutos e Soluções Alternativas
1. Toda ação aplicada é enviada automaticamente para o **Log de Auditoria**.
2. O cronômetro acompanha o tempo decorrido em tempo real (`00:00` -> `10:00`).
3. Se a simulação do jogo resolver o problema, o status passa para **`Resolvido`**.
4. Se o problema **continuar após mais de 10 minutos**:
   - O status muda para **`⚠️ Persiste (>10 min: MM:SS)`**.
   - O botão **`💡 Pesquisar Solução Alternativa com IA`** fica disponível.
   - A DeepSeek formula um diagnóstico macroestrutural identificando por que a intervenção convencional falhou e sugerindo uma estratégia alternativa.
   - Você pode revisar a recomendação e clicar em **`⚡ Aplicar Solução Alternativa`**!

---

## 4. Instalação e Ativação no Cities: Skylines

### 4.1 Localização dos Arquivos
O mod compilado é uma biblioteca de vínculo dinâmico (`TrafficAIOptimizer.dll`). O pipeline de build copia o arquivo diretamente para o diretório de mods locais do jogo:

```
[Pasta_Do_Jogo]\Files\Mods\TrafficAIOptimizer\TrafficAIOptimizer.dll
```

### 4.2 Ativando o Mod no Jogo
1. Inicie o **Cities: Skylines**.
2. No menu principal, vá em **Gerenciador de Conteúdo** (*Content Manager*).
3. Na barra lateral esquerda, clique em **Mods**.
4. Localize **`Traffic AI Optimizer (DeepSeek)`**.
5. Marque a caixa de seleção para **Habilitar** o mod.
6. Carregue sua cidade normalmente.

---

## 5. Compilação, Testes e Pipeline de Build

O projeto compila diretamente para o target `net35` compatível com a engine Mono do jogo.

### 5.1 Comandos de Compilação
```powershell
dotnet build "Source/TrafficAIOptimizer.sln"
```

### 5.2 Executando os Testes Unitários
```powershell
dotnet test "Source/TrafficAIOptimizer.sln"
```
*Status Atual:* **29 testes aprovados, 0 falhas**.

---

## 6. Resolução de Problemas (Troubleshooting)

| Sintoma | Causa Mais Provável | Solução |
| :--- | :--- | :--- |
| **"Erro: Nenhuma chave da DeepSeek informada"** | Chave não digitada ou já expurgada da RAM. | Digite sua chave no campo superior do painel e clique em *Definir na RAM*. |
| **"API Key inválida ou sem créditos (401)"** | Chave incorreta ou sem saldo na conta DeepSeek. | Verifique sua chave no console da DeepSeek e insira novamente. |
| **O painel não abre com `Alt + T`** | O jogo não está no modo de simulação ativa. | Carregue um mapa salvo ou inicie um novo jogo. Se preferir, use o botão `AI` no topo da tela. |
| **Alerta persistente após 10 minutos** | Causa macroestrutural (zoneamento ou estrangulamento de acesso). | Acesse a aba *Log & Alternativas (10m)* e clique em *Pesquisar Solução Alternativa com IA*. |
