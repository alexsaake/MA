﻿using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.CPU.Grid;
using ProceduralLandscapeGeneration.Simulation.CPU.Particle;
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
    private GridBasedErosion? myGridBasedErosion;
    private object myHydraulicErosionGridLock = new();

    public HeightMap? HeightMap { get; private set; }
    public uint HeightMapShaderBufferId => throw new NotImplementedException();
    public uint GridPointsShaderBufferId => throw new NotImplementedException();

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
            int lastCallback = 0;

            for (int iteration = 0; iteration <= myConfiguration.SimulationIterations; iteration += myConfiguration.ParallelExecutions)
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
            int iteration = 0;
            int lastCallback = 0;

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
            int lastCallback = 0;

            for (int iteration = 0; iteration <= myConfiguration.SimulationIterations; iteration += myConfiguration.ParallelExecutions)
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
        if (myRunningSimulation is not null
            && !myRunningSimulation.IsCompleted)
        {
            Console.WriteLine($"INFO: Simulation already running on CPU.");
            return;
        }

        //myGridBasedErosion = new GridBasedErosion(HeightMap!);

        Console.WriteLine($"INFO: Simulating hydraulic erosion grid.");
        myRunningSimulation = Task.Run(() =>
        {
            int iteration = 0;

            while (myGridBasedErosion is not null)
            {
                lock (myHydraulicErosionGridLock)
                {
                    //myGridBasedErosion.Simulate(HeightMap!);
                };
                if (iteration % 10 == 0)//% myConfiguration.SimulationCallbackEachIterations == 0
                {
                    ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                    Console.WriteLine($"INFO: Step {iteration}.");
                }
                iteration++;
            }
        });
    }

    public void SimulateHydraulicErosionGridAddRain()
    {
        const float waterIncrease = 0.01f;

        if (myGridBasedErosion is null)
        {
            return;
        }

        Console.WriteLine($"INFO: Adding {HeightMap!.Width} rain drops for hydraulic erosion grid.");
        lock (myHydraulicErosionGridLock)
        {
            for (int drop = 0; drop < HeightMap!.Width; drop++)
            {
                IVector2 newPosition = new(myRandom.Next(HeightMap!.Width), myRandom.Next(HeightMap.Depth));
                //myGridBasedErosion!.WaterIncrease(newPosition, waterIncrease);
            }
        }
    }

    public void SimulateHydraulicErosionGridStop()
    {
        myGridBasedErosion = null;
        Console.WriteLine($"INFO: End of simulation hydraulic erosion grid.");
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
