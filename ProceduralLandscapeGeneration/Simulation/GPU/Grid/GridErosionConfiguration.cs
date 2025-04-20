namespace ProceduralLandscapeGeneration.Simulation.GPU.Grid;

public struct GridErosionConfiguration
{
    public float TimeDelta;
    public float CellSizeX;
    public float CellSizeY;
    public float Gravity;
    public float Friction;
    public float MaximalErosionDepth;
    public float SedimentCapacity;
    public float SuspensionRate;
    public float DepositionRate;
    public float SedimentSofteningRate;
    public float EvaporationRate;
}
