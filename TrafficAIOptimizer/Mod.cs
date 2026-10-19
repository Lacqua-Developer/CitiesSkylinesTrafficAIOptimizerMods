using ICities;

namespace TrafficAIOptimizer
{
    /// <summary>
    /// Ponto de entrada do mod para o Cities: Skylines (Sunset Harbor).
    /// Implementa IUserMod e OnSettingsUI para integração nativa com o painel de opções.
    /// </summary>
    public class Mod : IUserMod
    {
        public string Name => "Traffic AI Optimizer (DeepSeek)";

        public string Description
        {
            get
            {
                switch (Core.Localization.LocalizationManager.CurrentLanguage)
                {
                    case Core.Localization.AppLanguage.English:
                        return "Smart City & Traffic AI Optimizer powered by DeepSeek in RAM memory, compatible with TM:PE.";
                    case Core.Localization.AppLanguage.French:
                        return "Optimiseur intelligent de ville et trafic avec l'IA DeepSeek en RAM, compatible TM:PE.";
                    case Core.Localization.AppLanguage.Spanish:
                        return "Optimizador inteligente de ciudad y tráfico con IA DeepSeek en RAM, compatible con TM:PE.";
                    case Core.Localization.AppLanguage.Portuguese:
                    default:
                        return "Otimizador inteligente de tráfego e alertas urbanos com IA DeepSeek via RAM, compatível com TM:PE.";
                }
            }
        }

        /// <summary>
        /// Hook de opções do jogo (chamado por OptionsMainPanel).
        /// Previne o erro IndexOutOfRangeException no Gerenciador de Conteúdo e Menu de Opções.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper == null) return;

            UIHelperBase group = helper.AddGroup("Traffic AI Optimizer");
            group.AddGroup("Para abrir o painel in-game: Pressione Alt + T ou clique no botão AI no topo da tela.");
        }
    }
}
