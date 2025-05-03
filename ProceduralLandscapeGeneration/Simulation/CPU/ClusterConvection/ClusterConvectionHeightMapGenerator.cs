using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.ClusterConvection;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class ClusterConvectionHeightMapGenerator : IHeightMapGenerator
{
    private const float Growth = 0.05f;

    private readonly IConfiguration myConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IPoissonDiskSampler myPoissonDiskSampler;

    private List<Segment> mySegments;

    public ClusterConvectionHeightMapGenerator(IConfiguration configuration, IRandom random, IShaderBuffers shaderBuffers, IPoissonDiskSampler poissonDiskSampler)
    {
        myConfiguration = configuration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;
        myPoissonDiskSampler = poissonDiskSampler;

        mySegments = new List<Segment>();
    }

    public HeightMap GenerateHeightMap()
    {
        float[,] noiseMap = new float[myConfiguration.HeightMapSideLength, myConfiguration.HeightMapSideLength];
        HeightMap heatMap = new HeightMapGeneratorCPU(myConfiguration, myRandom, myShaderBuffers).GenerateHeightMap();

        List<Vector2> points = myPoissonDiskSampler.GeneratePoints(1.0f, myConfiguration.HeightMapSideLength);
        foreach (Vector2 point in points)
        {
            Segment segment = new Segment(point);
            mySegments.Add(segment);

            if (!segment.IsAlive)
            {
                continue;
            }

            IVector2 position = new IVector2(segment.Position);
            float heatValue = heatMap.Height[position.X, position.Y];

            float rate = Growth * (1.0f - heatValue);
            float G = rate * (1.0f - heatValue - segment.Density * segment.Thickness);
            if (G < 0.0) G *= 0.05f;

            float D = Langmuir(3.0f, 1.0f - heatValue);

            segment.Mass += segment.Area * G * D;
            segment.Thickness += G;

            segment.Buoyancy();

            noiseMap[position.X, position.Y] = segment.Height;
        }

        return new HeightMap(myConfiguration, noiseMap);
    }

    private float Langmuir(float k, float x)
    {
        return k * x / (1.0f + k * x);
    }

    public unsafe void GenerateHeightMapShaderBuffer()
    {
        HeightMap heightMap = GenerateHeightMap();
        float[] heightMapValues = heightMap.Get1DHeightMapValues();

        uint heightMapShaderBufferSize = (uint)heightMapValues.Length * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapShaderBufferSize);
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapShaderBufferSize, 0);
        }
    }

    public void Dispose()
    {
        mySegments.Clear();
        myShaderBuffers.Dispose();
    }
}
