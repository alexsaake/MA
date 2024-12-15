using Raylib_CsLo;

namespace ProceduralLandscapeGeneration
{
    internal enum SoilTypes
    {
        Stone = 0,
        Sand = 1
    }

    internal static class SoilTypesExtension
    {
        public static Color GetColor(this SoilTypes soilType)
        {
            Color color;
            switch (soilType)
            {
                case SoilTypes.Stone:
                    return Raylib.GRAY;
                case SoilTypes.Sand:
                    return Raylib.YELLOW;
                default:
                    return Raylib.PINK;
            }
        }
    }
}
