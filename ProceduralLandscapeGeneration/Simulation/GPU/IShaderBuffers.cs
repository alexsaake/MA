namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal interface IShaderBuffers : IDisposable
{
    uint this[ShaderBufferTypes key] { get; }

    void Add(ShaderBufferTypes key, uint size);
}