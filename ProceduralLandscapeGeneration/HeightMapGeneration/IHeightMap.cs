namespace ProceduralLandscapeGeneration.HeightMapGeneration;

internal interface IHeightMap : IDisposable
{
    void Initialize();
    void SimulatePlateTectonics();
}