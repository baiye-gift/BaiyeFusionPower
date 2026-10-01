using TUNING;
using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class TritiumBreederConfig : IBuildingConfig
    {
        public const string ID = FusionIds.Breeder;

        public override string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(ID, 3, 3,
                "baiye_tritium_breeder_kanim", 100, 100f, new[] { 600f },
                MATERIALS.REFINED_METALS, 1873.15f, BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NOISY.TIER2);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 1200f;
            def.Overheatable = true;
            def.OverheatTemperature = 573.15f;
            def.Floodable = false;
            def.InputConduitType = ConduitType.Gas;
            def.UtilityInputOffset = new CellOffset(-1, 0);
            def.OutputConduitType = ConduitType.Gas;
            def.UtilityOutputOffset = new CellOffset(1, 1);
            def.PowerInputOffset = new CellOffset(0, 0);
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            def.AudioCategory = "Metal";
            FusionCodex.Attach(def);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            go.AddOrGet<Operational>();
            Storage lithium = go.AddComponent<Storage>();
            lithium.capacityKg = 50f;
            lithium.showInUI = true;
            lithium.storageFilters = new System.Collections.Generic.List<Tag> { FusionIds.TagOf(FusionIds.Lithium) };
            lithium.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage starterDeuterium = go.AddComponent<Storage>();
            starterDeuterium.capacityKg = 5f;
            starterDeuterium.showInUI = true;
            starterDeuterium.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage tritium = go.AddComponent<Storage>();
            tritium.capacityKg = 5f;
            tritium.showInUI = true;
            tritium.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage tailGas = go.AddComponent<Storage>();
            tailGas.capacityKg = 5f;
            tailGas.showInUI = true;
            tailGas.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);

            ManualDeliveryKG lithiumDelivery = go.AddComponent<ManualDeliveryKG>();
            lithiumDelivery.RequestedItemTag = FusionIds.TagOf(FusionIds.Lithium);
            lithiumDelivery.SetStorage(lithium);
            lithiumDelivery.choreTypeIDHash = Db.Get().ChoreTypes.PowerFetch.IdHash;
            lithiumDelivery.capacity = 50f;
            lithiumDelivery.refillMass = 10f;
            lithiumDelivery.MinimumMass = 0.01f;

            ConduitConsumer deuteriumConsumer = go.AddOrGet<ConduitConsumer>();
            deuteriumConsumer.conduitType = ConduitType.Gas;
            deuteriumConsumer.consumptionRate = 0.02f;
            deuteriumConsumer.capacityKG = 5f;
            deuteriumConsumer.capacityTag = FusionIds.TagOf(FusionIds.Deuterium);
            deuteriumConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            deuteriumConsumer.storage = starterDeuterium;
            deuteriumConsumer.forceAlwaysSatisfied = true;
            deuteriumConsumer.alwaysConsume = true;

            ConduitDispenser dispenser = go.AddOrGet<ConduitDispenser>();
            dispenser.conduitType = ConduitType.Gas;
            dispenser.elementFilter = new[] { FusionIds.HashOf(FusionIds.Tritium) };
            dispenser.storage = tritium;
            dispenser.alwaysDispense = true;

            ConduitSecondaryOutput tailPort = go.AddComponent<ConduitSecondaryOutput>();
            tailPort.portInfo = new ConduitPortInfo(ConduitType.Gas, new CellOffset(-1, 1));
            ConduitDispenser tailDispenser = go.AddComponent<ConduitDispenser>();
            tailDispenser.conduitType = ConduitType.Gas;
            tailDispenser.useSecondaryOutput = true;
            tailDispenser.elementFilter = new[] { FusionIds.HashOf(FusionIds.Helium), SimHashes.Hydrogen };
            tailDispenser.storage = tailGas;
            tailDispenser.alwaysDispense = true;

            RadiationEmitter emitter = go.AddComponent<RadiationEmitter>();
            emitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            emitter.emitRadiusX = 5;
            emitter.emitRadiusY = 5;
            emitter.radiusProportionalToRads = false;
            emitter.emitRads = 0f;

            go.AddOrGet<FusionSecondaryEndpoints>();
            go.AddOrGet<TritiumBreederProcess>();
            go.AddOrGet<FusionBatchDispenser>();
            go.AddOrGet<FusionNeutronStatus>();
            go.AddOrGet<FusionVisuals>().machine = FusionVisuals.Machine.Breeder;
        }

        public override void DoPostConfigureComplete(GameObject go) { }
    }
}
