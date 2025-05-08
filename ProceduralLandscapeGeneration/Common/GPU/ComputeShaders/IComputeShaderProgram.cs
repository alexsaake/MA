namespace ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;

internal interface IComputeShaderProgram : IDisposable
{
    uint Id { get; }
}