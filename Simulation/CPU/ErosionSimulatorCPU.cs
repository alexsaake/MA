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
            Console.WriteLine($"INFO: Simulating hydraulic erosion.");
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

        public void SimulateThermalErosion()
        {
            const float heightChange = 0.001f;

            Console.WriteLine($"INFO: Simulating thermal erosion on each cell of the height map.");
            Task.Run(() =>
            {
                uint mapSize = Configuration.HeightMapSideLength * Configuration.HeightMapSideLength;
                uint iteration = 0;
                uint lastCallback = 0;

                for (int y = 0; y <= Configuration.HeightMapSideLength; y++)
                {
                    for (int x = 0; x <= Configuration.HeightMapSideLength;)
                    {
                        List<Task> parallelExecutionTasks = new List<Task>();
                        for (int parallelExecution = 0; parallelExecution < Configuration.ParallelExecutions; parallelExecution++)
                        {
                            if (x > Configuration.HeightMapSideLength)
                            {
                                continue;
                            }
                            parallelExecutionTasks.Add(Task.Run(() =>
                            {
                                int localX = x;
                                int localY = y;
                                Vector3 normal = HeightMap.GetScaledNormal(localX, localY);
                                IVector2 neighborPosition = new IVector2(localX + (int)MathF.Ceiling(normal.X), localY + (int)MathF.Ceiling(normal.Y));
                                if (HeightMap.IsOutOfBounds(neighborPosition))
                                {
                                    return;
                                }
                                float neighborHeight = HeightMap.Height[neighborPosition.X, neighborPosition.Y] * Configuration.HeightMultiplier;
                                float zDiff = HeightMap.Height[localX, localY] * Configuration.HeightMultiplier - neighborHeight;

                                if (zDiff > Configuration.TangensThresholdAngle)
                                {
                                    HeightMap.Height[localX, localY] -= heightChange;
                                    HeightMap.Height[neighborPosition.X, neighborPosition.Y] += heightChange;
                                }
                            }));
                            x++;
                            iteration++;
                        }
                        Task.WaitAll(parallelExecutionTasks.ToArray());

                        if (iteration % Configuration.SimulationCallbackEachIterations == 0
                            && iteration != lastCallback)
                        {
                            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                            lastCallback = iteration;
                            Console.WriteLine($"INFO: Step {iteration} of {mapSize}.");
                        }
                    }
                }

                Console.WriteLine($"INFO: End of simulation after {Configuration.SimulationIterations} iterations.");
            });
        }

        public void SimulateWindErosion()
        {
            Console.WriteLine($"INFO: Simulating wind erosion.");
            Task.Run(() =>
            {
                uint lastCallback = 0;

                for (uint iteration = 0; iteration <= Configuration.SimulationIterations; iteration += Configuration.ParallelExecutions)
                {
                    List<Task> parallelExecutionTasks = new List<Task>();
                    for (int parallelExecution = 0; parallelExecution < Configuration.ParallelExecutions; parallelExecution++)
                    {
                        Vector2 newPosition = GetRandomPositionAtEdgeOfMap();
                        parallelExecutionTasks.Add(Task.Run(() =>
                        {
                            WindParticle windParticle = new(newPosition);
                            windParticle.Fly(HeightMap, new Vector3(0, 1, 0));
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

                Console.WriteLine($"INFO: End of simulation after {Configuration.SimulationIterations} iterations.");
            });
        }

        private Vector2 GetRandomPositionAtEdgeOfMap()
        {
            return new(myRandom.Next(HeightMap.Width), 0);
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
