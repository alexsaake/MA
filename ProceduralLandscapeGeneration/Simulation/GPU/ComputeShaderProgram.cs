using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class ComputeShaderProgram : IComputeShaderProgram
{
    public uint Id { get; private set; }

    public ComputeShaderProgram(string fileName)
    {
        Initialize(fileName);
    }

    private unsafe void Initialize(string fileName)
    {
        using var fileNameAnsiBuffer = fileName.ToAnsiBuffer();
        sbyte* fileNamePointer = Raylib.LoadFileText(fileNameAnsiBuffer.AsPointer());
        uint shaderId = Rlgl.CompileShader(fileNamePointer, (int)ShaderType.Compute);

        Id = Rlgl.LoadComputeShaderProgram(shaderId);
    }

    public void Dispose()
    {
        Rlgl.UnloadShaderProgram(Id);
    }
}
