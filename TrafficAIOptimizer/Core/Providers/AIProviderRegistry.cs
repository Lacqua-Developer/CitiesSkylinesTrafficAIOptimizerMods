using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TrafficAIOptimizer.Core.Providers
{
    /// <summary>
    /// Metadados, regras de comunicação e catálogo dinâmico de modelos para um provedor específico de IA.
    /// Suporta adição de modelos customizados e descoberta dinâmica via API em tempo de execução.
    /// </summary>
    public class AIProviderDescriptor
    {
        public AIProvider Provider { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<string> DynamicModels { get; } = new List<string>();
        public string DefaultModel { get; set; }
        public PayloadEnvelopeType EnvelopeType { get; set; }

        public string ModelsEndpointUrl { get; set; }
        public Func<string, string> EndpointResolver { get; set; }
        public Func<string, Dictionary<string, string>> HeaderResolver { get; set; }
        public Func<string, List<string>> ModelsParser { get; set; }

        public string[] AvailableModels
        {
            get => DynamicModels.ToArray();
            set
            {
                DynamicModels.Clear();
                if (value != null)
                {
                    DynamicModels.AddRange(value);
                }
            }
        }

        public void AddModel(string model)
        {
            if (string.IsNullOrEmpty(model)) return;
            string trimmed = model.Trim();
            if (!DynamicModels.Exists(m => string.Equals(m, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                DynamicModels.Add(trimmed);
            }
        }

        public string GetEndpointUrl(string model)
        {
            if (EndpointResolver != null)
            {
                return EndpointResolver(model);
            }
            return string.Empty;
        }

        public Dictionary<string, string> GetHeaders(string apiKey)
        {
            if (HeaderResolver != null)
            {
                return HeaderResolver(apiKey);
            }
            return new Dictionary<string, string>();
        }

        public bool IsValidModel(string model)
        {
            if (string.IsNullOrEmpty(model)) return false;
            return DynamicModels.Exists(m => string.Equals(m, model, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> ParseModelsFromApi(string jsonResponse)
        {
            if (ModelsParser != null)
            {
                return ModelsParser(jsonResponse);
            }
            return ParseStandardOpenAiModels(jsonResponse);
        }

        public static List<string> ParseStandardOpenAiModels(string json)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(json)) return list;

            try
            {
                // Regex para capturar campos "id": "nome-do-modelo"
                var matches = Regex.Matches(json, "\"id\"\\s*:\\s*\"([^\"]+)\"");
                foreach (Match m in matches)
                {
                    if (m.Groups.Count > 1)
                    {
                        string id = m.Groups[1].Value.Trim();
                        // Filtra modelos não conversacionais óbvios (áudio, embeddings, moderação, etc.)
                        string lower = id.ToLowerInvariant();
                        if (lower.Contains("embed") || lower.Contains("tts") || lower.Contains("whisper") ||
                            lower.Contains("moderation") || lower.Contains("dall-e") || lower.Contains("davinci") ||
                            lower.Contains("babbage"))
                        {
                            continue;
                        }

                        if (!list.Contains(id))
                        {
                            list.Add(id);
                        }
                    }
                }
            }
            catch
            {
                // Tratamento defensivo
            }

            return list;
        }

        public static List<string> ParseGeminiModels(string json)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(json)) return list;

            try
            {
                var matches = Regex.Matches(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                foreach (Match m in matches)
                {
                    if (m.Groups.Count > 1)
                    {
                        string name = m.Groups[1].Value.Trim();
                        // Remove prefixo "models/" se presente
                        if (name.StartsWith("models/"))
                        {
                            name = name.Substring(7);
                        }

                        string lower = name.ToLowerInvariant();
                        if (lower.Contains("embedding") || lower.Contains("aqa"))
                        {
                            continue;
                        }

                        if (!list.Contains(name))
                        {
                            list.Add(name);
                        }
                    }
                }
            }
            catch
            {
                // Tratamento defensivo
            }

            return list;
        }
    }

    /// <summary>
    /// Registro central de provedores de IA e modelos modernos disponíveis (DeepSeek, OpenAI, Anthropic, Gemini, Grok, Meta).
    /// Altamente preparado para evolução contínua com atualização dinâmica via API e suporte a modelos customizados.
    /// </summary>
    public static class AIProviderRegistry
    {
        private static readonly Dictionary<AIProvider, AIProviderDescriptor> _descriptors;

        static AIProviderRegistry()
        {
            _descriptors = new Dictionary<AIProvider, AIProviderDescriptor>
            {
                {
                    AIProvider.DeepSeek,
                    new AIProviderDescriptor
                    {
                        Provider = AIProvider.DeepSeek,
                        DisplayName = "DeepSeek",
                        Description = "DeepSeek AI (V3 e R1)",
                        AvailableModels = new[] { "deepseek-chat", "deepseek-reasoner" },
                        DefaultModel = "deepseek-chat",
                        EnvelopeType = PayloadEnvelopeType.OpenAiCompatible,
                        ModelsEndpointUrl = "https://api.deepseek.com/models",
                        EndpointResolver = (m) => "https://api.deepseek.com/chat/completions",
                        HeaderResolver = (key) => new Dictionary<string, string>
                        {
                            { "Authorization", "Bearer " + key },
                            { "Content-Type", "application/json" }
                        },
                        ModelsParser = AIProviderDescriptor.ParseStandardOpenAiModels
                    }
                },
                {
                    AIProvider.OpenAI,
                    new AIProviderDescriptor
                    {
                        Provider = AIProvider.OpenAI,
                        DisplayName = "OpenAI",
                        Description = "OpenAI GPT-4o, o3-mini, o1, GPT-4.5",
                        AvailableModels = new[] { "gpt-4o", "gpt-4o-mini", "o3-mini", "o1", "gpt-4.5-preview" },
                        DefaultModel = "gpt-4o-mini",
                        EnvelopeType = PayloadEnvelopeType.OpenAiCompatible,
                        ModelsEndpointUrl = "https://api.openai.com/v1/models",
                        EndpointResolver = (m) => "https://api.openai.com/v1/chat/completions",
                        HeaderResolver = (key) => new Dictionary<string, string>
                        {
                            { "Authorization", "Bearer " + key },
                            { "Content-Type", "application/json" }
                        },
                        ModelsParser = AIProviderDescriptor.ParseStandardOpenAiModels
                    }
                },
                {
                    AIProvider.Anthropic,
                    new AIProviderDescriptor
                    {
                        Provider = AIProvider.Anthropic,
                        DisplayName = "Anthropic",
                        Description = "Claude 3.7 Sonnet, Claude 3.5 Sonnet / Haiku",
                        AvailableModels = new[] { "claude-3-7-sonnet-20250219", "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022" },
                        DefaultModel = "claude-3-5-haiku-20241022",
                        EnvelopeType = PayloadEnvelopeType.AnthropicMessages,
                        ModelsEndpointUrl = "https://api.anthropic.com/v1/models",
                        EndpointResolver = (m) => "https://api.anthropic.com/v1/messages",
                        HeaderResolver = (key) => new Dictionary<string, string>
                        {
                            { "x-api-key", key },
                            { "anthropic-version", "2023-06-01" },
                            { "Content-Type", "application/json" }
                        },
                        ModelsParser = AIProviderDescriptor.ParseStandardOpenAiModels
                    }
                },
                {
                    AIProvider.Gemini,
                    new AIProviderDescriptor
                    {
                        Provider = AIProvider.Gemini,
                        DisplayName = "Google Gemini",
                        Description = "Gemini 2.0 Flash, 2.5 Pro / Flash, 1.5 Pro",
                        AvailableModels = new[] { "gemini-2.0-flash", "gemini-2.5-pro", "gemini-2.5-flash", "gemini-1.5-pro", "gemini-1.5-flash" },
                        DefaultModel = "gemini-2.0-flash",
                        EnvelopeType = PayloadEnvelopeType.GoogleGemini,
                        ModelsEndpointUrl = "https://generativelanguage.googleapis.com/v1beta/models",
                        EndpointResolver = (m) => string.Format("https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent", m ?? "gemini-2.0-flash"),
                        HeaderResolver = (key) => new Dictionary<string, string>
                        {
                            { "x-goog-api-key", key },
                            { "Content-Type", "application/json" }
                        },
                        ModelsParser = AIProviderDescriptor.ParseGeminiModels
                    }
                },
                {
                    AIProvider.Grok,
                    new AIProviderDescriptor
                    {
                        Provider = AIProvider.Grok,
                        DisplayName = "Grok (xAI)",
                        Description = "xAI Grok-2, Grok-Beta",
                        AvailableModels = new[] { "grok-2-latest", "grok-2", "grok-beta" },
                        DefaultModel = "grok-2-latest",
                        EnvelopeType = PayloadEnvelopeType.OpenAiCompatible,
                        ModelsEndpointUrl = "https://api.x.ai/v1/models",
                        EndpointResolver = (m) => "https://api.x.ai/v1/chat/completions",
                        HeaderResolver = (key) => new Dictionary<string, string>
                        {
                            { "Authorization", "Bearer " + key },
                            { "Content-Type", "application/json" }
                        },
                        ModelsParser = AIProviderDescriptor.ParseStandardOpenAiModels
                    }
                },
                {
                    AIProvider.Meta,
                    new AIProviderDescriptor
                    {
                        Provider = AIProvider.Meta,
                        DisplayName = "Meta (Llama)",
                        Description = "Meta Llama 3.3 70B, Llama 3.1 8B (via Groq / OpenRouter)",
                        AvailableModels = new[] { "llama-3.3-70b-versatile", "llama-3.1-8b-instant", "meta-llama/llama-3.3-70b-instruct" },
                        DefaultModel = "llama-3.3-70b-versatile",
                        EnvelopeType = PayloadEnvelopeType.OpenAiCompatible,
                        ModelsEndpointUrl = "https://api.groq.com/openai/v1/models",
                        EndpointResolver = (m) =>
                        {
                            if (!string.IsNullOrEmpty(m) && m.StartsWith("meta-llama/"))
                            {
                                return "https://openrouter.ai/api/v1/chat/completions";
                            }
                            return "https://api.groq.com/openai/v1/chat/completions";
                        },
                        HeaderResolver = (key) => new Dictionary<string, string>
                        {
                            { "Authorization", "Bearer " + key },
                            { "Content-Type", "application/json" }
                        },
                        ModelsParser = AIProviderDescriptor.ParseStandardOpenAiModels
                    }
                }
            };
        }

        public static AIProviderDescriptor GetDescriptor(AIProvider provider)
        {
            if (_descriptors.TryGetValue(provider, out var desc))
            {
                return desc;
            }
            return _descriptors[AIProvider.DeepSeek];
        }

        public static AIProvider[] GetAllProviders()
        {
            return new[]
            {
                AIProvider.DeepSeek,
                AIProvider.OpenAI,
                AIProvider.Anthropic,
                AIProvider.Gemini,
                AIProvider.Grok,
                AIProvider.Meta
            };
        }

        public static AIProvider GetNextProvider(AIProvider current)
        {
            var all = GetAllProviders();
            int idx = Array.IndexOf(all, current);
            if (idx < 0 || idx >= all.Length - 1)
            {
                return all[0];
            }
            return all[idx + 1];
        }

        public static string GetNextModel(AIProvider provider, string currentModel)
        {
            var desc = GetDescriptor(provider);
            var models = desc.AvailableModels;
            if (models == null || models.Length == 0) return desc.DefaultModel;

            int idx = Array.IndexOf(models, currentModel);
            if (idx < 0 || idx >= models.Length - 1)
            {
                return models[0];
            }
            return models[idx + 1];
        }

        public static void RegisterCustomModel(AIProvider provider, string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return;
            var desc = GetDescriptor(provider);
            desc.AddModel(modelName);
        }

        public static void UpdateModelsFromApi(AIProvider provider, List<string> newModels)
        {
            if (newModels == null || newModels.Count == 0) return;
            var desc = GetDescriptor(provider);
            foreach (var m in newModels)
            {
                desc.AddModel(m);
            }
        }
    }
}
