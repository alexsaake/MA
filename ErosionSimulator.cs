using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class ErosionSimulator : IErosionSimulator
    {
        private Random myRandom;

        public event EventHandler<HeightMap>? ErosionIterationFinished;

        public ErosionSimulator()
        {
            myRandom = Random.Shared;
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

            Console.WriteLine($"INFO: End of simulation after {simulationIterations} iterations.");
        }
    }
}
