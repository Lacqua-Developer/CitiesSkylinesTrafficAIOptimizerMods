using System.Collections.Generic;
using NUnit.Framework;
using TrafficAIOptimizer.Analysis;
using TrafficAIOptimizer.Core.Services;

namespace TrafficAIOptimizer.Tests.Services
{
    [TestFixture]
    public class DeepSeekPayloadBuilderTests
    {
        private DeepSeekPayloadBuilder _builder;

        [SetUp]
        public void Setup()
        {
            _builder = new DeepSeekPayloadBuilder();
        }

        [Test]
        public void GivenSpecialCharacters_WhenEscapeJson_ThenShouldEscapeProperly()
        {
            string raw = "Linha 1\nLinha 2\r com \"aspas\" e barra \\";
            string escaped = _builder.EscapeJson(raw);

            Assert.That(escaped, Does.Contain("\\n"));
            Assert.That(escaped, Does.Contain("\\\"aspas\\\""));
            Assert.That(escaped, Does.Not.Contain("\n"));
        }

        [Test]
        public void GivenBottlenecksList_WhenPayloadBuilt_ThenShouldContainModelAndNodeData()
        {
            var list = new List<TrafficBottleneck>
            {
                new TrafficBottleneck
                {
                    NodeId = 42,
                    RoadName = "Avenida Central",
                    CongestionScore = 75.5f,
                    AverageSpeedPercent = 25f,
                    VehicleCount = 8,
                    StuckVehicleCount = 2,
                    HasTrafficLight = true,
                    ConnectedSegmentsCount = 4
                }
            };

            string payload = _builder.BuildChatPayload(list, "deepseek-chat");

            Assert.IsNotNull(payload);
            Assert.That(payload, Does.Contain("\"model\": \"deepseek-chat\""));
            Assert.That(payload, Does.Contain("NodeId: 42"));
            Assert.That(payload, Does.Contain("Avenida Central"));
            Assert.That(payload, Does.Contain("Score: 75.5"));
        }

        [Test]
        public void GivenSingleLaneBottleneck_WhenPayloadBuilt_ThenShouldContainSingleLaneAndLaneCount()
        {
            var list = new List<TrafficBottleneck>
            {
                new TrafficBottleneck
                {
                    NodeId = 101,
                    RoadName = "Rodovia dos Imigrantes",
                    LaneCount = 3,
                    IsSingleLaneCongestion = true,
                    CongestionScore = 88.0f,
                    AverageSpeedPercent = 15f,
                    VehicleCount = 12,
                    StuckVehicleCount = 5,
                    HasTrafficLight = false,
                    ConnectedSegmentsCount = 2
                }
            };

            string payload = _builder.BuildChatPayload(list, "deepseek-chat");

            Assert.IsNotNull(payload);
            Assert.That(payload, Does.Contain("Faixas: 3"));
            Assert.That(payload, Does.Contain("FilaIndiana: SIM"));
            Assert.That(payload, Does.Contain("REDISTRIBUTE_LANES"));
        }
    }
}
