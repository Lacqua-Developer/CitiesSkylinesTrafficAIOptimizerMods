using System;
using System.Reflection;
using ColossalFramework;
using TrafficAIOptimizer.Analysis;
using UnityEngine;

namespace TrafficAIOptimizer.Integration
{
    /// <summary>
    /// Executor de ações viárias com arquitetura híbrida (integração direta com TM:PE se disponível,
    /// com fallback completo para a engine nativa de Cities: Skylines).
    /// </summary>
    public static class TrafficActionExecutor
    {
        private static bool? _isTMPEAvailable = null;

        /// <summary>
        /// Verifica se o Traffic Manager: President Edition (TM:PE) está carregado na sessão.
        /// </summary>
        public static bool IsTMPEAvailable()
        {
            if (!_isTMPEAvailable.HasValue)
            {
                _isTMPEAvailable = CheckForTMPEAssembly();
            }
            return _isTMPEAvailable.Value;
        }

        private static bool CheckForTMPEAssembly()
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly asm in assemblies)
                {
                    if (asm.GetName().Name == "TrafficManager")
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Silencioso por segurança
            }
            return false;
        }

        /// <summary>
        /// Aplica as ações recomendadas pela IA para o gargalo indicado.
        /// </summary>
        public static string ApplyBottleneckActions(TrafficBottleneck bottleneck)
        {
            if (bottleneck == null || bottleneck.NodeId == 0)
            {
                return "Erro: Gargalo inválido.";
            }

            string resultMessage = "";

            if (!Singleton<SimulationManager>.exists || !Singleton<NetManager>.exists)
            {
                return "Erro: Simulação não carregada.";
            }

            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                try
                {
                    NetManager netManager = Singleton<NetManager>.instance;
                    ushort nodeId = bottleneck.NodeId;

                    if ((netManager.m_nodes.m_buffer[nodeId].m_flags & NetNode.Flags.Created) == 0)
                    {
                        return;
                    }

                    // 1. Remover semáforo se sugerido
                    if (bottleneck.SuggestedActions.Contains("REMOVE_TRAFFIC_LIGHT"))
                    {
                        SetTrafficLight(nodeId, false);
                        bottleneck.HasTrafficLight = false;
                        resultMessage += "Semáforo desativado. ";
                    }
                    // 2. Adicionar semáforo se sugerido
                    else if (bottleneck.SuggestedActions.Contains("ADD_TRAFFIC_LIGHT"))
                    {
                        SetTrafficLight(nodeId, true);
                        bottleneck.HasTrafficLight = true;
                        resultMessage += "Semáforo adicionado. ";
                    }

                    // 3. Desobstruir veículos travados no cruzamento
                    if (bottleneck.SuggestedActions.Contains("CLEAR_STUCK_VEHICLES") || bottleneck.StuckVehicleCount > 0)
                    {
                        int cleared = ClearStuckVehiclesNearNode(nodeId);
                        resultMessage += string.Format("{0} veículo(s) travados removidos. ", cleared);
                    }

                    // Registra a correção para monitoramento e auditoria de 10 min
                    CorrectionLogManager.Instance.RecordCorrection(
                        bottleneck.NodeId,
                        CorrectionTargetType.Traffic,
                        bottleneck.RoadName,
                        "Congestionamento de Tráfego",
                        string.IsNullOrEmpty(resultMessage) ? "Otimização de semáforo e fluxo" : resultMessage.Trim()
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TrafficAI] Falha ao aplicar ação: " + ex.Message);
                }
            });

            return string.IsNullOrEmpty(resultMessage) ? "Ajustes de fluxo aplicados com sucesso!" : resultMessage;
        }

        /// <summary>
        /// Aplica correções viárias automaticamente em todos os gargalos da lista em lote.
        /// Caso a IA não tenha sugerido ações, aplica heurística de desobstrução e balanceamento semafórico imediato.
        /// </summary>
        public static int ApplyAllBottlenecks(System.Collections.Generic.List<TrafficBottleneck> bottlenecks)
        {
            if (bottlenecks == null || bottlenecks.Count == 0) return 0;
            if (!Singleton<SimulationManager>.exists || !Singleton<NetManager>.exists) return 0;

            int count = 0;
            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                try
                {
                    NetManager netManager = Singleton<NetManager>.instance;
                    foreach (var b in bottlenecks)
                    {
                        if (b == null || b.NodeId == 0) continue;
                        ushort nodeId = b.NodeId;

                        if ((netManager.m_nodes.m_buffer[nodeId].m_flags & NetNode.Flags.Created) == 0) continue;

                        string resultMessage = "";

                        // Heurística automática caso a lista não tenha passado por análise manual prévia da IA
                        if (b.SuggestedActions.Count == 0)
                        {
                            if (b.HasTrafficLight && b.CongestionScore > 50)
                            {
                                b.SuggestedActions.Add("REMOVE_TRAFFIC_LIGHT");
                            }
                            else if (!b.HasTrafficLight && b.CongestionScore > 70 && b.ConnectedSegmentsCount >= 3)
                            {
                                b.SuggestedActions.Add("ADD_TRAFFIC_LIGHT");
                            }
                            b.SuggestedActions.Add("CLEAR_STUCK_VEHICLES");
                        }

                        if (b.SuggestedActions.Contains("REMOVE_TRAFFIC_LIGHT"))
                        {
                            SetTrafficLight(nodeId, false);
                            b.HasTrafficLight = false;
                            resultMessage += "Semáforo desativado. ";
                        }
                        else if (b.SuggestedActions.Contains("ADD_TRAFFIC_LIGHT"))
                        {
                            SetTrafficLight(nodeId, true);
                            b.HasTrafficLight = true;
                            resultMessage += "Semáforo adicionado. ";
                        }

                        if (b.SuggestedActions.Contains("CLEAR_STUCK_VEHICLES") || b.StuckVehicleCount > 0)
                        {
                            int cleared = ClearStuckVehiclesNearNode(nodeId);
                            resultMessage += string.Format("{0} veículo(s) travados removidos. ", cleared);
                        }

                        CorrectionLogManager.Instance.RecordCorrection(
                            b.NodeId,
                            CorrectionTargetType.Traffic,
                            b.RoadName,
                            "Congestionamento de Tráfego",
                            string.IsNullOrEmpty(resultMessage) ? "Otimização de semáforo e fluxo" : resultMessage.Trim()
                        );
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TrafficAI] Falha na aplicação em lote de tráfego: " + ex.Message);
                }
            });

            return bottlenecks.Count;
        }

        /// <summary>
        /// Habilita ou desabilita semáforos no nó viário.
        /// </summary>
        public static void SetTrafficLight(ushort nodeId, bool enable)
        {
            if (IsTMPEAvailable())
            {
                if (TrySetTMPETrafficLight(nodeId, enable))
                {
                    return;
                }
            }

            // Fallback Nativo do Cities: Skylines
            NetManager netManager = Singleton<NetManager>.instance;
            if (enable)
            {
                netManager.m_nodes.m_buffer[nodeId].m_flags |= NetNode.Flags.TrafficLights;
            }
            else
            {
                netManager.m_nodes.m_buffer[nodeId].m_flags &= ~NetNode.Flags.TrafficLights;
            }

            // Notificar atualização do nó
            netManager.UpdateNode(nodeId);
        }

        private static bool TrySetTMPETrafficLight(ushort nodeId, bool enable)
        {
            try
            {
                Type tmpeManagerType = Type.GetType("TrafficManager.Manager.Impl.TrafficLightManager, TrafficManager");
                if (tmpeManagerType != null)
                {
                    PropertyInfo instanceProp = tmpeManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    object instance = instanceProp?.GetValue(null, null);

                    if (instance != null)
                    {
                        MethodInfo setLightMethod = tmpeManagerType.GetMethod("SetTrafficLight", new[] { typeof(ushort), typeof(bool) });
                        if (setLightMethod != null)
                        {
                            setLightMethod.Invoke(instance, new object[] { nodeId, enable });
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Fallback automático para a engine nativa
            }
            return false;
        }

        /// <summary>
        /// Remove veículos que estejam travados (velocidade zero ou bloqueando a passagem) nos arredores do nó.
        /// </summary>
        public static int ClearStuckVehiclesNearNode(ushort nodeId)
        {
            int clearedCount = 0;
            if (!Singleton<VehicleManager>.exists || !Singleton<NetManager>.exists)
            {
                return 0;
            }

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            NetManager netManager = Singleton<NetManager>.instance;

            Vector3 nodePos = netManager.m_nodes.m_buffer[nodeId].m_position;
            const float RADIUS = 60f; // 60 metros em torno do nó

            ushort maxVehicles = (ushort)vehicleManager.m_vehicles.m_buffer.Length;
            for (ushort vId = 1; vId < maxVehicles; vId++)
            {
                if ((vehicleManager.m_vehicles.m_buffer[vId].m_flags & Vehicle.Flags.Created) == 0)
                {
                    continue;
                }

                Vector3 vehPos = vehicleManager.m_vehicles.m_buffer[vId].GetLastFramePosition();
                if (Vector3.Distance(vehPos, nodePos) <= RADIUS)
                {
                    float speed = vehicleManager.m_vehicles.m_buffer[vId].GetLastFrameVelocity().magnitude;
                    if (speed < 0.15f)
                    {
                        vehicleManager.ReleaseVehicle(vId);
                        clearedCount++;
                    }
                }
            }

            return clearedCount;
        }
    }
}
