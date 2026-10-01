namespace Baiye.FusionPower
{
    // Game balance units: kDTU and electrical joules are separate ONI resources.
    // This allocation is not a real-world thermal conversion efficiency.
    internal static class FusionReactorBalance
    {
        internal const float GrossWatts = 16000f;
        internal const float OperatingWatts = 2000f;
        internal const float GeneratorCapacityJoules = 32000f;
        internal const float SourceHeatKDTUPerSecond = 20000f;
        internal const float ConvertedHeatKDTUPerSecond = 18000f;
        internal const float CoolantHeatKDTUPerSecond = SourceHeatKDTUPerSecond - ConvertedHeatKDTUPerSecond;
        internal const float CoolantKgPerSecond = 10f;
        internal const float CoolantReserveKg = 20f;
        internal const float CoolantSpecificHeat = 8.44f;
        internal const float MaxCoolantKelvin = 680f;
    }
}
