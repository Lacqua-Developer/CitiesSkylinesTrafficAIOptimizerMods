using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace TrafficAIOptimizer.Analysis
{
    /// <summary>
    /// Escaneia os edifícios de Cities: Skylines identificando alertas críticos urbanos
    /// (Cadáver aguardando transporte, Poucos compradores, Poucos trabalhadores, Falta de água,
    /// Falta de luz, Esgoto, Lixo acumulado, etc.).
    /// </summary>
    public static class CityAlertAnalyzer
    {
        private const int MAX_ALERTS_TO_RETURN = 10;

        /// <summary>
        /// Varre o buffer de edifícios do jogo e extrai os alertas mais severos.
        /// </summary>
        public static List<CityAlert> DetectTopAlerts()
        {
            List<CityAlert> alerts = new List<CityAlert>();

            if (!Singleton<BuildingManager>.exists)
            {
                return alerts;
            }

            BuildingManager bm = Singleton<BuildingManager>.instance;
            ushort maxBuildings = (ushort)bm.m_buildings.m_buffer.Length;

            for (ushort bId = 1; bId < maxBuildings; bId++)
            {
                if ((bm.m_buildings.m_buffer[bId].m_flags & Building.Flags.Created) == 0)
                {
                    continue;
                }

                Notification.Problem problems = bm.m_buildings.m_buffer[bId].m_problems;
                if (problems == Notification.Problem.None)
                {
                    continue;
                }

                BuildingInfo info = bm.m_buildings.m_buffer[bId].Info;
                if (info == null)
                {
                    continue;
                }

                string buildingName = bm.GetBuildingName(bId, InstanceID.Empty);
                if (string.IsNullOrEmpty(buildingName))
                {
                    buildingName = info.GetUncheckedLocalizedTitle() ?? "Edifício Municipal";
                }

                Vector3 position = bm.m_buildings.m_buffer[bId].m_position;

                // 1. Cadáver aguardando transporte (Death)
                if ((problems & Notification.Problem.Death) != 0)
                {
                    byte timer = bm.m_buildings.m_buffer[bId].m_deathProblemTimer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.Death,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertDeath"),
                        ProblemTimer = timer,
                        Severity = 50f + timer * 0.6f,
                        AIRootCause = "Cadáver retido no edifício aguardando translado fúnebre; frota saturada ou acesso viário bloqueado.",
                        AIRecommendation = "Despachar serviço funerário imediato e verificar rotas de acesso de carros fúnebres.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "DISPATCH_HEARSE" }
                    });
                }

                // 2. Poucos compradores (NoCustomers / NoGoods)
                if ((problems & (Notification.Problem.NoCustomers | Notification.Problem.NoGoods)) != 0)
                {
                    byte timer = bm.m_buildings.m_buffer[bId].m_incomingProblemTimer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.NoCustomers,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertNoCustomers"),
                        ProblemTimer = timer,
                        Severity = 35f + timer * 0.4f,
                        AIRootCause = "Fluxo insuficiente de pedestres/consumidores ou desabastecimento de mercadorias no estabelecimento.",
                        AIRecommendation = "Ajustar zoneamento residencial e transporte público próximo para atrair clientes.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "BOOST_COMMERCIAL" }
                    });
                }

                // 3. Poucos trabalhadores (NoWorkers / NoEducatedWorkers)
                if ((problems & (Notification.Problem.NoWorkers | Notification.Problem.NoEducatedWorkers)) != 0)
                {
                    byte timer = bm.m_buildings.m_buffer[bId].m_workerProblemTimer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.NoWorkers,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertNoWorkers"),
                        ProblemTimer = timer,
                        Severity = 35f + timer * 0.4f,
                        AIRootCause = "Falta de mão de obra qualificada no entorno ou transporte público insuficiente para conexão de bairros.",
                        AIRecommendation = "Expandir conexões viárias/metrô e rebalancear nível educacional das escolas da região.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "BOOST_WORKERS" }
                    });
                }

                // 4. Falta de Água / Esgoto (Water / Sewage)
                if ((problems & (Notification.Problem.Water | Notification.Problem.WaterNotConnected | Notification.Problem.DirtyWater)) != 0)
                {
                    byte timer = bm.m_buildings.m_buffer[bId].m_waterProblemTimer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.Water,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertWater"),
                        ProblemTimer = timer,
                        Severity = 45f + timer * 0.5f,
                        AIRootCause = "Rede hídrica desconectada ou capacidade de bombeamento de água potável saturada.",
                        AIRecommendation = "Conectar tubulação subterrânea de água e restabelecer pressão hidráulica municipal.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "RESTORE_WATER" }
                    });
                }
                else if ((problems & Notification.Problem.Sewage) != 0)
                {
                    byte timer = bm.m_buildings.m_buffer[bId].m_waterProblemTimer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.Sewage,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertSewage"),
                        ProblemTimer = timer,
                        Severity = 42f + timer * 0.5f,
                        AIRootCause = "Esgoto acumulado no imóvel por falta de tubos coletores ou saturação dos canos de descarte.",
                        AIRecommendation = "Desobstruir a tubulação e interligar nova saída de escoamento de esgoto sanitário.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "RESTORE_SEWAGE" }
                    });
                }

                // 5. Falta de Energia (Electricity)
                if ((problems & (Notification.Problem.Electricity | Notification.Problem.ElectricityNotConnected)) != 0)
                {
                    byte timer = bm.m_buildings.m_buffer[bId].m_electricityProblemTimer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.Electricity,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertElectricity"),
                        ProblemTimer = timer,
                        Severity = 45f + timer * 0.5f,
                        AIRootCause = "Edifício isolado da malha elétrica urbana ou consumo geral da cidade superior à geração ativa.",
                        AIRecommendation = "Reconectar malha com linhas de alta tensão e expandir capacidade das usinas geradoras.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "RESTORE_ELECTRICITY" }
                    });
                }

                // 6. Lixo acumulado (Garbage)
                if ((problems & Notification.Problem.Garbage) != 0)
                {
                    byte timer = bm.m_buildings.m_buffer[bId].m_serviceProblemTimer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.Garbage,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertGarbage"),
                        ProblemTimer = timer,
                        Severity = 38f + timer * 0.4f,
                        AIRootCause = "Lixo acumulado na calçada; capacidade de aterro/incineração sobrecarregada ou rotas emperradas.",
                        AIRecommendation = "Despachar caminhão de lixo prioritário e desobstruir vias de tráfego de caminhões de limpeza.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "DISPATCH_GARBAGE" }
                    });
                }

                // 7. Incêndio (Fire)
                if ((problems & Notification.Problem.Fire) != 0)
                {
                    int intensity = bm.m_buildings.m_buffer[bId].m_fireIntensity;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.Fire,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertFire"),
                        ProblemTimer = intensity,
                        Severity = 75f + intensity * 0.25f,
                        AIRootCause = "Incêndio ativo no imóvel com risco iminente de propagação e colapso estrutural.",
                        AIRecommendation = "Despacho prioritário do corpo de bombeiros para extinção das chamas.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "DISPATCH_FIREFIGHTER" }
                    });
                }

                // 8. Criminalidade (Crime)
                if ((problems & Notification.Problem.Crime) != 0)
                {
                    int crime = bm.m_buildings.m_buffer[bId].m_crimeBuffer;
                    alerts.Add(new CityAlert
                    {
                        BuildingId = bId,
                        BuildingName = buildingName,
                        Position = position,
                        ProblemType = CityProblemType.Crime,
                        ProblemDescription = Core.Localization.LocalizationManager.Get("AlertCrime"),
                        ProblemTimer = crime,
                        Severity = 35f + crime * 0.2f,
                        AIRootCause = "Ocorrência criminal ativa no local decorrente de baixa cobertura de patrulhamento.",
                        AIRecommendation = "Despachar viatura policial de emergência e restabelecer a segurança pública.",
                        SuggestedActions = new System.Collections.Generic.List<string> { "DISPATCH_POLICE" }
                    });
                }
            }

            // Ordena os alertas pelos mais graves primeiro
            alerts.Sort((a, b) => b.Severity.CompareTo(a.Severity));

            if (alerts.Count > MAX_ALERTS_TO_RETURN)
            {
                return alerts.GetRange(0, MAX_ALERTS_TO_RETURN);
            }

            return alerts;
        }
    }
}
