using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class PlateTectonicsHeightMapGenerator : IPlateTectonicsHeightMapGenerator
{
    private readonly IConfiguration myConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IHeightMapGenerator myHeatMapGenerator;

    private readonly List<Segment> mySegments;
    private readonly List<Plate> myPlates;
    private HeightMap? myHeatMap;
    private HeightMap? myHeightMap;

    public PlateTectonicsHeightMapGenerator(IConfiguration configuration, IRandom random, IShaderBuffers shaderBuffers, IHeightMapGenerator heatMapGenerator)
    {
        myConfiguration = configuration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;
        myHeatMapGenerator = heatMapGenerator;

        mySegments = new List<Segment>();
        myPlates = new List<Plate>();
    }

    public HeightMap GenerateHeightMap()
    {
        float[,] heightMap = new float[myConfiguration.HeightMapSideLength, myConfiguration.HeightMapSideLength];
        myHeatMap = myHeatMapGenerator.GenerateHeightMap();

        for (uint y = 0; y < myConfiguration.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < myConfiguration.HeightMapSideLength; x++)
            {
                mySegments.Add(new Segment(x, y));
            }
        }

        for (int i = 0; i < myConfiguration.PlateCount; i++)
        {
            myPlates.Add(new Plate(new Vector2(myRandom.Next((int)myConfiguration.HeightMapSideLength), myRandom.Next((int)myConfiguration.HeightMapSideLength))));
        }

        foreach (Segment segment in mySegments)
        {
            float distance = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
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

        myHeightMap = new HeightMap(myConfiguration, heightMap);
        return myHeightMap;
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

    public HeightMap SimulatePlateTectonics()
    {
        foreach (Plate plate in myPlates)
        {
            for (int segment = 0; segment < plate.Segments.Count; segment++)
            {
                IVector2 position = new IVector2(plate.Segments[segment].Position);
                if (position.X < 0 || position.X > myConfiguration.HeightMapSideLength - 1 ||
                position.Y < 0 || position.Y > myConfiguration.HeightMapSideLength - 1)
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

        for (int plate = 0; plate < myPlates.Count; plate++)
        {
            if (myPlates[plate].Segments.Count == 0)
            {
                myPlates.RemoveAt(plate);
                plate--;
                continue;
            }
        }

        mySegments.RemoveAll(x => !x.IsAlive);

        foreach (Plate plate in myPlates)
        {
            plate.Update(myHeatMap!, mySegments, myConfiguration.HeightMapSideLength);
        }
        foreach (Segment segment in mySegments)
        {
            segment.Update(myHeightMap!, myHeatMap!, myConfiguration.HeightMapSideLength);
        }

        //Fill Gaps

        const float generationCooling = -0.1f;
        foreach (Plate plate in myPlates)
        {
            foreach (Segment segment in plate.Segments.ToList())
            {
                float angle = myRandom.NextFloat() * 2.0f * MathF.PI;
                Vector2 scanPosition = segment.Position;
                scanPosition += 2.0f * new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                if (scanPosition.X < 0 || scanPosition.X >= myConfiguration.HeightMapSideLength ||
                scanPosition.Y < 0 || scanPosition.Y >= myConfiguration.HeightMapSideLength)
                {
                    continue;
                }

                IVector2 scanIntegerPosition = new IVector2(scanPosition);
                if (myHeightMap!.Height[scanIntegerPosition.X, scanIntegerPosition.Y] == 0)
                {
                    Segment newSegment = new Segment(scanPosition);
                    mySegments.Add(newSegment);
                    plate.Segments.Add(newSegment);
                    newSegment.Parent = plate;
                    plate.Recenter();
                    newSegment.Update(myHeightMap, myHeatMap, myConfiguration.HeightMapSideLength);
                    myHeatMap.Height[scanIntegerPosition.X, scanIntegerPosition.Y] += generationCooling;
                }
            }
        }

        return myHeightMap!;
    }

    public void Dispose()
    {
        mySegments.Clear();
        myPlates.Clear();
        myShaderBuffers.Dispose();
    }
}
