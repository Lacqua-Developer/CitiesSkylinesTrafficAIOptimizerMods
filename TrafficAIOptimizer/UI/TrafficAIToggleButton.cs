using ColossalFramework.UI;
using UnityEngine;

namespace TrafficAIOptimizer.UI
{
    /// <summary>
    /// Botão flutuante arrastável na tela para alternar a exibição do painel Traffic AI Optimizer.
    /// Projetado para alta visibilidade, fontes compatíveis e liberdade de posicionamento pelo jogador.
    /// </summary>
    public class TrafficAIToggleButton : UIButton
    {
        private UIDragHandle _dragHandle;

        public override void Start()
        {
            base.Start();

            name = "TrafficAIToggleButton";
            text = "AI";
            tooltip = "Traffic AI Optimizer (DeepSeek) - Clique para abrir/fechar [Alt + T]";
            
            // Dimensões compactas e elegantes
            width = 46f;
            height = 36f;
            textScale = 0.9f;
            textColor = new Color32(255, 220, 0, 255);
            hoveredTextColor = Color.white;
            pressedTextColor = Color.yellow;

            // Sprites nativos com alto contraste
            normalBgSprite = "ButtonMenu";
            hoveredBgSprite = "ButtonMenuHovered";
            pressedBgSprite = "ButtonMenuPressed";

            // Propriedades de exibição
            canFocus = false;
            isInteractive = true;
            isVisible = true;
            zOrder = 10000;

            // Posicionado na barra superior ao lado dos outros botões de mods
            relativePosition = new Vector3(110f, 15f);

            // Permite ao jogador arrastar o botão para qualquer canto da tela
            _dragHandle = AddUIComponent<UIDragHandle>();
            _dragHandle.width = width;
            _dragHandle.height = height;
            _dragHandle.relativePosition = Vector3.zero;
            _dragHandle.target = this;

            BringToFront();

            eventClick += (c, e) =>
            {
                if (TrafficAIPanel.Instance != null)
                {
                    TrafficAIPanel.Instance.ToggleVisibility();
                }
            };
        }
    }
}
