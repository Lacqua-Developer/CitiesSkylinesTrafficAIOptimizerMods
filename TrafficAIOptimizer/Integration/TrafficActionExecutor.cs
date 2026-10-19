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

                    // 4. Redistribuir faixas e re-rotear dinamicamente (Solução Anti-Fila Indiana)
                    if (bottleneck.SuggestedActions.Contains("REDISTRIBUTE_LANES") || bottleneck.IsSingleLaneCongestion)
                    {
                        int rerouted = RedistributeLanesAndReroute(bottleneck.SegmentId, nodeId);
                        resultMessage += string.Format("Anti-Fila Indiana: {0} veículo(s) re-roteados e faixas balanceadas. ", rerouted);
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
        /// Caso a IA não tenha sugerido ações, aplica heurística de desobstrução, anti-fila indiana e balanceamento semafórico imediato.
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

                            if (b.IsSingleLaneCongestion || b.LaneCount >= 2)
                            {
                                b.SuggestedActions.Add("REDISTRIBUTE_LANES");
                            }
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

                        if (b.SuggestedActions.Contains("REDISTRIBUTE_LANES") || b.IsSingleLaneCongestion)
                        {
                            int rerouted = RedistributeLanesAndReroute(b.SegmentId, nodeId);
                            resultMessage += string.Format("Anti-Fila Indiana: {0} veículos re-roteados. ", rerouted);
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

        /// <summary>
        /// Redistribui faixas viárias e força o recálculo dinâmico de rotas (Anti-Fila Indiana).
        /// Re-roteia veículos estagnados no raio do cruzamento e ajusta setas de faixas (Lane Arrows).
        /// Ativa também a IA Avançada de Veículos no TM:PE quando o mod estiver presente.
        /// </summary>
        public static int RedistributeLanesAndReroute(ushort segmentId, ushort nodeId)
        {
            int reroutedCount = 0;
            if (!Singleton<VehicleManager>.exists || !Singleton<NetManager>.exists || !Singleton<PathManager>.exists)
            {
                return 0;
            }

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            NetManager netManager = Singleton<NetManager>.instance;
            PathManager pathManager = Singleton<PathManager>.instance;

            Vector3 nodePos = netManager.m_nodes.m_buffer[nodeId].m_position;
            const float RADIUS = 80f;

            // 1. Re-roteamento dinâmico de veículos estagnados ou presos em fila
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
                    if (speed < 0.35f)
                    {
                        // Invalida o caminho estático pré-calculado no momento do spawn
                        uint currentPath = vehicleManager.m_vehicles.m_buffer[vId].m_path;
                        if (currentPath != 0)
                        {
                            pathManager.ReleasePath(currentPath);
                            vehicleManager.m_vehicles.m_buffer[vId].m_path = 0;
                        }
                        // Força o simulador a calcular nova rota no frame subsequente
                        vehicleManager.m_vehicles.m_buffer[vId].m_flags |= Vehicle.Flags.WaitingPath;
                        reroutedCount++;
                    }
                }
            }

            // 2. Balanceamento de Setas de Faixa (Lane Arrows) no segmento e nós adjacentes
            TryBalanceLaneArrows(segmentId, nodeId);

            // 3. Ativar IA Avançada de Veículos no TM:PE (se o mod estiver presente)
            if (IsTMPEAvailable())
            {
                TryEnableTMPEAdvancedVehicleAI();
            }

            return reroutedCount;
        }

        /// <summary>
        /// Balanceia as setas de faixa (Lane Arrows) nos segmentos para evitar afunilamento em fila única.
        /// </summary>
        public static void TryBalanceLaneArrows(ushort segmentId, ushort nodeId)
        {
            try
            {
                if (!Singleton<NetManager>.exists) return;
                NetManager netManager = Singleton<NetManager>.instance;

                if (segmentId == 0 || (netManager.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.Created) == 0) return;

                NetInfo info = netManager.m_segments.m_buffer[segmentId].Info;
                if (info == null || info.m_lanes == null) return;

                uint laneId = netManager.m_segments.m_buffer[segmentId].m_lanes;
                int laneIndex = 0;

                // Coletar faixas veiculares
                System.Collections.Generic.List<uint> vehicleLanes = new System.Collections.Generic.List<uint>();
                while (laneId != 0 && laneIndex < info.m_lanes.Length)
                {
                    if ((info.m_lanes[laneIndex].m_laneType & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) != 0)
                    {
                        vehicleLanes.Add(laneId);
                    }
                    laneId = netManager.m_lanes.m_buffer[laneId].m_nextLane;
                    laneIndex++;
                }

                if (vehicleLanes.Count >= 2)
                {
                    for (int i = 0; i < vehicleLanes.Count; i++)
                    {
                        uint lId = vehicleLanes[i];
                        ushort clearMask = (ushort)(NetLane.Flags.Left | NetLane.Flags.Right | NetLane.Flags.Forward);
                        netManager.m_lanes.m_buffer[lId].m_flags = (ushort)(netManager.m_lanes.m_buffer[lId].m_flags & ~clearMask);

                        if (vehicleLanes.Count == 2)
                        {
                            if (i == 0)
                            {
                                netManager.m_lanes.m_buffer[lId].m_flags |= (ushort)(NetLane.Flags.Left | NetLane.Flags.Forward);
                            }
                            else
                            {
                                netManager.m_lanes.m_buffer[lId].m_flags |= (ushort)(NetLane.Flags.Right | NetLane.Flags.Forward);
                            }
                        }
                        else
                        {
                            if (i == 0)
                            {
                                netManager.m_lanes.m_buffer[lId].m_flags |= (ushort)NetLane.Flags.Left;
                            }
                            else if (i == vehicleLanes.Count - 1)
                            {
                                netManager.m_lanes.m_buffer[lId].m_flags |= (ushort)NetLane.Flags.Right;
                            }
                            else
                            {
                                netManager.m_lanes.m_buffer[lId].m_flags |= (ushort)NetLane.Flags.Forward;
                            }
                        }
                    }
                    netManager.UpdateSegment(segmentId);
                }
            }
            catch
            {
                // Silencioso por segurança
            }
        }

        /// <summary>
        /// Tenta ativar as opções de Advanced Vehicle AI e Dynamic Lane Selection do TM:PE via Reflection segura.
        /// </summary>
        public static bool TryEnableTMPEAdvancedVehicleAI()
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly asm in assemblies)
                {
                    if (asm.GetName().Name == "TrafficManager")
                    {
                        Type optionsType = asm.GetType("TrafficManager.State.Options")
                                        ?? asm.GetType("TrafficManager.Lifecycle.TMPELifecycle")
                                        ?? asm.GetType("TrafficManager.Manager.Impl.OptionsManager");

                        if (optionsType != null)
                        {
                            PropertyInfo advProp = optionsType.GetProperty("AdvancedVehicleAI", BindingFlags.Public | BindingFlags.Static);
                            if (advProp != null && advProp.CanWrite)
                            {
                                advProp.SetValue(null, true, null);
                            }

                            PropertyInfo dlsProp = optionsType.GetProperty("AltLaneSelectionRatio", BindingFlags.Public | BindingFlags.Static);
                            if (dlsProp != null && dlsProp.CanWrite)
                            {
                                dlsProp.SetValue(null, 50, null);
                            }
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Silencioso por segurança
            }
            return false;
        }
    }
}
