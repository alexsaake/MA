namespace ProceduralLandscapeGeneration
{
    internal interface IComputeShader : IDisposable
    {
        uint Id { get; }
        uint CreateShaderProgram(string fileName);
    }
}