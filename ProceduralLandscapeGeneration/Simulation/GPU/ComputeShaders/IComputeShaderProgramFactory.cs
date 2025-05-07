namespace ProceduralLandscapeGeneration.Simulation.GPU.ComputeShaders;

internal interface IComputeShaderProgramFactory
{
    ComputeShaderProgram CreateComputeShaderProgram(string fileName);
}