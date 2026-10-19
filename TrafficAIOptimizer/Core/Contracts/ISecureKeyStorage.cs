using TrafficAIOptimizer.Core.Providers;

namespace TrafficAIOptimizer.Core.Contracts
{
    /// <summary>
    /// Contrato de armazenamento seguro de chaves estritamente em memória RAM para múltiplos provedores.
    /// Respeita o Princípio da Responsabilidade Única (SRP) e Inversão de Dependência (DIP).
    /// </summary>
    public interface ISecureKeyStorage
    {
        AIProvider ActiveProvider { get; set; }

        bool HasKey { get; }
        void SetKey(string key);
        string GetKeyUnsafe();
        void Clear();
        string GetMaskedDisplay();

        bool HasKeyFor(AIProvider provider);
        void SetKey(string key, AIProvider provider);
        string GetKeyUnsafe(AIProvider provider);
        void Clear(AIProvider provider);
        void ClearAll();
        string GetMaskedDisplay(AIProvider provider);
    }
}
