using ProceduralLandscapeGeneration.Config.Types;
using Raylib_cs;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

class ShaderBuffers : IDictionary<ShaderBufferTypes, uint>, IShaderBuffers
{
    private readonly Dictionary<ShaderBufferTypes, uint> myShaderBufferIds = new Dictionary<ShaderBufferTypes, uint>();

    public uint this[ShaderBufferTypes key] { get => myShaderBufferIds[key]; set => throw new NotImplementedException(); }

    public ICollection<ShaderBufferTypes> Keys => throw new NotImplementedException();

    public ICollection<uint> Values => throw new NotImplementedException();

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public unsafe void Add(ShaderBufferTypes key, uint size)
    {
        uint shaderBufferId = Rlgl.LoadShaderBuffer(size, null, Rlgl.DYNAMIC_COPY);
        myShaderBufferIds.Add(key, shaderBufferId);
    }

    public void Add(KeyValuePair<ShaderBufferTypes, uint> item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(KeyValuePair<ShaderBufferTypes, uint> item)
    {
        throw new NotImplementedException();
    }

    public bool ContainsKey(ShaderBufferTypes key)
    {
        return myShaderBufferIds.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<ShaderBufferTypes, uint>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<ShaderBufferTypes, uint>> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public bool Remove(ShaderBufferTypes key)
    {
        return myShaderBufferIds.Remove(key);
    }

    public bool Remove(KeyValuePair<ShaderBufferTypes, uint> item)
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(ShaderBufferTypes key, [MaybeNullWhen(false)] out uint value)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
