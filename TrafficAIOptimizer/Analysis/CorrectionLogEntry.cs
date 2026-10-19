using System;

namespace TrafficAIOptimizer.Analysis
{
    public enum CorrectionTargetType
    {
        Traffic,
        CityAlert
    }

    public enum CorrectionStatus
    {
        Active,
        Resolved,
        PersistentExceeded10Min,
        AlternativeRequested,
        AlternativeReady,
        AlternativeApplied
    }

    /// <summary>
    /// Registro de auditoria de uma intervenção corretiva executada pelo mod.
    /// Monitora a eficácia e reavalia se o problema persistir por mais de 10 minutos (600 segundos).
    /// </summary>
    public class CorrectionLogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        public ushort TargetId { get; set; }
        public CorrectionTargetType TargetType { get; set; }
        public string TargetName { get; set; }
        public string ProblemDescription { get; set; }
        public string AppliedAction { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public float RealtimeSinceStartupApplied { get; set; }

        public CorrectionStatus Status { get; set; } = CorrectionStatus.Active;
        public float ElapsedSeconds { get; set; }

        // Solução Alternativa da IA (caso persista > 10 min)
        public string AlternativeRootCause { get; set; }
        public string AlternativeRecommendation { get; set; }
        public string AlternativeAction { get; set; }

        public bool IsOver10Minutes()
        {
            return ElapsedSeconds >= 600f;
        }

        public string GetFormattedElapsedTime()
        {
            int minutes = (int)(ElapsedSeconds / 60f);
            int seconds = (int)(ElapsedSeconds % 60f);
            return string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }

        public string GetStatusDisplay()
        {
            switch (Status)
            {
                case CorrectionStatus.Active:
                    return Core.Localization.LocalizationManager.Get("StatusActive", GetFormattedElapsedTime());
                case CorrectionStatus.Resolved:
                    return Core.Localization.LocalizationManager.Get("StatusResolved");
                case CorrectionStatus.PersistentExceeded10Min:
                    return Core.Localization.LocalizationManager.Get("StatusPersistentExceeded", GetFormattedElapsedTime());
                case CorrectionStatus.AlternativeRequested:
                    return Core.Localization.LocalizationManager.Get("StatusAlternativeRequested");
                case CorrectionStatus.AlternativeReady:
                    return Core.Localization.LocalizationManager.Get("StatusAlternativeReady");
                case CorrectionStatus.AlternativeApplied:
                    return Core.Localization.LocalizationManager.Get("StatusAlternativeApplied");
                default:
                    return Status.ToString();
            }
        }
    }
}
