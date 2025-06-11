using Autofac;
using NUnit.Framework;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.ErosionSimulation.HydraulicErosion.Grid;

[TestFixture]
[SingleThreaded]
public class GridHydraulicErosionTests
{
    private const int AngleOfRepose = 45;

    private IContainer? myContainer;
    private IMapGenerationConfiguration? myMapGenerationConfiguration;

    private uint CenterIndex => myMapGenerationConfiguration!.GetIndex(1, 1);
    private uint LeftIndex => myMapGenerationConfiguration!.GetIndex(0, 1);
    private uint RightIndex => myMapGenerationConfiguration!.GetIndex(2, 1);
    private uint UpIndex => myMapGenerationConfiguration!.GetIndex(1, 2);
    private uint DownIndex => myMapGenerationConfiguration!.GetIndex(1, 0);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        myContainer = Container.Create();
        Raylib.InitWindow(1, 1, nameof(GridHydraulicErosionTests));
        SetUpMapGenerationConfiguration();
        SetUpRockTypesConfiguration();
        SetUpHydraulicErosionConfiguration();
    }

    [SetUp]
    public void SetUp()
    {
        SetUpErosionConfiguration(1);
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
        shaderBuffers.Remove(ShaderBufferTypes.HydraulicErosionHeightMapIndices);
    }

    [Test]
    public void VerticalFlow_Flat3x3HeightMapWithNoWater_AllWaterFlowIsZero()
    {
        InitializeConfiguration();
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer cell in gridHydraulicErosionCellsShaderBuffers)
        {
            Assert.That(cell.WaterFlowLeft, Is.Zero);
            Assert.That(cell.WaterFlowRight, Is.Zero);
            Assert.That(cell.WaterFlowUp, Is.Zero);
            Assert.That(cell.WaterFlowDown, Is.Zero);
        }
    }

    [Test]
    [TestCase(0u)]
    [TestCase(1u)]
    public void VerticalFlow_3x3HeightMapWithWaterAndBedrockInMiddle_WaterFlowIsEqualToAllFourNeighbors(uint layer)
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle();

        testee.VerticalFlow();

        float expectedFlow = 0.125f;
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex];
        Assert.That(expectedFlow,
            Is.EqualTo(centerCell.WaterFlowLeft).Within(0.0001f)
            .And.EqualTo(centerCell.WaterFlowRight).Within(0.0001f)
            .And.EqualTo(centerCell.WaterFlowDown).Within(0.0001f)
            .And.EqualTo(centerCell.WaterFlowUp).Within(0.0001f));
        Assert.That(expectedFlow,
            Is.EqualTo(centerCell.SedimentFlowLeft).Within(0.0001f)
            .And.EqualTo(centerCell.SedimentFlowRight).Within(0.0001f)
            .And.EqualTo(centerCell.SedimentFlowDown).Within(0.0001f)
            .And.EqualTo(centerCell.SedimentFlowUp).Within(0.0001f));
    }

    [Test]
    public void HorizontalSuspend__()
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithSlopedChannelLayerOne();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterInMiddle();
        testee.VerticalFlow();
        testee.VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate();

        testee.HorizontalSplit();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float[] heightMap = ReadHeightMapShaderBuffer();
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpMapGenerationConfiguration()
    {
        SetUpMapGenerationConfiguration(1u, 1u, 0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount)
    {
        SetUpMapGenerationConfiguration(layerCount, 1u, 0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint rockTypeCount, float seaLevel)
    {
        myMapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        myMapGenerationConfiguration!.HeightMapSideLength = 3;
        myMapGenerationConfiguration!.HeightMultiplier = 10;
        myMapGenerationConfiguration!.RockTypeCount = rockTypeCount;
        myMapGenerationConfiguration!.LayerCount = layerCount;
        myMapGenerationConfiguration!.SeaLevel = seaLevel;
    }

    private void SetUpErosionConfiguration(uint iterationsPerStep)
    {
        IErosionConfiguration erosionConfiguration = myContainer!.Resolve<IErosionConfiguration>();
        erosionConfiguration.IterationsPerStep = iterationsPerStep;
    }

    private void SetUpHydraulicErosionConfiguration()
    {
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        gridHydraulicErosionConfiguration.MaximalErosionDepth = 1.0f;
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
        heightMap[CenterIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithSlopedChannelLayerOne()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (uint y = 0; y < myMapGenerationConfiguration!.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < myMapGenerationConfiguration!.HeightMapSideLength; x++)
            {
                if(y == 1)
                {
                    heightMap[myMapGenerationConfiguration.GetIndex(x, y)] = x;
                }
                else
                {
                    heightMap[myMapGenerationConfiguration.GetIndex(x, y)] = myMapGenerationConfiguration!.HeightMapSideLength;
                }
            }
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle()
    {

        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        gridHydraulicErosionCellsShaderBuffer[CenterIndex].WaterHeight = 1.0f;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex].SuspendedSediment = 1.0f;
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHydraulicErosionCellsWithWaterInMiddle()
    {

        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        gridHydraulicErosionCellsShaderBuffer[CenterIndex].WaterHeight = 1.0f;
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
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
