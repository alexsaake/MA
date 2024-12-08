namespace ProceduralLandscapeGeneration
{
    internal class ErosionSimulator : IErosionSimulator
    {
        public event EventHandler<HeightMap>? ErosionIterationFinished;

        public void SimulateHydraulicErosion(HeightMap heightMap)
        {
            Random random = new Random();
            int lastCallback = 0;

            for (int iteration = 0; iteration <= Configuration.SimulationIterations; iteration += Configuration.ParallelExecutions)
            {
                List<Task> parallelExecutionTasks = new List<Task>();
                for (int parallelExecution = 0; parallelExecution < Configuration.ParallelExecutions; parallelExecution++)
                {
                    parallelExecutionTasks.Add(Task.Run(() =>
                    {
                        WaterParticle waterParticle = new(new(random.Next(heightMap.Width), random.Next(heightMap.Height)));
                        waterParticle.Erode(heightMap);
                    }));
                }
                Task.WaitAll(parallelExecutionTasks.ToArray());

                if (iteration % Configuration.SimulationCallbackEachIterations == 0
                    && iteration != lastCallback)
                {
                    ErosionIterationFinished?.Invoke(this, heightMap);
                    lastCallback = iteration;
                    Console.WriteLine($"INFO: Step {iteration} of {Configuration.SimulationIterations}.");
                }
            }
        }
    }
}
