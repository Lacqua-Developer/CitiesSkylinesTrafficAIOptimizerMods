using System.Collections.Generic;
using NUnit.Framework;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Providers;
using TrafficAIOptimizer.Core.Services;
using TrafficAIOptimizer.Security;

namespace TrafficAIOptimizer.Tests.Services
{
    [TestFixture]
    public class MultiProviderTests
    {
        [SetUp]
        public void Setup()
        {
            SecureKeyStorage.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            SecureKeyStorage.ClearAll();
        }

        [Test]
        public void AIProviderRegistry_ShouldContainAllRequestedProviders()
        {
            var providers = AIProviderRegistry.GetAllProviders();
            Assert.AreEqual(6, providers.Length);
            Assert.Contains(AIProvider.DeepSeek, providers);
            Assert.Contains(AIProvider.OpenAI, providers);
            Assert.Contains(AIProvider.Anthropic, providers);
            Assert.Contains(AIProvider.Gemini, providers);
            Assert.Contains(AIProvider.Grok, providers);
            Assert.Contains(AIProvider.Meta, providers);
        }

        [Test]
        public void AIProviderRegistry_ShouldResolveCorrectEndpointsAndHeaders()
        {
            // OpenAI
            var openAiDesc = AIProviderRegistry.GetDescriptor(AIProvider.OpenAI);
            Assert.AreEqual("https://api.openai.com/v1/chat/completions", openAiDesc.GetEndpointUrl("gpt-4o"));
            var openAiHeaders = openAiDesc.GetHeaders("test-openai-key");
            Assert.AreEqual("Bearer test-openai-key", openAiHeaders["Authorization"]);

            // Anthropic
            var anthropicDesc = AIProviderRegistry.GetDescriptor(AIProvider.Anthropic);
            Assert.AreEqual("https://api.anthropic.com/v1/messages", anthropicDesc.GetEndpointUrl("claude-3-7-sonnet-20250219"));
            var anthropicHeaders = anthropicDesc.GetHeaders("test-anthropic-key");
            Assert.AreEqual("test-anthropic-key", anthropicHeaders["x-api-key"]);
            Assert.AreEqual("2023-06-01", anthropicHeaders["anthropic-version"]);

            // Gemini
            var geminiDesc = AIProviderRegistry.GetDescriptor(AIProvider.Gemini);
            Assert.AreEqual("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent", geminiDesc.GetEndpointUrl("gemini-2.0-flash"));
            var geminiHeaders = geminiDesc.GetHeaders("test-gemini-key");
            Assert.AreEqual("test-gemini-key", geminiHeaders["x-goog-api-key"]);

            // Grok
            var grokDesc = AIProviderRegistry.GetDescriptor(AIProvider.Grok);
            Assert.AreEqual("https://api.x.ai/v1/chat/completions", grokDesc.GetEndpointUrl("grok-2-latest"));

            // Meta
            var metaDesc = AIProviderRegistry.GetDescriptor(AIProvider.Meta);
            Assert.AreEqual("https://api.groq.com/openai/v1/chat/completions", metaDesc.GetEndpointUrl("llama-3.3-70b-versatile"));
            Assert.AreEqual("https://openrouter.ai/api/v1/chat/completions", metaDesc.GetEndpointUrl("meta-llama/llama-3.3-70b-instruct"));
        }

        [Test]
        public void AIProviderRegistry_ShouldSupportCustomAndDynamicModels()
        {
            var desc = AIProviderRegistry.GetDescriptor(AIProvider.OpenAI);
            AIProviderRegistry.RegisterCustomModel(AIProvider.OpenAI, "gpt-5-future-test");

            Assert.IsTrue(desc.IsValidModel("gpt-5-future-test"));

            // Test parsing standard OpenAI models JSON
            string fakeModelsJson = "{\"data\": [{\"id\": \"gpt-4.5-custom\"}, {\"id\": \"text-embedding-3-small\"}, {\"id\": \"o4-mini\"}]}";
            var parsed = AIProviderDescriptor.ParseStandardOpenAiModels(fakeModelsJson);
            Assert.Contains("gpt-4.5-custom", parsed);
            Assert.Contains("o4-mini", parsed);
            Assert.IsFalse(parsed.Contains("text-embedding-3-small")); // Filtered out
        }

