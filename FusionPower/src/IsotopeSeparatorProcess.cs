using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class IsotopeSeparatorProcess : FusionProcessBase
    {
        private const float LithiumBatchKg = 5f;
        internal static readonly SimHashes[] AcceptedLiquids = { SimHashes.Water, SimHashes.Brine, SimHashes.SaltWater };
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
            PurgeOtherElements(feed, AcceptedLiquids);
            float inputMass = 10f * dt;
            if (!ChooseFeed(inputMass, out SimHashes input)) return false;
            bool makeDeuterium = input == SimHashes.Water;

            float yield = (makeDeuterium ? 0.02f : 0.06f) * dt;
            if (makeDeuterium && Mathf.Min(gas.capacityKg, 5f) - gas.ExactMassStored() < yield) return false;
            if (!makeDeuterium && Mathf.Min(solids.capacityKg, 10f) - solids.ExactMassStored() < yield) return false;
            if (Mathf.Min(depletedLiquid.capacityKg, 20f) - depletedLiquid.ExactMassStored() < inputMass - yield) return false;
            return true;
        }

        private bool ChooseFeed(float mass, out SimHashes input)
        {
            // Prefer water, then concentrated brine, then ordinary salt water.
            // Consume and return the same real element; do not turn one saline
            // liquid into the other or discard SaltWater as a foreign input.
            foreach (SimHashes candidate in AcceptedLiquids)
                if (feed.GetMassAvailable(candidate) >= mass) { input = candidate; return true; }
            input = SimHashes.Water;
            return false;
        }

        protected override bool Process(float dt)
        {
            if (!CanProcess(dt)) return false;
            float inputMass = 10f * dt;
            if (!ChooseFeed(inputMass, out SimHashes input)) return false;
            bool makeDeuterium = input == SimHashes.Water;
            float yield = (makeDeuterium ? 0.02f : 0.06f) * dt;
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
