using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.Grid;

public struct GridPointShaderBuffer
{
    public float WaterHeight;
    public float SuspendedSediment;
    public float TempSediment;
    public float Hardness;

    public float FlowLeft;
    public float FlowRight;
    public float FlowTop;
    public float FlowBottom;

    public float ThermalLeft;
    public float ThermalRight;
    public float ThermalTop;
    public float ThermalBottom;

    public Vector2 Velocity;
}
