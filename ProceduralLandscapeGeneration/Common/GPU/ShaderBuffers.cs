using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralLandscapeGeneration.Common.GPU;

class ShaderBuffers : IDictionary<ShaderBufferTypes, uint>, IShaderBuffers
{
    private readonly Dictionary<ShaderBufferTypes, uint> myIds = new Dictionary<ShaderBufferTypes, uint>();
    private readonly Dictionary<ShaderBufferTypes, uint> myIndices = new Dictionary<ShaderBufferTypes, uint>();

    public uint this[ShaderBufferTypes key] { get => myIds[key]; set => throw new NotImplementedException(); }

    public ICollection<ShaderBufferTypes> Keys => throw new NotImplementedException();

    public ICollection<uint> Values => throw new NotImplementedException();

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public ShaderBuffers()
    {
        myIndices.Add(ShaderBufferTypes.HeightMap, 0);
        myIndices.Add(ShaderBufferTypes.HeatMap, 1);
        myIndices.Add(ShaderBufferTypes.ParticlesHydraulicErosion, 2);
        myIndices.Add(ShaderBufferTypes.ParticlesWindErosion, 3);
        myIndices.Add(ShaderBufferTypes.GridHydraulicErosionCell, 4);
        myIndices.Add(ShaderBufferTypes.MapGenerationConfiguration, 5);
        myIndices.Add(ShaderBufferTypes.ErosionConfiguration, 6);
        myIndices.Add(ShaderBufferTypes.ParticleHydraulicErosionConfiguration, 7);
        myIndices.Add(ShaderBufferTypes.ParticleWindErosionConfiguration, 8);
        myIndices.Add(ShaderBufferTypes.GridHydraulicErosionConfiguration, 9);
        myIndices.Add(ShaderBufferTypes.ThermalErosionConfiguration, 10);
        myIndices.Add(ShaderBufferTypes.HydraulicErosionHeightMapIndices, 11);
        myIndices.Add(ShaderBufferTypes.HeightMapParameters, 12);
        myIndices.Add(ShaderBufferTypes.GridThermalErosionCells, 13);
        myIndices.Add(ShaderBufferTypes.WindErosionHeightMapIndices, 14);
        myIndices.Add(ShaderBufferTypes.PlateTectonicsSegments, 15);
        myIndices.Add(ShaderBufferTypes.PlateTectonicsPlates, 16);
        myIndices.Add(ShaderBufferTypes.PlateTectonicsTempSegments, 17);
        myIndices.Add(ShaderBufferTypes.RockTypeConfiguration, 18);
        myIndices.Add(ShaderBufferTypes.PlateTectonicsConfiguration, 19);
    }

    public unsafe void Add(ShaderBufferTypes key, uint size)
    {
        uint shaderBufferId = Rlgl.LoadShaderBuffer(size, null, Rlgl.DYNAMIC_COPY);
        myIds.Add(key, shaderBufferId);
        Rlgl.BindShaderBuffer(shaderBufferId, myIndices[key]);
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
        return myIds.ContainsKey(key);
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
        if (myIds.TryGetValue(key, out uint value))
        {
            Rlgl.UnloadShaderBuffer(value);
        }
        return myIds.Remove(key);
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
