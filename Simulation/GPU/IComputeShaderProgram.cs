namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal interface IComputeShaderProgram : IDisposable
{
    uint Id { get; }
}