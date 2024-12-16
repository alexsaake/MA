using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal struct Soil
    {
        public SoilTypes Type { get; set; }
        public float Height { get; set; }
        public Vector2 Momentum { get; set; }
        public Vector2 MomentumTrack { get; set; }
        public float Discharge { get; set; }
        public float DischargeTrack { get; set; }
        public float Resistance { get; set; }
    }
}
