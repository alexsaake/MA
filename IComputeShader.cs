namespace ProceduralLandscapeGeneration
{
    internal interface IComputeShader : IDisposable
    {
        uint Id { get; }
        uint CreateComputeShaderProgram(string fileName);
    }
}