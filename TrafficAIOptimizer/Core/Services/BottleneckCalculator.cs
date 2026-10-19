using System;
using TrafficAIOptimizer.Core.Contracts;

namespace TrafficAIOptimizer.Core.Services
{
    /// <summary>
    /// Implementação desacoplada do cálculo de gargalos viários.
    /// Livre de dependências diretas de Unity, ideal para testes unitários isolados.
    /// </summary>
    public class BottleneckCalculator : IBottleneckCalculator
    {
        public float CalculateScore(byte trafficDensity, int stuckVehicleCount, int totalVehicleCount, bool hasTrafficLight)
        {
            float densityWeight = trafficDensity * 0.5f;
            float stuckWeight = stuckVehicleCount * 10f;
            float volumeWeight = totalVehicleCount * 1.5f;
            float trafficLightWeight = hasTrafficLight ? 5f : 0f;

            float total = densityWeight + stuckWeight + volumeWeight + trafficLightWeight;
            return (float)Math.Round(total * 10f) / 10f;
        }

        public float CalculateSpeedRatio(byte trafficDensity)
        {
            float ratio = 1.0f - (trafficDensity / 100f);
            if (ratio < 0f) ratio = 0f;
            if (ratio > 1f) ratio = 1f;

            return (float)Math.Round(ratio * 100f);
        }
    }
}
