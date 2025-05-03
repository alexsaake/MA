using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.ClusterConvection;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class ClusterConvectionHeightMapGenerator : IClusterConvectionHeightMapGenerator
{
    private const float Radius = 0.5f;
    private const int PlateCount = 10;

    private readonly IConfiguration myConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IPoissonDiskSampler myPoissonDiskSampler;

    private List<Segment> mySegments;
    private List<Plate> myPlates;
    private HeightMap myHeatMap;

    public ClusterConvectionHeightMapGenerator(IConfiguration configuration, IRandom random, IShaderBuffers shaderBuffers, IPoissonDiskSampler poissonDiskSampler)
    {
        myConfiguration = configuration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;
        myPoissonDiskSampler = poissonDiskSampler;

        mySegments = new List<Segment>();
        myPlates = new List<Plate>();
    }

    public HeightMap GenerateHeightMap()
    {
        float[,] heightMap = new float[myConfiguration.HeightMapSideLength, myConfiguration.HeightMapSideLength];
        myHeatMap = new HeightMapGeneratorCPU(myConfiguration, myRandom, myShaderBuffers).GenerateHeightMap();

        List<Vector2> points = myPoissonDiskSampler.GeneratePoints(Radius, myConfiguration.HeightMapSideLength);
        foreach (Vector2 point in points)
        {
            Segment newSegment = new Segment(point);
            mySegments.Add(newSegment);

            for (int i = 0; i < 10; i++)
            {
                newSegment.Update(heightMap, myHeatMap);
            }
        }

        for (int i = 0; i < PlateCount; i++)
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

        return new HeightMap(myConfiguration, heightMap);
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

    public HeightMap Update()
    {
        foreach (Plate plate in myPlates)
        {
            for (int segment = 0; segment < plate.Segments.Count; segment++)
            {
                IVector2 position = new IVector2(plate.Segments[segment].Position);
                if (position.X < -myConfiguration.HeightMapSideLength || position.X > 2 * myConfiguration.HeightMapSideLength - 1 ||
                position.Y < -myConfiguration.HeightMapSideLength || position.Y > 2 * myConfiguration.HeightMapSideLength - 1)
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

        // Fill Gaps

        //foreach(Plate plate in myPlates)
        //{
        //    foreach(Segment segment in plate.Segments)
        //    {
        //        float angle = myRandom.NextFloat() * 2.0f * MathF.PI;
        //        Vector2 scan = segment.Position;
        //        scan += myConfiguration.HeightMapSideLength * Radius / 2.0f * new Vector2(MathF.Cos(angle), MathF.Sin(angle));

        //        if (scan.X < 0 || scan.X >= myConfiguration.HeightMapSideLength ||
        //        scan.Y < 0 || scan.Y >= myConfiguration.HeightMapSideLength)
        //        {
        //            continue;
        //        }

        //        Compute Color at Scan
        //        Index of the current guy in general
        //        int csind = cluster.sample(scan);

        //        if (csind < 0)
        //        {

        //            cluster.points.push_back(scan);
        //            p.seg.push_back(cluster.add(cluster.points.back()));
        //            p.seg.back()->parent = &p;
        //            cluster.reassign();
        //            plate.Recenter();
        //            break;

        //        }
        //    }
        //}

        float[,] heightMap = new float[myConfiguration.HeightMapSideLength, myConfiguration.HeightMapSideLength];
        foreach (Segment segment in mySegments)
        {
            segment.Update(heightMap, myHeatMap);
        }

        return new HeightMap(myConfiguration, heightMap);
    }

    public void Dispose()
    {
        mySegments.Clear();
        myPlates.Clear();
        myShaderBuffers.Dispose();
    }
}
