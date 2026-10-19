# Diretrizes de Contribuição e Cooperação 🤝

Bem-vindo ao projeto **CitiesSkylinesTrafficAIOptimizerMods**! Este repositório foi criado para colaboração e desenvolvimento contínuo da extensão inteligente para Cities: Skylines 1 (Sunset Harbor).

---

## 🏛 Diretrizes de Engenharia e Arquitetura

1. **Clean Code & SOLID**:
   - Mantenha classes e métodos coesos com Responsabilidade Única (SRP).
   - Use abstrações injetáveis (`IPayloadBuilder`, `IResponseParser`, `ISecureKeyStorage`, etc.) para permitir testes unitários sem dependência estática do Unity Mono.
2. **Segurança Estrita de Chaves**:
   - **NUNCA** salve chaves de API em disco, PlayerPrefs, XML ou logs de console (`Debug.Log`).
   - Use exclusivamente `SecureKeyStorage` (memória RAM volátil `char[]` com sobrescrita explícita de `\0`).
3. **Internacionalização (i18n)**:
   - Qualquer nova mensagem na interface deve ser adicionada em `LocalizationManager.cs` com suporte aos 4 idiomas:
     - 🇧🇷 Português (`pt-BR`)
     - 🇺🇸 Inglês (`en`)
     - 🇫🇷 Francês (`fr`)
     - 🇪🇸 Espanhol (`es`)
4. **Performance & Zero Polling**:
   - Evite loops contínuos ou threads em `Update()` que consumam ciclos da CPU do jogo. A reavaliação de alertas e métricas deve ser estritamente sob demanda.

---

## 🌿 Fluxo de Trabalho (Git Workflow)

1. Faça um Fork do repositório ou clone diretamente se tiver permissão de escrita.
2. Crie uma branch descritiva a partir de `main`:
   ```bash
   git checkout -b feature/minha-melhoria
   ```
3. Realize as alterações e certifique-se de que a compilação e os testes passam com **0 erros e 0 avisos**:
   ```bash
   dotnet build TrafficAIOptimizer.sln
   dotnet test TrafficAIOptimizer.sln
   ```
4. Faça commit seguindo as convenções de commits semânticos (`feat:`, `fix:`, `refactor:`, `perf:`, `test:`).
5. Abra um Pull Request (PR) detalhando as mudanças e validações realizadas.

---

Agradecemos a sua cooperação para construir o mod de IA mais avançado e seguro do Cities: Skylines!
