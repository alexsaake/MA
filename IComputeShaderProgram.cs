namespace ProceduralLandscapeGeneration
{
    internal interface IComputeShaderProgram : IDisposable
    {
        uint Id { get; }
    }
}