namespace TrafficAIOptimizer.Core.Providers
{
    /// <summary>
    /// Provedores de Inteligência Artificial suportados pelo mod.
    /// </summary>
    public enum AIProvider
    {
        DeepSeek,
        OpenAI,
        Anthropic,
        Gemini,
        Grok,
        Meta
    }

    /// <summary>
    /// Formato estrutural de envelopamento da requisição HTTP para a IA.
    /// </summary>
    public enum PayloadEnvelopeType
    {
        OpenAiCompatible,
        AnthropicMessages,
        GoogleGemini
    }
}
