using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;

public struct GridHydraulicErosionCellShaderBuffer
{
    public float WaterHeight;

    public float WaterFlowLeft;
    public float WaterFlowRight;
    public float WaterFlowUp;
    public float WaterFlowDown;

    public float SuspendedSediment;

    public float SedimentFlowLeft;
    public float SedimentFlowRight;
    public float SedimentFlowUp;
    public float SedimentFlowDown;

    public Vector2 WaterVelocity;
}
