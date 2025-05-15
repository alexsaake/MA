using System.Numerics;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics;

internal struct PlateTectonicsSegmentShaderBuffer
{
    public uint Plate;
    public float Mass;
    public float Inertia;
    private float padding1;
    public Vector2 Position;
}
