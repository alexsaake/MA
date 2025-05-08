namespace ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;

internal interface IComputeShaderProgramFactory
{
    ComputeShaderProgram CreateComputeShaderProgram(string fileName);
}