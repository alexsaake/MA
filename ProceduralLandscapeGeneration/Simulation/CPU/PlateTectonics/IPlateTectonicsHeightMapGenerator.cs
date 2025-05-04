using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics
{
    internal interface IPlateTectonicsHeightMapGenerator : IDisposable
    {
        HeightMap GenerateHeightMap();
        void GenerateHeightMapShaderBuffer();
        HeightMap SimulatePlateTectonics();
    }
}