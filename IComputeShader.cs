namespace ProceduralLandscapeGeneration
{
    internal interface IComputeShader : IDisposable
    {
        uint Id { get; }
        uint CreateComputeShaderProgram(string fileName);
        uint CreateMeshShaderProgram(string meshShaderFileName, string fragmentShaderFileName);
        uint CreateMeshShaderProgram(string taskShaderFileName, string meshShaderFileName, string fragmentShaderFileName);
    }
}