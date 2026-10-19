using System.Collections.Generic;
using TrafficAIOptimizer.Analysis;

namespace TrafficAIOptimizer.Core.Contracts
{
    /// <summary>
    /// Contrato para parsing da resposta JSON e estruturação de diagnósticos da IA.
    /// </summary>
    public interface IResponseParser
    {
        void ParseAndApply(string jsonResponse, List<TrafficBottleneck> targetBottlenecks);
        void ParseAndApplyCityAlerts(string jsonResponse, List<CityAlert> targetAlerts);
        void ParseAndApplyAlternativeSolution(string jsonResponse, CorrectionLogEntry entry);
    }
}
