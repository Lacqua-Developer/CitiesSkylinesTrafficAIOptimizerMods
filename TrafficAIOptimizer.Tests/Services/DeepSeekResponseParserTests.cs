using System.Collections.Generic;
using NUnit.Framework;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Services;

namespace TrafficAIOptimizer.Tests.Services
{
    [TestFixture]
    public class DeepSeekResponseParserTests
    {
        private DeepSeekResponseParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new DeepSeekResponseParser();
        }

        [Test]
        public void GivenValidAIResponse_WhenParsed_ThenBottleneckShouldBeEnrichedWithRecommendationsAndActions()
        {
            var bottlenecks = new List<TrafficBottleneck>
            {
                new TrafficBottleneck
                {
                    NodeId = 101,
                    RoadName = "Rodovia das Nações"
                }
            };

            string fakeJsonResponse = "{\n" +
                "  \"choices\": [{\n" +
                "    \"message\": {\n" +
                "      \"content\": \"NODE: 101\\nCAUSA: Semáforo longo em cruzamento de alto fluxo.\\nSOLUCAO: Transformar em rotatória ou remover semáforo e criar via preferencial.\\nACAO_AUTO: REMOVE_TRAFFIC_LIGHT\"\n" +
                "    }\n" +
                "  }]\n" +
                "}";

            _parser.ParseAndApply(fakeJsonResponse, bottlenecks);

            Assert.AreEqual("Semáforo longo em cruzamento de alto fluxo.", bottlenecks[0].AIRootCause);
            Assert.AreEqual("Transformar em rotatória ou remover semáforo e criar via preferencial.", bottlenecks[0].AIRecommendation);
            Assert.IsTrue(bottlenecks[0].HasAutomatedAction);
            Assert.That(bottlenecks[0].SuggestedActions, Contains.Item("REMOVE_TRAFFIC_LIGHT"));
        }

        [Test]
        public void GivenEmptyOrMalformedResponse_WhenParsed_ThenShouldApplyGracefulFallbackWithoutThrowing()
        {
            var bottlenecks = new List<TrafficBottleneck>
            {
                new TrafficBottleneck
                {
                    NodeId = 202,
                    RoadName = "Via Secundária"
                }
            };

            string brokenJson = "{\"choices\": []}";

            Assert.DoesNotThrow(() => _parser.ParseAndApply(brokenJson, bottlenecks));
            Assert.IsNotNull(bottlenecks[0].AIRootCause);
            Assert.IsNotNull(bottlenecks[0].AIRecommendation);
        }
    }
}
