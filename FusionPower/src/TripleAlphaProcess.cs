using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class TripleAlphaProcess : FusionProcessBase, ISaveLoadable
    {
        internal enum StopCause { None, Helium, Coolant, CoolantReserve, CarbonOutput, CoolantOutput, ThermalRange, BodyHot, PowerOutput }
        private Storage helium, carbon, coolantInput, coolantOutput, carbonDispatch;
        private Generator generator;
        private bool coolantPrimed;
        private StopCause stopCause;
        private float nextLogTime;
        [Serialize] private float totalMassDefectKg;
        private readonly Queue<Sample> samples = new Queue<Sample>();
        private struct Sample { internal float time, coolantHeat; }

        internal StopCause CurrentStopCause => stopCause;
        internal float HeliumKg => helium.GetMassAvailable(FusionIds.HashOf(FusionIds.Helium));
        internal float CarbonKg => carbon.GetMassAvailable(SimHashes.RefinedCarbon) + carbonDispatch.GetMassAvailable(SimHashes.RefinedCarbon);
        internal float CoolantInputKg => coolantInput.GetMassAvailable(SimHashes.SuperCoolant);
        internal float CoolantOutputKg => coolantOutput.GetMassAvailable(SimHashes.SuperCoolant);
        internal float CoolantInputKelvin => SliceTemperature(coolantInput, SimHashes.SuperCoolant);
        internal float CoolantOutputKelvin => SliceTemperature(coolantOutput, SimHashes.SuperCoolant);
        internal float StoredJoules => generator != null ? generator.JoulesAvailable : 0f;
        internal float TotalMassDefectKg => totalMassDefectKg;
        internal float AverageGrossWatts
        {
            get { TrimSamples(); return Mathf.Min(TripleAlphaBalance.GrossWatts, samples.Count * TripleAlphaBalance.GrossWatts * PowerStep / 5f); }
        }
        internal float AverageCoolantHeat
        {
            get { TrimSamples(); float sum = 0f; foreach (Sample sample in samples) sum += sample.coolantHeat; return sum / 5f; }
        }
        private void TrimSamples()
        {
            float cutoff = GameClock.Instance.GetTime() - 5f;
            while (samples.Count > 0 && samples.Peek().time <= cutoff) samples.Dequeue();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Storage[] stores = GetComponents<Storage>();
            helium = stores[0]; carbon = stores[1]; coolantInput = stores[2]; coolantOutput = stores[3]; carbonDispatch = stores[4];
            generator = GetComponent<Generator>();
            coolantPrimed = false;
            samples.Clear();
            nextLogTime = GameClock.Instance.GetTime() + 10f;
            if (radiation != null) { radiation.emitRads = 150f; radiation.Refresh(); radiation.SetEmitting(false); }
        }

        protected override void OnPowerLost() { coolantPrimed = false; }

        private bool Stop(StopCause cause)
        {
            if (cause == StopCause.Coolant) coolantPrimed = false;
            if (cause != stopCause) Debug.Log("[BaiyeFusionPower] Triple alpha stopped: " + cause);
            stopCause = cause;
            return false;
        }

        // Match native Storage's consumption order, including mixed-temperature chunks.
        private static float SliceTemperature(Storage storage, SimHashes hash, float requested = float.PositiveInfinity)
        {
            float mass = 0f, weighted = 0f;
            foreach (GameObject item in storage.items)
            {
                PrimaryElement e = item != null ? item.GetComponent<PrimaryElement>() : null;
                if (e == null || e.ElementID != hash || e.Mass <= 0f) continue;
                float taken = Mathf.Min(e.Mass, requested - mass);
                mass += taken; weighted += taken * e.Temperature;
                if (mass >= requested) break;
            }
            return mass > 0f ? weighted / mass : float.NaN;
        }

        private static float SpecificHeat(SimHashes hash, float fallback)
        {
            Element e = ElementLoader.FindElementByHash(hash);
            return e != null && e.specificHeatCapacity > 0f ? e.specificHeatCapacity : fallback;
        }

        private float CoolantHeat(float dt)
        {
            float heliumMass = TripleAlphaBalance.HeliumKgPerSecond * dt;
            float carbonMass = heliumMass * TripleAlphaBalance.CarbonMassRatio;
            float inletHeliumHeat = heliumMass * SpecificHeat(FusionIds.HashOf(FusionIds.Helium), 0.14f)
                * SliceTemperature(helium, FusionIds.HashOf(FusionIds.Helium), heliumMass);
            float productHeat = carbonMass * SpecificHeat(SimHashes.RefinedCarbon, 0.71f) * TripleAlphaBalance.CarbonKelvin;
            // Allocate the residual once. Product sensible heat is included, not
            // added for free on top of the full coolant heat budget.
            return TripleAlphaBalance.ResidualHeatKDTUPerSecond * dt + inletHeliumHeat - productHeat;
        }

        protected override bool CanProcess(float dt)
        {
            PurgeOtherElements(helium, FusionIds.HashOf(FusionIds.Helium));
            PurgeOtherElements(coolantInput, SimHashes.SuperCoolant);
            float heliumMass = TripleAlphaBalance.HeliumKgPerSecond * dt;
            float carbonMass = heliumMass * TripleAlphaBalance.CarbonMassRatio;
            float coolantMass = FusionReactorBalance.CoolantKgPerSecond * dt;
            if (HeliumKg < heliumMass) return Stop(StopCause.Helium);
            if (CoolantInputKg < coolantMass) return Stop(StopCause.Coolant);
            if (!coolantPrimed && CoolantInputKg < FusionReactorBalance.CoolantReserveKg - 0.001f) return Stop(StopCause.CoolantReserve);
            if (Mathf.Min(carbon.capacityKg, TripleAlphaBalance.CarbonBufferKg) - carbon.ExactMassStored() < carbonMass) return Stop(StopCause.CarbonOutput);
            if (Mathf.Min(coolantOutput.capacityKg, 20f) - coolantOutput.ExactMassStored() < coolantMass) return Stop(StopCause.CoolantOutput);
            if (generator == null || generator.Capacity - generator.JoulesAvailable < TripleAlphaBalance.GrossWatts * dt - 0.01f) return Stop(StopCause.PowerOutput);
            if (GetComponent<PrimaryElement>().Temperature >= 1473.15f) return Stop(StopCause.BodyHot);
            float temperature = SliceTemperature(coolantInput, SimHashes.SuperCoolant, coolantMass)
                + CoolantHeat(dt) / (coolantMass * SpecificHeat(SimHashes.SuperCoolant, 8.44f));
            if (float.IsNaN(temperature) || temperature >= FusionReactorBalance.MaxCoolantKelvin || temperature <= 2f) return Stop(StopCause.ThermalRange);
            return true;
        }

        protected override bool Process(float dt)
        {
            if (!CanProcess(dt)) return false;
            float heliumMass = TripleAlphaBalance.HeliumKgPerSecond * dt;
            float carbonMass = heliumMass * TripleAlphaBalance.CarbonMassRatio;
            float coolantMass = FusionReactorBalance.CoolantKgPerSecond * dt;
            float heat = CoolantHeat(dt);
            coolantInput.ConsumeAndGetDisease(SimHashes.SuperCoolant.CreateTag(), coolantMass,
                out float consumed, out var germs, out float inputTemperature);
            float outputTemperature = inputTemperature + heat / (consumed * SpecificHeat(SimHashes.SuperCoolant, 8.44f));
            helium.ConsumeIgnoringDisease(FusionIds.TagOf(FusionIds.Helium), heliumMass);
            carbon.AddOre(SimHashes.RefinedCarbon, carbonMass, TripleAlphaBalance.CarbonKelvin, byte.MaxValue, 0);
            coolantOutput.AddLiquid(SimHashes.SuperCoolant, consumed, outputTemperature, germs.idx, germs.count);
            generator.GenerateJoules(TripleAlphaBalance.GrossWatts * dt);
            totalMassDefectKg += heliumMass - carbonMass;
            coolantPrimed = true;
            samples.Enqueue(new Sample { time = GameClock.Instance.GetTime(), coolantHeat = heat });
            TrimSamples();
            if (stopCause != StopCause.None) Debug.Log("[BaiyeFusionPower] Triple alpha resumed");
            stopCause = StopCause.None;
            if (GameClock.Instance.GetTime() >= nextLogTime)
            {
                Debug.Log("[BaiyeFusionPower] Triple alpha telemetry: gross5s=" + AverageGrossWatts
                    + " W, coolantHeat5s=" + AverageCoolantHeat + " kDTU/s, helium=" + HeliumKg
                    + " kg, carbon=" + CarbonKg + " kg, in=" + (inputTemperature - 273.15f)
                    + " C, out=" + (outputTemperature - 273.15f) + " C, defect=" + totalMassDefectKg + " kg");
                nextLogTime = GameClock.Instance.GetTime() + 10f;
            }
            return true;
        }
    }
}
