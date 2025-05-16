using System.Numerics;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics.GPU;

internal struct PlateTectonicsSegmentShaderBuffer
{
    public int Plate;
    public float Mass;
    public float Inertia;
    public float Density;
    public float Height;
    public float Thickness;
    public bool IsAlive;
    private bool padding1;
    private bool padding2;
    private bool padding3;
    public bool IsColliding;
    private bool padding4;
    private bool padding5;
    private bool padding6;
    public Vector2 Position;
}
