using System.Numerics;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics.GPU;

internal struct PlateTectonicsPlateShaderBuffer
{
    public float Mass;
    public float Inertia;
    public float Rotation;
    public float Torque;
    public float AngularVelocity;
    private float padding1;
    public Vector2 Position;
    public Vector2 Acceleration;
    public Vector2 Speed;
}
