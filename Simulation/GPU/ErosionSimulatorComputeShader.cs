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

        private ComputeShaderProgram myWaterErosionSimulationComputeShaderProgram;
        private ComputeShaderProgram myWindErosionSimulationComputeShaderProgram;
        private uint myHeightMapIndicesShaderBufferSize;
        private uint myHeightMapIndicesShaderBufferId;
        private uint myHeightMapSize;
        private bool myIsDisposed;

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
            myHeightMapIndicesShaderBufferSize = Configuration.SimulationIterations * sizeof(uint);
            myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);

            myWaterErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/WaterErosionSimulationComputeShader.glsl");
            myWindErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/WindErosionSimulationComputeShader.glsl");
        }

        public void SimulateHydraulicErosion()
        {
            CreateRandomIndices();

            Rlgl.EnableShader(myWaterErosionSimulationComputeShaderProgram.Id);
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

        public void SimulateWindErosion()
        {
            CreateRandomIndicesAlongBorder();

            Rlgl.EnableShader(myWindErosionSimulationComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
            Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
            Rlgl.ComputeShaderDispatch(Configuration.SimulationIterations, 1, 1);
            Rlgl.DisableShader();

            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"INFO: End of simulation after {Configuration.SimulationIterations} iterations.");
        }

        private unsafe void CreateRandomIndicesAlongBorder()
        {
            uint[] randomHeightMapIndices = new uint[Configuration.SimulationIterations];
            for (uint i = 0; i < Configuration.SimulationIterations; i++)
            {
                randomHeightMapIndices[i] = (uint)myRandom.Next((int)Configuration.HeightMapSideLength);
            }
            fixed (uint* randomHeightMapIndicesPointer = randomHeightMapIndices)
            {
                Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
            }
        }

        public void Dispose()
        {
            if (myIsDisposed)
            {
                return;
            }

            Rlgl.UnloadShaderBuffer(HeightMapShaderBufferId);
            Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);

            myWaterErosionSimulationComputeShaderProgram.Dispose();
            myWindErosionSimulationComputeShaderProgram.Dispose();

            myIsDisposed = true;
        }
    }
}
