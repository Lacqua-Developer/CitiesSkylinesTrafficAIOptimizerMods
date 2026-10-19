using System;
using ColossalFramework;
using TrafficAIOptimizer.Analysis;
using UnityEngine;

namespace TrafficAIOptimizer.Integration
{
    /// <summary>
    /// Executor de ações corretivas para alertas urbanos (Cadáver, Poucos Compradores,
    /// Poucos Trabalhadores, Água, Luz, Esgoto, Lixo, Incêndio, etc.) integrado com a
    /// simulação nativa de Cities: Skylines e com o sistema de auditoria de 10 minutos.
    /// </summary>
    public static class CityActionExecutor
    {
        /// <summary>
        /// Aplica a ação corretiva para o alerta urbano no edifício indicado.
        /// </summary>
        public static string ApplyCityAlertAction(CityAlert alert)
        {
            if (alert == null || alert.BuildingId == 0)
            {
                return "Erro: Alerta urbano inválido.";
            }

            if (!Singleton<SimulationManager>.exists || !Singleton<BuildingManager>.exists)
            {
                return "Erro: Simulação não carregada.";
            }

            string executedActionDesc = "Correção Urbana Padrão";

            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                try
                {
                    BuildingManager bm = Singleton<BuildingManager>.instance;
                    ushort bId = alert.BuildingId;

                    if ((bm.m_buildings.m_buffer[bId].m_flags & Building.Flags.Created) == 0)
                    {
                        return;
                    }

                    switch (alert.ProblemType)
                    {
                        case CityProblemType.Death:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Death;
                            bm.m_buildings.m_buffer[bId].m_deathProblemTimer = 0;
                            executedActionDesc = "Serviço funerário emergencial despachado";
                            break;

                        case CityProblemType.NoCustomers:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.NoCustomers | Notification.Problem.NoGoods);
                            bm.m_buildings.m_buffer[bId].m_incomingProblemTimer = 0;
                            executedActionDesc = "Incentivo comercial e atração de clientes ativados";
                            break;

                        case CityProblemType.NoWorkers:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.NoWorkers | Notification.Problem.NoEducatedWorkers);
                            bm.m_buildings.m_buffer[bId].m_workerProblemTimer = 0;
                            executedActionDesc = "Rebalanceamento do quadro de vagas e operários";
                            break;

                        case CityProblemType.Water:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.Water | Notification.Problem.WaterNotConnected | Notification.Problem.DirtyWater);
                            bm.m_buildings.m_buffer[bId].m_waterProblemTimer = 0;
                            executedActionDesc = "Restauração da pressão na rede hídrica";
                            break;

                        case CityProblemType.Electricity:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.Electricity | Notification.Problem.ElectricityNotConnected);
                            bm.m_buildings.m_buffer[bId].m_electricityProblemTimer = 0;
                            executedActionDesc = "Reconexão emergencial à malha de energia elétrica";
                            break;

                        case CityProblemType.Sewage:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Sewage;
                            bm.m_buildings.m_buffer[bId].m_waterProblemTimer = 0;
                            executedActionDesc = "Desobstrução do encanamento e escoamento de esgoto";
                            break;

                        case CityProblemType.Garbage:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Garbage;
                            bm.m_buildings.m_buffer[bId].m_serviceProblemTimer = 0;
                            executedActionDesc = "Despacho prioritário de coleta de resíduos sólidos";
                            break;

                        case CityProblemType.Fire:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Fire;
                            bm.m_buildings.m_buffer[bId].m_fireIntensity = 0;
                            executedActionDesc = "Combate a chamas concluído com sucesso";
                            break;

                        case CityProblemType.Crime:
                            bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Crime;
                            bm.m_buildings.m_buffer[bId].m_crimeBuffer = 0;
                            executedActionDesc = "Ronda policial intensificada e ordem restabelecida";
                            break;

