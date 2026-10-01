using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Baiye.FusionPower
{
    // List choices and conduit packet matching must use the same element tag.
    [HarmonyPatch]
    internal static class FusionFilterTagPatch
    {
        // U59 uses Create for Filterable choices and CreateTag for actual
        // ElementFilter routing. They are separate methods, not aliases.
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(GameTagExtensions), nameof(GameTagExtensions.Create), new[] { typeof(SimHashes) });
            yield return AccessTools.Method(typeof(GameTagExtensions), nameof(GameTagExtensions.CreateTag), new[] { typeof(SimHashes) });
        }

        private static void Postfix(SimHashes id, ref Tag __result)
        {
            if (!FusionStrings.IsFusionElement(id)) return;
            Element element = ElementLoader.FindElementByHash(id);
            if (element != null) __result = element.tag;
        }
    }
}
