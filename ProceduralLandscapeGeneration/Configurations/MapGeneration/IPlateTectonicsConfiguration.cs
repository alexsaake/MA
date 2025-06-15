namespace ProceduralLandscapeGeneration.Configurations.MapGeneration;

internal interface IPlateTectonicsConfiguration
{
    bool IsPlateTectonicsRunning { get; set; }
    int PlateCount { get; set; }

    float TransferRate { get; set; }
    float SubductionHeating { get; set; }
    float GenerationCooling { get; set; }
    float GrowthRate { get; set; }
    float DissolutionRate { get; set; }
    float AccelerationConvection { get; set; }
    float TorqueConvection { get; set; }
    float DeltaTime { get; set; }

    event EventHandler? ResetRequired;

    void Dispose();
    void Initialize();
}