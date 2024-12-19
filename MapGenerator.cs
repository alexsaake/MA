using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class MapGenerator : IMapGenerator
    {
        private readonly INoise myNoise;
        private readonly IComputeShader myComputeShader;

        public MapGenerator(INoise noise, IComputeShader computeShader)
        {
            myNoise = noise;
            myComputeShader = computeShader;
        }

        public HeightMap GenerateHeightMap(int width, int depth)
        {
            HeightMap noiseMap = myNoise.GenerateNoiseMap(width, depth, 2, 8, 0.5f, 2, Vector2.Zero);

            return noiseMap;
        }

        public HeightMap GenerateHeightMapGPU(uint size)
        {
            return GenerateHeightMapGPU(size, 2, 8, 0.5f, 2);
        }

        private unsafe HeightMap GenerateHeightMapGPU(uint size, float scale, uint octaves, float persistance, float lacunarity)
        {
            var random = new Random(Configuration.Seed);

            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                octaveOffsets[i] = new Vector2(random.Next(-10000, 10000), random.Next(-10000, 10000));
            }

            HeightMapParameters heightMapParameters = new HeightMapParameters()
            {
                Size = size,
                Scale = scale,
                Octaves = octaves,
                OctaveOffsets = octaveOffsets,
                Persistence = persistance,
                Lacunarity = lacunarity
            };
            uint heightMapParametersSize = (uint)sizeof(HeightMapParameters);
            uint heightMapParametersShaderBufferId = Rlgl.LoadShaderBuffer(heightMapParametersSize, null, Rlgl.DYNAMIC_COPY);

            float[] heightMap = new float[size * size];
            uint heightMapShaderBufferSize = (uint)heightMap.Length * sizeof(float);
            uint heightMapShaderBufferId = Rlgl.LoadShaderBuffer(heightMapShaderBufferSize, null, Rlgl.DYNAMIC_COPY);

            myComputeShader.CreateShaderProgram("Shaders/HeightMapGenerator.glsl");

            Rlgl.EnableShader(myComputeShader.Id);
            Rlgl.BindShaderBuffer(heightMapParametersShaderBufferId, 1);
            Rlgl.BindShaderBuffer(heightMapShaderBufferId, 2);
            Rlgl.ComputeShaderDispatch((uint)heightMap.Length, 1, 1);
            Rlgl.ReadShaderBuffer(heightMapShaderBufferId, &heightMap, heightMapShaderBufferSize, 0);
            Rlgl.DisableShader();

            Rlgl.UnloadShaderBuffer(heightMapParametersShaderBufferId);
            Rlgl.UnloadShaderBuffer(heightMapShaderBufferId);

            myComputeShader.Dispose();

            return new HeightMap(heightMap);
        }
    }

    internal struct HeightMapParameters
    {
        public uint Size;
        public float Scale;
        public uint Octaves;
        public Vector2[] OctaveOffsets;
        public float Persistence;
        public float Lacunarity;
    };
}
