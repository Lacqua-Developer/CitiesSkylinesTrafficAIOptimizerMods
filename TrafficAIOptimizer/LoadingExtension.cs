using ColossalFramework.UI;
using ICities;
using TrafficAIOptimizer.Security;
using TrafficAIOptimizer.UI;
using TrafficAIOptimizer.Visualizer;
using UnityEngine;

namespace TrafficAIOptimizer
{
    /// <summary>
    /// Gerencia o ciclo de vida do mod no carregamento de mapas e descarregamento da sessão.
    /// </summary>
    public class LoadingExtension : LoadingExtensionBase
    {
        private GameObject _overlayObject;
        private TrafficAIPanel _panel;
        private TrafficAIToggleButton _toggleButton;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            // Carrega apenas em modos de jogo ativos (Novo Jogo ou Carregar Jogo)
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame && mode != LoadMode.NewGameFromScenario)
            {
                return;
            }

            try
            {
                UIView view = UIView.GetAView();
                if (view == null) return;

                // 1. Instanciar GameObject para o Renderizador de Overlay 3D
                if (_overlayObject == null)
                {
                    _overlayObject = new GameObject("TrafficAI_OverlayRenderer");
                    _overlayObject.AddComponent<TrafficOverlayRenderer>();
                }

                // 2. Instanciar o Painel de UI
                if (_panel == null)
                {
                    _panel = view.AddUIComponent(typeof(TrafficAIPanel)) as TrafficAIPanel;
                }

                // 3. Instanciar o Botão de Atalho Flutuante
                if (_toggleButton == null)
                {
                    _toggleButton = view.AddUIComponent(typeof(TrafficAIToggleButton)) as TrafficAIToggleButton;
                    if (_toggleButton != null)
                    {
                        _toggleButton.BringToFront();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[TrafficAI] Falha ao carregar componentes de interface: " + ex.Message);
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            try
            {
                // Limpeza estrita de segurança: purga a API Key da memória RAM ao descarregar a sessão
                SecureKeyStorage.Clear();
                TrafficAIOptimizer.Analysis.CorrectionLogManager.Instance.ClearLogs();

                if (_toggleButton != null)
                {
                    Object.Destroy(_toggleButton.gameObject);
                    _toggleButton = null;
                }

                if (_panel != null)
                {
                    Object.Destroy(_panel.gameObject);
                    _panel = null;
                }

                if (_overlayObject != null)
                {
                    Object.Destroy(_overlayObject);
                    _overlayObject = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TrafficAI] Exceção durante descarregamento: " + ex.Message);
            }
        }
    }
}
