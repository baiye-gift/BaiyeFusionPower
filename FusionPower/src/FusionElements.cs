using System;
using System.Collections.Generic;
using System.IO;
using ElementData;
using HarmonyLib;
using Klei;
using UnityEngine;

namespace Baiye.FusionPower
{
    [HarmonyPatch(typeof(ElementLoader), nameof(ElementLoader.CollectElementsFromYAML))]
    internal static class FusionElementLoadPatch
    {
        private static void Postfix(ref List<ElementEntry> __result)
        {
            string folder = Path.GetDirectoryName(typeof(FusionMod).Assembly.Location);
            string path = Path.Combine(folder, "elem", "elements.yaml");
            ElementEntryCollection collection;
            FileHandle handle = new FileHandle { full_path = path };
            if (KYaml.LoadFile(handle, out collection, (file, ex) => Debug.LogError("[BaiyeFusionPower] Element YAML: " + ex))
                && collection != null && collection.elements != null)
            {
                __result.AddRange(collection.elements);
            }
        }
    }

    [HarmonyPatch(typeof(ElementLoader), nameof(ElementLoader.Load))]
    internal static class FusionElementNamesPatch
    {
        private static void Postfix()
        {
            FusionStrings.Register();
            FusionStrings.RefreshLoadedElements();
        }
    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
    internal static class FusionLocalizationPatch
    {
        private static void Postfix()
        {
            FusionStrings.Register();
            FusionStrings.RefreshLoadedElements();
        }
    }

    [HarmonyPatch(typeof(Game), "OnSpawn")]
    [HarmonyPriority(Priority.Last)]
    internal static class FusionElementFinalNamesPatch
    {
        private static void Postfix()
        {
            FusionStrings.Register();
            FusionStrings.RefreshLoadedElements();
            if (ElementLoader.elementTable == null) return;
            foreach (Element element in ElementLoader.elements)
            {
                if (element == null) continue;
                if (FusionStrings.IsFusionElement(element.id) || element.name == "-")
                {
                    Tag filterTag = GameTagExtensions.Create(element.id);
                    Tag packetTag = GameTagExtensions.CreateTag(element.id);
                    Debug.Log("[BaiyeFusionPower] Element label check: " + element.id + " = " + element.name
                        + ", filter=" + filterTag.ProperName() + ", selectable="
                        + (ElementLoader.GetElement(filterTag) == element)
                        + ", packet=" + packetTag.Name + ", routingMatch=" + (filterTag == packetTag));
                }
            }
        }
    }

    [HarmonyPatch(typeof(SubstanceTable), nameof(SubstanceTable.GetSubstance))]
    internal static class FusionSubstancePatch
    {
        private static void Postfix(SimHashes substance, ref Substance __result)
        {
            string id = null;
            SimHashes template = SimHashes.Hydrogen;
            Element.State state = Element.State.Gas;
            Color32 tint = new Color32(130, 210, 255, 255);
            if (substance == FusionIds.HashOf(FusionIds.Deuterium)) id = FusionIds.Deuterium;
            else if (substance == FusionIds.HashOf(FusionIds.LiquidDeuterium)) { id = FusionIds.LiquidDeuterium; template = SimHashes.LiquidHydrogen; state = Element.State.Liquid; }
            else if (substance == FusionIds.HashOf(FusionIds.SolidDeuterium)) { id = FusionIds.SolidDeuterium; template = SimHashes.SolidHydrogen; state = Element.State.Solid; }
            else if (substance == FusionIds.HashOf(FusionIds.Tritium)) { id = FusionIds.Tritium; tint = new Color32(184, 129, 255, 255); }
            else if (substance == FusionIds.HashOf(FusionIds.LiquidTritium)) { id = FusionIds.LiquidTritium; template = SimHashes.LiquidHydrogen; state = Element.State.Liquid; tint = new Color32(184, 129, 255, 255); }
            else if (substance == FusionIds.HashOf(FusionIds.SolidTritium)) { id = FusionIds.SolidTritium; template = SimHashes.SolidHydrogen; state = Element.State.Solid; tint = new Color32(184, 129, 255, 255); }
            else if (substance == FusionIds.HashOf(FusionIds.Helium)) { id = FusionIds.Helium; tint = new Color32(255, 230, 164, 255); }
            else if (substance == FusionIds.HashOf(FusionIds.LiquidHelium)) { id = FusionIds.LiquidHelium; template = SimHashes.LiquidHydrogen; state = Element.State.Liquid; tint = new Color32(255, 230, 164, 255); }
            else if (substance == FusionIds.HashOf(FusionIds.Lithium)) { id = FusionIds.Lithium; template = SimHashes.Salt; state = Element.State.Solid; tint = new Color32(226, 220, 154, 255); }
            else if (substance == FusionIds.HashOf(FusionIds.LiquidLithium)) { id = FusionIds.LiquidLithium; template = SimHashes.MoltenSalt; state = Element.State.Liquid; tint = new Color32(226, 220, 154, 255); }
            else if (substance == FusionIds.HashOf(FusionIds.LithiumVapor)) { id = FusionIds.LithiumVapor; template = SimHashes.Hydrogen; state = Element.State.Gas; tint = new Color32(226, 220, 154, 255); }
            if (id == null) return;

            Element source = ElementLoader.FindElementByHash(template);
            if (source == null || source.substance == null)
            {
                Debug.LogError("[BaiyeFusionPower] Missing template substance: " + template);
                return;
            }
            Substance baseSubstance = source.substance;
            __result = ModUtil.CreateSubstance(id, state, baseSubstance.anim, baseSubstance.material,
                tint, tint, tint);
        }
    }
}
