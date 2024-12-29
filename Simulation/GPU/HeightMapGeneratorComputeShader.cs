using ProceduralLandscapeGeneration.Common;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.GPU
{
    internal class HeightMapGeneratorComputeShader : IHeightMapGenerator
    {
        private const uint Scale = 500;
        private const uint Octaves = 8;
        private const float Persistance = 0.5f;
        private const float Lacunarity = 2;

        private readonly IRandom myRandom;
        private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

        public HeightMapGeneratorComputeShader(IRandom random, IComputeShaderProgramFactory computeShaderProgramFactory)
        {
            myRandom = random;
            myComputeShaderProgramFactory = computeShaderProgramFactory;
        }

        public unsafe HeightMap GenerateHeightMap()
        {
            uint heightMapShaderBufferId = GenerateHeightMapShaderBuffer();

            uint heightMapSize = Configuration.HeightMapSideLength * Configuration.HeightMapSideLength;
            return new HeightMap(heightMapShaderBufferId, heightMapSize);
        }

        public unsafe uint GenerateHeightMapShaderBuffer()
        {
            Vector2[] octaveOffsets = new Vector2[Octaves];
            for (int i = 0; i < Octaves; i++)
            {
                octaveOffsets[i] = new Vector2(myRandom.Next(-10000, 10000), myRandom.Next(-10000, 10000));
            }

            HeightMapParameters heightMapParameters = new HeightMapParameters()
            {
                SideLength = Configuration.HeightMapSideLength,
                Scale = Scale,
                Octaves = Octaves,
                Persistence = Persistance,
                Lacunarity = Lacunarity,
                //TODO: Offsets
                //OctaveOffsets = octaveOffsets
            };
            uint heightMapParametersBufferSize = (uint)sizeof(HeightMapParameters);
            uint heightMapParametersBufferId = Rlgl.LoadShaderBuffer(heightMapParametersBufferSize, &heightMapParameters, Rlgl.DYNAMIC_COPY);

            uint heightMapSize = Configuration.HeightMapSideLength * Configuration.HeightMapSideLength;
            uint heightMapBufferSize = heightMapSize * sizeof(float);
            uint heightMapBufferId = Rlgl.LoadShaderBuffer(heightMapBufferSize, null, Rlgl.DYNAMIC_COPY);

            ComputeShaderProgram heightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/HeightMapGeneratorComputeShader.glsl");
            Rlgl.EnableShader(heightMapComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
            Rlgl.BindShaderBuffer(heightMapBufferId, 2);
            Rlgl.ComputeShaderDispatch(heightMapSize, 1, 1);
            Rlgl.DisableShader();
            heightMapComputeShaderProgram.Dispose();

            ComputeShaderProgram normalizeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/NormalizeComputeShader.glsl");
            Rlgl.EnableShader(normalizeComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
            Rlgl.BindShaderBuffer(heightMapBufferId, 2);
            Rlgl.ComputeShaderDispatch(heightMapSize, 1, 1);
            Rlgl.DisableShader();
            normalizeComputeShaderProgram.Dispose();

            Rlgl.UnloadShaderBuffer(heightMapParametersBufferId);

            return heightMapBufferId;
        }
    }
}
