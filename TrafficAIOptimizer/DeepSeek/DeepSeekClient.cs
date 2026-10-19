using System;
using System.Collections.Generic;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Contracts;
using TrafficAIOptimizer.Core.Providers;
using TrafficAIOptimizer.Core.Services;

namespace TrafficAIOptimizer.DeepSeek
{
    /// <summary>
    /// Facade de compatibilidade para DeepSeek, delegando chamadas para o motor unificado AIClient.
    /// </summary>
    public static class DeepSeekClient
    {
        public static string CurrentModel
        {
            get => AIClient.GetModelForProvider(AIProvider.DeepSeek);
            set => AIClient.SetModelForProvider(AIProvider.DeepSeek, value);
        }

        public static IPayloadBuilder PayloadBuilder
        {
            get => AIClient.PayloadBuilder;
            set => AIClient.PayloadBuilder = value;
        }

        public static IResponseParser ResponseParser
        {
            get => AIClient.ResponseParser;
            set => AIClient.ResponseParser = value;
        }

        public static ISecureKeyStorage KeyStorage
        {
            get => AIClient.KeyStorage;
            set => AIClient.KeyStorage = value;
        }

        public static void AnalyzeBottlenecksAsync(
            List<TrafficBottleneck> bottlenecks,
            Action<List<TrafficBottleneck>> onSuccess,
            Action<string> onError)
        {
            AIProvider prev = AIClient.CurrentProvider;
            AIClient.CurrentProvider = AIProvider.DeepSeek;
            AIClient.AnalyzeBottlenecksAsync(
                bottlenecks,
                (res) =>
                {
                    AIClient.CurrentProvider = prev;
                    onSuccess?.Invoke(res);
                },
                (err) =>
                {
                    AIClient.CurrentProvider = prev;
                    onError?.Invoke(err);
                }
            );
        }

        public static void AnalyzeCityAlertsAsync(
            List<CityAlert> alerts,
            Action<List<CityAlert>> onSuccess,
            Action<string> onError)
        {
            AIProvider prev = AIClient.CurrentProvider;
            AIClient.CurrentProvider = AIProvider.DeepSeek;
            AIClient.AnalyzeCityAlertsAsync(
                alerts,
                (res) =>
                {
                    AIClient.CurrentProvider = prev;
                    onSuccess?.Invoke(res);
                },
                (err) =>
                {
                    AIClient.CurrentProvider = prev;
                    onError?.Invoke(err);
                }
            );
        }

        public static void RequestAlternativeSolutionAsync(
            CorrectionLogEntry entry,
            Action<CorrectionLogEntry> onSuccess,
            Action<string> onError)
        {
            AIProvider prev = AIClient.CurrentProvider;
            AIClient.CurrentProvider = AIProvider.DeepSeek;
            AIClient.RequestAlternativeSolutionAsync(
                entry,
                (res) =>
                {
                    AIClient.CurrentProvider = prev;
                    onSuccess?.Invoke(res);
                },
                (err) =>
                {
                    AIClient.CurrentProvider = prev;
                    onError?.Invoke(err);
                }
            );
        }
    }
}
