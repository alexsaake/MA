namespace ProceduralLandscapeGeneration.Simulation.GPU
{
    internal interface IComputeShaderProgramFactory
    {
        ComputeShaderProgram CreateComputeShaderProgram(string fileName);
    }
}