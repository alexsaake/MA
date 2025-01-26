using Autofac;
using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU;

internal class ErosionSimulatorCPU : IErosionSimulator
{
    private readonly IConfiguration myConfiguration;
    private readonly IRandom myRandom;
    private readonly ILifetimeScope myLifetimeScope;
    private IHeightMapGenerator myHeightMapGenerator;

    private Task myRunningSimulation;
    private bool myIsDisposed;

    public HeightMap? HeightMap { get; private set; }
    public uint HeightMapShaderBufferId => throw new NotImplementedException();

    public event EventHandler? ErosionIterationFinished;

    public ErosionSimulatorCPU(IConfiguration configuration, IRandom random, ILifetimeScope lifetimeScope)
    {
        myConfiguration = configuration;
        myRandom = random;
        myLifetimeScope = lifetimeScope;
    }

    public void Initialize()
    {
        myHeightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myConfiguration.HeightMapGeneration);
        HeightMap = myHeightMapGenerator.GenerateHeightMap();
    }

    public void SimulateHydraulicErosion()
    {
        if (myRunningSimulation is not null
            && !myRunningSimulation.IsCompleted)
        {
            Console.WriteLine($"INFO: Simulation already running on CPU.");
            return;
        }
        Console.WriteLine($"INFO: Simulating hydraulic erosion.");
        myRunningSimulation = Task.Run(() =>
        {
            uint lastCallback = 0;

            for (uint iteration = 0; iteration <= myConfiguration.SimulationIterations; iteration += myConfiguration.ParallelExecutions)
            {
                List<Task> parallelExecutionTasks = new List<Task>();
                for (int parallelExecution = 0; parallelExecution < myConfiguration.ParallelExecutions; parallelExecution++)
                {
                    Vector2 newPosition = new(myRandom.Next(HeightMap!.Width), myRandom.Next(HeightMap.Depth));
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

                if (iteration % myConfiguration.SimulationCallbackEachIterations == 0
            && iteration != lastCallback)
                {
                    ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                    lastCallback = iteration;
                    Console.WriteLine($"INFO: Step {iteration} of {myConfiguration.SimulationIterations}.");
                }
            }

            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"INFO: End of simulation after {myConfiguration.SimulationIterations} iterations.");
        });
    }

    public void SimulateThermalErosion()
    {
        if (myRunningSimulation is not null
            && !myRunningSimulation.IsCompleted)
        {
            Console.WriteLine($"INFO: Simulation already running on CPU.");
            return;
        }
        float tangensTalusAngle = MathF.Tan(myConfiguration.TalusAngle);

        Console.WriteLine($"INFO: Simulating thermal erosion on each cell of the height map.");
        myRunningSimulation = Task.Run(() =>
        {
            uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
            uint iteration = 0;
            uint lastCallback = 0;

            for (int y = 0; y <= myConfiguration.HeightMapSideLength; y++)
            {
                for (int x = 0; x <= myConfiguration.HeightMapSideLength;)
                {
                    List<Task> parallelExecutionTasks = new List<Task>();
                    for (int parallelExecution = 0; parallelExecution < myConfiguration.ParallelExecutions; parallelExecution++)
                    {
                        if (x > myConfiguration.HeightMapSideLength)
                        {
                            continue;
                        }
                        parallelExecutionTasks.Add(Task.Run(() =>
                        {
                            int localX = x;
                            int localY = y;
                            Vector3 normal = HeightMap!.GetScaledNormal(localX, localY);
                            IVector2 neighborPosition = new IVector2(localX + (int)MathF.Ceiling(normal.X), localY + (int)MathF.Ceiling(normal.Y));
                            if (HeightMap.IsOutOfBounds(neighborPosition))
                            {
                                return;
                            }
                            float neighborHeight = HeightMap.Height[neighborPosition.X, neighborPosition.Y] * myConfiguration.HeightMultiplier;
                            float zDiff = HeightMap.Height[localX, localY] * myConfiguration.HeightMultiplier - neighborHeight;

                            if (zDiff > tangensTalusAngle)
                            {
                                HeightMap.Height[localX, localY] -= myConfiguration.ThermalErosionHeightChange;
                                HeightMap.Height[neighborPosition.X, neighborPosition.Y] += myConfiguration.ThermalErosionHeightChange;
                            }
                        }));
                        x++;
                        iteration++;
                    }
                    Task.WaitAll(parallelExecutionTasks.ToArray());

                    if (iteration % myConfiguration.SimulationCallbackEachIterations == 0
                        && iteration != lastCallback)
                    {
                        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                        lastCallback = iteration;
                        Console.WriteLine($"INFO: Step {iteration} of {mapSize}.");
                    }
                }
            }

            Console.WriteLine($"INFO: End of simulation after {myConfiguration.SimulationIterations} iterations.");
        });
    }

    public void SimulateWindErosion()
    {
        if (myRunningSimulation is not null
            && !myRunningSimulation.IsCompleted)
        {
            Console.WriteLine($"INFO: Simulation already running on CPU.");
            return;
        }
        Console.WriteLine($"INFO: Simulating wind erosion.");
        myRunningSimulation = Task.Run(() =>
        {
            uint lastCallback = 0;

            for (uint iteration = 0; iteration <= myConfiguration.SimulationIterations; iteration += myConfiguration.ParallelExecutions)
            {
                List<Task> parallelExecutionTasks = new List<Task>();
                for (int parallelExecution = 0; parallelExecution < myConfiguration.ParallelExecutions; parallelExecution++)
                {
                    Vector2 newPosition = GetRandomPositionAtEdgeOfMap();
                    parallelExecutionTasks.Add(Task.Run(() =>
                    {
                        WindParticle windParticle = new(newPosition);
                        windParticle.Fly(HeightMap!, new Vector3(0, 1, 0));
                    }));
                }
                Task.WaitAll(parallelExecutionTasks.ToArray());

                if (iteration % myConfiguration.SimulationCallbackEachIterations == 0
                    && iteration != lastCallback)
                {
                    ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                    lastCallback = iteration;
                    Console.WriteLine($"INFO: Step {iteration} of {myConfiguration.SimulationIterations}.");
                }
            }

            Console.WriteLine($"INFO: End of simulation after {myConfiguration.SimulationIterations} iterations.");
        });
    }

    public void SimulateHydraulicErosionGrid()
    {
        int dropCount = 10000;
        float waterIncrease = 0.01f;

        if (myRunningSimulation is not null
            && !myRunningSimulation.IsCompleted)
        {
            Console.WriteLine($"INFO: Simulation already running on CPU.");
            return;
        }
        Console.WriteLine($"INFO: Simulating hydraulic erosion grid.");
        myRunningSimulation = Task.Run(() =>
        {
            GridBasedErosion gridBasedErosion = new GridBasedErosion(HeightMap);

            for (int drop = 0; drop < dropCount; drop++)
            {
                IVector2 newPosition = new(myRandom.Next(HeightMap!.Width), myRandom.Next(HeightMap.Depth));
                gridBasedErosion.WaterIncrease(newPosition, waterIncrease);
            }

            for (uint iteration = 0; iteration <= myConfiguration.SimulationIterations; iteration ++)
            {

                gridBasedErosion.Simulate();

                for (int y = 0; y < HeightMap.Depth; y++)
                {
                    for (int x = 0; x < HeightMap.Depth; x++)
                    {
                        HeightMap.Height[x,y] = gridBasedErosion.GridPoints[x,y].TerrainHeight;
                    }
                }
                ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            }

            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"INFO: End of simulation after {myConfiguration.SimulationIterations} iterations.");
        });
    }

    private Vector2 GetRandomPositionAtEdgeOfMap()
    {
        return new(myRandom.Next(HeightMap!.Width), 0);
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
