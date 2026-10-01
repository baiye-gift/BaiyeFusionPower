using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class FusionReactorProcess : FusionProcessBase, ISaveLoadable
    {
        internal enum StopCause { None, Deuterium, Tritium, Coolant, Output, CoolantHot, BodyHot, Reserve, CoolantReserve, PowerOutput }
        internal const float StartTritium = 2f;

        private static readonly List<FusionReactorProcess> reactors = new List<FusionReactorProcess>();
        private Storage deuterium;
        private Storage tritium;
        private Storage exhaust;
        private Storage coolantInput;
        private Storage coolantOutput;
        private ConduitConsumer[] inputConsumers;
        private Generator generator;
        private bool coolantPrimed;
        private readonly Queue<float> workSamples = new Queue<float>();
        private float nextTelemetryLog;
        private TritiumBreederProcess linkedBreeder;
        private float neutronCredit;
        private StopCause stopCause;
        [Serialize] private bool hasIgnited;

        internal TritiumBreederProcess LinkedBreeder => linkedBreeder;
        internal float AvailableNeutrons => neutronCredit;
        internal StopCause CurrentStopCause => stopCause;
        internal float CoolantInputKg => coolantInput.GetMassAvailable(SimHashes.SuperCoolant);
        internal float CoolantOutputKg => coolantOutput.GetMassAvailable(SimHashes.SuperCoolant);
        internal float CoolantInputKelvin => StoredTemperature(coolantInput);
        internal float CoolantOutputKelvin => StoredTemperature(coolantOutput);
        internal float StoredJoules => generator != null ? generator.JoulesAvailable : 0f;
        private float CoolantSpecificHeat
        {
            get
            {
                Element element = ElementLoader.FindElementByHash(SimHashes.SuperCoolant);
                return element != null && element.specificHeatCapacity > 0f
                    ? element.specificHeatCapacity : FusionReactorBalance.CoolantSpecificHeat;
            }
        }
        internal float AverageGrossWatts
        {
            get
            {
                float cutoff = GameClock.Instance.GetTime() - 5f;
                while (workSamples.Count > 0 && workSamples.Peek() <= cutoff) workSamples.Dequeue();
                return Mathf.Min(FusionReactorBalance.GrossWatts,
                    workSamples.Count * FusionReactorBalance.GrossWatts * PowerStep / 5f);
            }
        }
        internal float AverageCoolantHeat => AverageGrossWatts / FusionReactorBalance.GrossWatts
            * FusionReactorBalance.CoolantHeatKDTUPerSecond;

        private static float StoredTemperature(Storage storage, float requestedMass = float.PositiveInfinity)
        {
            float mass = 0f, weighted = 0f;
            foreach (GameObject item in storage.items)
            {
                if (item == null) continue;
                PrimaryElement element = item.GetComponent<PrimaryElement>();
                if (element == null || element.ElementID != SimHashes.SuperCoolant || element.Mass <= 0f) continue;
                // Storage.ConsumeIgnoringDisease consumes matching items in this
                // same order. Predict the actual slice rather than the whole tank.
                float taken = Mathf.Min(element.Mass, requestedMass - mass);
                mass += taken;
                weighted += taken * element.Temperature;
                if (mass >= requestedMass) break;
            }
            return mass > 0f ? weighted / mass : float.NaN;
        }

        private bool Stop(StopCause cause)
        {
            neutronCredit = 0f;
            if (cause == StopCause.Coolant) coolantPrimed = false;
            if (cause == StopCause.Deuterium || cause == StopCause.Tritium || cause == StopCause.Reserve)
                hasIgnited = false;
            if (stopCause != cause)
            {
                string ports = "";
                if (inputConsumers != null)
                    foreach (ConduitConsumer input in inputConsumers)
                        ports += " [" + input.conduitType + ": connected=" + input.IsConnected
                            + ", enabled=" + input.isOn + ", stored=" + input.storage.ExactMassStored() + "]";
                Debug.Log("[BaiyeFusionPower] Reactor stopped: " + cause + ports);
            }
            stopCause = cause;
            return false;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Storage[] stores = GetComponents<Storage>();
            deuterium = stores[0];
            tritium = stores[1];
            exhaust = stores[2];
            coolantInput = stores[3];
            coolantOutput = stores[4];
            inputConsumers = GetComponents<ConduitConsumer>();
            generator = GetComponent<Generator>();
            // Rebuild transient neutron links and qualify fuel reserve after a load.
            hasIgnited = false;
            coolantPrimed = false;
            workSamples.Clear();
            nextTelemetryLog = GameClock.Instance.GetTime() + 10f;
            radiation.emitRads = 300f;
            radiation.Refresh();
            radiation.SetEmitting(false);
            reactors.Add(this);
        }

        protected override void OnCleanUp()
        {
            reactors.Remove(this);
            base.OnCleanUp();
        }

        internal static FusionReactorProcess FindProvider(TritiumBreederProcess breeder, float minimumCredit = 0f)
        {
            FusionReactorProcess current = breeder.LinkedReactor;
            if (current != null && current.neutronCredit >= minimumCredit && current.CanSupply(breeder))
                return current;
            FusionReactorProcess nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (FusionReactorProcess reactor in reactors)
            {
                if (reactor == null || reactor.neutronCredit < minimumCredit || !reactor.CanSupply(breeder)) continue;
                float distance = Vector3.Distance(reactor.transform.position, breeder.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = reactor;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private bool CanSupply(TritiumBreederProcess breeder)
        {
            if (operational == null || !operational.IsOperational || !operational.IsActive || breeder == null) return false;
            if (!InRange(breeder)) return false;
            if (linkedBreeder != null && linkedBreeder != breeder)
            {
                if (InRange(linkedBreeder) && linkedBreeder.CanAcceptNeutrons(PowerStep))
                    return false;
                linkedBreeder = null;
            }
            return true;
        }

        private bool InRange(TritiumBreederProcess breeder)
        {
            int reactorCell = Grid.PosToCell(gameObject);
            int breederCell = Grid.PosToCell(breeder.gameObject);
            return Grid.IsValidCell(reactorCell) && Grid.IsValidCell(breederCell)
                && Grid.WorldIdx[reactorCell] == Grid.WorldIdx[breederCell]
                && Vector3.Distance(transform.position, breeder.transform.position) <= 8f;
        }

        internal void ReleaseBreeder(TritiumBreederProcess breeder)
        {
            if (linkedBreeder == breeder) linkedBreeder = null;
        }

        protected override void OnPowerLost()
        {
            hasIgnited = false;
            coolantPrimed = false;
            neutronCredit = 0f;
            linkedBreeder = null;
        }

        internal bool TryTakeNeutrons(TritiumBreederProcess breeder, float mass)
        {
            if (mass <= 0f || !CanSupply(breeder) || neutronCredit < mass) return false;
            neutronCredit = Mathf.Max(0f, neutronCredit - mass);
            linkedBreeder = breeder;
            return true;
        }

        protected override bool CanProcess(float dt)
        {
            PurgeOtherElements(deuterium, FusionIds.HashOf(FusionIds.Deuterium));
            PurgeOtherElements(tritium, FusionIds.HashOf(FusionIds.Tritium));
            PurgeOtherElements(coolantInput, SimHashes.SuperCoolant);

            float deuteriumMass = 0.02f * dt;
            float tritiumMass = 0.03f * dt;
            float heliumMass = 0.04f * dt;
            float coolantMass = FusionReactorBalance.CoolantKgPerSecond * dt;
            if (deuterium.GetMassAvailable(FusionIds.HashOf(FusionIds.Deuterium)) < deuteriumMass)
                return Stop(StopCause.Deuterium);
            if (tritium.GetMassAvailable(FusionIds.HashOf(FusionIds.Tritium)) < tritiumMass)
                return Stop(StopCause.Tritium);
            if (!hasIgnited && tritium.GetMassAvailable(FusionIds.HashOf(FusionIds.Tritium)) < StartTritium)
                return Stop(StopCause.Reserve);
            if (coolantInput.GetMassAvailable(SimHashes.SuperCoolant) < coolantMass)
                return Stop(StopCause.Coolant);
            if (!coolantPrimed && CoolantInputKg < FusionReactorBalance.CoolantReserveKg - 0.001f)
                return Stop(StopCause.CoolantReserve);
            if (generator == null || generator.Capacity - generator.JoulesAvailable
                < FusionReactorBalance.GrossWatts * dt - 0.01f)
                return Stop(StopCause.PowerOutput);
            if (Mathf.Min(exhaust.capacityKg, 5f) - exhaust.ExactMassStored() < heliumMass ||
                Mathf.Min(coolantOutput.capacityKg, 20f) - coolantOutput.ExactMassStored() < coolantMass)
                return Stop(StopCause.Output);
            if (GetComponent<PrimaryElement>().Temperature >= 1473.15f)
                return Stop(StopCause.BodyHot);

            float inputTemperature = StoredTemperature(coolantInput, coolantMass);
            if (float.IsNaN(inputTemperature)) return Stop(StopCause.Coolant);
            float outputTemperature = inputTemperature + FusionReactorBalance.CoolantHeatKDTUPerSecond * dt
                / (coolantMass * CoolantSpecificHeat);
            if (outputTemperature >= FusionReactorBalance.MaxCoolantKelvin) return Stop(StopCause.CoolantHot);
            return true;
        }

        protected override bool Process(float dt)
        {
            if (!CanProcess(dt)) return false;
            float deuteriumMass = 0.02f * dt;
            float tritiumMass = 0.03f * dt;
            float heliumMass = 0.04f * dt;
            float coolantMass = FusionReactorBalance.CoolantKgPerSecond * dt;
            float inputTemperature = StoredTemperature(coolantInput, coolantMass);
            float outputTemperature = inputTemperature + FusionReactorBalance.CoolantHeatKDTUPerSecond * dt
                / (coolantMass * CoolantSpecificHeat);
            deuterium.ConsumeIgnoringDisease(FusionIds.TagOf(FusionIds.Deuterium), deuteriumMass);
            tritium.ConsumeIgnoringDisease(FusionIds.TagOf(FusionIds.Tritium), tritiumMass);
            coolantInput.ConsumeIgnoringDisease(SimHashes.SuperCoolant.CreateTag(), coolantMass);
            exhaust.AddGasChunk(FusionIds.HashOf(FusionIds.Helium), heliumMass, 700f, byte.MaxValue, 0, false);
            coolantOutput.AddLiquid(SimHashes.SuperCoolant, coolantMass, outputTemperature, byte.MaxValue, 0);
            generator.GenerateJoules(FusionReactorBalance.GrossWatts * dt);
            coolantPrimed = true;
            workSamples.Enqueue(GameClock.Instance.GetTime());
            // Bound telemetry even when no UI is inspecting this reactor.
            float measuredWatts = AverageGrossWatts;
            if (GameClock.Instance.GetTime() >= nextTelemetryLog)
            {
                Debug.Log("[BaiyeFusionPower] Reactor telemetry: gross5s=" + measuredWatts
                    + " W, coolantHeat5s=" + AverageCoolantHeat + " kDTU/s, cold=" + CoolantInputKg
                    + " kg, hot=" + CoolantOutputKg + " kg, stepIn=" + (inputTemperature - 273.15f)
                    + " C, stepOut=" + (outputTemperature - 273.15f) + " C, generator=" + StoredJoules + " J");
                nextTelemetryLog = GameClock.Instance.GetTime() + 10f;
            }
            neutronCredit = Mathf.Min(0.03f, neutronCredit + 0.01f * dt);
            hasIgnited = true;
            if (stopCause != StopCause.None) Debug.Log("[BaiyeFusionPower] Reactor resumed");
            stopCause = StopCause.None;
            return true;
        }
    }
}
