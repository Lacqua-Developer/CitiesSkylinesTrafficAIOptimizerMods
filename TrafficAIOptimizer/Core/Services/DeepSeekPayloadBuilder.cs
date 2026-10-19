using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Contracts;
using TrafficAIOptimizer.Core.Localization;
using TrafficAIOptimizer.Core.Providers;

namespace TrafficAIOptimizer.Core.Services
{
    /// <summary>
    /// Construtor de payloads seguros e otimizados para múltiplos provedores de IA
    /// (DeepSeek, OpenAI, Anthropic, Gemini, Grok, Meta).
    /// Respeita SRP, OCP e formatação invariante (CultureInfo.InvariantCulture) para compatibilidade internacional.
    /// </summary>
    public class DeepSeekPayloadBuilder : IPayloadBuilder
    {
        public string BuildChatPayload(List<TrafficBottleneck> bottlenecks, string model)
        {
            return BuildChatPayload(bottlenecks, model, AIProvider.DeepSeek);
        }

        public string BuildChatPayload(List<TrafficBottleneck> bottlenecks, string model, AIProvider provider)
        {
            if (bottlenecks == null)
            {
                bottlenecks = new List<TrafficBottleneck>();
            }

            string langInstruction = LocalizationManager.GetAiLanguageInstruction();
            string systemPrompt = string.Format(
                "Você é um engenheiro de tráfego sênior especialista no jogo Cities: Skylines 1 (Sunset Harbor). Analise os gargalos recebidos. {0} Para cada gargalo (identificado por NodeId), forneça: CAUSA (uma frase), SOLUCAO (orientação de engenharia viária para o jogador) e ACAO_AUTO (uma das opções: REMOVE_TRAFFIC_LIGHT, ADD_TRAFFIC_LIGHT, CLEAR_STUCK_VEHICLES ou NONE).",
                langInstruction
            );

            StringBuilder userContent = new StringBuilder();
            userContent.Append("Aqui estão os nós mais congestionados da cidade:\n");
            for (int i = 0; i < bottlenecks.Count; i++)
            {
                var b = bottlenecks[i];
                userContent.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "Gargalo #{0}: [NodeId: {1}, Via: {2}, Score: {3:0.0}, VelMedia: {4:0}%, Veiculos: {5}, Bloqueados: {6}, Semaforo: {7}, ViasConectadas: {8}]\n",
                    i + 1,
                    b.NodeId,
                    b.RoadName,
                    b.CongestionScore,
                    b.AverageSpeedPercent,
                    b.VehicleCount,
                    b.StuckVehicleCount,
                    b.HasTrafficLight ? "SIM" : "NAO",
                    b.ConnectedSegmentsCount
                );
            }
            userContent.Append("\nFormate a resposta estritamente assim para cada gargalo:\nNODE: <NodeId>\nCAUSA: <texto>\nSOLUCAO: <texto>\nACAO_AUTO: <REMOVE_TRAFFIC_LIGHT|ADD_TRAFFIC_LIGHT|CLEAR_STUCK_VEHICLES|NONE>");

            return BuildEnvelope(systemPrompt, userContent.ToString(), model, provider, 0.3);
        }

        public string BuildCityAlertPayload(List<CityAlert> alerts, string model)
        {
            return BuildCityAlertPayload(alerts, model, AIProvider.DeepSeek);
        }

        public string BuildCityAlertPayload(List<CityAlert> alerts, string model, AIProvider provider)
        {
            if (alerts == null)
            {
                alerts = new List<CityAlert>();
            }

            string langInstruction = LocalizationManager.GetAiLanguageInstruction();
            string systemPrompt = string.Format(
                "Você é um urbanista e engenheiro municipal sênior especialista no jogo Cities: Skylines 1 (Sunset Harbor). Analise os alertas urbanos recebidos (edifícios com problemas críticos de cadáver, compradores, trabalhadores, água, luz, esgoto ou lixo). {0} Para cada edifício (identificado por BuildingId), forneça: CAUSA (uma frase explicando a raiz no zoneamento/serviço), SOLUCAO (orientação prática para o jogador) e ACAO_AUTO (uma das opções: DISPATCH_DEATH_CARE, DISPATCH_GARBAGE, RESTORE_WATER, RESTORE_ELECTRICITY, CLEAR_SEWAGE, STIMULATE_CUSTOMERS, REBALANCE_WORKERS ou NONE).",
                langInstruction
            );

            StringBuilder userContent = new StringBuilder();
            userContent.Append("Aqui estão os alertas urbanos mais críticos da cidade:\n");
            for (int i = 0; i < alerts.Count; i++)
            {
                var a = alerts[i];
                userContent.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "Alerta #{0}: [BuildingId: {1}, Edificio: {2}, Problema: {3}, Gravidade: {4:0.0}, Timer: {5}]\n",
                    i + 1,
                    a.BuildingId,
                    a.BuildingName,
                    a.ProblemDescription,
                    a.Severity,
                    a.ProblemTimer
                );
            }
            userContent.Append("\nFormate a resposta estritamente assim para cada alerta:\nBUILDING: <BuildingId>\nCAUSA: <texto>\nSOLUCAO: <texto>\nACAO_AUTO: <DISPATCH_DEATH_CARE|DISPATCH_GARBAGE|RESTORE_WATER|RESTORE_ELECTRICITY|CLEAR_SEWAGE|STIMULATE_CUSTOMERS|REBALANCE_WORKERS|NONE>");

