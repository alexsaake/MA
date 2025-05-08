namespace ProceduralLandscapeGeneration.HeightMapGeneration.GPU;

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
