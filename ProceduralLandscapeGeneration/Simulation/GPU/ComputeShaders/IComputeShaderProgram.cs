namespace ProceduralLandscapeGeneration.Simulation.GPU.ComputeShaders;

internal interface IComputeShaderProgram : IDisposable
{
    uint Id { get; }
}