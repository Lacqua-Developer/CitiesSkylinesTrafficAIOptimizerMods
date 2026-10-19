using NUnit.Framework;
using TrafficAIOptimizer.Core.Services;

namespace TrafficAIOptimizer.Tests.Services
{
    [TestFixture]
    public class BottleneckCalculatorTests
    {
        private BottleneckCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _calculator = new BottleneckCalculator();
        }

        [Test]
        public void GivenHighCongestionParameters_WhenCalculated_ThenScoreShouldReflectSeverity()
        {
            byte density = 80;
            int stuckVehicles = 5;
            int totalVehicles = 12;
            bool hasTrafficLight = true;

            // (80 * 0.5) + (5 * 10) + (12 * 1.5) + 5 = 40 + 50 + 18 + 5 = 113.0
            float score = _calculator.CalculateScore(density, stuckVehicles, totalVehicles, hasTrafficLight);

            Assert.AreEqual(113f, score);
        }

        [Test]
        public void GivenZeroDensity_WhenCalculateSpeedRatio_ThenSpeedRatioShouldBe100Percent()
        {
            float speedPercent = _calculator.CalculateSpeedRatio(0);
            Assert.AreEqual(100f, speedPercent);
        }

        [Test]
        public void GivenMaxDensity_WhenCalculateSpeedRatio_ThenSpeedRatioShouldBe0Percent()
        {
            float speedPercent = _calculator.CalculateSpeedRatio(100);
            Assert.AreEqual(0f, speedPercent);
        }
    }
}
