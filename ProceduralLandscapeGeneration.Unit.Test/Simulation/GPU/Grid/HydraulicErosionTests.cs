using Moq;
using NUnit.Framework;
using ProceduralLandscapeGeneration.Simulation.CPU.Grid;
using ProceduralLandscapeGeneration.Simulation.GPU;
using ProceduralLandscapeGeneration.Simulation.GPU.Grid;
using Raylib_cs;
using Random = ProceduralLandscapeGeneration.Simulation.Random;

namespace ProceduralLandscapeGeneration.Int.Test.Simulation.GPU.Grid;

[TestFixture]
[SingleThreaded]
public class HydraulicErosionTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Raylib.InitWindow(1, 1, nameof(HydraulicErosionTests));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Raylib.CloseWindow();
    }

    [Test]
    public unsafe void FlowCalculation_Flat3x3HeightMapWithoutWater_AllFlowIsZero()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap,  heightMapSize}
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();

        testee.FlowCalculation();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
        {
            Assert.That(gridPoint.FlowLeft, Is.Zero);
            Assert.That(gridPoint.FlowRight, Is.Zero);
            Assert.That(gridPoint.FlowTop, Is.Zero);
            Assert.That(gridPoint.FlowBottom, Is.Zero);
        }
    }

    [Test]
    public unsafe void FlowCalculation_Flat3x3HeightMapWithWaterInMiddle_OutFlowIsEqualInAllDirections()
    {
        float timeDelta = 1.0f;
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);

        testee.FlowCalculation();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        GridPoint center = gridPointValues[testee.GetIndex(1, 1)];
        float expectedFlow = waterIncrease / (4 * timeDelta);
        Assert.That(center.FlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(center.FlowRight, Is.EqualTo(expectedFlow));
        Assert.That(center.FlowTop, Is.EqualTo(expectedFlow));
        Assert.That(center.FlowBottom, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void FlowCalculation_Slope3x3HeightMapWithWaterInMiddle_OutFlowIsHigherDownSlope()
    {
        float timeDelta = 1.0f;
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(1, 0)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 2)] = 1;
        heightMapValues[testee.GetIndex(2, 0)] = 2;
        heightMapValues[testee.GetIndex(2, 1)] = 2;
        heightMapValues[testee.GetIndex(2, 2)] = 2;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);

        testee.FlowCalculation();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        GridPoint center = gridPointValues[testee.GetIndex(1, 1)];
        float expectedFlow = waterIncrease / (4 * timeDelta);
        Assert.That(center.FlowLeft, Is.EqualTo(expectedFlow + 0.25f));
        Assert.That(center.FlowRight, Is.EqualTo(expectedFlow - 0.25f));
        Assert.That(center.FlowTop, Is.EqualTo(expectedFlow));
        Assert.That(center.FlowBottom, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithoutWater_AllVelocityIsZero()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap,  heightMapSize}
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        testee.FlowCalculation();

        testee.VelocityMap();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
        {
            Assert.That(gridPoint.VelocityX, Is.Zero);
            Assert.That(gridPoint.VelocityY, Is.Zero);
        }
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithWaterInMiddle_VelocityIsEqualInAllDirections()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.FlowCalculation();

        testee.VelocityMap();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        GridPoint center = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(center.VelocityX, Is.EqualTo(0));
        Assert.That(center.VelocityY, Is.EqualTo(0));
        GridPoint left = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(left.VelocityX, Is.EqualTo(-0.5f));
        Assert.That(left.VelocityY, Is.EqualTo(0));
        GridPoint right = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(right.VelocityX, Is.EqualTo(0.5f));
        Assert.That(right.VelocityY, Is.EqualTo(0));
        GridPoint top = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(top.VelocityX, Is.EqualTo(0));
        Assert.That(top.VelocityY, Is.EqualTo(0.5f));
        GridPoint bottom = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottom.VelocityX, Is.EqualTo(0));
        Assert.That(bottom.VelocityY, Is.EqualTo(-0.5f));
    }

    [Test]
    public unsafe void VelocityMap_Slope3x3HeightMapWithWaterInMiddle_VelocityIsHigherDownSlope()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(1, 0)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 2)] = 1;
        heightMapValues[testee.GetIndex(2, 0)] = 2;
        heightMapValues[testee.GetIndex(2, 1)] = 2;
        heightMapValues[testee.GetIndex(2, 2)] = 2;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.FlowCalculation();

        testee.VelocityMap();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        GridPoint center = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(center.VelocityX, Is.EqualTo(-0.25));
        Assert.That(center.VelocityY, Is.EqualTo(0));
        GridPoint left = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(left.VelocityX, Is.EqualTo(-0.625f));
        Assert.That(left.VelocityY, Is.EqualTo(0));
        GridPoint right = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(right.VelocityX, Is.EqualTo(0.375f));
        Assert.That(right.VelocityY, Is.EqualTo(0));
        GridPoint top = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(top.VelocityX, Is.EqualTo(0));
        Assert.That(top.VelocityY, Is.EqualTo(0.5f));
        GridPoint bottom = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottom.VelocityX, Is.EqualTo(0));
        Assert.That(bottom.VelocityY, Is.EqualTo(-0.5f));
    }
}
