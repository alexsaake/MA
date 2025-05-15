using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;

public struct GridHydraulicErosionCellShaderBuffer
{
    public float WaterHeight;
    public float SuspendedSediment;
    public float TempSediment;
    public float FlowLeft;
    public float FlowRight;
    public float FlowTop;
    public float FlowBottom;
    private readonly float padding1;
    public Vector2 Velocity;
}
