namespace ProceduralLandscapeGeneration.Config.ShaderBuffers;

internal struct HeightMapParametersShaderBuffer
{
    public uint Seed;
    public float Scale;
    public uint Octaves;
    public float Persistence;
    public float Lacunarity;
    public int Min;
    public int Max;
};
