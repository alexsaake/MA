using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Config.Types;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class PlateTectonicsHeightMapGenerator : IPlateTectonicsHeightMapGenerator
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly ILifetimeScope myLifetimeScope;

    private Segment?[]? mySegments;
    private readonly List<Plate> myPlates;

    public PlateTectonicsHeightMapGenerator(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IRandom random, IShaderBuffers shaderBuffers, ILifetimeScope lifetimeScope)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;
        myLifetimeScope = lifetimeScope;

        myPlates = new List<Plate>();
    }

    public void GenerateHeightMap()
    {
        IHeightMapGenerator heightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myConfiguration.HeightMapGeneration);
        heightMapGenerator.GenerateNoiseHeatMap();

        CreateSegments();
        CreatePlates();
        AddSegmentsToNearesPlate();
        CreateEmptyHeightMapShaderBuffer();
    }

    private void CreateSegments()
    {
        mySegments = new Segment[myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength];
        for (uint y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
            {
                mySegments[x + y * myMapGenerationConfiguration.HeightMapSideLength] = new Segment(x, y, myMapGenerationConfiguration, myShaderBuffers);
            }
        }
    }

    private void CreatePlates()
    {
        for (int i = 0; i < myConfiguration.PlateCount; i++)
        {
            myPlates.Add(new Plate(new Vector2(myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength), myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength)),myMapGenerationConfiguration, myShaderBuffers));
        }
    }

    private void AddSegmentsToNearesPlate()
    {
        foreach (Segment segment in mySegments!)
        {
            float distance = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
            Plate? nearestPlate = null;
            foreach (Plate plate in myPlates)
            {
                float plateToSegmentDistance = (plate.Position - segment.Position).Length();
                if (plateToSegmentDistance < distance)
                {
                    distance = plateToSegmentDistance;
                    nearestPlate = plate;
                }
            }
            nearestPlate!.Segments.Add(segment);
            segment.Parent = nearestPlate;
        }

        foreach (Plate plate in myPlates)
        {
            plate.Recenter();
        }
    }

    private void CreateEmptyHeightMapShaderBuffer()
    {
        uint heightMapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
    }

    public void SimulatePlateTectonics()
    {
        MarkOutOfBoundsSegmentsAsDeadAndRemoveFromPlates();
        RemoveEmptyPlates();
        RemoveDeadSegmentsFromHeightMap();
        MovePlates();
        FloatSegments();
        FillSegmentGaps();
        UpdateHeightMap();
    }

    private void MarkOutOfBoundsSegmentsAsDeadAndRemoveFromPlates()
    {
        foreach (Plate plate in myPlates)
        {
            for (int segment = 0; segment < plate.Segments.Count; segment++)
            {
                IVector2 position = new IVector2(plate.Segments[segment].Position);
                if (position.X < 0 || position.X > myMapGenerationConfiguration.HeightMapSideLength - 1 ||
                position.Y < 0 || position.Y > myMapGenerationConfiguration.HeightMapSideLength - 1)
                {
                    plate.Segments[segment].IsAlive = false;
                }
            }

            bool erased = false;
            for (int segment = 0; segment < plate.Segments.Count; segment++)
            {
                if (!plate.Segments[segment].IsAlive)
                {
                    plate.Segments.RemoveAt(segment);
                    segment--;
                    erased = true;
                }
            }
            if (erased)
            {
                plate.Recenter();
            }
        }
    }

    private void RemoveEmptyPlates()
    {
        for (int plate = 0; plate < myPlates.Count; plate++)
        {
            if (myPlates[plate].Segments.Count == 0)
            {
                myPlates.RemoveAt(plate);
                plate--;
                continue;
            }
        }
    }

    private void RemoveDeadSegmentsFromHeightMap()
    {
        for (int segment = 0; segment < mySegments!.Length; segment++)
        {
            if (mySegments[segment] is null)
            {
                continue;
            }
            if (!mySegments[segment]!.IsAlive)
            {
                mySegments[segment] = null;
            }
        }
    }

    private void MovePlates()
    {
        foreach (Plate plate in myPlates)
        {
            plate.Move(mySegments!);
        }
    }

    private void FloatSegments()
    {
        foreach (Segment segment in mySegments!)
        {
            if (segment is null)
            {
                continue;
            }
            segment.Float();
        }
    }

    private unsafe void FillSegmentGaps()
    {
        const float generationCooling = -0.1f;

        uint heatMapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        uint heatMapBufferSize = heatMapSize * sizeof(float);
        float[] heatMap = new float[heatMapSize];
        Rlgl.MemoryBarrier();
        fixed (float* heatMapPointer = heatMap)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], heatMapPointer, heatMapBufferSize, 0);
        }

        foreach (Plate plate in myPlates)
        {
            foreach (Segment segment in plate.Segments.ToList())
            {
                float angle = myRandom.NextFloat() * 2.0f * MathF.PI;
                Vector2 scanPosition = segment.Position;
                scanPosition += 2.0f * new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                if (scanPosition.X < 0 || scanPosition.X >= myMapGenerationConfiguration.HeightMapSideLength ||
                scanPosition.Y < 0 || scanPosition.Y >= myMapGenerationConfiguration.HeightMapSideLength)
                {
                    continue;
                }

                IVector2 scanIntegerPosition = new IVector2(scanPosition);
                if (mySegments![scanIntegerPosition.X + scanIntegerPosition.Y * myMapGenerationConfiguration.HeightMapSideLength] is null)
                {
                    Segment newSegment = new Segment(scanPosition, myMapGenerationConfiguration, myShaderBuffers);
                    mySegments![scanIntegerPosition.X + scanIntegerPosition.Y * myMapGenerationConfiguration.HeightMapSideLength] = newSegment;
                    plate.Segments.Add(newSegment);
                    newSegment.Parent = plate;
                    plate.Recenter();
                    newSegment.Float();
                    heatMap[scanIntegerPosition.X + scanIntegerPosition.Y * myMapGenerationConfiguration.HeightMapSideLength] += generationCooling;
                }
            }
        }

        fixed (float* heatMapPointer = heatMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], heatMapPointer, heatMapBufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void UpdateHeightMap()
    {
        uint heightMapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        uint heightMapBufferSize = heightMapSize * sizeof(float);
        float[] heightMap = new float[heightMapSize];
        for (int segment = 0; segment < mySegments!.Length; segment++)
        {
            if(mySegments![segment] is null)
            {
                heightMap[segment] = 0;
                continue;
            }
            heightMap[segment] = mySegments![segment]!.Height;
        }

        fixed (float* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, heightMapBufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    public void Dispose()
    {
        mySegments = null;
        myPlates.Clear();
    }
}
