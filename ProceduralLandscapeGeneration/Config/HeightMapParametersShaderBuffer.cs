namespace ProceduralLandscapeGeneration.Config;

internal struct HeightMapParametersShaderBuffer
{
    public uint Seed;
    public uint SideLength;
    public float Scale;
    public uint Octaves;
    public float Persistence;
    public float Lacunarity;
    public int Min;
    public int Max;
};
