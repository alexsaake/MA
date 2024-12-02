namespace ProceduralLandscapeGeneration
{
    internal class ErosionSimulator : IErosionSimulator
    {
        public event EventHandler<HeightMap>? ErosionIterationFinished;

        public void SimulateHydraulicErosion(HeightMap heightMap, int iterations, int callbackEachIterations)
        {
            const int ParallelExecutions = 10;
            Random random = new Random();
            int lastCallback = 0;

            for (int iteration = 0; iteration <= iterations; iteration += ParallelExecutions)
            {
                List<Task> parallelExecutionTasks = new List<Task>();
                for (int parallelExecution = 0; parallelExecution < ParallelExecutions; parallelExecution++)
                {
                    parallelExecutionTasks.Add(Task.Run(() =>
                    {
                        WaterParticle waterParticle = new(new(random.Next(heightMap.Width), random.Next(heightMap.Height)));
                        waterParticle.Erode(heightMap);
                    }));
                }
                Task.WaitAll(parallelExecutionTasks.ToArray(), 10000);

                if (iteration % callbackEachIterations == 0
                    && iteration != lastCallback)
                {
                    ErosionIterationFinished?.Invoke(this, heightMap);
                    lastCallback = iteration;
                    Console.WriteLine($"INFO: Step {iteration} of {iterations}.");
                }
            }
        }
    }
}
