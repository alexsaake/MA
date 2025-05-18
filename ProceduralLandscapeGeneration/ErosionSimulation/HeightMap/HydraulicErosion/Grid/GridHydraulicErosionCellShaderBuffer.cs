using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.HydraulicErosion.Grid;

public struct GridHydraulicErosionCellShaderBuffer
{
    public float WaterHeight;
    public float FlowLeft;
    public float FlowRight;
    public float FlowUp;
    public float FlowDown;
    public float SuspendedSediment;
    public float SedimentFlowLeft;
    public float SedimentFlowRight;
    public float SedimentFlowUp;
    public float SedimentFlowDown;
    public Vector2 Velocity;
}
