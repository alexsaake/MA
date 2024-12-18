using Raylib_cs;

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
            return soilType switch
            {
                SoilTypes.Stone => Color.Gray,
                SoilTypes.Sand => Color.Yellow,
                _ => Color.Pink,
            };
        }
    }
}
