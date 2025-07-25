﻿using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Diagnostics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;

internal class GridThermalErosion : IGridThermalErosion
{
    private const string ShaderDirectory = "ErosionSimulation/ThermalErosion/Grid/Shaders/";

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IThermalErosionConfiguration myThermalErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;

    private ComputeShaderProgram? myVerticalFlowComputeShaderProgram;
    private ComputeShaderProgram? myLimitVerticalInflowComputeShaderProgram;
    private ComputeShaderProgram? myDepositeComputeShaderProgram;

    private bool myIsDisposed;

    public GridThermalErosion(IMapGenerationConfiguration mapGenerationConfiguration,IErosionConfiguration erosionConfiguration, IThermalErosionConfiguration thermalErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myThermalErosionConfiguration = thermalErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
    }

    public void Initialize()
    {
        myVerticalFlowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}0VerticalFlowComputeShader.glsl");
        myLimitVerticalInflowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}1LimitVerticalInflowComputeShader.glsl");
        myDepositeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}2DepositeComputeShader.glsl");

        AddGridThermalErosionCellShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void AddGridThermalErosionCellShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.GridThermalErosionCells, (uint)(myThermalErosionConfiguration.GridCellsSize * sizeof(GridThermalErosionCellShaderBuffer)));
    }

    public void ResetShaderBuffers()
    {
        RemoveGridThermalErosionCellShaderBuffer();
        AddGridThermalErosionCellShaderBuffer();
    }

    public void Simulate()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            VerticalFlow();
            if (myMapGenerationConfiguration.LayerCount > 1)
            {
                LimitVerticalInflow();
            }
            Deposite();
        }
        stopwatch.Stop();
        Console.WriteLine($"Grid Thermal erosion: {stopwatch.Elapsed}");
    }

    internal void VerticalFlow()
    {
        Rlgl.EnableShader(myVerticalFlowComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void LimitVerticalInflow()
    {
        Rlgl.EnableShader(myLimitVerticalInflowComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void Deposite()
    {
        Rlgl.EnableShader(myDepositeComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myVerticalFlowComputeShaderProgram?.Dispose();
        myLimitVerticalInflowComputeShaderProgram?.Dispose();
        myDepositeComputeShaderProgram?.Dispose();

        RemoveGridThermalErosionCellShaderBuffer();

        myIsDisposed = true;
    }

    private void RemoveGridThermalErosionCellShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.GridThermalErosionCells);
    }
}
