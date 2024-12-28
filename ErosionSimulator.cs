using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class ErosionSimulator : IErosionSimulator
    {
        private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

        private readonly Random myRandom;
        private ComputeShaderProgram myErosionSimulationComputeShaderProgram;

        public event EventHandler<HeightMap>? ErosionIterationFinished;

        public ErosionSimulator(IComputeShaderProgramFactory computeShaderProgramFactory)
        {
            myComputeShaderProgramFactory = computeShaderProgramFactory;

            myRandom = Random.Shared;
        }

        public void Initialize()
        {
            myErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Shaders/ErosionSimulationComputeShader.glsl");
        }

        public void SimulateHydraulicErosion(HeightMap heightMap, uint simulationIterations)
        {
            int lastCallback = 0;

            for (int iteration = 0; iteration <= simulationIterations; iteration += Configuration.ParallelExecutions)
            {
                List<Task> parallelExecutionTasks = new List<Task>();
                for (int parallelExecution = 0; parallelExecution < Configuration.ParallelExecutions; parallelExecution++)
                {
                    parallelExecutionTasks.Add(Task.Run(() =>
                    {
                        Vector2 newPosition = new(myRandom.Next(heightMap.Width), myRandom.Next(heightMap.Depth));
                        WaterParticle waterParticle = new(newPosition);
                        while (true)
                        {
                            if (!waterParticle.Move(heightMap))
                            {
                                break;
                            }

                            if (!waterParticle.Interact(heightMap))
                            {
                                break;
                            }
                        }
                    }));
                }
                Task.WaitAll(parallelExecutionTasks.ToArray());

                if (iteration % Configuration.SimulationCallbackEachIterations == 0
            && iteration != lastCallback)
                {
                    ErosionIterationFinished?.Invoke(this, heightMap);
                    lastCallback = iteration;
                    Console.WriteLine($"INFO: Step {iteration} of {simulationIterations}.");
                }
            }

            ErosionIterationFinished?.Invoke(this, heightMap);
            Console.WriteLine($"INFO: End of simulation after {simulationIterations} iterations.");
        }

        public unsafe void SimulateHydraulicErosion(uint heightMapShaderBufferId, uint heightMapSize, uint simulationIterations)
        {
            uint[] randomHeightMapIndices = new uint[simulationIterations];
            uint heightMapIndicesShaderBufferSize = simulationIterations * sizeof(uint);
            for (uint i = 0; i < simulationIterations; i++)
            {
                randomHeightMapIndices[i] = (uint)myRandom.Next((int)heightMapSize);
            }
            uint heightMapIndicesShaderBufferId;
            fixed (uint* randomHeightMapIndicesPointer = randomHeightMapIndices)
            {
                heightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(heightMapIndicesShaderBufferSize, randomHeightMapIndicesPointer, Rlgl.DYNAMIC_COPY);
            }

            Rlgl.EnableShader(myErosionSimulationComputeShaderProgram.Id);
            Rlgl.BindShaderBuffer(heightMapShaderBufferId, 1);
            Rlgl.BindShaderBuffer(heightMapIndicesShaderBufferId, 2);
            Rlgl.ComputeShaderDispatch(simulationIterations, 1, 1);
            Rlgl.DisableShader();

            ErosionIterationFinished?.Invoke(this, new HeightMap(heightMapShaderBufferId, heightMapSize));
            Console.WriteLine($"INFO: End of simulation after {simulationIterations} iterations.");
            Rlgl.UnloadShaderBuffer(heightMapIndicesShaderBufferId);
        }

        public void Dispose()
        {
            myErosionSimulationComputeShaderProgram.Dispose();
        }
    }
}
