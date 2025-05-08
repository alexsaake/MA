using ProceduralLandscapeGeneration.Config.Types;

namespace ProceduralLandscapeGeneration.Config
{
    internal interface IErosionConfiguration : IDisposable
    {
        ErosionModeTypes Mode { get; set; }
        bool IsRunning { get; set; }
        bool IsRainAdded { get; set; }
        bool IsWaterDisplayed { get; set; }
        bool IsSedimentDisplayed { get; set; }

        int TalusAngle { get; set; }
        float ThermalErosionHeightChange { get; set; }

        void Initialize();
    }
}