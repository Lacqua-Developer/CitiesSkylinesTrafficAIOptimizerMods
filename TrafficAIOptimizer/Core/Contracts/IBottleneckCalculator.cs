namespace TrafficAIOptimizer.Core.Contracts
{
    /// <summary>
    /// Contrato para cálculo de índice de congestionamento viário.
    /// Segue SRP (focado apenas no cálculo matemático/lógico de gargalos).
    /// </summary>
    public interface IBottleneckCalculator
    {
        float CalculateScore(byte trafficDensity, int stuckVehicleCount, int totalVehicleCount, bool hasTrafficLight);
        float CalculateSpeedRatio(byte trafficDensity);
    }
}
