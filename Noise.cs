using DotnetNoise;

namespace ProceduralLandscapeGeneration
{
    internal class Noise : INoise
    {
        private FastNoise myNoiseGenerator;

        public Noise()
        {
            myNoiseGenerator = new FastNoise();
        }

        public float[,] GenerateNoiseMap(int width, int height, float scale)
        {
            float[,] noiseMap = new float[width, height];

            if (scale <= 0)
            {
                scale = 0.0001f;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sampleX = x / scale;
                    float sampleY = y / scale;

                    float perlinValue = myNoiseGenerator.GetPerlin(sampleX, sampleY);
                    noiseMap[x, y] = perlinValue;
                }
            }

            return noiseMap;
        }
    }
}
