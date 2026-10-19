using ColossalFramework;
using UnityEngine;

namespace TrafficAIOptimizer.Visualizer
{
    /// <summary>
    /// Renderiza destaques visuais 3D sobre o nó crítico e controla a movimentação suave de câmera.
    /// </summary>
    public class TrafficOverlayRenderer : MonoBehaviour
    {
        public static TrafficOverlayRenderer Instance { get; private set; }

        public Vector3? TargetPosition { get; set; }
        public float HighlightRadius { get; set; } = 35f;
        public Color HighlightColor { get; set; } = new Color(1f, 0.2f, 0.2f, 0.8f);

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Move a câmera do jogo até a localização exata do nó viário com problema.
        /// Utiliza tanto NetNode InstanceID quanto m_targetPosition para compatibilidade garantida.
        /// </summary>
        public void FocusCameraOn(ushort nodeId, Vector3 worldPos)
        {
            TargetPosition = worldPos;

            CameraController cameraController = ToolsModifierControl.cameraController;
            if (cameraController != null)
            {
                // Limpa alvos anteriores e translada a câmera para a posição do gargalo
                cameraController.ClearTarget();
                cameraController.m_targetPosition = worldPos;

                if (nodeId > 0)
                {
                    InstanceID id = new InstanceID { NetNode = nodeId };
                    cameraController.SetTarget(id, worldPos, false);
                }
            }
        }

        /// <summary>
        /// Move a câmera do jogo até a localização exata do edifício com alerta e desenha anel de destaque.
        /// </summary>
        public void FocusCameraOnBuilding(ushort buildingId, Vector3 worldPos)
        {
            TargetPosition = worldPos;
            HighlightColor = new Color(1f, 0.6f, 0.1f, 0.85f); // Âmbar luminoso para alertas prediais
            HighlightRadius = 25f;

            CameraController cameraController = ToolsModifierControl.cameraController;
            if (cameraController != null)
            {
                cameraController.ClearTarget();
                cameraController.m_targetPosition = worldPos;

                if (buildingId > 0)
                {
                    InstanceID id = new InstanceID { Building = buildingId };
                    cameraController.SetTarget(id, worldPos, false);
                }
            }
        }

        /// <summary>
        /// Limpa o destaque visual atual e interrompe a renderização do anel.
        /// </summary>
        public void ClearHighlight()
        {
            TargetPosition = null;
        }

        private void LateUpdate()
        {
            if (!TargetPosition.HasValue || !Singleton<RenderManager>.exists)
            {
                return;
            }

            Vector3 pos = TargetPosition.Value;
            RenderManager.CameraInfo cameraInfo = Singleton<RenderManager>.instance.CurrentCameraInfo;

            if (cameraInfo != null)
            {
                // Anel pulsante brilhante sobre o nó do gargalo
                float pulse = 1f + 0.12f * Mathf.Sin(Time.realtimeSinceStartup * 5f);
                float radius = HighlightRadius * pulse;

                Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(
                    cameraInfo,
                    HighlightColor,
                    pos,
                    radius,
                    pos.y - 10f,
                    pos.y + 25f,
                    false,
                    true
                );
            }
        }
    }
}
