using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.ClusterConvection;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class Segment
{
    private Vector2 mySpeed = Vector2.Zero;

    public Vector2 Position { get; private set; }
    public int Area { get; private set; } = 1;
    public float Mass { get; set; } = 0.1f;
    public float Height { get; private set; } = 0.0f;
    public float Thickness { get; set; } = 0.1f;
    public float Density { get; private set; } = 1.0f;
    public bool IsAlive { get; private set; } = true;
    private bool myIsColliding = false;

    public Segment(Vector2 position)
    {
        Position = position;
    }

    public void Buoyancy()
    {
        Density = Mass / (Area * Thickness);
        Height = Thickness * (1.0f - Density);
    }
}
