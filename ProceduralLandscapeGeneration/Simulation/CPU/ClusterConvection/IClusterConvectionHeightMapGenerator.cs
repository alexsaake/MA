using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.Simulation.CPU.ClusterConvection
{
    internal interface IClusterConvectionHeightMapGenerator : IDisposable
    {
        HeightMap GenerateHeightMap();
        void GenerateHeightMapShaderBuffer();
        HeightMap Update();
    }
}