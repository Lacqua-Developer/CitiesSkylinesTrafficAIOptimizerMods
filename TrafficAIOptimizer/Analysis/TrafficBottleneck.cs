using System.Collections.Generic;
using UnityEngine;

namespace TrafficAIOptimizer.Analysis
{
    /// <summary>
    /// Representa um gargalo de tráfego detectado na malha viária.
    /// </summary>
    public class TrafficBottleneck
    {
        public ushort NodeId { get; set; }
        public ushort SegmentId { get; set; }
        public Vector3 Position { get; set; }
        public string RoadName { get; set; }
        public float CongestionScore { get; set; }
        public float AverageSpeedPercent { get; set; }
        public int VehicleCount { get; set; }
        public int StuckVehicleCount { get; set; }
        public bool HasTrafficLight { get; set; }
        public int ConnectedSegmentsCount { get; set; }
        public List<ushort> ConnectedSegmentIds { get; set; } = new List<ushort>();

        // Resposta da IA para este gargalo
        public string AIRootCause { get; set; }
        public string AIRecommendation { get; set; }
        public List<string> SuggestedActions { get; set; } = new List<string>();
        public bool HasAutomatedAction { get; set; }
    }
}
