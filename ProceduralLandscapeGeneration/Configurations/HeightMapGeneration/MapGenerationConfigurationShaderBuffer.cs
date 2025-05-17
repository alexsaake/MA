namespace ProceduralLandscapeGeneration.Configurations.HeightMapGeneration;

internal struct MapGenerationConfigurationShaderBuffer
{
    public float HeightMultiplier;
    public bool AreTerrainColorsEnabled;
    private bool padding1;
    private bool padding2;
    private bool padding3;
    public bool ArePlateTectonicsPlateColorsEnabled;
}
