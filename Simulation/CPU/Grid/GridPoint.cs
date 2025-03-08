namespace ProceduralLandscapeGeneration.Simulation.CPU.Grid
{
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

        public float VelocityX;
        public float VelocityY;
    }
}
