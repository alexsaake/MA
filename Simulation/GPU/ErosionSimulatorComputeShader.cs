using ProceduralLandscapeGeneration.Common;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU
{
    internal class ErosionSimulatorComputeShader : IErosionSimulator
    {
        private readonly IHeightMapGenerator myHeightMapGenerator;
        private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
        private readonly IRandom myRandom;

        public HeightMap HeightMap => throw new NotImplementedException();
        public uint HeightMapShaderBufferId { get; private set; }

        public event EventHandler? ErosionIterationFinished;

        private ComputeShaderProgram myErosionSimulationComputeShaderProgram;
        private uint myHeightMapIndicesShaderBufferSize;
        private uint myHeightMapIndicesShaderBufferId;
        private uint myHeightMapSize;

        public ErosionSimulatorComputeShader(IHeightMapGenerator heightMapGenerator, IComputeShaderProgramFactory computeShaderProgramFactory, IRandom random)
        {
            myHeightMapGenerator = heightMapGenerator;
            myComputeShaderProgramFactory = computeShaderProgramFactory;
            myRandom = random;
        }

        public unsafe void Initialize()
        {
            HeightMapShaderBufferId = myHeightMapGenerator.GenerateHeightMapShaderBuffer();
            myHeightMapSize = Configuration.HeightMapSideLength * Configuration.HeightMapSideLength;

            myErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/ErosionSimulationComputeShader.glsl");

            myHeightMapIndicesShaderBufferSize = Configuration.SimulationIterations * sizeof(uint);
            myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);
        }

        public void SimulateHydraulicErosion()
        {
            CreateRandomIndices();

            Rlgl.EnableShader(myErosionSimulationComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
            Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
            Rlgl.ComputeShaderDispatch(Configuration.SimulationIterations, 1, 1);
            Rlgl.DisableShader();

            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"INFO: End of simulation after {Configuration.SimulationIterations} iterations.");
        }

        private unsafe void CreateRandomIndices()
        {
            uint[] randomHeightMapIndices = new uint[Configuration.SimulationIterations];
            for (uint i = 0; i < Configuration.SimulationIterations; i++)
            {
                randomHeightMapIndices[i] = (uint)myRandom.Next((int)myHeightMapSize);
            }
            fixed (uint* randomHeightMapIndicesPointer = randomHeightMapIndices)
            {
                Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
            }
        }

        public void Dispose()
        {
            myErosionSimulationComputeShaderProgram.Dispose();
            Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);
        }
    }
}
