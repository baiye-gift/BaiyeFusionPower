using System;

namespace Baiye.FusionPower
{
    internal static class FusionIds
    {
        internal const string Deuterium = "BaiyeDeuterium";
        internal const string LiquidDeuterium = "BaiyeLiquidDeuterium";
        internal const string SolidDeuterium = "BaiyeSolidDeuterium";
        internal const string Tritium = "BaiyeTritium";
        internal const string LiquidTritium = "BaiyeLiquidTritium";
        internal const string SolidTritium = "BaiyeSolidTritium";
        internal const string Helium = "BaiyeHelium4";
        internal const string LiquidHelium = "BaiyeLiquidHelium4";
        internal const string Lithium = "BaiyeLithium6";
        internal const string LiquidLithium = "BaiyeLiquidLithium6";
        internal const string LithiumVapor = "BaiyeLithium6Vapor";

        internal const string Separator = "BaiyeIsotopeSeparator";
        internal const string Breeder = "BaiyeTritiumBreeder";
        internal const string Reactor = "BaiyeFusionReactor";
        internal const string TripleAlpha = "BaiyeTripleAlphaFurnace";

        internal static SimHashes HashOf(string id) => (SimHashes)Hash.SDBMLower(id);
        internal static Tag TagOf(string id) => TagManager.Create(id);
    }
}
