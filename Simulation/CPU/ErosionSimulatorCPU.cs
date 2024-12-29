using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU
{
    internal class ErosionSimulatorCPU : IErosionSimulator
    {
        private readonly IRandom myRandom;
        private readonly IHeightMapGenerator myHeightMapGenerator;

        private bool myIsDisposed;

        public HeightMap HeightMap { get; private set; }
        public uint HeightMapShaderBufferId => throw new NotImplementedException();

        public event EventHandler? ErosionIterationFinished;

        public ErosionSimulatorCPU(IRandom random, IHeightMapGenerator heightMapGenerator)
        {
            myRandom = random;
            myHeightMapGenerator = heightMapGenerator;
        }

        public void Initialize()
        {
            HeightMap = myHeightMapGenerator.GenerateHeightMap();
        }

        public void SimulateHydraulicErosion()
        {
            Task.Run(() =>
            {
                uint lastCallback = 0;

                for (uint iteration = 0; iteration <= Configuration.SimulationIterations; iteration += Configuration.ParallelExecutions)
                {
                    List<Task> parallelExecutionTasks = new List<Task>();
                    for (int parallelExecution = 0; parallelExecution < Configuration.ParallelExecutions; parallelExecution++)
                    {
                        Vector2 newPosition = new(myRandom.Next(HeightMap.Width), myRandom.Next(HeightMap.Depth));
                        parallelExecutionTasks.Add(Task.Run(() =>
                        {
                            WaterParticle waterParticle = new(newPosition);
                            while (true)
                            {
                                if (!waterParticle.Move(HeightMap))
                                {
                                    break;
                                }

                                if (!waterParticle.Interact(HeightMap))
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
                        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                        lastCallback = iteration;
                        Console.WriteLine($"INFO: Step {iteration} of {Configuration.SimulationIterations}.");
                    }
                }

                ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"INFO: End of simulation after {Configuration.SimulationIterations} iterations.");
            });
        }

        public void Dispose()
        {
            if (myIsDisposed)
            {
                return;
            }

            myIsDisposed = true;
        }
    }
}
