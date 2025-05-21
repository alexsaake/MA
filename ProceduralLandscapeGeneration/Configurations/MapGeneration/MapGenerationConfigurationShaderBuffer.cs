namespace ProceduralLandscapeGeneration.Configurations.MapGeneration;

internal struct MapGenerationConfigurationShaderBuffer
{
    public float HeightMultiplier;
    public uint RockTypeCount;
    public bool AreTerrainColorsEnabled;
    private readonly bool padding1;
    private readonly bool padding2;
    private readonly bool padding3;
    public bool ArePlateTectonicsPlateColorsEnabled;
}
