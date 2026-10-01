using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class TritiumBreederProcess : FusionProcessBase
    {
        private Storage lithium;
        private Storage starterDeuterium;
        private Storage tritium;
        private Storage tailGas;
        private ConduitConsumer deuteriumConsumer;
        private FusionReactorProcess linkedReactor;
        private bool breeding;

        internal bool IsBreeding => breeding;
        internal FusionReactorProcess LinkedReactor => linkedReactor;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Storage[] stores = GetComponents<Storage>();
            lithium = stores[0];
            starterDeuterium = stores[1];
            tritium = stores[2];
            tailGas = stores[3];
            deuteriumConsumer = GetComponent<ConduitConsumer>();
            radiation.emitRads = 50f;
            radiation.Refresh();
            radiation.SetEmitting(false);
        }

        internal bool CanAcceptNeutrons(float dt)
        {
            return operational != null && operational.IsOperational && lithium != null
                && lithium.GetMassAvailable(FusionIds.HashOf(FusionIds.Lithium)) >= 0.06f * dt
                && Mathf.Min(tritium.capacityKg, 5f) - tritium.ExactMassStored() >= 0.03f * dt
                && Mathf.Min(tailGas.capacityKg, 5f) - tailGas.ExactMassStored() >= 0.04f * dt;
        }

        private float NeutronAmount(float dt)
        {
            if (linkedReactor == null) return 0f;
            // Catch up missed scheduler steps using only already earned credit.
            float amount = Mathf.Min(linkedReactor.AvailableNeutrons, 0.03f * dt);
            amount = Mathf.Min(amount, lithium.GetMassAvailable(FusionIds.HashOf(FusionIds.Lithium)) / 6f);
            amount = Mathf.Min(amount, (Mathf.Min(tritium.capacityKg, 5f) - tritium.ExactMassStored()) / 3f);
            return Mathf.Min(amount, (Mathf.Min(tailGas.capacityKg, 5f) - tailGas.ExactMassStored()) / 4f);
        }

        protected override bool CanProcess(float dt)
        {
            PurgeOtherElements(starterDeuterium, FusionIds.HashOf(FusionIds.Deuterium));
            PurgeOtherElements(tritium, FusionIds.HashOf(FusionIds.Tritium));
            PurgeOtherElements(tailGas, FusionIds.HashOf(FusionIds.Helium), SimHashes.Hydrogen);
            FusionReactorProcess provider = CanAcceptNeutrons(dt) ? FusionReactorProcess.FindProvider(this) : null;
            if (linkedReactor != provider) linkedReactor?.ReleaseBreeder(this);
            linkedReactor = provider;
            breeding = provider != null;
            // Suppress startup feed only when lithium, output space and a link
            // are usable. Missing lithium falls back to D-D, even with a live reactor.
            deuteriumConsumer?.SetOnState(!breeding);
            if (breeding)
            {
                FusionReactorProcess funded = FusionReactorProcess.FindProvider(this, 0.01f * dt);
                if (funded == null) return false;
                if (linkedReactor != funded) linkedReactor.ReleaseBreeder(this);
                linkedReactor = funded;
                return NeutronAmount(dt) >= 0.01f * dt;
            }
            return starterDeuterium.GetMassAvailable(FusionIds.HashOf(FusionIds.Deuterium)) >= 0.02f * dt
                && Mathf.Min(tritium.capacityKg, 5f) - tritium.ExactMassStored() >= 0.015f * dt
                && Mathf.Min(tailGas.capacityKg, 5f) - tailGas.ExactMassStored() >= 0.005f * dt;
        }

        protected override bool Process(float dt)
        {
            if (!CanProcess(dt)) return false;
            if (breeding)
            {
                float neutrons = NeutronAmount(dt);
                if (!linkedReactor.TryTakeNeutrons(this, neutrons)) return false;
                lithium.ConsumeIgnoringDisease(FusionIds.TagOf(FusionIds.Lithium), neutrons * 6f);
                tritium.AddGasChunk(FusionIds.HashOf(FusionIds.Tritium), neutrons * 3f, 350f, byte.MaxValue, 0, false);
                tailGas.AddGasChunk(FusionIds.HashOf(FusionIds.Helium), neutrons * 4f, 350f, byte.MaxValue, 0, false);
                AddHeat(30f * (neutrons / (0.01f * dt)), dt);
                return true;
            }
            starterDeuterium.ConsumeIgnoringDisease(FusionIds.TagOf(FusionIds.Deuterium), 0.02f * dt);
            tritium.AddGasChunk(FusionIds.HashOf(FusionIds.Tritium), 0.015f * dt, 350f, byte.MaxValue, 0, false);
            tailGas.AddGasChunk(SimHashes.Hydrogen, 0.005f * dt, 350f, byte.MaxValue, 0, false);
            AddHeat(100f, dt);
            return true;
        }

        protected override void OnPowerLost()
        {
            linkedReactor?.ReleaseBreeder(this);
            linkedReactor = null;
            breeding = false;
        }

        protected override void OnCleanUp()
        {
            linkedReactor?.ReleaseBreeder(this);
            base.OnCleanUp();
        }
    }
}
