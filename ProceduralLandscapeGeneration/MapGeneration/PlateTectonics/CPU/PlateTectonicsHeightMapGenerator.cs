using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.MapGeneration.PlateTectonics.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.MapGeneration.PlateTectonics.CPU;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class PlateTectonicsHeightMapGenerator : IPlateTectonicsHeightMapGenerator
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly ILifetimeScope myLifetimeScope;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

    private IComputeShaderProgram? myAddSegmentsToNearestPlateComputeShaderProgram;
    private IComputeShaderProgram? myRecenterPlateComputeShaderProgram;

    private Segment?[]? mySegments;
    private bool myIsDispoed;
    private readonly List<Plate> myPlates;

    public event EventHandler? PlateTectonicsIterationFinished;

    public PlateTectonicsHeightMapGenerator(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IRandom random, IShaderBuffers shaderBuffers, ILifetimeScope lifetimeScope, IComputeShaderProgramFactory computeShaderProgramFactory)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;
        myLifetimeScope = lifetimeScope;
        myComputeShaderProgramFactory = computeShaderProgramFactory;

        myPlates = new List<Plate>();
    }

    public void Initialize()
    {
        myAddSegmentsToNearestPlateComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("HeightMapGeneration/PlateTectonics/Shaders/AddSegmentsToNearestPlateComputeShader.glsl");
        myRecenterPlateComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("HeightMapGeneration/PlateTectonics/Shaders/RecenterPlateComputeShader.glsl");

        myIsDispoed = false;
    }

    public void GenerateHeightMap()
    {
        IHeightMapGenerator heightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myMapGenerationConfiguration.HeightMapGeneration);
        heightMapGenerator.GenerateNoiseHeatMap();

        CreateSegments();
        CreatePlates();
        AddSegmentsToNearesPlate();
        CreateEmptyHeightMapShaderBuffer();
    }

    private unsafe void CreateSegments()
    {
        uint plateTectonicsSegmentsSize = (uint)(myMapGenerationConfiguration.MapSize * sizeof(PlateTectonicsSegmentShaderBuffer));
        myShaderBuffers.Add(ShaderBufferTypes.PlateTectonicsSegments, plateTectonicsSegmentsSize);
    }

    private unsafe void CreatePlates()
    {
        uint plateTectonicsPlatesSize = (uint)(myMapGenerationConfiguration.PlateCount * sizeof(PlateTectonicsPlateShaderBuffer));
        myShaderBuffers.Add(ShaderBufferTypes.PlateTectonicsPlates, plateTectonicsPlatesSize);
        PlateTectonicsPlateShaderBuffer[] plateTectonicsPlates = new PlateTectonicsPlateShaderBuffer[myMapGenerationConfiguration.PlateCount];
        for (int plate = 0; plate < myMapGenerationConfiguration.PlateCount; plate++)
        {
            plateTectonicsPlates[plate].Position = new Vector2(myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength), myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength));
        }
        fixed (void* plateTectonicsPlatesPointer = plateTectonicsPlates)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.PlateTectonicsPlates], plateTectonicsPlatesPointer, plateTectonicsPlatesSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    private void AddSegmentsToNearesPlate()
    {
        Rlgl.EnableShader(myAddSegmentsToNearestPlateComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();

        RecenterPlate();
    }

    private void RecenterPlate()
    {
        Rlgl.EnableShader(myRecenterPlateComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.PlateCount / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void CreateEmptyHeightMapShaderBuffer()
    {
        uint heightMapSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
    }

    public void SimulatePlateTectonics()
    {
        Console.WriteLine($"INFO: Simulating plate tectonics.");

        MarkOutOfBoundsSegmentsAsDeadAndRemoveFromPlates();
        RemoveEmptyPlates();
        RemoveDeadSegmentsFromHeightMap();
        MovePlates();
        FloatSegments();
        FillSegmentGaps();
        UpdateHeightMap();

        PlateTectonicsIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of plate tectonics simulation.");
    }

    private void MarkOutOfBoundsSegmentsAsDeadAndRemoveFromPlates()
    {
        foreach (Plate plate in myPlates)
        {
            for (int segment = 0; segment < plate.Segments.Count; segment++)
            {
                IntVector2 position = new IntVector2(plate.Segments[segment].Position);
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

        uint heatMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        float[] heatMap = new float[myMapGenerationConfiguration.MapSize];
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

                IntVector2 scanIntegerPosition = new IntVector2(scanPosition);
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
        uint heightMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        float[] heightMap = new float[myMapGenerationConfiguration.MapSize];
        for (int segment = 0; segment < mySegments!.Length; segment++)
        {
            if (mySegments![segment] is null)
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
        if (myIsDispoed)
        {
            return;
        }

        myAddSegmentsToNearestPlateComputeShaderProgram?.Dispose();
        myRecenterPlateComputeShaderProgram?.Dispose();

        mySegments = null;
        myPlates.Clear();

        myIsDispoed = true;
    }
}