            return BuildEnvelope(systemPrompt, userContent.ToString(), model, provider, 0.3);
        }

        public string BuildAlternativeSolutionPayload(CorrectionLogEntry entry, string model)
        {
            return BuildAlternativeSolutionPayload(entry, model, AIProvider.DeepSeek);
        }

        public string BuildAlternativeSolutionPayload(CorrectionLogEntry entry, string model, AIProvider provider)
        {
            if (entry == null)
            {
                entry = new CorrectionLogEntry();
            }

            string langInstruction = LocalizationManager.GetAiLanguageInstruction();
            string systemPrompt = string.Format(
                "Você é um especialista em planejamento urbano e engenharia sênior no Cities: Skylines 1. Uma intervenção padrão falhou e o problema persiste por mais de 10 minutos. {0} Forneça uma SOLUÇÃO ALTERNATIVA AVANÇADA e inovadora com: CAUSA_FALHA (por que a primeira intervenção não funcionou), SOLUCAO_ALTERNATIVA (estratégia macro/estrutural detalhada) e ACAO_ALTERNATIVA (intervenção direta alternativa).",
                langInstruction
            );

            StringBuilder userContent = new StringBuilder();
            userContent.AppendFormat(
                CultureInfo.InvariantCulture,
                "A intervenção padrão '{0}' foi aplicada há mais de 10 minutos ({1}) no alvo [ID: {2}, Nome: {3}, Problema: {4}], mas o problema AINDA PERSISTE!\n" +
                "Analise e forneça uma alternativa avançada estrutural.\n\n" +
                "Formate estritamente assim:\n" +
                "CAUSA_FALHA: <motivo da persistência>\n" +
                "SOLUCAO_ALTERNATIVA: <estratégia alternativa avançada>\n" +
                "ACAO_ALTERNATIVA: <ação alternativa direta>",
                entry.AppliedAction,
                entry.GetFormattedElapsedTime(),
                entry.TargetId,
                entry.TargetName,
                entry.ProblemDescription
            );

            return BuildEnvelope(systemPrompt, userContent.ToString(), model, provider, 0.4);
        }

        private string BuildEnvelope(string systemPrompt, string userPrompt, string model, AIProvider provider, double temperature)
        {
            var desc = AIProviderRegistry.GetDescriptor(provider);
            string safeModel = EscapeJson(model ?? desc.DefaultModel);

            switch (desc.EnvelopeType)
            {
                case PayloadEnvelopeType.AnthropicMessages:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{{\n  \"model\": \"{0}\",\n  \"system\": \"{1}\",\n  \"messages\": [\n    {{\n      \"role\": \"user\",\n      \"content\": \"{2}\"\n    }}\n  ],\n  \"max_tokens\": 1024,\n  \"temperature\": {3:0.0}\n}}",
                        safeModel,
                        EscapeJson(systemPrompt),
                        EscapeJson(userPrompt),
                        temperature
                    );

                case PayloadEnvelopeType.GoogleGemini:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{{\n  \"systemInstruction\": {{\n    \"parts\": [\n      {{\n        \"text\": \"{0}\"\n      }}\n    ]\n  }},\n  \"contents\": [\n    {{\n      \"role\": \"user\",\n      \"parts\": [\n        {{\n          \"text\": \"{1}\"\n        }}\n      ]\n    }}\n  ],\n  \"generationConfig\": {{\n    \"temperature\": {2:0.0}\n  }}\n}}",
                        EscapeJson(systemPrompt),
                        EscapeJson(userPrompt),
                        temperature
                    );

                case PayloadEnvelopeType.OpenAiCompatible:
                default:
                    bool isReasoningModel = safeModel.StartsWith("o1") || safeModel.StartsWith("o3-mini");
                    if (isReasoningModel)
                    {
                        return string.Format(
                            "{{\n  \"model\": \"{0}\",\n  \"messages\": [\n    {{\n      \"role\": \"system\",\n      \"content\": \"{1}\"\n    }},\n    {{\n      \"role\": \"user\",\n      \"content\": \"{2}\"\n    }}\n  ]\n}}",
                            safeModel,
                            EscapeJson(systemPrompt),
                            EscapeJson(userPrompt)
                        );
                    }
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{{\n  \"model\": \"{0}\",\n  \"messages\": [\n    {{\n      \"role\": \"system\",\n      \"content\": \"{1}\"\n    }},\n    {{\n      \"role\": \"user\",\n      \"content\": \"{2}\"\n    }}\n  ],\n  \"temperature\": {3:0.0}\n}}",
                        safeModel,
                        EscapeJson(systemPrompt),
                        EscapeJson(userPrompt),
                        temperature
                    );
            }
        }

        public string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", "\\n");
        }
    }
}
