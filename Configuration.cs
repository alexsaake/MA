namespace ProceduralLandscapeGeneration
{
    internal static class Configuration
    {
        public const int Seed = 1337;
        public const int GLSL_VERSION = 330;
        public const int ScreenWidth = 1920;
        public const int ScreenHeight = 1080;
        public const int ParallelExecutions = 10;
        public const int MaximumModelVertices = ushort.MaxValue;
        public const int HeightMultiplier = 60;
        public const int SimulationCallbackEachIterations = 100;
    }
}
