namespace TrafficAIOptimizer.Core.Contracts
{
    /// <summary>
    /// Provedor de tempo desacoplado da engine Unity, garantindo testabilidade unitária fora do jogo.
    /// </summary>
    public interface ITimeProvider
    {
        float GetCurrentTime();
    }
}
