using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class MapGenerator : IMapGenerator
    {
        private readonly INoise myNoise;
        private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

        public MapGenerator(INoise noise, IComputeShaderProgramFactory computeShaderProgramFactory)
        {
            myNoise = noise;
            myComputeShaderProgramFactory = computeShaderProgramFactory;
        }

        public HeightMap GenerateHeightMap(uint width, uint depth)
        {
            HeightMap noiseMap = myNoise.GenerateNoiseMap(width, depth, 2, 8, 0.5f, 2, Vector2.Zero);

            return noiseMap;
        }

        public uint GenerateHeightMapShaderBuffer(uint size)
        {
            return GenerateHeightMapShaderBuffer(size, 500, 8, 0.5f, 2);
        }

        private unsafe uint GenerateHeightMapShaderBuffer(uint size, float scale, uint octaves, float persistance, float lacunarity)
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

            uint heightMapSize = size * size;
            uint heightMapBufferSize = heightMapSize * sizeof(float);
            uint heightMapBufferId = Rlgl.LoadShaderBuffer(heightMapBufferSize, null, Rlgl.DYNAMIC_COPY);

            ComputeShaderProgram heightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Shaders/HeightMapGeneratorComputeShader.glsl");
            Rlgl.EnableShader(heightMapComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
            Rlgl.BindShaderBuffer(heightMapBufferId, 2);
            Rlgl.ComputeShaderDispatch(heightMapSize, 1, 1);
            Rlgl.DisableShader();
            heightMapComputeShaderProgram.Dispose();

            ComputeShaderProgram normalizeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Shaders/NormalizeComputeShader.glsl");
            Rlgl.EnableShader(normalizeComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
            Rlgl.BindShaderBuffer(heightMapBufferId, 2);
            Rlgl.ComputeShaderDispatch(heightMapSize, 1, 1);
            Rlgl.DisableShader();
            normalizeComputeShaderProgram.Dispose();

            Rlgl.UnloadShaderBuffer(heightMapParametersBufferId);

            var a = new float[heightMapSize];
            fixed (float* ptr = a)
            {
                Rlgl.ReadShaderBuffer(heightMapBufferId, ptr, heightMapBufferSize, 0);
            }

            return heightMapBufferId;
        }
    }

    internal struct HeightMapParameters
    {
        public uint Size;
        public float Scale;
        public uint Octaves;
        public float Persistence;
        public float Lacunarity;
        public int Min;
        public int Max;
        //public Vector2[] OctaveOffsets;
    };
}
