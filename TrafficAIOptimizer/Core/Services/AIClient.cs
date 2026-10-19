using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Contracts;
using TrafficAIOptimizer.Core.Localization;
using TrafficAIOptimizer.Core.Providers;
using TrafficAIOptimizer.DeepSeek;
using TrafficAIOptimizer.Security;
using UnityEngine.Networking;

namespace TrafficAIOptimizer.Core.Services
{
    /// <summary>
    /// Cliente HTTP assíncrono unificado para comunicação com múltiplos provedores de IA
    /// (DeepSeek, OpenAI, Anthropic, Gemini, Grok, Meta).
    /// </summary>
    public static class AIClient
    {
        private static AIProvider _currentProvider = AIProvider.DeepSeek;
        private static readonly Dictionary<AIProvider, string> _selectedModels = new Dictionary<AIProvider, string>();

        public static IPayloadBuilder PayloadBuilder { get; set; } = new DeepSeekPayloadBuilder();
        public static IResponseParser ResponseParser { get; set; } = new DeepSeekResponseParser();
        public static ISecureKeyStorage KeyStorage { get; set; } = SecureKeyStorage.Instance;

        public static AIProvider CurrentProvider
        {
            get => _currentProvider;
            set
            {
                _currentProvider = value;
                SecureKeyStorage.ActiveProvider = value;
            }
        }

        public static string CurrentModel
        {
            get
            {
                if (_selectedModels.TryGetValue(_currentProvider, out string model) && !string.IsNullOrEmpty(model))
                {
                    return model;
                }
                return AIProviderRegistry.GetDescriptor(_currentProvider).DefaultModel;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _selectedModels[_currentProvider] = value;
                }
            }
        }

        public static string GetModelForProvider(AIProvider provider)
        {
            if (_selectedModels.TryGetValue(provider, out string model) && !string.IsNullOrEmpty(model))
            {
                return model;
            }
            return AIProviderRegistry.GetDescriptor(provider).DefaultModel;
        }

        public static void SetModelForProvider(AIProvider provider, string model)
        {
            if (!string.IsNullOrEmpty(model))
            {
                _selectedModels[provider] = model;
            }
        }

        public static void CycleNextProvider()
        {
            CurrentProvider = AIProviderRegistry.GetNextProvider(CurrentProvider);
        }

        public static void CycleNextModel()
        {
            string next = AIProviderRegistry.GetNextModel(CurrentProvider, CurrentModel);
            CurrentModel = next;
        }

        /// <summary>
        /// Envia os gargalos identificados para análise da IA de forma assíncrona.
        /// </summary>
        public static void AnalyzeBottlenecksAsync(
            List<TrafficBottleneck> bottlenecks,
            Action<List<TrafficBottleneck>> onSuccess,
            Action<string> onError)
        {
            AIProvider provider = CurrentProvider;
            string model = CurrentModel;
            var desc = AIProviderRegistry.GetDescriptor(provider);

            if (!KeyStorage.HasKeyFor(provider))
            {
                string errMsg = string.Format(
                    "Erro: Nenhuma chave da API ({0}) informada. Registre sua chave antes de analisar.",
                    desc.DisplayName
                );
                onError?.Invoke(errMsg);
                return;
            }

            if (bottlenecks == null || bottlenecks.Count == 0)
            {
                onError?.Invoke("Nenhum gargalo significativo detectado na malha viária no momento.");
                return;
            }

            string apiKey = KeyStorage.GetKeyUnsafe(provider);
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Erro: API Key não encontrada na memória RAM.");
                return;
            }

            string payload = PayloadBuilder.BuildChatPayload(bottlenecks, model, provider);
            UnityWebRequest request = CreateRequest(payload, apiKey, provider, model);