        [Test]
        public void SecureKeyStorage_ShouldStoreKeysPerProviderSeparately()
        {
            SecureKeyStorage.SetKey("sk-proj-openai-12345678", AIProvider.OpenAI);
            SecureKeyStorage.SetKey("ant-api-claude-87654321", AIProvider.Anthropic);

            Assert.IsTrue(SecureKeyStorage.HasKeyFor(AIProvider.OpenAI));
            Assert.IsTrue(SecureKeyStorage.HasKeyFor(AIProvider.Anthropic));
            Assert.IsFalse(SecureKeyStorage.HasKeyFor(AIProvider.Gemini));

            Assert.AreEqual("sk-proj-openai-12345678", SecureKeyStorage.GetKeyUnsafe(AIProvider.OpenAI));
            Assert.AreEqual("ant-api-claude-87654321", SecureKeyStorage.GetKeyUnsafe(AIProvider.Anthropic));

            // Clear one provider
            SecureKeyStorage.Clear(AIProvider.OpenAI);
            Assert.IsFalse(SecureKeyStorage.HasKeyFor(AIProvider.OpenAI));
            Assert.IsTrue(SecureKeyStorage.HasKeyFor(AIProvider.Anthropic));

            // Clear all
            SecureKeyStorage.ClearAll();
            Assert.IsFalse(SecureKeyStorage.HasKeyFor(AIProvider.Anthropic));
        }

        [Test]
        public void PayloadBuilder_ShouldGenerateAppropriatePayloadPerProviderEnvelope()
        {
            var builder = new DeepSeekPayloadBuilder();
            var bottlenecks = new List<TrafficBottleneck>
            {
                new TrafficBottleneck { NodeId = 42, RoadName = "Main Ave", CongestionScore = 85f, AverageSpeedPercent = 15f }
            };

            // OpenAI / Reasoning
            string openAiPayload = builder.BuildChatPayload(bottlenecks, "o3-mini", AIProvider.OpenAI);
            Assert.IsTrue(openAiPayload.Contains("\"model\": \"o3-mini\""));
            Assert.IsTrue(openAiPayload.Contains("\"messages\":"));
            Assert.IsFalse(openAiPayload.Contains("\"temperature\":")); // o3-mini omits temperature

            // Anthropic
            string anthropicPayload = builder.BuildChatPayload(bottlenecks, "claude-3-7-sonnet-20250219", AIProvider.Anthropic);
            Assert.IsTrue(anthropicPayload.Contains("\"model\": \"claude-3-7-sonnet-20250219\""));
            Assert.IsTrue(anthropicPayload.Contains("\"system\":"));
            Assert.IsTrue(anthropicPayload.Contains("\"max_tokens\": 1024"));

            // Gemini
            string geminiPayload = builder.BuildChatPayload(bottlenecks, "gemini-2.0-flash", AIProvider.Gemini);
            Assert.IsTrue(geminiPayload.Contains("\"systemInstruction\":"));
            Assert.IsTrue(geminiPayload.Contains("\"contents\":"));
        }

        [Test]
        public void ResponseParser_ShouldExtractContentFromOpenAiAnthropicAndGemini()
        {
            // OpenAI format
            string openAiJson = "{\"choices\": [{\"message\": {\"role\": \"assistant\", \"content\": \"NODE: 42\\nCAUSA: Excesso de veículos\\nSOLUCAO: Rotatória\\nACAO_AUTO: REMOVE_TRAFFIC_LIGHT\"}}]}";
            string openAiText = DeepSeekResponseParser.ExtractContentFromJson(openAiJson);
            Assert.IsNotNull(openAiText);
            Assert.IsTrue(openAiText.Contains("NODE: 42"));

            // Anthropic format
            string anthropicJson = "{\"id\": \"msg_1\", \"type\": \"message\", \"content\": [{\"type\": \"text\", \"text\": \"NODE: 42\\nCAUSA: Gargalo em bifurcação\\nSOLUCAO: Adicionar faixa adicional\\nACAO_AUTO: NONE\"}]}";
            string anthropicText = DeepSeekResponseParser.ExtractContentFromJson(anthropicJson);
            Assert.IsNotNull(anthropicText);
            Assert.IsTrue(anthropicText.Contains("Gargalo em bifurcação"));

            // Gemini format
            string geminiJson = "{\"candidates\": [{\"content\": {\"parts\": [{\"text\": \"NODE: 42\\nCAUSA: Sinal mal sincronizado\\nSOLUCAO: Sincronia semafórica\\nACAO_AUTO: CLEAR_STUCK_VEHICLES\"}], \"role\": \"model\"}}]}";
            string geminiText = DeepSeekResponseParser.ExtractContentFromJson(geminiJson);
            Assert.IsNotNull(geminiText);
            Assert.IsTrue(geminiText.Contains("Sinal mal sincronizado"));
        }
    }
}
