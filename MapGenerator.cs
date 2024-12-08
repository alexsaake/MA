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

        public HeightMap GenerateHeightMap(int width, int height)
        {
            HeightMap noiseMap = myNoise.GenerateNoiseMap(width, height, 2, 8, 0.5f, 2, Vector2.Zero);

            return noiseMap;
        }
    }
}
