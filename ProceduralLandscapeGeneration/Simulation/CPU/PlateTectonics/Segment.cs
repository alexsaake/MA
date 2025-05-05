using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class Segment
{
    private const float Growth = 0.05f;

    private readonly IConfiguration myConfiguration;
    private readonly IShaderBuffers myShaderBuffers;

    public Plate? Parent { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Speed { get; set; } = Vector2.Zero;
    public int Area { get; private set; } = 1;
    public float Mass { get; set; } = 0.1f;
    public float Height { get; set; } = 0.0f;
    public float Thickness { get; set; } = 0.1f;
    public float Density { get; set; } = 1.0f;
    public bool IsAlive { get; set; } = true;
    public bool IsColliding { get; set; } = false;

    public Segment(uint x, uint y, IConfiguration configuration, IShaderBuffers shaderBuffers) : this(new Vector2(x, y), configuration, shaderBuffers) { }

    public Segment(Vector2 position, IConfiguration configuration, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myShaderBuffers = shaderBuffers;

        Position = position;
    }

    public void Float()
    {
        if (!IsAlive)
        {
            return;
        }

        IVector2 position = new IVector2(Position);
        if (position.X < 0 || position.X > myConfiguration.HeightMapSideLength - 1
            || position.Y < 0 || position.Y > myConfiguration.HeightMapSideLength - 1)
        {
            return;
        }
        float[] heatMap = ReadHeatMap();
        float heatValue = heatMap[position.X + position.Y * myConfiguration.HeightMapSideLength];

        float rate = Growth * (1.0f - heatValue);
        float G = rate * (1.0f - heatValue - Density * Thickness);
        if (G < 0.0) G *= 0.05f;

        float D = Langmuir(3.0f, 1.0f - heatValue);

        Mass += Area * G * D;
        Thickness += G;

        Buoyancy();
    }

    private unsafe float[] ReadHeatMap()
    {
        uint heatMapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint heatMapBufferSize = heatMapSize * sizeof(float);
        float[] heatMap = new float[heatMapSize];
        Rlgl.MemoryBarrier();
        fixed (float* heatMapPointer = heatMap)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], heatMapPointer, heatMapBufferSize, 0);
        }

        return heatMap;
    }

    private static float Langmuir(float k, float x)
    {
        return k * x / (1.0f + k * x);
    }

    public void Buoyancy()
    {
        Density = Mass / (Area * Thickness);
        Height = Thickness * (1.0f - Density);
    }
}
