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
    private const float TolerancePercentage = 0.001f;

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
    public void VerticalFlow_3x3HeightMapWithRockTypeInMiddle_FlowIsEqualToAllFourNeighbors([Values(0u, 1u)] uint layer,
                                                                                        [Values(0u, 1u, 2u)] uint rockType)
    {
        uint layerCount = layer + 1;
        uint rockTypeCount = rockType + 1;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypeInMiddle(layer, rockType);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        float expectedFlow = 0.05f;
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        uint gridThermalErosionCellsOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + layer * myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex + gridThermalErosionCellsOffset];
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow));
    }

    [Test]
    public void VerticalFlow_3x3HeightMapWithRockTypesInMiddle_FlowIsEqualToAllFourNeighbors([Values(0u, 1u)] uint layer,
                                                                                        [Values(1u, 2u, 3u)] uint rockTypeCount)
    {
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        float expectedFlow = 0.05f;
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        for (uint rockType = 0; rockType < rockTypeCount; rockType++)
        {
            uint gridThermalErosionCellsOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + layer * myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize;
            GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex + gridThermalErosionCellsOffset];
            Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
            Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
            Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
            Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        }
    }

    [Test]
    public void VerticalFlow_3x3HeightMapWithRockTypesInMiddleSurroundedByBedrockWithGapAndSplitSize_FlowIsEqualToAllFourNeighbors([Values(0u, 1u)] uint centerLayer,
                                                                                                                                [Values(1u, 2u, 3u)] uint rockTypeCount)
    {
        uint layer = 1;
        uint layerCount = layer + 1;
        float aboveLayerFloorHeight = 1.0f + rockTypeCount - 0.5f;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypesInMiddleSurroundedByBedrockWithGapAndAboveLayerFloorHeight(layer, centerLayer, rockTypeCount, aboveLayerFloorHeight);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        float expectedFlow = 0.05f;
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        for (uint rockType = 0; rockType < rockTypeCount; rockType++)
        {
            uint gridThermalErosionCellsOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + centerLayer * myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize;
            GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex + gridThermalErosionCellsOffset];
            Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
            Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
            Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
            Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        }
    }

    [Test]
    public void VerticalFlow_3x3HeightMapWithRockTypeInMiddleSurroundedByBedrockWithoutGap_FlowIsZeroToAllFourNeighbors([Values(0u, 1u, 2u)] uint rockType)
    {
        uint layer = 1;
        uint layerCount = layer + 1;
        uint rockTypeCount = rockType + 1;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypeInMiddleSurroundedByBedrockWithoutGap(layer, rockType);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        uint gridThermalErosionCellsOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + layer * myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex + gridThermalErosionCellsOffset];
        Assert.That(centerCell.SedimentFlowLeft, Is.Zero);
        Assert.That(centerCell.SedimentFlowRight, Is.Zero);
        Assert.That(centerCell.SedimentFlowDown, Is.Zero);
        Assert.That(centerCell.SedimentFlowUp, Is.Zero);
    }

    [Test]
    public void VerticalFlow_3x3HeightMapWithRockTypeInMiddleSurroundedByBedrockWitGap_FlowIsEqualToAllFourNeighbors([Values(0u, 1u, 2u)] uint rockType)
    {
        uint layer = 1;
        uint layerCount = layer + 1;
        uint rockTypeCount = rockType + 1;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypeInMiddleSurroundedByBedrockWithGap(layer, rockType);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.VerticalFlow();
        float[] heightMap = ReadHeightMapShaderBuffer();

        float expectedFlow = 0.05f;
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        uint gridThermalErosionCellsOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + layer * myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex + gridThermalErosionCellsOffset];
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow));
    }

    [Test]
    public void LimitVerticalInflow_3x3HeightMapWithRockTypeInMiddle_FlowIsEqualToAllFourNeighbors([Values(0u, 1u, 2u)] uint rockType,
                                                                                                [Values(0.0f, 0.01f, 0.02f)] float splitSize)
    {
        uint layer = 1;
        uint layerCount = layer + 1;
        uint rockTypeCount = rockType + 1;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypeInMiddleSurroundedByBedrockWithGapAndSplitSize(layer, rockType, splitSize);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();
        testee.VerticalFlow();

        testee.LimitVerticalInflow();

        float expectedFlow = splitSize;
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        uint gridThermalErosionCellsOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + layer * myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex + gridThermalErosionCellsOffset];
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
    }

    [Test]
    public void DepositeAndCloseSplit_3x3HeightMapWithRockTypeInMiddle_FlowIsEqualToAllFourNeighbors([Values(0u, 1u)] uint layer,
                                                                                        [Values(0u, 1u, 2u)] uint rockType)
    {
        uint layerCount = layer + 1;
        uint rockTypeCount = rockType + 1;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypeInMiddle(layer, rockType);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();
        testee.VerticalFlow();

        testee.DepositeAndCloseSplit();

        float expectedHeight = 0.05f;
        float[] heightMap = ReadHeightMapShaderBuffer();
        uint rockTypeOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize;
        uint heightMapOffset = rockTypeOffset + (layer * myMapGenerationConfiguration!.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        float centerHeight = heightMap[CenterIndex + heightMapOffset];
        float leftHeight = heightMap[LeftIndex + rockTypeOffset];
        float rightHeight = heightMap[RightIndex + rockTypeOffset];
        float downHeight = heightMap[DownIndex + rockTypeOffset];
        float upHeight = heightMap[UpIndex + rockTypeOffset];
        Assert.That(centerHeight, Is.EqualTo(1.0f - 4 * expectedHeight));
        Assert.That(leftHeight, Is.EqualTo(expectedHeight));
        Assert.That(rightHeight, Is.EqualTo(expectedHeight));
        Assert.That(downHeight, Is.EqualTo(expectedHeight));
        Assert.That(upHeight, Is.EqualTo(expectedHeight));
    }

    [Test]
    public void Simulate_HeightMapWithGivenSizeLayersIterationsAndRockTypesInMiddle_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                    [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                    [Values(0u, 1u)] uint layer,
                                                                                                    [Values(1u, 100u, 10000u)] uint iterations)
    {
        SetUpErosionConfiguration(iterations);
        uint layerCount = layer + 1u;
        SetUpMapGenerationConfiguration(layerCount, rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = startHeightMap.Sum(cell => cell);

        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        float endVolume = endHeightMap.Sum(cell => cell);

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
    }

    [Test]
    public void Simulate_LayerOneHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddleAndHorizontalErosion_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                                    [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                                    [Values(0u, 1u)] uint layer,
                                                                                                                                    [Values(1u, 100u, 10000u)] uint iterations)
    {
        SetUpErosionConfiguration(iterations);
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount);
        InitializeConfiguration();
        SetUpNoneFloatingHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridThermalErosion gridThermalErosionTestee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        gridThermalErosionTestee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        gridThermalErosionTestee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridThermalErosionCellShaderBuffer[] endGridThermalErosionCellsShaderBuffers = ReadGridThermalErosionCellShaderBuffer();
        float endVolume = SumUpVolume(endHeightMap);

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
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
        SetUpMapGenerationConfiguration(1u, 1u);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint rockTypeCount)
    {
        SetUpMapGenerationConfiguration(layerCount, 3u, rockTypeCount);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint heightMapSideLength, uint rockTypeCount)
    {
        myMapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        myMapGenerationConfiguration!.HeightMapSideLength = heightMapSideLength;
        myMapGenerationConfiguration!.HeightMultiplier = 10;
        myMapGenerationConfiguration!.RockTypeCount = rockTypeCount;
        myMapGenerationConfiguration!.LayerCount = layerCount;
        myMapGenerationConfiguration!.SeaLevel = 0.0f;
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
        rockTypesConfiguration.CoarseSedimentAngleOfRepose = AngleOfRepose;
        rockTypesConfiguration.FineSedimentAngleOfRepose = AngleOfRepose;
    }

    private void SetUpFlatHeightMap()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
    }

    private unsafe void SetUpHeightMapWithRockTypesInMiddle(uint layer, uint rockTypeCount)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (int rockType = 0; rockType < rockTypeCount; rockType++)
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

    private unsafe void SetUpHeightMapWithRockTypeInMiddle(uint layer, uint rockType)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        heightMap[CenterIndex + rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
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

    private unsafe void SetUpHeightMapWithRockTypeInMiddleSurroundedByBedrockWithoutGap(uint layer, uint rockType)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        uint rockTypeOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize;
        uint layerOffset = (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        heightMap[CenterIndex + rockTypeOffset + layerOffset] = 1.0f;
        heightMap[LeftIndex + layerOffset] = 2.0f;
        heightMap[RightIndex + layerOffset] = 2.0f;
        heightMap[UpIndex + layerOffset] = 2.0f;
        heightMap[DownIndex + layerOffset] = 2.0f;
        if (layer > 0)
        {
            uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + layerFloorOffset] = 1.0f;
            heightMap[LeftIndex + layerFloorOffset] = 1.0f;
            heightMap[RightIndex + layerFloorOffset] = 1.0f;
            heightMap[UpIndex + layerFloorOffset] = 1.0f;
            heightMap[DownIndex + layerFloorOffset] = 1.0f;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithRockTypeInMiddleSurroundedByBedrockWithGap(uint layer, uint rockType)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        uint rockTypeOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize;
        uint layerOffset = (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        heightMap[CenterIndex + rockTypeOffset + layerOffset] = 1.0f;
        heightMap[LeftIndex + layerOffset] = 2.0f;
        heightMap[RightIndex + layerOffset] = 2.0f;
        heightMap[UpIndex + layerOffset] = 2.0f;
        heightMap[DownIndex + layerOffset] = 2.0f;
        if (layer > 0)
        {
            uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + layerFloorOffset] = 1.0f;
            heightMap[LeftIndex + layerFloorOffset] = 1.1f;
            heightMap[RightIndex + layerFloorOffset] = 1.1f;
            heightMap[UpIndex + layerFloorOffset] = 1.1f;
            heightMap[DownIndex + layerFloorOffset] = 1.1f;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithRockTypeInMiddleSurroundedByBedrockWithGapAndSplitSize(uint layer, uint rockType, float splitSize)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        uint rockTypeOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize;
        uint layerOffset = (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        heightMap[CenterIndex + rockTypeOffset + layerOffset] = 1.0f;
        heightMap[LeftIndex + layerOffset] = 2.0f;
        heightMap[RightIndex + layerOffset] = 2.0f;
        heightMap[UpIndex + layerOffset] = 2.0f;
        heightMap[DownIndex + layerOffset] = 2.0f;
        float aboveLayerFloorHeight = 1.1f;
        heightMap[LeftIndex] = aboveLayerFloorHeight - splitSize;
        heightMap[RightIndex] = aboveLayerFloorHeight - splitSize;
        heightMap[UpIndex] = aboveLayerFloorHeight - splitSize;
        heightMap[DownIndex] = aboveLayerFloorHeight - splitSize;
        if (layer > 0)
        {
            uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + layerFloorOffset] = 1.0f;
            heightMap[LeftIndex + layerFloorOffset] = aboveLayerFloorHeight;
            heightMap[RightIndex + layerFloorOffset] = aboveLayerFloorHeight;
            heightMap[UpIndex + layerFloorOffset] = aboveLayerFloorHeight;
            heightMap[DownIndex + layerFloorOffset] = aboveLayerFloorHeight;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithRockTypesInMiddleSurroundedByBedrockWithGapAndAboveLayerFloorHeight(uint layer, uint centerLayer, uint rockTypeCount, float aboveLayerFloorHeight)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        uint layerOffset = (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        uint centerLayerOffset = (centerLayer * myMapGenerationConfiguration.RockTypeCount + centerLayer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        for (uint rockType = 0; rockType < rockTypeCount; rockType++)
        {
            uint rockTypeOffset = rockType * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + rockTypeOffset + centerLayerOffset] = 1.0f;
        }
        heightMap[LeftIndex + layerOffset] = 2.0f;
        heightMap[RightIndex + layerOffset] = 2.0f;
        heightMap[UpIndex + layerOffset] = 2.0f;
        heightMap[DownIndex + layerOffset] = 2.0f;
        if (centerLayer > 0)
        {
            uint centerLayerFloorOffset = (centerLayer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + centerLayerFloorOffset] = 1.0f;
        }
        if (layer > 0)
        {
            uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[LeftIndex + layerFloorOffset] = aboveLayerFloorHeight;
            heightMap[RightIndex + layerFloorOffset] = aboveLayerFloorHeight;
            heightMap[UpIndex + layerFloorOffset] = aboveLayerFloorHeight;
            heightMap[DownIndex + layerFloorOffset] = aboveLayerFloorHeight;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpNoneFloatingHeightMapWithRockTypesInMiddle(uint layer, uint rockTypeCount)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (int rockType = 0; rockType < rockTypeCount; rockType++)
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
