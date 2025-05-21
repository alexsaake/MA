namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;

public struct GridThermalErosionCellShaderBuffer
{
    public float FineSedimentFlowLeft;
    public float FineSedimentFlowRight;
    public float FineSedimentFlowUp;
    public float FineSedimentFlowDown;

    public float CoarseSedimentFlowLeft;
    public float CoarseSedimentFlowRight;
    public float CoarseSedimentFlowUp;
    public float CoarseSedimentFlowDown;

    public float BedrockFlowLeft;
    public float BedrockFlowRight;
    public float BedrockFlowUp;
    public float BedrockFlowDown;
}
