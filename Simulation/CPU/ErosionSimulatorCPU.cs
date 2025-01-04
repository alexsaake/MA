﻿using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU;

internal class ErosionSimulatorCPU : IErosionSimulator
{
    private readonly IConfiguration myConfigration;
    private readonly IRandom myRandom;
    private readonly IHeightMapGenerator myHeightMapGenerator;

    private bool myIsDisposed;

    public HeightMap? HeightMap { get; private set; }
    public uint HeightMapShaderBufferId => throw new NotImplementedException();

    public event EventHandler? ErosionIterationFinished;

    public ErosionSimulatorCPU(IConfiguration configuration, IRandom random, IHeightMapGenerator heightMapGenerator)
    {
        myConfigration = configuration;
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

            for (uint iteration = 0; iteration <= myConfigration.SimulationIterations; iteration += myConfigration.ParallelExecutions)
            {
                List<Task> parallelExecutionTasks = new List<Task>();
                for (int parallelExecution = 0; parallelExecution < myConfigration.ParallelExecutions; parallelExecution++)
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

                if (iteration % myConfigration.SimulationCallbackEachIterations == 0
            && iteration != lastCallback)
                {
                    ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                    lastCallback = iteration;
                    Console.WriteLine($"INFO: Step {iteration} of {myConfigration.SimulationIterations}.");
                }
            }

            ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"INFO: End of simulation after {myConfigration.SimulationIterations} iterations.");
        });
    }

    public void SimulateThermalErosion()
    {
        const float heightChange = 0.001f;

        float tangensTalusAngle = MathF.Tan(myConfigration.TalusAngle);

        Console.WriteLine($"INFO: Simulating thermal erosion on each cell of the height map.");
        Task.Run(() =>
        {
            uint mapSize = myConfigration.HeightMapSideLength * myConfigration.HeightMapSideLength;
            uint iteration = 0;
            uint lastCallback = 0;

            for (int y = 0; y <= myConfigration.HeightMapSideLength; y++)
            {
                for (int x = 0; x <= myConfigration.HeightMapSideLength;)
                {
                    List<Task> parallelExecutionTasks = new List<Task>();
                    for (int parallelExecution = 0; parallelExecution < myConfigration.ParallelExecutions; parallelExecution++)
                    {
                        if (x > myConfigration.HeightMapSideLength)
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
                            float neighborHeight = HeightMap.Height[neighborPosition.X, neighborPosition.Y] * myConfigration.HeightMultiplier;
                            float zDiff = HeightMap.Height[localX, localY] * myConfigration.HeightMultiplier - neighborHeight;

                            if (zDiff > tangensTalusAngle)
                            {
                                HeightMap.Height[localX, localY] -= heightChange;
                                HeightMap.Height[neighborPosition.X, neighborPosition.Y] += heightChange;
                            }
                        }));
                        x++;
                        iteration++;
                    }
                    Task.WaitAll(parallelExecutionTasks.ToArray());

                    if (iteration % myConfigration.SimulationCallbackEachIterations == 0
                        && iteration != lastCallback)
                    {
                        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                        lastCallback = iteration;
                        Console.WriteLine($"INFO: Step {iteration} of {mapSize}.");
                    }
                }
            }

            Console.WriteLine($"INFO: End of simulation after {myConfigration.SimulationIterations} iterations.");
        });
    }

    public void SimulateWindErosion()
    {
        Console.WriteLine($"INFO: Simulating wind erosion.");
        Task.Run(() =>
        {
            uint lastCallback = 0;

            for (uint iteration = 0; iteration <= myConfigration.SimulationIterations; iteration += myConfigration.ParallelExecutions)
            {
                List<Task> parallelExecutionTasks = new List<Task>();
                for (int parallelExecution = 0; parallelExecution < myConfigration.ParallelExecutions; parallelExecution++)
                {
                    Vector2 newPosition = GetRandomPositionAtEdgeOfMap();
                    parallelExecutionTasks.Add(Task.Run(() =>
                    {
                        WindParticle windParticle = new(newPosition);
                        windParticle.Fly(HeightMap!, new Vector3(0, 1, 0));
                    }));
                }
                Task.WaitAll(parallelExecutionTasks.ToArray());

                if (iteration % myConfigration.SimulationCallbackEachIterations == 0
                    && iteration != lastCallback)
                {
                    ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
                    lastCallback = iteration;
                    Console.WriteLine($"INFO: Step {iteration} of {myConfigration.SimulationIterations}.");
                }
            }

            Console.WriteLine($"INFO: End of simulation after {myConfigration.SimulationIterations} iterations.");
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
