using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.Particles;

internal struct ParticleHydraulicErosionShaderBuffer
{
    public int Age;
    public float Volume;
    public float Sediment;
    private float padding1;
    public Vector2 Position;
    public Vector2 Speed;
}
