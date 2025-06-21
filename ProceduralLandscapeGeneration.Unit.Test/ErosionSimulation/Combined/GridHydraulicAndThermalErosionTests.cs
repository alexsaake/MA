using Autofac;
using NUnit.Framework;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.ErosionSimulation.Combined;

[TestFixture]
[SingleThreaded]
public class GridHydraulicAndThermalErosionTests
{
    private const int AngleOfRepose = 45;
    private const float TolerancePercentage = 0.00001f;

    private IContainer? myContainer;
    private IMapGenerationConfiguration? myMapGenerationConfiguration;

    private uint mySideLength => myMapGenerationConfiguration!.HeightMapSideLength - 1;
    private uint CenterIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f), (uint)(mySideLength / 2.0f));
    private uint LeftIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f) - 1, (uint)(mySideLength / 2.0f));
    private uint RightIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f) + 1, (uint)(mySideLength / 2.0f));
    private uint UpIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f), (uint)(mySideLength / 2.0f) + 1);
    private uint DownIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f), (uint)(mySideLength / 2.0f) - 1);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        myContainer = Container.Create();
        Raylib.InitWindow(1, 1, nameof(GridHydraulicAndThermalErosionTests));
    }

    [SetUp]
    public void SetUp()
    {
        SetUpMapGenerationConfiguration();
        SetUpRockTypesConfiguration();
        SetUpHydraulicErosionConfiguration();
        SetUpErosionConfiguration();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Raylib.CloseWindow();
        myContainer!.Dispose();
    }

    [TearDown]
    public void TearDown()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        shaderBuffers.Remove(ShaderBufferTypes.HeightMap);
        shaderBuffers.Remove(ShaderBufferTypes.GridHydraulicErosionCell);
        shaderBuffers.Remove(ShaderBufferTypes.GridThermalErosionCells);
        shaderBuffers.Remove(ShaderBufferTypes.HydraulicErosionHeightMapIndices);
    }

    [Test]
    public void Simulate_LayerOneHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddleAndHorizontalErosionWithRain_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                                        [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                                        [Values(1u, 100u, 10000u)] uint iterations,
                                                                                                                                        [Values(0.0f, 1.0f, 2.0f)] float seaLevel)
    {
        SetUpErosionConfiguration(iterations, true);
        SetUpHydraulicErosionConfiguration(true);
        uint layer = 1;
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount, seaLevel);
        InitializeConfiguration();
        SetUpNoneFloatingHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridHydraulicErosion gridHydraulicErosionTestee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        GridThermalErosion gridThermalErosionTestee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        gridHydraulicErosionTestee.Initialize();
        gridThermalErosionTestee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        gridHydraulicErosionTestee.Simulate();
        gridThermalErosionTestee.Simulate();

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = SumUpVolume(intermediateHeightMap) + intermediateSuspendedSediment;

        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f, true);
        gridHydraulicErosionTestee.Simulate();
        gridThermalErosionTestee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float endVolume = SumUpVolume(endHeightMap) + endSuspendedSediment;

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        if (seaLevel == 0)
        {
            Assert.That(endSuspendedSediment, Is.Zero);
        }
    }

    private float SumUpVolume(float[] heightMap)
    {
        if (myMapGenerationConfiguration!.LayerCount == 1)
        {
            return heightMap.Sum(cell => cell);
        }
        float floorHeights = heightMap.Where((_, index) => index >= myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize && index < myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize + myMapGenerationConfiguration!.HeightMapPlaneSize).Sum(cell => cell);
        return heightMap.Sum(cell => cell) - floorHeights;
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpMapGenerationConfiguration()
    {
        SetUpMapGenerationConfiguration(1u, 3u, 1u, 0.0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint heightMapSideLength, uint rockTypeCount, float seaLevel)
    {
        myMapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        myMapGenerationConfiguration!.HeightMapSideLength = heightMapSideLength;
        myMapGenerationConfiguration!.HeightMultiplier = 10;
        myMapGenerationConfiguration!.RockTypeCount = rockTypeCount;
        myMapGenerationConfiguration!.LayerCount = layerCount;
        myMapGenerationConfiguration!.SeaLevel = seaLevel;
    }

    private void SetUpErosionConfiguration()
    {
        SetUpErosionConfiguration(1, false);
    }

    private void SetUpErosionConfiguration(uint iterationsPerStep, bool isWaterAdded)
    {
        IErosionConfiguration erosionConfiguration = myContainer!.Resolve<IErosionConfiguration>();
        erosionConfiguration.IterationsPerStep = iterationsPerStep;
        erosionConfiguration.IsWaterKeptInBoundaries = true;
        erosionConfiguration.IsWaterAdded = isWaterAdded;
    }

    private void SetUpHydraulicErosionConfiguration()
    {
        SetUpHydraulicErosionConfiguration(0.0f, false);
    }

    private void SetUpHydraulicErosionConfiguration(bool isHorizontalErosionEnabled)
    {
        SetUpHydraulicErosionConfiguration(0.0f, isHorizontalErosionEnabled);
    }

    private void SetUpHydraulicErosionConfiguration(float evaporationRate, bool isHorizontalErosionEnabled)
    {
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        gridHydraulicErosionConfiguration.MaximalErosionDepth = float.MaxValue;
        gridHydraulicErosionConfiguration.EvaporationRate = evaporationRate;
        gridHydraulicErosionConfiguration.IsHorizontalErosionEnabled = isHorizontalErosionEnabled;
    }

    private void SetUpRockTypesConfiguration()
    {
        IRockTypesConfiguration rockTypesConfiguration = myContainer!.Resolve<IRockTypesConfiguration>();
        rockTypesConfiguration.BedrockCollapseThreshold = float.MaxValue;
    }

    private unsafe void SetUpNoneFloatingHeightMapWithRockTypesInMiddle(uint layer, uint rockTypes)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (int rockType = 0; rockType < rockTypes; rockType++)
        {
            heightMap[CenterIndex + rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        if (layer > 0)
        {
            heightMap[CenterIndex + layer * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        heightMap[LeftIndex] = 2.0f;
        heightMap[DownIndex] = 2.0f;
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe GridHydraulicErosionCellShaderBuffer[] ReadGridHydraulicErosionCellShaderBuffer()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
        }
        return gridHydraulicErosionCellsShaderBuffer;
    }

    private unsafe GridThermalErosionCellShaderBuffer[] ReadGridThermalErosionCellShaderBuffer()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IThermalErosionConfiguration thermalErosionConfiguration = myContainer!.Resolve<IThermalErosionConfiguration>();
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCellsShaderBuffer = new GridThermalErosionCellShaderBuffer[thermalErosionConfiguration.GridCellsSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridThermalErosionCellsShaderBufferPointer = gridThermalErosionCellsShaderBuffer)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridThermalErosionCells], gridThermalErosionCellsShaderBufferPointer, (uint)(thermalErosionConfiguration.GridCellsSize * sizeof(GridThermalErosionCellShaderBuffer)), 0);
        }
        return gridThermalErosionCellsShaderBuffer;
    }

    private unsafe float[] ReadHeightMapShaderBuffer()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMapValues = new float[myMapGenerationConfiguration!.HeightMapSize];
        Rlgl.MemoryBarrier();
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        return heightMapValues;
    }
}
