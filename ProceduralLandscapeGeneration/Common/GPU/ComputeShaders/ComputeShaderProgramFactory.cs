namespace ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;

internal class ComputeShaderProgramFactory : IComputeShaderProgramFactory
{
    public ComputeShaderProgram CreateComputeShaderProgram(string fileName)
    {
        return new ComputeShaderProgram(fileName);
    }
}
