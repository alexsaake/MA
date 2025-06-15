namespace ProceduralLandscapeGeneration.Configurations.MapGeneration;

internal struct MapGenerationConfigurationShaderBuffer
{
    public float HeightMultiplier;
    public uint RockTypeCount;
    public uint LayerCount;
    public float SeaLevel;
    public bool AreTerrainColorsEnabled;
    private readonly bool padding1;
    private readonly bool padding2;
    private readonly bool padding3;
    public bool ArePlateTectonicsPlateColorsEnabled;
    private readonly bool padding4;
    private readonly bool padding5;
    private readonly bool padding6;
    public bool AreLayerColorsEnabled;
}
