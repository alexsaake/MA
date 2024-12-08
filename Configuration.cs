namespace ProceduralLandscapeGeneration
{
    internal static class Configuration
    {
        public const int Seed = 1337;
        public const int GLSL_VERSION = 330;
        public const int ScreenWidth = 3840;
        public const int ScreenHeight = 2160;
        public const int ParallelExecutions = 10;
        public const int MaximumModelVertices = ushort.MaxValue + 1;
        public const int HeightMultiplier = 60;
        public const int SimulationIterations = 2000000;
        public const int SimulationCallbackEachIterations = 1000;
    }
}
