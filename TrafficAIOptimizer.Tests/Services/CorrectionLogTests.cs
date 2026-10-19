using System;
using System.Collections.Generic;
using NUnit.Framework;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Services;

namespace TrafficAIOptimizer.Tests.Services
{
    [TestFixture]
    public class CorrectionLogTests
    {
        [Test]
        public void GivenElapsedSecondsLessThan600_WhenIsOver10MinutesCalled_ThenShouldReturnFalse()
        {
            var entry = new CorrectionLogEntry
            {
                ElapsedSeconds = 599f
            };

            Assert.IsFalse(entry.IsOver10Minutes());
        }

        [Test]
        public void GivenElapsedSecondsGreaterThanOrEqualTo600_WhenIsOver10MinutesCalled_ThenShouldReturnTrue()
        {
            var entry = new CorrectionLogEntry
            {
                ElapsedSeconds = 600f
            };

            Assert.IsTrue(entry.IsOver10Minutes());

            entry.ElapsedSeconds = 725f;
            Assert.IsTrue(entry.IsOver10Minutes());
        }

        [Test]
        public void GivenElapsedSeconds_WhenGetFormattedElapsedTimeCalled_ThenShouldReturnMMSS()
        {
            var entry = new CorrectionLogEntry
            {
                ElapsedSeconds = 625f // 10 minutos e 25 segundos
            };

            string formatted = entry.GetFormattedElapsedTime();
            Assert.AreEqual("10:25", formatted);
        }

        [Test]
        public void GivenDifferentStatuses_WhenGetStatusDisplayCalled_ThenShouldReturnExpectedText()
        {
            Core.Localization.LocalizationManager.ForceLanguage(Core.Localization.AppLanguage.Portuguese);

            var entry = new CorrectionLogEntry
            {
                ElapsedSeconds = 120f,
                Status = CorrectionStatus.Active
            };
            Assert.That(entry.GetStatusDisplay(), Does.Contain("Ativo (02:00)"));

            entry.Status = CorrectionStatus.Resolved;
            Assert.AreEqual("Resolvido", entry.GetStatusDisplay());

            entry.ElapsedSeconds = 660f;
            entry.Status = CorrectionStatus.PersistentExceeded10Min;
            Assert.That(entry.GetStatusDisplay(), Does.Contain("Persiste (>10 min: 11:00)"));

            entry.Status = CorrectionStatus.AlternativeReady;
            Assert.AreEqual("Alternativa Disponível!", entry.GetStatusDisplay());

            Core.Localization.LocalizationManager.ForceLanguage(null);
        }

        [Test]
        public void GivenCorrectionManager_WhenRecordCorrectionCalled_ThenShouldStoreInLogs()
        {
            var manager = CorrectionLogManager.Instance;
            manager.ClearLogs();

            var log = manager.RecordCorrection(
                101,
                CorrectionTargetType.CityAlert,
                "Edifício Comercial Alvorada",
                "Poucos compradores comerciais",
                "Incentivo comercial ativado"
            );

            Assert.IsNotNull(log);
            Assert.AreEqual(101, log.TargetId);
            Assert.AreEqual(CorrectionTargetType.CityAlert, log.TargetType);
            Assert.AreEqual(CorrectionStatus.Active, log.Status);

            var allLogs = manager.GetAllLogs();
            Assert.AreEqual(1, allLogs.Count);
            Assert.AreEqual("Edifício Comercial Alvorada", allLogs[0].TargetName);
        }

        [Test]
        public void GivenCityAlerts_WhenPayloadBuilt_ThenShouldContainAlertData()
        {
            var builder = new DeepSeekPayloadBuilder();
            var alerts = new List<CityAlert>
            {
                new CityAlert
                {
                    BuildingId = 55,
                    BuildingName = "Residencial Jardins",
                    ProblemType = CityProblemType.Death,
                    ProblemDescription = "Aguardando transporte de cadáver",
                    Severity = 65f,
                    ProblemTimer = 25
                }
            };

            string payload = builder.BuildCityAlertPayload(alerts, "deepseek-chat");

            Assert.IsNotNull(payload);
            Assert.That(payload, Does.Contain("\"model\": \"deepseek-chat\""));
            Assert.That(payload, Does.Contain("BuildingId: 55"));
            Assert.That(payload, Does.Contain("Residencial Jardins"));
            Assert.That(payload, Does.Contain("Aguardando transporte de cadáver"));
        }

