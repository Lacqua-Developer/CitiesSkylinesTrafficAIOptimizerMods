using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace TrafficAIOptimizer.Analysis
{
    /// <summary>
    /// Gerenciador central de logs de auditoria de correções.
    /// Monitora a eficácia das intervenções e identifica reincidências após 10 minutos (600s),
    /// disparando pesquisa de soluções alternativas com a IA.
    /// </summary>
    public class CorrectionLogManager
    {
        private static CorrectionLogManager _instance;
        public static CorrectionLogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CorrectionLogManager();
                }
                return _instance;
            }
        }

        public const float TEN_MINUTES_SECONDS = 600f;
        public Core.Contracts.ITimeProvider TimeProvider { get; set; } = new Core.Services.UnityTimeProvider();
        private readonly List<CorrectionLogEntry> _logs = new List<CorrectionLogEntry>();
        private readonly object _lock = new object();

        public List<CorrectionLogEntry> GetAllLogs()
        {
            lock (_lock)
            {
                return new List<CorrectionLogEntry>(_logs);
            }
        }

        public CorrectionLogEntry RecordCorrection(
            ushort targetId,
            CorrectionTargetType targetType,
            string targetName,
            string problemDescription,
            string appliedAction)
        {
            CorrectionLogEntry entry = new CorrectionLogEntry
            {
                TargetId = targetId,
                TargetType = targetType,
                TargetName = targetName ?? "Alvo Desconhecido",
                ProblemDescription = problemDescription ?? "Problema não especificado",
                AppliedAction = appliedAction ?? "Ação padrão",
                Timestamp = DateTime.Now,
                RealtimeSinceStartupApplied = TimeProvider.GetCurrentTime(),
                ElapsedSeconds = 0f,
                Status = CorrectionStatus.Active
            };

            lock (_lock)
            {
                _logs.Insert(0, entry);
                if (_logs.Count > 50)
                {
                    _logs.RemoveAt(_logs.Count - 1);
                }
            }

            return entry;
        }

        /// <summary>
        /// Reavalia o status de todas as correções sob demanda (quando o usuário executa nova análise ou solicita atualização).
        /// Elimina qualquer necessidade de threads ou polling constante em segundo plano, poupando ciclos de CPU da simulação.
        /// </summary>
        public void ReanalyzeCorrections()
        {
            lock (_lock)
            {
                float currentRealtime = TimeProvider.GetCurrentTime();

                foreach (var log in _logs)
                {
                    log.ElapsedSeconds = currentRealtime - log.RealtimeSinceStartupApplied;

                    if (log.Status == CorrectionStatus.Active || log.Status == CorrectionStatus.PersistentExceeded10Min)
                    {
                        bool isStillTroubled = CheckIfTargetStillHasProblem(log);

                        if (!isStillTroubled)
                        {
                            log.Status = CorrectionStatus.Resolved;
                        }
                        else if (log.ElapsedSeconds >= TEN_MINUTES_SECONDS)
                        {
                            log.Status = CorrectionStatus.PersistentExceeded10Min;
                        }
                        else
                        {
                            log.Status = CorrectionStatus.Active;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Verifica se o edifício ou nó correspondente ainda apresenta problemas na simulação.
        /// </summary>
        public bool CheckIfTargetStillHasProblem(CorrectionLogEntry log)
        {
            if (log.TargetType == CorrectionTargetType.CityAlert)
            {
                if (!Singleton<BuildingManager>.exists) return false;
                BuildingManager bm = Singleton<BuildingManager>.instance;
                ushort bId = log.TargetId;

                if ((bm.m_buildings.m_buffer[bId].m_flags & Building.Flags.Created) == 0)
                {
                    return false; // Edifício demolido ou reconstruído
                }

                Notification.Problem problems = bm.m_buildings.m_buffer[bId].m_problems;
                return problems != Notification.Problem.None;
            }
            else if (log.TargetType == CorrectionTargetType.Traffic)
            {
                if (!Singleton<NetManager>.exists) return false;
                NetManager nm = Singleton<NetManager>.instance;
                ushort nId = log.TargetId;

                if ((nm.m_nodes.m_buffer[nId].m_flags & NetNode.Flags.Created) == 0)
                {
                    return false;
                }

                // Verifica densidade de segmentos conectados
                for (int i = 0; i < 8; i++)
                {
                    ushort sId = nm.m_nodes.m_buffer[nId].GetSegment(i);
                    if (sId > 0 && nm.m_segments.m_buffer[sId].m_trafficDensity > 50)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }
    }
}
