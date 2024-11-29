namespace ProceduralLandscapeGeneration
{
    internal class MapGenerator : IMapGenerator
    {
        private INoise myNoise;

        private int mapWidth;
        private int mapHeight;
        private float noiseScale;

        public MapGenerator(INoise noise)
        {
            myNoise = noise;
        }

        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] noiseMap = myNoise.GenerateNoiseMap(width, height, 1);

            return noiseMap;
        }
    }
}