        [Test]
        public void GivenCorrectionPersistingOver10Min_WhenAlternativePayloadBuilt_ThenShouldContainPersistenceContext()
        {
            var builder = new DeepSeekPayloadBuilder();
            var entry = new CorrectionLogEntry
            {
                TargetId = 77,
                TargetName = "Bairro Industrial Norte",
                ProblemDescription = "Poucos trabalhadores para operação",
                AppliedAction = "Rebalanceamento do quadro de vagas",
                ElapsedSeconds = 680f,
                Status = CorrectionStatus.PersistentExceeded10Min
            };

            string payload = builder.BuildAlternativeSolutionPayload(entry, "deepseek-reasoner");

            Assert.IsNotNull(payload);
            Assert.That(payload, Does.Contain("\"model\": \"deepseek-reasoner\""));
            Assert.That(payload, Does.Contain("ID: 77"));
            Assert.That(payload, Does.Contain("Bairro Industrial Norte"));
            Assert.That(payload, Does.Contain("11:20"));
        }

        [Test]
        public void GivenJsonResponseForCityAlerts_WhenParsed_ThenShouldEnrichAlerts()
        {
            var parser = new DeepSeekResponseParser();
            var alerts = new List<CityAlert>
            {
                new CityAlert
                {
                    BuildingId = 88,
                    BuildingName = "Hospital Geral",
                    ProblemType = CityProblemType.Garbage,
                    ProblemDescription = "Lixo acumulado sem coleta"
                }
            };

            string fakeJson = "{\n  \"choices\": [{\n    \"message\": {\n      \"content\": \"BUILDING: 88\\nCAUSA: Caminhões de coleta retidos em avenida arterial.\\nSOLUCAO: Criar corredor de serviços dedicado ou rota de mão única.\\nACAO_AUTO: DISPATCH_GARBAGE\"\n    }\n  }]\n}";

            parser.ParseAndApplyCityAlerts(fakeJson, alerts);

            Assert.AreEqual("Caminhões de coleta retidos em avenida arterial.", alerts[0].AIRootCause);
            Assert.AreEqual("Criar corredor de serviços dedicado ou rota de mão única.", alerts[0].AIRecommendation);
            Assert.AreEqual("DISPATCH_GARBAGE", alerts[0].SuggestedAction);
            Assert.IsTrue(alerts[0].HasAutomatedAction);
        }

        [Test]
        public void GivenAlternativeResponseJson_WhenParsed_ThenShouldEnrichCorrectionLog()
        {
            var parser = new DeepSeekResponseParser();
            var entry = new CorrectionLogEntry
            {
                TargetId = 99,
                TargetName = "Avenida Comercial",
                ProblemDescription = "Poucos compradores",
                ElapsedSeconds = 640f,
                Status = CorrectionStatus.PersistentExceeded10Min
            };

            string fakeJson = "{\n  \"choices\": [{\n    \"message\": {\n      \"content\": \"CAUSA_FALHA: Isolamento viário impede chegada de pedestres dos distritos vizinhos.\\nSOLUCAO_ALTERNATIVA: Implantar linha de metrô/VLT interligando bairros densos ao polo comercial.\\nACAO_ALTERNATIVA: Rezoneamento misto de alta densidade no quarteirão oposto.\"\n    }\n  }]\n}";

            parser.ParseAndApplyAlternativeSolution(fakeJson, entry);

            Assert.AreEqual("Isolamento viário impede chegada de pedestres dos distritos vizinhos.", entry.AlternativeRootCause);
            Assert.AreEqual("Implantar linha de metrô/VLT interligando bairros densos ao polo comercial.", entry.AlternativeRecommendation);
            Assert.AreEqual("Rezoneamento misto de alta densidade no quarteirão oposto.", entry.AlternativeAction);
            Assert.AreEqual(CorrectionStatus.AlternativeReady, entry.Status);
        }

        [Test]
        public void GivenActiveLogs_WhenReanalyzeCorrectionsCalled_ThenShouldEvaluateStatusDemandBased()
        {
            var manager = CorrectionLogManager.Instance;
            manager.ClearLogs();

            var log = manager.RecordCorrection(
                202,
                CorrectionTargetType.CityAlert,
                "Torre Residencial",
                "Falta de água",
                "Restauração de pressão"
            );

            // Simula passagem de tempo sem necessidade de threads ou background loops
            log.RealtimeSinceStartupApplied = manager.TimeProvider.GetCurrentTime() - 650f;

            manager.ReanalyzeCorrections();

            // Como Singleton<BuildingManager>.exists é falso no ambiente isolado NUnit,
            // CheckIfTargetStillHasProblem retorna false, resolvendo a questão
            Assert.AreEqual(CorrectionStatus.Resolved, log.Status);
            Assert.IsTrue(log.ElapsedSeconds >= 600f);
        }
    }
}
