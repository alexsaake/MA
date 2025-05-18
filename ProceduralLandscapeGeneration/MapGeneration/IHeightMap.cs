namespace ProceduralLandscapeGeneration.MapGeneration;

internal interface IHeightMap : IDisposable
{
    void Initialize();
    void SimulatePlateTectonics();
}