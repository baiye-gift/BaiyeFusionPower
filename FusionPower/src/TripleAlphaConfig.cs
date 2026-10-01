using TUNING;
using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class TripleAlphaConfig : IBuildingConfig
    {
        public const string ID = FusionIds.TripleAlpha;
        public override string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(ID, 4, 4,
                "baiye_triple_alpha_kanim", 250, 200f, new[] { 1200f }, MATERIALS.REFINED_METALS,
                2473.15f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NOISY.TIER3);
            def.RequiresPowerInput = def.RequiresPowerOutput = true;
            def.EnergyConsumptionWhenActive = TripleAlphaBalance.OperatingWatts;
            def.GeneratorWattageRating = TripleAlphaBalance.GrossWatts;
            def.GeneratorBaseCapacity = TripleAlphaBalance.GeneratorCapacityJoules;
            def.PowerInputOffset = def.PowerOutputOffset = new CellOffset(0, 0);
            def.InputConduitType = ConduitType.Gas;
            def.UtilityInputOffset = new CellOffset(-1, 0);
            def.OutputConduitType = ConduitType.Solid;
            def.UtilityOutputOffset = new CellOffset(2, 0);
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            def.Overheatable = def.Floodable = false;
            def.AudioCategory = "HollowMetal";
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SolidConveyorIDs, ID);
            return def;
        }

        private static Storage AddStore(GameObject go, float capacity)
        {
            Storage store = go.AddComponent<Storage>();
            store.capacityKg = capacity;
            store.showInUI = true;
            store.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            return store;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            KPrefabID prefab = go.GetComponent<KPrefabID>();
            prefab.AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            prefab.AddTag(RoomConstraints.ConstraintTags.PowerBuilding);
            prefab.AddTag(RoomConstraints.ConstraintTags.GeneratorType);
            go.AddOrGet<Operational>();
            go.AddOrGet<Generator>().connectedTags = new Tag[0];
            Storage helium = AddStore(go, 10f);
            Storage carbon = AddStore(go, TripleAlphaBalance.CarbonBufferKg);
            Storage cold = AddStore(go, 20f);
            Storage hot = AddStore(go, 20f);
            Storage dispatch = AddStore(go, TripleAlphaBalance.CarbonPacketKg);

            ConduitConsumer gas = go.AddComponent<ConduitConsumer>();
            gas.conduitType = ConduitType.Gas;
            gas.consumptionRate = 1f;
            gas.capacityKG = 10f;
            gas.capacityTag = FusionIds.TagOf(FusionIds.Helium);
            gas.storage = helium;
            gas.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            gas.alwaysConsume = gas.forceAlwaysSatisfied = true;

            go.AddComponent<ConduitSecondaryInput>().portInfo = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(0, 0));
            ConduitConsumer liquid = go.AddComponent<ConduitConsumer>();
            liquid.conduitType = ConduitType.Liquid;
            liquid.useSecondaryInput = true;
            liquid.consumptionRate = FusionReactorBalance.CoolantKgPerSecond;
            liquid.capacityKG = 20f;
            liquid.capacityTag = SimHashes.SuperCoolant.CreateTag();
            liquid.storage = cold;
            liquid.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            liquid.alwaysConsume = liquid.forceAlwaysSatisfied = true;

            go.AddComponent<ConduitSecondaryOutput>().portInfo = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(1, 0));
            ConduitDispenser coolant = go.AddComponent<ConduitDispenser>();
            coolant.conduitType = ConduitType.Liquid;
            coolant.useSecondaryOutput = true;
            coolant.storage = hot;
            coolant.elementFilter = new[] { SimHashes.SuperCoolant };
            coolant.alwaysDispense = true;

            SolidConduitDispenser product = go.AddComponent<SolidConduitDispenser>();
            product.storage = dispatch;
            product.solidOnly = true;
            product.alwaysDispense = true;

            RadiationEmitter emitter = go.AddComponent<RadiationEmitter>();
            emitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            emitter.emitRadiusX = emitter.emitRadiusY = 4;
            emitter.radiusProportionalToRads = false;
            emitter.emissionOffset = new Vector3(0f, 1f, 0f);
            emitter.emitRads = 0f;

            go.AddOrGet<FusionSecondaryEndpoints>();
            go.AddOrGet<TripleAlphaCarbonBatch>();
            go.AddOrGet<TripleAlphaProcess>();
            go.AddOrGet<TripleAlphaStatus>();
            go.AddOrGet<FusionVisuals>().machine = FusionVisuals.Machine.TripleAlpha;
        }

        public override void DoPostConfigureComplete(GameObject go) => go.AddOrGet<KBatchedAnimHeatPostProcessingEffect>();
    }
}
