using TUNING;
using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class FusionReactorConfig : IBuildingConfig
    {
        public const string ID = FusionIds.Reactor;

        public override string[] GetRequiredDlcIds() => DlcManager.EXPANSION1;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(ID, 5, 4,
                "baiye_fusion_reactor_kanim", 250, 200f, new[] { 1200f },
                MATERIALS.REFINED_METALS, 2473.15f, BuildLocationRule.OnFloor,
                BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NOISY.TIER3);
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = FusionReactorBalance.OperatingWatts;
            def.RequiresPowerOutput = true;
            def.GeneratorWattageRating = FusionReactorBalance.GrossWatts;
            def.GeneratorBaseCapacity = FusionReactorBalance.GeneratorCapacityJoules;
            def.PowerOutputOffset = new CellOffset(0, 0);
            def.Overheatable = false;
            def.Floodable = false;
            def.InputConduitType = ConduitType.Gas;
            def.UtilityInputOffset = new CellOffset(-2, 0);
            def.OutputConduitType = ConduitType.Gas;
            def.UtilityOutputOffset = new CellOffset(2, 0);
            def.PowerInputOffset = new CellOffset(0, 0);
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            def.AudioCategory = "HollowMetal";
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            go.AddOrGet<Operational>();
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.PowerBuilding);
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.GeneratorType);
            // Keep already generated energy connected during idle/disabled states.
            // Only the paid reaction callback creates new joules.
            go.AddOrGet<Generator>().connectedTags = new Tag[0];
            Storage deuterium = go.AddComponent<Storage>();
            deuterium.capacityKg = 2f;
            deuterium.showInUI = true;
            deuterium.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage tritium = go.AddComponent<Storage>();
            tritium.capacityKg = 10f;
            tritium.showInUI = true;
            tritium.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage exhaust = go.AddComponent<Storage>();
            exhaust.capacityKg = 5f;
            exhaust.showInUI = true;
            exhaust.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage coolantInput = go.AddComponent<Storage>();
            coolantInput.capacityKg = 20f;
            coolantInput.showInUI = true;
            coolantInput.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            Storage coolantOutput = go.AddComponent<Storage>();
            coolantOutput.capacityKg = 20f;
            coolantOutput.showInUI = true;
            coolantOutput.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);

            ConduitConsumer deuteriumConsumer = go.AddComponent<ConduitConsumer>();
            deuteriumConsumer.conduitType = ConduitType.Gas;
            deuteriumConsumer.consumptionRate = 1f;
            deuteriumConsumer.capacityKG = 2f;
            deuteriumConsumer.capacityTag = FusionIds.TagOf(FusionIds.Deuterium);
            deuteriumConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            deuteriumConsumer.storage = deuterium;
            deuteriumConsumer.forceAlwaysSatisfied = true;
            deuteriumConsumer.alwaysConsume = true;

            ConduitSecondaryInput tritiumPort = go.AddComponent<ConduitSecondaryInput>();
            tritiumPort.portInfo = new ConduitPortInfo(ConduitType.Gas, new CellOffset(0, 0));
            ConduitConsumer tritiumConsumer = go.AddComponent<ConduitConsumer>();
            tritiumConsumer.conduitType = ConduitType.Gas;
            tritiumConsumer.useSecondaryInput = true;
            tritiumConsumer.consumptionRate = 1f;
            tritiumConsumer.capacityKG = 10f;
            tritiumConsumer.capacityTag = FusionIds.TagOf(FusionIds.Tritium);
            tritiumConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            tritiumConsumer.storage = tritium;
            tritiumConsumer.forceAlwaysSatisfied = true;
            tritiumConsumer.alwaysConsume = true;

            ConduitSecondaryInput coolantInputPort = go.AddComponent<ConduitSecondaryInput>();
            coolantInputPort.portInfo = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(-1, 0));
            ConduitConsumer coolantConsumer = go.AddComponent<ConduitConsumer>();
            coolantConsumer.conduitType = ConduitType.Liquid;
            coolantConsumer.useSecondaryInput = true;
            coolantConsumer.consumptionRate = 10f;
            coolantConsumer.capacityKG = 20f;
            coolantConsumer.capacityTag = SimHashes.SuperCoolant.CreateTag();
            coolantConsumer.wrongElementResult = ConduitConsumer.WrongElementResult.Dump;
            coolantConsumer.storage = coolantInput;
            coolantConsumer.forceAlwaysSatisfied = true;
            coolantConsumer.alwaysConsume = true;

            ConduitDispenser dispenser = go.AddOrGet<ConduitDispenser>();
            dispenser.conduitType = ConduitType.Gas;
            dispenser.elementFilter = new[] { FusionIds.HashOf(FusionIds.Helium) };
            dispenser.storage = exhaust;
            dispenser.alwaysDispense = true;

            ConduitSecondaryOutput coolantOutputPort = go.AddComponent<ConduitSecondaryOutput>();
            coolantOutputPort.portInfo = new ConduitPortInfo(ConduitType.Liquid, new CellOffset(1, 0));
            ConduitDispenser coolantDispenser = go.AddComponent<ConduitDispenser>();
            coolantDispenser.conduitType = ConduitType.Liquid;
            coolantDispenser.useSecondaryOutput = true;
            coolantDispenser.elementFilter = new[] { SimHashes.SuperCoolant };
            coolantDispenser.storage = coolantOutput;
            coolantDispenser.alwaysDispense = true;

            RadiationEmitter emitter = go.AddComponent<RadiationEmitter>();
            emitter.emitType = RadiationEmitter.RadiationEmitterType.Constant;
            emitter.emitRadiusX = 8;
            emitter.emitRadiusY = 8;
            emitter.radiusProportionalToRads = false;
            emitter.emissionOffset = new Vector3(0f, 1f, 0f);
            emitter.emitRads = 0f;

            go.AddOrGet<FusionSecondaryEndpoints>();
            go.AddOrGet<FusionReactorProcess>();
            go.AddOrGet<FusionBatchDispenser>();
            go.AddOrGet<FusionNeutronStatus>();
            go.AddOrGet<FusionVisuals>().machine = FusionVisuals.Machine.Reactor;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<KBatchedAnimHeatPostProcessingEffect>();
        }
    }
}
