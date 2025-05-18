using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.MapGeneration.PlateTectonics.GPU;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class PlateTectonicsHeightMapGenerator : IPlateTectonicsHeightMapGenerator
{
    private const string ShaderDirectory = "MapGeneration/PlateTectonics/GPU/Shaders/";

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly ILifetimeScope myLifetimeScope;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

    private IComputeShaderProgram? myAddSegmentsToNearestPlateComputeShaderProgram;
    private IComputeShaderProgram? myPrepareRecenterPlatesComputeShaderProgram;
    private IComputeShaderProgram? myAddPlateSegmentsComputeShaderProgram;
    private IComputeShaderProgram? myRecenterPlatesPositionComputeShaderProgram;
    private IComputeShaderProgram? myCollideSegmentsComputeShaderProgram;
    private IComputeShaderProgram? myCascadeSegmentsComputeShaderProgram;
    private IComputeShaderProgram? myGrowSegmentsComputeShaderProgram;
    private IComputeShaderProgram? myUpdatePlatesAccelerationAndTorqueComputeShaderProgram;
    private IComputeShaderProgram? myUpdatePlatesPositionComputeShaderProgram;
    private IComputeShaderProgram? myUpdateTempSegmentsPositionComputeShaderProgram;
    private IComputeShaderProgram? myCopyTempSegmentsToSegmentsComputeShaderProgram;
    private IComputeShaderProgram? myFloatSegmentsComputeShaderProgram;
    private IComputeShaderProgram? myFillSegmentsGapComputeShaderProgram;
    private IComputeShaderProgram? myUpdateHeightMapComputeShaderProgram;

    private bool myIsDisposed;

    public PlateTectonicsHeightMapGenerator(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IRandom random, IShaderBuffers shaderBuffers, ILifetimeScope lifetimeScope, IComputeShaderProgramFactory computeShaderProgramFactory)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;
        myLifetimeScope = lifetimeScope;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
    }

    public void Initialize()
    {
        myAddSegmentsToNearestPlateComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}AddSegmentsToNearestPlateComputeShader.glsl");
        myPrepareRecenterPlatesComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}PrepareRecenterPlatesComputeShader.glsl");
        myAddPlateSegmentsComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}AddPlateSegmentsComputeShader.glsl");
        myRecenterPlatesPositionComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}RecenterPlatesPositionComputeShader.glsl");
        myCollideSegmentsComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}CollideSegmentsComputeShader.glsl");
        myCascadeSegmentsComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}CascadeSegmentsComputeShader.glsl");
        myGrowSegmentsComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}GrowSegmentsComputeShader.glsl");
        myUpdatePlatesAccelerationAndTorqueComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}UpdatePlatesAccelerationAndTorqueComputeShader.glsl");
        myUpdatePlatesPositionComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}UpdatePlatesPositionComputeShader.glsl");
        myUpdateTempSegmentsPositionComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}UpdateTempSegmentsPositionComputeShader.glsl");
        myCopyTempSegmentsToSegmentsComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}CopyTempSegmentsToSegmentsComputeShader.glsl");
        myFloatSegmentsComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}FloatSegmentsComputeShader.glsl");
        myFillSegmentsGapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}FillSegmentsGapComputeShader.glsl");
        myUpdateHeightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}UpdateHeightMapComputeShader.glsl");

        AddPlateTectonicsSegmentsShaderBuffer();
        AddPlateTectonicsPlatesShaderBuffer();
        AddHeightMapShaderBuffer();
        AddPlateTectonicsTempSegmentsShaderBuffer();

        AddSegmentsToNearesPlate();
        GenerateHeatMap();

        myIsDisposed = false;
    }

    private unsafe void AddPlateTectonicsSegmentsShaderBuffer()
    {
        uint plateTectonicsSegmentsSize = (uint)(myMapGenerationConfiguration.MapSize * sizeof(PlateTectonicsSegmentShaderBuffer));
        myShaderBuffers.Add(ShaderBufferTypes.PlateTectonicsSegments, plateTectonicsSegmentsSize);
    }

    private unsafe void AddPlateTectonicsPlatesShaderBuffer()
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

    private void AddHeightMapShaderBuffer()
    {
        uint heightMapSize = myMapGenerationConfiguration.MapSize * sizeof(float) * GetLayerCount();
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
    }

    private uint GetLayerCount()
    {
        switch (myMapGenerationConfiguration.MapType)
        {
            case MapTypes.MultiLayeredHeightMap:
                return 2;
            default:
                return 1;
        }
    }

    private unsafe void AddPlateTectonicsTempSegmentsShaderBuffer()
    {
        uint plateTectonicsTempSegmentsSize = (uint)(myMapGenerationConfiguration.MapSize * sizeof(PlateTectonicsSegmentShaderBuffer));
        myShaderBuffers.Add(ShaderBufferTypes.PlateTectonicsTempSegments, plateTectonicsTempSegmentsSize);
    }

    private void AddSegmentsToNearesPlate()
    {
        Rlgl.EnableShader(myAddSegmentsToNearestPlateComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();

        RecenterPlates();
    }

    private void RecenterPlates()
    {
        PrepareRecenterPlates();
        AddPlateSegments();
        RecenterPlatesPosition();
    }

    private void PrepareRecenterPlates()
    {
        Rlgl.EnableShader(myPrepareRecenterPlatesComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.PlateCount / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void AddPlateSegments()
    {
        Rlgl.EnableShader(myAddPlateSegmentsComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void RecenterPlatesPosition()
    {
        Rlgl.EnableShader(myRecenterPlatesPositionComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.PlateCount / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void GenerateHeatMap()
    {
        IHeightMapGenerator heightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myMapGenerationConfiguration.HeightMapGeneration);
        heightMapGenerator.GenerateNoiseHeatMap();
    }

    public unsafe void SimulatePlateTectonics()
    {
        Console.WriteLine($"INFO: Simulating plate tectonics.");

        MovePlates();
        FloatSegments();
        FillSegmentsGap();
        UpdateHeightMap();

        PlateTectonicsSegmentShaderBuffer[] plateTectonicsSegments = new PlateTectonicsSegmentShaderBuffer[myMapGenerationConfiguration.MapSize];
        uint plateTectonicsSegmentsSize = (uint)(myMapGenerationConfiguration.MapSize * sizeof(PlateTectonicsSegmentShaderBuffer));
        PlateTectonicsPlateShaderBuffer[] plateTectonicsPlate = new PlateTectonicsPlateShaderBuffer[myMapGenerationConfiguration.PlateCount];
        uint plateTectonicsPlateSize = (uint)(myMapGenerationConfiguration.PlateCount * sizeof(PlateTectonicsPlateShaderBuffer));
        fixed (void* plateTectonicsSegmentsPointer = plateTectonicsSegments)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.PlateTectonicsSegments], plateTectonicsSegmentsPointer, plateTectonicsSegmentsSize, 0);
        }
        fixed (void* plateTectonicsPlatePointer = plateTectonicsPlate)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.PlateTectonicsPlates], plateTectonicsPlatePointer, plateTectonicsPlateSize, 0);
        }


        Console.WriteLine($"INFO: End of plate tectonics simulation.");
    }

    private void MovePlates()
    {
        CollideSegments();
        CascadeSegments();
        GrowSegments();
        MoveSegments();
    }

    private void CollideSegments()
    {
        Rlgl.EnableShader(myCollideSegmentsComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();

        RecenterPlates();
    }

    private void CascadeSegments()
    {
        Rlgl.EnableShader(myCascadeSegmentsComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void GrowSegments()
    {
        Rlgl.EnableShader(myGrowSegmentsComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void MoveSegments()
    {
        UpdatePlatesAccelerationAndTorque();
        UpdatePlatesPosition();
        UpdateSegmentsPosition();
    }

    private void UpdatePlatesAccelerationAndTorque()
    {
        Rlgl.EnableShader(myUpdatePlatesAccelerationAndTorqueComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void UpdatePlatesPosition()
    {
        Rlgl.EnableShader(myUpdatePlatesPositionComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.PlateCount / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void UpdateSegmentsPosition()
    {
        UpdateTempSegmentsPosition();
        CopyTempSegmentsToSegments();
    }

    private void UpdateTempSegmentsPosition()
    {
        Rlgl.EnableShader(myUpdateTempSegmentsPositionComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void CopyTempSegmentsToSegments()
    {
        Rlgl.EnableShader(myCopyTempSegmentsToSegmentsComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void FloatSegments()
    {
        Rlgl.EnableShader(myFloatSegmentsComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private void FillSegmentsGap()
    {
        Rlgl.EnableShader(myFillSegmentsGapComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();

        RecenterPlates();
    }

    private void UpdateHeightMap()
    {
        Rlgl.EnableShader(myUpdateHeightMapComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myAddSegmentsToNearestPlateComputeShaderProgram?.Dispose();
        myPrepareRecenterPlatesComputeShaderProgram?.Dispose();
        myAddPlateSegmentsComputeShaderProgram?.Dispose();
        myRecenterPlatesPositionComputeShaderProgram?.Dispose();
        myCollideSegmentsComputeShaderProgram?.Dispose();
        myCascadeSegmentsComputeShaderProgram?.Dispose();
        myGrowSegmentsComputeShaderProgram?.Dispose();
        myUpdatePlatesAccelerationAndTorqueComputeShaderProgram?.Dispose();
        myUpdatePlatesPositionComputeShaderProgram?.Dispose();
        myUpdateTempSegmentsPositionComputeShaderProgram?.Dispose();
        myCopyTempSegmentsToSegmentsComputeShaderProgram?.Dispose();
        myFloatSegmentsComputeShaderProgram?.Dispose();
        myFillSegmentsGapComputeShaderProgram?.Dispose();
        myUpdateHeightMapComputeShaderProgram?.Dispose();

        RemovePlateTectonicsSegmentsShaderBuffer();
        RemovePlateTectonicsPlatesShaderBuffer();
        RemoveHeightMapShaderBuffer();
        RemovePlateTectonicsTempSegmentsShaderBuffer();

        myIsDisposed = true;
    }

    private void RemovePlateTectonicsSegmentsShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.PlateTectonicsSegments);
    }

    private void RemovePlateTectonicsPlatesShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.PlateTectonicsPlates);
    }

    private void RemoveHeightMapShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.HeightMap);
    }

    private void RemovePlateTectonicsTempSegmentsShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.PlateTectonicsTempSegments);
    }
}
