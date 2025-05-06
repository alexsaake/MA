using ProceduralLandscapeGeneration.Config.Types;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal interface IShaderBuffers
{
    uint this[ShaderBufferTypes key] { get; }

    void Add(ShaderBufferTypes key, uint size);
    bool ContainsKey(ShaderBufferTypes key);
    bool Remove(ShaderBufferTypes key);
}