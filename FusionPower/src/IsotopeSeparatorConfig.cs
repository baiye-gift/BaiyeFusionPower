using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class IsotopeSeparatorConfig : IBuildingConfig
    {
        public const string ID = FusionIds.Separator;

        public override string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(ID, 3, 3,
                "baiye_isotope_separator_kanim", 100, 80f, new[] { 400f },
                MATERIALS.REFINED_METALS, 1873.15f, BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.PENALTY.TIER1, NOISE_POLLUTION.NOISY.TIER1);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 480f;
            def.SelfHeatKilowattsWhenActive = 1f;
            def.Overheatable = true;
            def.OverheatTemperature = 398.15f;
            def.Floodable = true;
            def.InputConduitType = ConduitType.Liquid;
            def.UtilityInputOffset = new CellOffset(-1, 0);
            def.OutputConduitType = ConduitType.Gas;
            def.UtilityOutputOffset = new CellOffset(1, 1);
            def.PowerInputOffset = new CellOffset(0, 0);
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            def.AudioCategory = "Metal";
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            go.AddOrGet<Operational>();
            Storage input = go.AddComponent<Storage>();
            input.capacityKg = 20f;
            input.showInUI = true;
            input.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage gasOutput = go.AddComponent<Storage>();
            gasOutput.capacityKg = 5f;
            gasOutput.showInUI = true;
            gasOutput.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage solids = go.AddComponent<Storage>();
            solids.capacityKg = 10f;
            solids.showInUI = true;
            solids.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage depletedLiquid = go.AddComponent<Storage>();
            depletedLiquid.capacityKg = 20f;
            depletedLiquid.showInUI = true;
            depletedLiquid.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);

            ConduitConsumer consumer = go.AddOrGet<ConduitConsumer>();
            consumer.conduitType = ConduitType.Liquid;
            consumer.consumptionRate = 10f;
            consumer.capacityKG = 20f;
            consumer.capacityTag = GameTags.Liquid;
            consumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            consumer.storage = input;
            consumer.forceAlwaysSatisfied = true;

            ConduitDispenser dispenser = go.AddOrGet<ConduitDispenser>();
            dispenser.conduitType = ConduitType.Gas;
            dispenser.elementFilter = new[] { FusionIds.HashOf(FusionIds.Deuterium) };
            dispenser.storage = gasOutput;
            dispenser.alwaysDispense = true;

            ConduitSecondaryOutput liquidPort = go.AddComponent<ConduitSecondaryOutput>();
            liquidPort.portInfo = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(1, 0));
            ConduitDispenser liquidDispenser = go.AddComponent<ConduitDispenser>();
            liquidDispenser.conduitType = ConduitType.Liquid;
            liquidDispenser.useSecondaryOutput = true;
            liquidDispenser.elementFilter = new[] { SimHashes.Water, SimHashes.Brine };
            liquidDispenser.storage = depletedLiquid;
            liquidDispenser.alwaysDispense = true;

            go.AddOrGet<IsotopeSeparatorProcess>();
            go.AddOrGet<FusionBatchDispenser>();
            go.AddOrGet<FusionVisuals>().machine = FusionVisuals.Machine.Separator;
        }

        public override void DoPostConfigureComplete(GameObject go) { }
    }
}
