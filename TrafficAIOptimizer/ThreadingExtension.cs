using ICities;
using TrafficAIOptimizer.UI;
using UnityEngine;

namespace TrafficAIOptimizer
{
    /// <summary>
    /// ThreadingExtension para capturar atalhos de teclado no jogo (Alt + T).
    /// </summary>
    public class ThreadingExtension : ThreadingExtensionBase
    {
        private bool _processedHotKey = false;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            base.OnUpdate(realTimeDelta, simulationTimeDelta);

            // Atalho: Alt + T
            bool isAltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool isTPressed = Input.GetKey(KeyCode.T);

            if (isAltPressed && isTPressed)
            {
                if (!_processedHotKey)
                {
                    _processedHotKey = true;
                    if (TrafficAIPanel.Instance != null)
                    {
                        TrafficAIPanel.Instance.ToggleVisibility();
                    }
                }
            }
            else
            {
                _processedHotKey = false;
            }
        }
    }
}
