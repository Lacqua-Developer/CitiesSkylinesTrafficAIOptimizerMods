using System.Collections.Generic;
using ColossalFramework;
using TrafficAIOptimizer.Core.Contracts;
using TrafficAIOptimizer.Core.Services;
using UnityEngine;

namespace TrafficAIOptimizer.Analysis
{
    /// <summary>
    /// Analisa a simulação de tráfego de Cities: Skylines, extraindo métricas de velocidade,
    /// densidade e congestionamento de nós e segmentos.
    /// Utiliza IBottleneckCalculator para delegar a fórmula matemática de gravidade.
    /// </summary>
    public static class TrafficAnalyzer
    {
        private const int MAX_BOTTLENECKS_TO_RETURN = 8;
        private const float MIN_CONGESTION_THRESHOLD = 20f;
        private const float SEARCH_RADIUS = 75f;

        public static IBottleneckCalculator Calculator { get; set; } = new BottleneckCalculator();

        /// <summary>
        /// Escaneia a malha viária e retorna os principais gargalos ordenados por gravidade.
        /// </summary>
        public static List<TrafficBottleneck> DetectTopBottlenecks()
        {
            List<TrafficBottleneck> bottlenecks = new List<TrafficBottleneck>();

            if (!Singleton<NetManager>.exists || !Singleton<VehicleManager>.exists)
            {
                return bottlenecks;
            }

            NetManager netManager = Singleton<NetManager>.instance;
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;

            // Varrer segmentos viários ativos
            ushort maxSegments = (ushort)netManager.m_segments.m_buffer.Length;
            for (ushort sId = 1; sId < maxSegments; sId++)
            {
                if ((netManager.m_segments.m_buffer[sId].m_flags & NetSegment.Flags.Created) == 0)
                {
                    continue;
                }

                NetInfo info = netManager.m_segments.m_buffer[sId].Info;
                if (info == null || info.m_netAI == null || !(info.m_netAI is RoadBaseAI))
                {
                    continue; // Apenas vias de tráfego rodoviário
                }

                byte density = netManager.m_segments.m_buffer[sId].m_trafficDensity;
                if (density < 25)
                {
                    continue;
                }

                ushort startNode = netManager.m_segments.m_buffer[sId].m_startNode;
                ushort endNode = netManager.m_segments.m_buffer[sId].m_endNode;

                ushort criticalNode = endNode;
                if (startNode > 0 && (netManager.m_nodes.m_buffer[startNode].m_flags & NetNode.Flags.TrafficLights) != 0)
                {
                    criticalNode = startNode;
                }
                else if (endNode > 0 && (netManager.m_nodes.m_buffer[endNode].m_flags & NetNode.Flags.TrafficLights) != 0)
                {
                    criticalNode = endNode;
                }

                if (criticalNode == 0 || (netManager.m_nodes.m_buffer[criticalNode].m_flags & NetNode.Flags.Created) == 0)
                {
                    continue;
                }

                Vector3 nodePos = netManager.m_nodes.m_buffer[criticalNode].m_position;
                bool hasTrafficLight = (netManager.m_nodes.m_buffer[criticalNode].m_flags & NetNode.Flags.TrafficLights) != 0;

                // Contar segmentos conectados ao nó
                List<ushort> connectedSegs = new List<ushort>();
                for (int i = 0; i < 8; i++)
                {
                    ushort segId = netManager.m_nodes.m_buffer[criticalNode].GetSegment(i);
                    if (segId > 0 && (netManager.m_segments.m_buffer[segId].m_flags & NetSegment.Flags.Created) != 0)
                    {
                        connectedSegs.Add(segId);
                    }
                }

                // Amostragem de veículos nos arredores do nó crítico
                int vehicleCount = 0;
                int stuckCount = 0;
                ushort maxVehicles = (ushort)vehicleManager.m_vehicles.m_buffer.Length;

                for (ushort vId = 1; vId < maxVehicles; vId++)
                {
                    if ((vehicleManager.m_vehicles.m_buffer[vId].m_flags & Vehicle.Flags.Created) == 0)
                    {
                        continue;
                    }

                    Vector3 vehPos = vehicleManager.m_vehicles.m_buffer[vId].GetLastFramePosition();
                    if (Vector3.Distance(vehPos, nodePos) <= SEARCH_RADIUS)
                    {
                        vehicleCount++;
                        float speed = vehicleManager.m_vehicles.m_buffer[vId].GetLastFrameVelocity().magnitude;
                        if (speed < 0.2f)
                        {
                            stuckCount++;
                        }
                    }
                }

                // Cálculo via calculadora desacoplada (SOLID)
                float score = Calculator.CalculateScore(density, stuckCount, vehicleCount, hasTrafficLight);
                float speedPercent = Calculator.CalculateSpeedRatio(density);

                if (score < MIN_CONGESTION_THRESHOLD)
                {
                    continue;
                }

                string roadName = netManager.GetSegmentName(sId);
                if (string.IsNullOrEmpty(roadName))
                {
                    roadName = info.GetUncheckedLocalizedTitle();
                }

                TrafficBottleneck bottleneck = new TrafficBottleneck
                {
                    NodeId = criticalNode,
                    SegmentId = sId,
                    Position = nodePos,
                    RoadName = roadName ?? "Via sem nome",
                    CongestionScore = score,
                    AverageSpeedPercent = speedPercent,
                    VehicleCount = vehicleCount,
                    StuckVehicleCount = stuckCount,
                    HasTrafficLight = hasTrafficLight,
                    ConnectedSegmentsCount = connectedSegs.Count,
                    ConnectedSegmentIds = connectedSegs
                };

                bottlenecks.Add(bottleneck);
            }

            bottlenecks.Sort((a, b) => b.CongestionScore.CompareTo(a.CongestionScore));

            List<TrafficBottleneck> filtered = new List<TrafficBottleneck>();
            HashSet<ushort> visitedNodes = new HashSet<ushort>();

            foreach (var b in bottlenecks)
            {
                if (!visitedNodes.Contains(b.NodeId))
                {
                    visitedNodes.Add(b.NodeId);
                    filtered.Add(b);

                    if (filtered.Count >= MAX_BOTTLENECKS_TO_RETURN)
                    {
                        break;
                    }
                }
            }

            return filtered;
        }
    }
}
