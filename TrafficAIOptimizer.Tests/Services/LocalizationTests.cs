using System.Collections.Generic;
using NUnit.Framework;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Localization;
using TrafficAIOptimizer.Core.Services;

namespace TrafficAIOptimizer.Tests.Services
{
    [TestFixture]
    public class LocalizationTests
    {
        [TearDown]
        public void TearDown()
        {
            LocalizationManager.ForceLanguage(null);
        }

        [Test]
        public void GivenLanguageCodes_WhenParsed_ThenShouldMapCorrectlyWithEnglishFallback()
        {
            // Português
            Assert.AreEqual(AppLanguage.Portuguese, LocalizationManager.ParseLanguageCode("pt"));
            Assert.AreEqual(AppLanguage.Portuguese, LocalizationManager.ParseLanguageCode("pt-BR"));
            Assert.AreEqual(AppLanguage.Portuguese, LocalizationManager.ParseLanguageCode("pt_br"));

            // Inglês
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode("en"));
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode("en-US"));

            // Francês
            Assert.AreEqual(AppLanguage.French, LocalizationManager.ParseLanguageCode("fr"));
            Assert.AreEqual(AppLanguage.French, LocalizationManager.ParseLanguageCode("fr-FR"));

            // Espanhol
            Assert.AreEqual(AppLanguage.Spanish, LocalizationManager.ParseLanguageCode("es"));
            Assert.AreEqual(AppLanguage.Spanish, LocalizationManager.ParseLanguageCode("es-ES"));

            // Qualquer outro idioma (Alemão, Russo, Chinês, Japonês, etc.) deve fazer fallback para Inglês
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode("de"));
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode("ru"));
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode("zh"));
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode("ja"));
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode(""));
            Assert.AreEqual(AppLanguage.English, LocalizationManager.ParseLanguageCode(null));
        }

        [Test]
        public void GivenDifferentLanguages_WhenGetCalled_ThenShouldReturnTranslatedText()
        {
            // Português
            LocalizationManager.ForceLanguage(AppLanguage.Portuguese);
            Assert.AreEqual("⚡ Analisar Tráfego", LocalizationManager.Get("AnalyzeTraffic"));
            Assert.AreEqual("Aguardando transporte de cadáver", LocalizationManager.Get("AlertDeath"));
            Assert.AreEqual("Resolvido", LocalizationManager.Get("StatusResolved"));

            // Inglês
            LocalizationManager.ForceLanguage(AppLanguage.English);
            Assert.AreEqual("⚡ Analyze Traffic", LocalizationManager.Get("AnalyzeTraffic"));
            Assert.AreEqual("Waiting for hearse / death care", LocalizationManager.Get("AlertDeath"));
            Assert.AreEqual("Resolved", LocalizationManager.Get("StatusResolved"));

            // Francês
            LocalizationManager.ForceLanguage(AppLanguage.French);
            Assert.AreEqual("⚡ Analyser le Trafic", LocalizationManager.Get("AnalyzeTraffic"));
            Assert.AreEqual("En attente de corbillard", LocalizationManager.Get("AlertDeath"));
            Assert.AreEqual("Résolu", LocalizationManager.Get("StatusResolved"));

            // Espanhol
            LocalizationManager.ForceLanguage(AppLanguage.Spanish);
            Assert.AreEqual("⚡ Analizar Tráfico", LocalizationManager.Get("AnalyzeTraffic"));
            Assert.AreEqual("Esperando coche fúnebre", LocalizationManager.Get("AlertDeath"));
            Assert.AreEqual("Resuelto", LocalizationManager.Get("StatusResolved"));
        }

        [Test]
        public void GivenLanguage_WhenGetAiLanguageInstructionCalled_ThenShouldMatchLanguage()
        {
            LocalizationManager.ForceLanguage(AppLanguage.Portuguese);
            Assert.AreEqual("Responda estritamente em Português Brasileiro.", LocalizationManager.GetAiLanguageInstruction());

            LocalizationManager.ForceLanguage(AppLanguage.English);
            Assert.AreEqual("Respond strictly in English.", LocalizationManager.GetAiLanguageInstruction());

            LocalizationManager.ForceLanguage(AppLanguage.French);
            Assert.AreEqual("Répondez strictement en Français.", LocalizationManager.GetAiLanguageInstruction());

            LocalizationManager.ForceLanguage(AppLanguage.Spanish);
            Assert.AreEqual("Responda estrictamente en Español.", LocalizationManager.GetAiLanguageInstruction());
        }

        [Test]
        public void GivenFrenchLanguage_WhenPayloadBuilt_ThenShouldIncludeFrenchInstruction()
        {
            LocalizationManager.ForceLanguage(AppLanguage.French);
            var builder = new DeepSeekPayloadBuilder();

            var alerts = new List<CityAlert>
            {
                new CityAlert
                {
                    BuildingId = 12,
                    BuildingName = "Immeuble Paris",
                    ProblemType = CityProblemType.Water,
                    ProblemDescription = LocalizationManager.Get("AlertWater")
                }
            };

            string payload = builder.BuildCityAlertPayload(alerts, "deepseek-chat");

            Assert.IsNotNull(payload);
            Assert.That(payload, Does.Contain("Répondez strictement en Français."));
            Assert.That(payload, Does.Contain("Immeuble Paris"));
        }

        [Test]
        public void GivenSpanishLanguage_WhenParserFallbackTriggered_ThenShouldReturnSpanishFallback()
        {
            LocalizationManager.ForceLanguage(AppLanguage.Spanish);
            var parser = new DeepSeekResponseParser();

            var bottlenecks = new List<TrafficBottleneck>
            {
                new TrafficBottleneck
                {
                    NodeId = 15,
                    RoadName = "Gran Vía",
                    HasTrafficLight = true
                }
            };

            // String vazia aciona o fallback defensivo
            parser.ParseAndApply("", bottlenecks);

            Assert.That(bottlenecks[0].AIRootCause, Does.Contain("Conflicto de semáforos"));
            Assert.That(bottlenecks[0].AIRecommendation, Does.Contain("sincronizar los semáforos"));
        }
    }
}
