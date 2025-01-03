﻿using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration
{
    internal static class Configuration
    {
        public const ProcessorType HeightMapGeneration = ProcessorType.GPU;
        public const ProcessorType ErosionSimulation = ProcessorType.GPU;
        public const ProcessorType MeshCreation = ProcessorType.GPU;

        public const uint HeightMapSideLength = 512;
        public const uint SimulationIterations = 100000;
        public const int Seed = 1337;
        public const float TangensThresholdAngle = 0.65f;
        public const uint HeightMultiplier = 64;
        public const int ScreenWidth = 1920;
        public const int ScreenHeight = 1080;
        public const uint ParallelExecutions = 10;
        public const uint MaximumModelVertices = ushort.MaxValue;
        public const uint SimulationCallbackEachIterations = 1000;
        public const int ShadowMapResolution = 1028;
    }
}
