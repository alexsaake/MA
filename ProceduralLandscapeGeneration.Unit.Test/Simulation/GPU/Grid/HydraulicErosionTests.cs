using Moq;
using NUnit.Framework;
using ProceduralLandscapeGeneration.Simulation.GPU;
using ProceduralLandscapeGeneration.Simulation.GPU.Grid;
using Raylib_cs;

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
    public void a()
    {
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), Mock.Of<IConfiguration>(), Mock.Of<IShaderBuffers>());

        testee.Initialize();

        testee.Erode();
    }
}
