using Autofac;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.ErosionSimulation.ThermalErosion.Grid;

[TestFixture]
[SingleThreaded]
public class GridThermalErosionTests
{
    private const int AngleOfRepose = 45;
    private const float TolerancePercentage = 0.0001f;

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
        Raylib.InitWindow(1, 1, nameof(GridThermalErosionTests));
    }

    [SetUp]
    public void SetUp()
    {
        SetUpMapGenerationConfiguration();
        SetUpRockTypesConfiguration();
        SetUpThermalErosionConfiguration();
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
        shaderBuffers.Remove(ShaderBufferTypes.GridThermalErosionCells);
    }

    [Test]
    public void VerticalFlow_Flat3x3HeightMap_AllFlowIsZero()
    {
        InitializeConfiguration();
        SetUpFlatHeightMap();
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        foreach (GridThermalErosionCellShaderBuffer cell in gridThermalErosionCells)
        {
            Assert.That(cell.SedimentFlowLeft, Is.Zero);
            Assert.That(cell.SedimentFlowRight, Is.Zero);
            Assert.That(cell.SedimentFlowUp, Is.Zero);
            Assert.That(cell.SedimentFlowDown, Is.Zero);
        }
    }

    [Test]
    public void VerticalFlow_3x3HeightMapWithBedrockInMiddle_FlowIsEqualToAllFourNeighbors()
    {
        SetUpMapGenerationConfiguration();
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddle(0u);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        float expectedFlow = 0.05f;
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex];
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));

        testee.DepositeAndCloseSplit();

        float expectedHeight = expectedFlow;
        float[] heightMap = ReadHeightMapShaderBuffer();
        float centerHeight = heightMap[CenterIndex];
        float leftHeight = heightMap[LeftIndex];
        float rightHeight = heightMap[RightIndex];
        float downHeight = heightMap[DownIndex];
        float upHeight = heightMap[UpIndex];
        Assert.That(1.0f - 4 * expectedFlow, Is.EqualTo(centerHeight));
        Assert.That(leftHeight, Is.EqualTo(expectedHeight));
        Assert.That(rightHeight, Is.EqualTo(expectedHeight));
        Assert.That(downHeight, Is.EqualTo(expectedHeight));
        Assert.That(upHeight, Is.EqualTo(expectedHeight));
    }

    [Test]
    public void Simulate_HeightMapWithGivenSizeLayersIterationsAndRockTypesInMiddle_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength, [Values(1u, 2u, 3u)] uint rockTypeCount, [Values(0u, 1u)] uint layers, [Values(1u, 100u, 10000u)] uint iterations)
    {
        SetUpErosionConfiguration(iterations);
        uint layerCount = layers + 1u;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypesInMiddle(layers, rockTypeCount);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = startHeightMap.Sum(cell => cell);

        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        float endVolume = endHeightMap.Sum(cell => cell);

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0));
        Assert.That(startVolume, Is.EqualTo(endVolume).Within(endVolume * TolerancePercentage));
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpMapGenerationConfiguration()
    {
        SetUpMapGenerationConfiguration(1u, 1u);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint rockType)
    {
        SetUpMapGenerationConfiguration(layerCount, 3u, rockType);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint heightMapSideLength, uint rockTypeCount)
    {
        myMapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        myMapGenerationConfiguration!.HeightMapSideLength = heightMapSideLength;
        myMapGenerationConfiguration!.HeightMultiplier = 10;
        myMapGenerationConfiguration!.RockTypeCount = rockTypeCount;
        myMapGenerationConfiguration!.LayerCount = layerCount;
        myMapGenerationConfiguration!.SeaLevel = 0f;
    }

    private void SetUpErosionConfiguration()
    {
        SetUpErosionConfiguration(1);
    }

    private void SetUpErosionConfiguration(uint iterationsPerStep)
    {
        IErosionConfiguration erosionConfiguration = myContainer!.Resolve<IErosionConfiguration>();
        erosionConfiguration.IterationsPerStep = iterationsPerStep;
    }

    private void SetUpThermalErosionConfiguration()
    {
        IThermalErosionConfiguration thermalErosionConfiguration = myContainer!.Resolve<IThermalErosionConfiguration>();
        thermalErosionConfiguration.ErosionRate = 0.2f;
    }

    private void SetUpRockTypesConfiguration()
    {
        IRockTypesConfiguration rockTypesConfiguration = myContainer!.Resolve<IRockTypesConfiguration>();
        rockTypesConfiguration.BedrockAngleOfRepose = AngleOfRepose;
    }

    private void SetUpFlatHeightMap()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
    }

    private unsafe void SetUpHeightMapWithBedrockInMiddle(uint layer)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        heightMap[CenterIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        if (layer > 0)
        {
            heightMap[CenterIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithRockTypesInMiddle(uint layer, uint rockTypes)
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
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
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
