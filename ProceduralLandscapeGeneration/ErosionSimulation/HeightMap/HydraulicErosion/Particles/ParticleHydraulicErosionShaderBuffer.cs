using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.HydraulicErosion.Particles;

internal struct ParticleHydraulicErosionShaderBuffer
{
    public int Age;
    public float Volume;
    public float Sediment;
    private readonly float padding1;
    public Vector2 Position;
    public Vector2 Speed;
}
