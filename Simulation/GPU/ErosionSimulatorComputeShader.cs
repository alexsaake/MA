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

        private ComputeShaderProgram myHydraulicErosionSimulationComputeShaderProgram;
        private ComputeShaderProgram myThermalErosionSimulationComputeShaderProgram;
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

            myHydraulicErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/HydraulicErosionSimulationComputeShader.glsl");
            myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/ThermalErosionSimulationComputeShader.glsl");
            myWindErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/WindErosionSimulationComputeShader.glsl");
        }

        public void SimulateHydraulicErosion()
        {
            Console.WriteLine($"INFO: Simulating hydraulic erosion.");

            CreateRandomIndices();

            Rlgl.EnableShader(myHydraulicErosionSimulationComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
            Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
            Rlgl.ComputeShaderDispatch(Configuration.SimulationIterations, 1, 1);
            Rlgl.DisableShader();

            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"INFO: End of simulation after {Configuration.SimulationIterations} iterations.");
        }

        public void SimulateThermalErosion()
        {
            Console.WriteLine($"INFO: Simulating thermal erosion on each cell of the height map.");

            uint mapSize = Configuration.HeightMapSideLength * Configuration.HeightMapSideLength;

            Rlgl.EnableShader(myThermalErosionSimulationComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
            Rlgl.ComputeShaderDispatch(mapSize, 1, 1);
            Rlgl.DisableShader();

            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"INFO: End of simulation after {mapSize} iterations.");
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
            Console.WriteLine($"INFO: Simulating wind erosion.");

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

            myHydraulicErosionSimulationComputeShaderProgram.Dispose();
            myThermalErosionSimulationComputeShaderProgram.Dispose();
            myWindErosionSimulationComputeShaderProgram.Dispose();

            myIsDisposed = true;
        }
    }
}
