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

        public HeightMap GenerateHeightMap(uint width, uint depth)
        {
            HeightMap noiseMap = myNoise.GenerateNoiseMap(width, depth, 2, 8, 0.5f, 2, Vector2.Zero);

            return noiseMap;
        }

        public HeightMap GenerateHeightMapGPU(uint size)
        {
            return GenerateHeightMapGPU(size, 200, 8, 0.5f, 2);
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
                Persistence = persistance,
                Lacunarity = lacunarity,
                //OctaveOffsets = octaveOffsets
            };
            uint heightMapParametersBufferSize = (uint)sizeof(HeightMapParameters);
            uint heightMapParametersBufferId = Rlgl.LoadShaderBuffer(heightMapParametersBufferSize, &heightMapParameters, Rlgl.DYNAMIC_COPY);

            float[] heightMap = new float[size * size];
            uint heightMapBufferSize = (uint)heightMap.Length * sizeof(float);
            uint heightMapBufferId = Rlgl.LoadShaderBuffer(heightMapBufferSize, null, Rlgl.DYNAMIC_COPY);

            myComputeShader.CreateComputeShaderProgram("Shaders/HeightMapGenerator.glsl");

            Rlgl.EnableShader(myComputeShader.Id);
            Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
            Rlgl.BindShaderBuffer(heightMapBufferId, 2);
            Rlgl.ComputeShaderDispatch((uint)heightMap.Length, 1, 1);
            fixed (float* heightMapPointer = heightMap)
            {
                Rlgl.ReadShaderBuffer(heightMapBufferId, heightMapPointer, heightMapBufferSize, 0);
            }
            Rlgl.DisableShader();

            Rlgl.UnloadShaderBuffer(heightMapParametersBufferId);
            Rlgl.UnloadShaderBuffer(heightMapBufferId);

            myComputeShader.Dispose();

            float minHeightMapValue = heightMap.Min();
            float maxHeightMapValue = heightMap.Max();

            for (int i = 0; i < heightMap.Length; i++)
            {
                heightMap[i] = Math.InverseLerp(minHeightMapValue, maxHeightMapValue, heightMap[i]);
            }

            return new HeightMap(heightMap);
        }
    }

    internal struct HeightMapParameters
    {
        public uint Size;
        public float Scale;
        public uint Octaves;
        public float Persistence;
        public float Lacunarity;
        //public Vector2[] OctaveOffsets;
    };
}
