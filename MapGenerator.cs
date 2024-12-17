using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class MapGenerator : IMapGenerator
    {
        private readonly INoise myNoise;

        public MapGenerator(INoise noise)
        {
            myNoise = noise;
        }

        public HeightMap GenerateHeightMap(int width, int depth)
        {
            HeightMap noiseMap = myNoise.GenerateNoiseMap(width, depth, 2, 8, 0.5f, 2, Vector2.Zero);

            return noiseMap;
        }
    }
}