            TrafficCoroutineDispatcher.RunCoroutine(SendRequestRoutine(
                request,
                desc.DisplayName,
                (responseBody) =>
                {
                    ResponseParser.ParseAndApply(responseBody, bottlenecks);
                    onSuccess?.Invoke(bottlenecks);
                },
                onError
            ));
        }

        /// <summary>
        /// Envia os alertas urbanos identificados para análise da IA de forma assíncrona.
        /// </summary>
        public static void AnalyzeCityAlertsAsync(
            List<CityAlert> alerts,
            Action<List<CityAlert>> onSuccess,
            Action<string> onError)
        {
            AIProvider provider = CurrentProvider;
            string model = CurrentModel;
            var desc = AIProviderRegistry.GetDescriptor(provider);

            if (!KeyStorage.HasKeyFor(provider))
            {
                string errMsg = string.Format(
                    "Erro: Nenhuma chave da API ({0}) informada. Registre sua chave antes de analisar.",
                    desc.DisplayName
                );
                onError?.Invoke(errMsg);
                return;
            }

            if (alerts == null || alerts.Count == 0)
            {
                onError?.Invoke("Nenhum alerta urbano crítico detectado na cidade no momento.");
                return;
            }

            string apiKey = KeyStorage.GetKeyUnsafe(provider);
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Erro: API Key não encontrada na memória RAM.");
                return;
            }

            string payload = PayloadBuilder.BuildCityAlertPayload(alerts, model, provider);
            UnityWebRequest request = CreateRequest(payload, apiKey, provider, model);

            TrafficCoroutineDispatcher.RunCoroutine(SendRequestRoutine(
                request,
                desc.DisplayName,
                (responseBody) =>
                {
                    ResponseParser.ParseAndApplyCityAlerts(responseBody, alerts);
                    onSuccess?.Invoke(alerts);
                },
                onError
            ));
        }

        /// <summary>
        /// Envia solicitação de solução alternativa quando um problema persiste após mais de 10 minutos.
        /// </summary>
        public static void RequestAlternativeSolutionAsync(
            CorrectionLogEntry entry,
            Action<CorrectionLogEntry> onSuccess,
            Action<string> onError)
        {
            AIProvider provider = CurrentProvider;
            string model = CurrentModel;
            var desc = AIProviderRegistry.GetDescriptor(provider);

            if (!KeyStorage.HasKeyFor(provider))
            {
                string errMsg = string.Format(
                    "Erro: Nenhuma chave da API ({0}) informada.",
                    desc.DisplayName
                );
                onError?.Invoke(errMsg);
                return;
            }

            if (entry == null)
            {
                onError?.Invoke("Erro: Registro de correção nulo.");
                return;
            }

            string apiKey = KeyStorage.GetKeyUnsafe(provider);
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Erro: API Key não encontrada na memória RAM.");
                return;
            }

            string payload = PayloadBuilder.BuildAlternativeSolutionPayload(entry, model, provider);
            UnityWebRequest request = CreateRequest(payload, apiKey, provider, model);

            TrafficCoroutineDispatcher.RunCoroutine(SendRequestRoutine(
                request,
                desc.DisplayName,
                (responseBody) =>
                {
                    ResponseParser.ParseAndApplyAlternativeSolution(responseBody, entry);
                    onSuccess?.Invoke(entry);
                },
                onError
            ));
        }

        /// <summary>
        /// Consulta a API do provedor em tempo real para obter a lista atualizada de modelos disponíveis.
        /// </summary>
        public static void FetchAvailableModelsAsync(
            AIProvider provider,
            Action<List<string>> onSuccess,
            Action<string> onError)
        {
            var desc = AIProviderRegistry.GetDescriptor(provider);
            if (string.IsNullOrEmpty(desc.ModelsEndpointUrl))
            {
                onError?.Invoke(string.Format("Provedor {0} não possui endpoint de listagem de modelos configurado.", desc.DisplayName));
                return;
            }

            if (!KeyStorage.HasKeyFor(provider))
            {
                onError?.Invoke(string.Format("Erro: Nenhuma chave da API ({0}) informada para consultar modelos.", desc.DisplayName));
                return;
            }

            string apiKey = KeyStorage.GetKeyUnsafe(provider);
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Erro: API Key não encontrada na memória RAM.");
                return;
            }

            UnityWebRequest request = new UnityWebRequest(desc.ModelsEndpointUrl, "GET");
            request.downloadHandler = new DownloadHandlerBuffer();
            var headers = desc.GetHeaders(apiKey);
            foreach (var kvp in headers)
            {
                if (kvp.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                request.SetRequestHeader(kvp.Key, kvp.Value);
            }
            request.timeout = 25;

            TrafficCoroutineDispatcher.RunCoroutine(FetchModelsRoutine(
                request,
                desc,
                (models) =>
                {
                    AIProviderRegistry.UpdateModelsFromApi(provider, models);
                    onSuccess?.Invoke(models);
                },
                onError
            ));
        }

        private static IEnumerator FetchModelsRoutine(
            UnityWebRequest request,
            AIProviderDescriptor desc,
            Action<List<string>> onSuccess,
            Action<string> onError)
        {
            using (request)
            {
                yield return request.Send();

                if (request.isError)
                {
                    onError?.Invoke(string.Format("Falha ao consultar modelos de {0}: {1}", desc.DisplayName, request.error));
                }
                else
                {
                    try
                    {
                        string json = request.downloadHandler != null ? request.downloadHandler.text : null;
                        var models = desc.ParseModelsFromApi(json);
                        if (models == null || models.Count == 0)
                        {
                            onError?.Invoke(string.Format("Nenhum modelo compatível retornado pela API de {0}.", desc.DisplayName));
                        }
                        else
                        {
                            onSuccess?.Invoke(models);
                        }
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke("Erro ao processar lista de modelos: " + ex.Message);
                    }
                }
            }
        }

        private static UnityWebRequest CreateRequest(string jsonPayload, string apiKey, AIProvider provider, string model)
        {
            var desc = AIProviderRegistry.GetDescriptor(provider);
            string endpoint = desc.GetEndpointUrl(model);

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            var headers = desc.GetHeaders(apiKey);
            foreach (var kvp in headers)
            {
                request.SetRequestHeader(kvp.Key, kvp.Value);
            }

            request.timeout = 40;
            return request;
        }

        private static IEnumerator SendRequestRoutine(
            UnityWebRequest request,
            string providerName,
            Action<string> onSuccess,
            Action<string> onError)
        {
            using (request)
            {
                yield return request.Send();

                if (request.isError)
                {
                    string errorMsg;
                    long responseCode = request.responseCode;

                    if (responseCode == 401)
                    {
                        errorMsg = string.Format("API Key inválida ou não autorizada no provedor {0} (401).", providerName);
                    }
                    else if (responseCode == 403)
                    {
                        errorMsg = string.Format("Acesso negado no provedor {0} (403). Verifique permissões do projeto/chave.", providerName);
                    }
                    else if (responseCode == 429)
                    {
                        errorMsg = string.Format("Limite de requisições excedido no provedor {0} (429). Aguarde alguns segundos.", providerName);
                    }
                    else if (responseCode > 0)
                    {
                        errorMsg = string.Format("API {0} retornou código {1}: {2}", providerName, responseCode, request.error);
                    }
                    else
                    {
                        errorMsg = string.Format("Falha de conexão com {0}: {1}", providerName, request.error);
                    }

                    onError?.Invoke(errorMsg);
                }
                else
                {
                    try
                    {
                        string responseBody = request.downloadHandler != null ? request.downloadHandler.text : null;
                        if (string.IsNullOrEmpty(responseBody))
                        {
                            onError?.Invoke(string.Format("Aviso: Resposta vazia recebida do provedor {0}.", providerName));
                            yield break;
                        }

                        onSuccess?.Invoke(responseBody);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke("Falha ao processar resposta da IA: " + ex.Message);
                    }
                }
            }
        }
    }
}
