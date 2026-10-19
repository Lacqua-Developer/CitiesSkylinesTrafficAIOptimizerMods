using System.Collections.Generic;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Providers;

namespace TrafficAIOptimizer.Core.Contracts
{
    /// <summary>
    /// Contrato para montagem de payloads sanitizados para múltiplos provedores de IA
    /// (DeepSeek, OpenAI, Anthropic, Gemini, Grok, Meta).
    /// </summary>
    public interface IPayloadBuilder
    {
        string BuildChatPayload(List<TrafficBottleneck> bottlenecks, string model);
        string BuildChatPayload(List<TrafficBottleneck> bottlenecks, string model, AIProvider provider);

        string BuildCityAlertPayload(List<CityAlert> alerts, string model);
        string BuildCityAlertPayload(List<CityAlert> alerts, string model, AIProvider provider);

        string BuildAlternativeSolutionPayload(CorrectionLogEntry entry, string model);
        string BuildAlternativeSolutionPayload(CorrectionLogEntry entry, string model, AIProvider provider);

        string EscapeJson(string text);
    }
}
