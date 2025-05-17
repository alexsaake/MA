using Autofac;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.ErosionSimulation.HydraulicErosion.Grid;

[TestFixture]
[SingleThreaded]
public class GridThermalErosionTests
{
    private IContainer? myContainer;

    [SetUp]
    public void SetUp()
    {
        myContainer = Container.Create();
        Raylib.InitWindow(1, 1, nameof(GridThermalErosionTests));
    }

    [TearDown]
    public void TearDown()
    {
        Raylib.CloseWindow();
        myContainer!.Dispose();
    }

    [Test]
    public void Flow_Flat3x3HeightMap_AllFlowIsZero()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.Flow();

        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        foreach (GridThermalErosionCellShaderBuffer cell in gridThermalErosionCells)
        {
            Assert.That(cell.FlowLeft, Is.Zero);
            Assert.That(cell.FlowRight, Is.Zero);
            Assert.That(cell.FlowUp, Is.Zero);
            Assert.That(cell.FlowDown, Is.Zero);
        }
    }

    [Test]
    public void Flow_3x3HeightMapWithSandInMiddle_FlowIsEqualToAllSides()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpHeightMapWithSandInMiddle();
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        while (true)
        {
            testee.Flow();

            GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
            float[] heightMap = ReadHeightMapShaderBuffer();

            testee.Deposite();
        }
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpMapGenerationConfiguration(uint heightMapSideLength)
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        mapGenerationConfiguration.HeightMapSideLength = heightMapSideLength;
    }

    private void SetUpFlatHeightMap()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
    }

    private unsafe void SetUpHeightMapWithSandInMiddle()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        float[] heightMap = new float[mapSize];
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
        uint middleIndex = mapGenerationConfiguration.GetIndex(1, 1);
        heightMap[middleIndex] = 1.0f;
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, heightMapSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe GridThermalErosionCellShaderBuffer[] ReadGridThermalErosionCellShaderBuffer()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint gridThermalErosionCellsSize = (uint)(mapSize * sizeof(GridThermalErosionCellShaderBuffer));
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = new GridThermalErosionCellShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridThermalErosionCellsPointer = gridThermalErosionCells)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridThermalErosionCell], gridThermalErosionCellsPointer, gridThermalErosionCellsSize, 0);
        }
        return gridThermalErosionCells;
    }

    private unsafe float[] ReadHeightMapShaderBuffer()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        float[] heightMapValues = new float[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        return heightMapValues;
    }
}