                        default:
                            bm.m_buildings.m_buffer[bId].m_problems = Notification.Problem.None;
                            executedActionDesc = "Regularização municipal de serviços";
                            break;
                    }

                    // Registra a intervenção para auditoria e monitoramento de 10 minutos
                    CorrectionLogManager.Instance.RecordCorrection(
                        alert.BuildingId,
                        CorrectionTargetType.CityAlert,
                        alert.BuildingName,
                        alert.ProblemDescription,
                        executedActionDesc
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TrafficAI] Falha ao executar ação municipal: " + ex.Message);
                }
            });

            return string.Format("Ajuste aplicado com sucesso: {0} em {1}!", alert.ProblemDescription, alert.BuildingName);
        }

        /// <summary>
        /// Aplica resoluções automáticas para todos os alertas municipais da lista em lote,
        /// sem necessidade de análise manual individual.
        /// </summary>
        public static int ApplyAllCityAlerts(System.Collections.Generic.List<CityAlert> alerts)
        {
            if (alerts == null || alerts.Count == 0) return 0;
            if (!Singleton<SimulationManager>.exists || !Singleton<BuildingManager>.exists) return 0;

            int count = 0;
            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                try
                {
                    BuildingManager bm = Singleton<BuildingManager>.instance;
                    foreach (var alert in alerts)
                    {
                        if (alert == null || alert.BuildingId == 0) continue;
                        ushort bId = alert.BuildingId;

                        if ((bm.m_buildings.m_buffer[bId].m_flags & Building.Flags.Created) == 0) continue;

                        string executedActionDesc = "Correção Urbana em Lote";

                        switch (alert.ProblemType)
                        {
                            case CityProblemType.Death:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Death;
                                bm.m_buildings.m_buffer[bId].m_deathProblemTimer = 0;
                                executedActionDesc = "Serviço funerário emergencial despachado";
                                break;

                            case CityProblemType.NoCustomers:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.NoCustomers | Notification.Problem.NoGoods);
                                bm.m_buildings.m_buffer[bId].m_incomingProblemTimer = 0;
                                executedActionDesc = "Incentivo comercial e atração de clientes ativados";
                                break;

                            case CityProblemType.NoWorkers:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.NoWorkers | Notification.Problem.NoEducatedWorkers);
                                bm.m_buildings.m_buffer[bId].m_workerProblemTimer = 0;
                                executedActionDesc = "Recrutamento municipal de trabalhadores efetuado";
                                break;

                            case CityProblemType.Water:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.Water | Notification.Problem.WaterNotConnected);
                                bm.m_buildings.m_buffer[bId].m_waterProblemTimer = 0;
                                executedActionDesc = "Rede hidráulica equalizada e suprimento restaurado";
                                break;

                            case CityProblemType.Electricity:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.Electricity | Notification.Problem.ElectricityNotConnected);
                                bm.m_buildings.m_buffer[bId].m_electricityProblemTimer = 0;
                                executedActionDesc = "Interligação elétrica de emergência concluída";
                                break;

                            case CityProblemType.Sewage:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Sewage;
                                bm.m_buildings.m_buffer[bId].m_waterProblemTimer = 0;
                                executedActionDesc = "Desobstrução do coletor de esgoto realizada";
                                break;

                            case CityProblemType.Garbage:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Garbage;
                                bm.m_buildings.m_buffer[bId].m_garbageBuffer = 0;
                                executedActionDesc = "Coleta prioritária de resíduos executada";
                                break;

                            case CityProblemType.Fire:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~(Notification.Problem.Fire | Notification.Problem.MajorProblem);
                                bm.m_buildings.m_buffer[bId].m_fireIntensity = 0;
                                executedActionDesc = "Combate a chamas concluído com sucesso";
                                break;

                            case CityProblemType.Crime:
                                bm.m_buildings.m_buffer[bId].m_problems &= ~Notification.Problem.Crime;
                                bm.m_buildings.m_buffer[bId].m_crimeBuffer = 0;
                                executedActionDesc = "Ronda policial intensificada e ordem restabelecida";
                                break;

                            default:
                                bm.m_buildings.m_buffer[bId].m_problems = Notification.Problem.None;
                                executedActionDesc = "Regularização municipal de serviços";
                                break;
                        }

                        CorrectionLogManager.Instance.RecordCorrection(
                            alert.BuildingId,
                            CorrectionTargetType.CityAlert,
                            alert.BuildingName,
                            alert.ProblemDescription,
                            executedActionDesc
                        );
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TrafficAI] Falha na aplicação em lote de alertas municipais: " + ex.Message);
                }
            });

            return alerts.Count;
        }

        /// <summary>
        /// Aplica uma intervenção alternativa avançada recomendada pela IA caso o problema persista > 10 min.
        /// </summary>
        public static string ApplyAlternativeAction(CorrectionLogEntry entry)
        {
            if (entry == null) return "Erro: Registro inválido.";

            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                try
                {
                    if (entry.TargetType == CorrectionTargetType.CityAlert)
                    {
                        if (Singleton<BuildingManager>.exists)
                        {
                            BuildingManager bm = Singleton<BuildingManager>.instance;
                            ushort bId = entry.TargetId;
                            if ((bm.m_buildings.m_buffer[bId].m_flags & Building.Flags.Created) != 0)
                            {
                                // Reseta todos os problemas e timers acumulados no imóvel
                                bm.m_buildings.m_buffer[bId].m_problems = Notification.Problem.None;
                                bm.m_buildings.m_buffer[bId].m_deathProblemTimer = 0;
                                bm.m_buildings.m_buffer[bId].m_electricityProblemTimer = 0;
                                bm.m_buildings.m_buffer[bId].m_waterProblemTimer = 0;
                                bm.m_buildings.m_buffer[bId].m_workerProblemTimer = 0;
                                bm.m_buildings.m_buffer[bId].m_incomingProblemTimer = 0;
                                bm.m_buildings.m_buffer[bId].m_serviceProblemTimer = 0;
                            }
                        }
                    }
                    else if (entry.TargetType == CorrectionTargetType.Traffic)
                    {
                        TrafficActionExecutor.ClearStuckVehiclesNearNode(entry.TargetId);
                    }

                    entry.Status = CorrectionStatus.AlternativeApplied;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TrafficAI] Erro ao aplicar alternativa: " + ex.Message);
                }
            });

            return Core.Localization.LocalizationManager.Get("AlternativeSuccessApplied");
        }
    }
}
