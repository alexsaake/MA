using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.WindErosion;

internal struct ParticleWindErosionShaderBuffer
{
    public int Age;
    public float Sediment;
    private float padding1;
    private float padding2;
    public Vector3 Position;
    private float padding3;
    public Vector3 Speed;
    private float padding4;
};
