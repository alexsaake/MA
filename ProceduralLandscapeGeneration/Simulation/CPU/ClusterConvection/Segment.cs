using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.ClusterConvection;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class Segment
{
    private const float Growth = 0.05f;

    public Plate? Parent { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Speed { get; set; } = Vector2.Zero;
    public int Area { get; private set; } = 1;
    public float Mass { get; set; } = 0.1f;
    public float Height { get; private set; } = 0.0f;
    public float Thickness { get; set; } = 0.1f;
    public float Density { get; private set; } = 1.0f;
    public bool IsAlive { get; set; } = true;
    public bool IsColliding { get; set; } = false;

    public Segment(Vector2 position)
    {
        Position = position;
    }

    public void Update(float[,] heightMap, HeightMap heatMap)
    {
        if (!IsAlive)
        {
            return;
        }

        IVector2 position = new IVector2(Position);
        float heatValue = heatMap.Height[position.X, position.Y];

        float rate = Growth * (1.0f - heatValue);
        float G = rate * (1.0f - heatValue - Density * Thickness);
        if (G < 0.0) G *= 0.05f;

        float D = Langmuir(3.0f, 1.0f - heatValue);

        Mass += Area * G * D;
        Thickness += G;

        Buoyancy();

        heightMap[position.X, position.Y] = Height;
    }

    private void Buoyancy()
    {
        Density = Mass / (Area * Thickness);
        Height = Thickness * (1.0f - Density);
    }

    private float Langmuir(float k, float x)
    {
        return k * x / (1.0f + k * x);
    }
}
