﻿using ProceduralLandscapeGeneration.Configurations.Types;

namespace ProceduralLandscapeGeneration.Common.GPU;

internal interface IShaderBuffers
{
    uint this[ShaderBufferTypes key] { get; }

    void Add(ShaderBufferTypes key, uint shaderBufferSize);
    bool ContainsKey(ShaderBufferTypes key);
    bool Remove(ShaderBufferTypes key);
}