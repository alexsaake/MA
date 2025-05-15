using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.WindErosion;

internal struct ParticleWindErosionShaderBuffer
{
    public int Age;
    public float Sediment;
    private readonly float padding1;
    private readonly float padding2;
    public Vector3 Position;
    private readonly float padding3;
    public Vector3 Speed;
    private readonly float padding4;
};
