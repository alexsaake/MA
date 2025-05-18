using System.Numerics;

namespace ProceduralLandscapeGeneration.MapGeneration.PlateTectonics.GPU;

internal struct PlateTectonicsPlateShaderBuffer
{
    public float Mass;
    public float Inertia;
    public float Rotation;
    public float Torque;
    public float AngularVelocity;
    public int PlateSegments;
    public Vector2 Position;
    public Vector2 TempPosition;
    public Vector2 Acceleration;
    public Vector2 Speed;
}
