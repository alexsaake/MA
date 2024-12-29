namespace ProceduralLandscapeGeneration.Simulation.GPU
{
    internal struct HeightMapParameters
    {
        public uint SideLength;
        public float Scale;
        public uint Octaves;
        public float Persistence;
        public float Lacunarity;
        public int Min;
        public int Max;
        //public Vector2[] OctaveOffsets;
    };
}
