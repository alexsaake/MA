using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.Grid;

public struct GridPointShaderBuffer
{
    public float WaterHeight;
    public float SuspendedSediment;
    public float TempSediment;
    public float FlowLeft;
    public float FlowRight;
    public float FlowTop;
    public float FlowBottom;
    private float padding1;
    public Vector2 Velocity;
}
