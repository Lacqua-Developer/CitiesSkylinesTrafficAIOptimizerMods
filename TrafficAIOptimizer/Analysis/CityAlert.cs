using System.Collections.Generic;
using UnityEngine;

namespace TrafficAIOptimizer.Analysis
{
    /// <summary>
    /// Tipos de problemas urbanos rastreados pelo Cities: Skylines.
    /// </summary>
    public enum CityProblemType
    {
        Death,
        NoCustomers,
        NoWorkers,
        Water,
        Electricity,
        Sewage,
        Garbage,
        Fire,
        Crime,
        Other
    }

    /// <summary>
    /// Representa um alerta urbano detectado em um edifício da cidade (Cadáver, Falta de Água, Luz,
    /// Esgoto, Compradores, Trabalhadores, Lixo, etc.).
    /// </summary>
    public class CityAlert
    {
        public ushort BuildingId { get; set; }
        public string BuildingName { get; set; }
        public Vector3 Position { get; set; }
        public CityProblemType ProblemType { get; set; }
        public string ProblemDescription { get; set; }
        public float Severity { get; set; }
        public int ProblemTimer { get; set; }

        public string AIRootCause { get; set; }
        public string AIRecommendation { get; set; }
        public string SuggestedAction { get; set; }
        public bool HasAutomatedAction { get; set; }

        public List<string> SuggestedActions { get; set; } = new List<string>();

        public override string ToString()
        {
            return string.Format("[BuildingId: {0}, Nome: {1}, Tipo: {2}, Severidade: {3:0.0}]",
                BuildingId, BuildingName, ProblemType, Severity);
        }
    }
}
