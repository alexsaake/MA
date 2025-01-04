namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal struct HeightMapParameters
{
    public uint Seed;
    public uint SideLength;
    public float Scale;
    public uint Octaves;
    public float Persistence;
    public float Lacunarity;
};
