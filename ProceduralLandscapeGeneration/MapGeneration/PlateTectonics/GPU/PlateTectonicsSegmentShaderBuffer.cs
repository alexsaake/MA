using System.Numerics;

namespace ProceduralLandscapeGeneration.MapGeneration.PlateTectonics.GPU;

internal struct PlateTectonicsSegmentShaderBuffer
{
    public int Plate;
    public float Mass;
    public float Inertia;
    public float Density;
    public float Height;
    public float Thickness;
    public bool IsAlive;
    private readonly bool padding1;
    private readonly bool padding2;
    private readonly bool padding3;
    public bool IsColliding;
    private readonly bool padding4;
    private readonly bool padding5;
    private readonly bool padding6;
    public Vector2 Position;
}
