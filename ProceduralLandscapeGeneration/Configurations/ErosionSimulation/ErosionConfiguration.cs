using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation;

internal class ErosionConfiguration : IErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    public ulong IterationCount { get; set; }
    public bool IsSimulationRunning { get; set; }
    public bool IsHydraulicErosionEnabled { get; set; }
    public HydraulicErosionModeTypes HydraulicErosionMode { get; set; }
    public WaterSourceTypes WaterSource { get; set; }
    public uint WaterSourceXCoordinate { get; set; }
    public uint WaterSourceYCoordinate { get; set; }
    public uint WaterSourceRadius { get; set; }
    public bool IsWindErosionEnabled { get; set; }
    public WindErosionModeTypes WindErosionMode { get; set; }
    public bool IsThermalErosionEnabled { get; set; }
    public ThermalErosionModeTypes ThermalErosionMode { get; set; }

    public uint IterationsPerStep { get; set; }
    public bool IsWaterAdded { get; set; }
    public bool IsWaterDisplayed { get; set; }
    public bool IsSedimentDisplayed { get; set; }

    public bool IsSeaLevelDisplayed { get; set; }

    private bool myIsWaterKeptInBoundaries;
    public bool IsWaterKeptInBoundaries
    {
        get => myIsWaterKeptInBoundaries;
        set
        {
            if (myIsWaterKeptInBoundaries == value)
            {
                return;
            }
            myIsWaterKeptInBoundaries = value;
            UpdateShaderBuffer();
        }
    }

    private float myDeltaTime;
    public float DeltaTime
    {
        get => myDeltaTime;
        set
        {
            if (myDeltaTime == value)
            {
                return;
            }
            myDeltaTime = value;
            UpdateShaderBuffer();
        }
    }

    public ErosionConfiguration(IShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myShaderBuffers = shaderBuffers;

        IsHydraulicErosionEnabled = false;
        HydraulicErosionMode = HydraulicErosionModeTypes.GridHydraulic;
        WaterSource = WaterSourceTypes.Rain;
        WaterSourceXCoordinate = mapGenerationConfiguration.HeightMapSideLength / 2;
        WaterSourceYCoordinate = mapGenerationConfiguration.HeightMapSideLength / 10;
        WaterSourceRadius = 5;
        IsThermalErosionEnabled = false;
        ThermalErosionMode = ThermalErosionModeTypes.GridThermal;
        IsWindErosionEnabled = false;
        WindErosionMode = WindErosionModeTypes.ParticleWind;

        IsSimulationRunning = false;
        IterationsPerStep = 1;
        IsWaterAdded = false;

        IsWaterDisplayed = false;
        IsSedimentDisplayed = false;
        IsSeaLevelDisplayed = true;

        myIsWaterKeptInBoundaries = false;

        myDeltaTime = 1.0f;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.ErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.ErosionConfiguration, (uint)sizeof(ErosionConfigurationShaderBuffer));
        }
        ErosionConfigurationShaderBuffer erosionConfigurationShaderBuffer = new ErosionConfigurationShaderBuffer()
        {
            DeltaTime = DeltaTime,
            IsWaterKeptInBoundaries = IsWaterKeptInBoundaries
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], &erosionConfigurationShaderBuffer, (uint)sizeof(ErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.ErosionConfiguration);

        myIsDisposed = true;
    }
}
