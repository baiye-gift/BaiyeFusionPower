using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class IsotopeSeparatorProcess : FusionProcessBase
    {
        private const float LithiumBatchKg = 5f;
        private Storage feed;
        private Storage gas;
        private Storage solids;
        private Storage depletedLiquid;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Storage[] stores = GetComponents<Storage>();
            feed = stores[0];
            gas = stores[1];
            solids = stores[2];
            // Also expose the retained native inventory on existing buildings.
            solids.showInUI = true;
            depletedLiquid = stores[3];
        }

        protected override bool CanProcess(float dt)
        {
            PurgeOtherElements(feed, SimHashes.Water, SimHashes.Brine);
            float water = feed.GetMassAvailable(SimHashes.Water);
            float brine = feed.GetMassAvailable(SimHashes.Brine);
            float inputMass = 10f * dt;
            bool makeDeuterium = water >= inputMass;
            if (!makeDeuterium && brine < inputMass) return false;

            float yield = (makeDeuterium ? 0.02f : 0.06f) * dt;
            if (makeDeuterium && Mathf.Min(gas.capacityKg, 5f) - gas.ExactMassStored() < yield) return false;
            if (!makeDeuterium && Mathf.Min(solids.capacityKg, 10f) - solids.ExactMassStored() < yield) return false;
            if (Mathf.Min(depletedLiquid.capacityKg, 20f) - depletedLiquid.ExactMassStored() < inputMass - yield) return false;
            return true;
        }

        protected override bool Process(float dt)
        {
            if (!CanProcess(dt)) return false;
            float inputMass = 10f * dt;
            bool makeDeuterium = feed.GetMassAvailable(SimHashes.Water) >= inputMass;
            float yield = (makeDeuterium ? 0.02f : 0.06f) * dt;
            SimHashes input = makeDeuterium ? SimHashes.Water : SimHashes.Brine;
            var item = feed.FindFirst(input.CreateTag());
            float temperature = item != null ? item.GetComponent<PrimaryElement>().Temperature : 300f;
            feed.ConsumeIgnoringDisease(input.CreateTag(), inputMass);
            if (makeDeuterium)
                gas.AddGasChunk(FusionIds.HashOf(FusionIds.Deuterium), yield, Mathf.Max(300f, temperature), byte.MaxValue, 0, false);
            else
            {
                solids.AddOre(FusionIds.HashOf(FusionIds.Lithium), yield, temperature, byte.MaxValue, 0);
                // Native AddOre merges into the existing stored pile. Keep that
                // pile across ticks/blackouts/loads rather than spawning debris
                // every 200 ms. Drop the complete pile; do not lose threshold
                // overshoot by rounding or discarding a remainder.
                if (solids.GetMassAvailable(FusionIds.HashOf(FusionIds.Lithium)) >= LithiumBatchKg)
                    solids.DropAll();
            }
            depletedLiquid.AddLiquid(input, inputMass - yield, temperature, byte.MaxValue, 0);
            return true;
        }
    }
}
