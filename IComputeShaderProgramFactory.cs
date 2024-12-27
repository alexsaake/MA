namespace ProceduralLandscapeGeneration
{
    internal interface IComputeShaderProgramFactory
    {
        ComputeShaderProgram CreateComputeShaderProgram(string fileName);
    }
}