namespace ProceduralLandscapeGeneration.Simulation.CPU.Grid;

public struct GridPoint
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

    public float VelocityX;
    public float VelocityY;
}
