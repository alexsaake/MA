namespace ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics
{
    internal interface IPlateTectonicsHeightMapGenerator : IDisposable
    {
        void GenerateHeightMap();
        void SimulatePlateTectonics();
    }
}