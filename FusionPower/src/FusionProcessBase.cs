using UnityEngine;

namespace Baiye.FusionPower
{
    public abstract class FusionProcessBase : KMonoBehaviour, ISim200ms
    {
        internal const float PowerStep = 0.2f;
        protected Operational operational;
        protected RadiationEmitter radiation;
        private bool spawned;
        internal float LastWorkTime { get; private set; } = float.NegativeInfinity;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            operational = GetComponent<Operational>();
            radiation = GetComponent<RadiationEmitter>();
            if (radiation != null) radiation.SetEmitting(false);
            spawned = true;
            Subscribe((int)GameHashes.OperationalChanged, OnOperationalChanged);
        }

        public void Sim200ms(float dt)
        {
            // Advertise demand. A recipe is committed only after the native circuit
            // manager has successfully paid for this consumer's 200 ms power step.
            bool active = operational != null && operational.IsOperational && dt > 0f && CanProcess(PowerStep);
            if (operational != null) operational.SetActive(active);
            if (!active && radiation != null) radiation.SetEmitting(false);
        }

        internal void OnPowerSettlement(float demandedWatts, bool fullyFunded)
        {
            if (!spawned || demandedWatts <= 0f) return;
            bool worked = fullyFunded && operational != null && operational.IsOperational && Process(PowerStep);
            if (worked) LastWorkTime = GameClock.Instance.GetTime();
            if (radiation != null) radiation.SetEmitting(worked);
            if (operational != null)
                // Do not preflight the following recipe here. A successful last
                // coolant slice must remain active until the next scheduled check;
                // native liquid packets arrive in one-second bursts.
                operational.SetActive(worked && operational.IsOperational);
        }

        private void OnOperationalChanged(object data)
        {
            if (operational == null || operational.IsOperational) return;
            if (radiation != null) radiation.SetEmitting(false);
            OnPowerLost();
        }

        protected virtual void OnPowerLost() { }
        protected abstract bool CanProcess(float dt);
        protected abstract bool Process(float dt);

        protected void AddHeat(float kilowatts, float dt)
        {
            var temperatures = GameComps.StructureTemperatures;
            if (temperatures != null)
                temperatures.ProduceEnergy(temperatures.GetHandle(gameObject), kilowatts * dt, "FusionPower", dt);
        }

        protected void SpillLiquid(SimHashes element, float mass, float temperature)
        {
            int cell = Grid.PosToCell(gameObject);
            var material = ElementLoader.FindElementByHash(element);
            if (FallingWater.instance != null && material != null)
                FallingWater.instance.AddParticle(cell, material.idx, mass, temperature, byte.MaxValue, 0);
        }

        protected static void PurgeOtherElements(Storage storage, params SimHashes[] accepted)
        {
            for (int i = storage.items.Count - 1; i >= 0; i--)
            {
                var item = storage.items[i];
                if (item == null) continue;
                var element = item.GetComponent<PrimaryElement>();
                bool allowed = false;
                foreach (SimHashes hash in accepted)
                    if (element != null && element.ElementID == hash) { allowed = true; break; }
                if (!allowed) storage.Drop(item);
            }
        }
    }
}
