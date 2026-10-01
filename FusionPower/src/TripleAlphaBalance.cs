namespace Baiye.FusionPower
{
    internal static class TripleAlphaBalance
    {
        // 3 helium-4 atoms form carbon-12. Atomic electrons cancel in this ratio.
        // Released energy and power below use the existing mod's game scaling.
        internal const float CarbonMassRatio = (float)(12.0 / (3.0 * 4.00260325413));
        internal const float HeliumKgPerSecond = 0.08f;
        internal const float GrossWatts = 4400f;
        internal const float OperatingWatts = 1200f;
        internal const float GeneratorCapacityJoules = 8800f;
        internal const float SourceHeatKDTUPerSecond = 5500f;
        internal const float ConvertedHeatKDTUPerSecond = 4950f;
        internal const float ResidualHeatKDTUPerSecond = SourceHeatKDTUPerSecond - ConvertedHeatKDTUPerSecond;
        internal const float CarbonKelvin = 350f;
        internal const float CarbonBufferKg = 50f;
        internal const float CarbonPacketKg = 20f;
    }
}
