using System.Collections.Generic;
using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class FusionBatchDispenser : KMonoBehaviour
    {
        internal const float MinimumPacket = 0.95f;
        private readonly List<ConduitDispenser> dispensers = new List<ConduitDispenser>();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            foreach (ConduitDispenser dispenser in GetComponents<ConduitDispenser>())
            {
                if (dispenser.conduitType != ConduitType.Gas) continue;
                dispenser.SetOnState(false);
                dispensers.Add(dispenser);
            }
            // The ordinary dispenser still maintains native port/operational
            // flags; this updater replaces only its gas transfer, atomically.
            Game.Instance.gasConduitFlow.AddConduitUpdater(DispenseBatches, ConduitFlowPriority.Dispense);
        }

        internal static float PacketSize(float storedMass, float pipeMass)
        {
            float packet = Mathf.Min(1f, storedMass);
            return packet >= MinimumPacket && 1f - pipeMass >= packet ? packet : 0f;
        }

        private int OutputCell(ConduitDispenser dispenser)
        {
            Building building = GetComponent<Building>();
            if (!dispenser.useSecondaryOutput) return building.GetUtilityOutputCell();
            foreach (ISecondaryOutput output in GetComponents<ISecondaryOutput>())
                if (output.HasSecondaryConduitType(ConduitType.Gas))
                    return Grid.OffsetCell(building.NaturalBuildingCell(), output.GetSecondaryConduitOffset(ConduitType.Gas));
            return -1;
        }

        private void DispenseBatches(float dt)
        {
            ConduitFlow flow = Game.Instance.gasConduitFlow;
            foreach (ConduitDispenser dispenser in dispensers)
            {
                dispenser.SetOnState(false);
                if (dispenser.storage == null || !dispenser.IsConnected) continue;
                int cell = OutputCell(dispenser);
                if (!Grid.IsValidCell(cell)) continue;
                ConduitFlow.ConduitContents contents = flow.GetContents(cell);
                dispenser.empty = true;
                dispenser.blocked = false;
                foreach (GameObject item in dispenser.storage.items)
                {
                    PrimaryElement element = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (element == null || !element.Element.IsGas || !Allowed(dispenser, element.ElementID)) continue;
                    if (element.Mass > 0f) dispenser.empty = false;
                    float packet = PacketSize(element.Mass, contents.mass);
                    if (packet <= 0f || (contents.mass > 0f && contents.element != element.ElementID))
                    {
                        if (element.Mass >= MinimumPacket) dispenser.blocked = true;
                        continue;
                    }
                    float transferred = flow.AddElement(cell, element.ElementID, packet,
                        element.Temperature, element.DiseaseIdx,
                        (int)(element.DiseaseCount * packet / element.Mass));
                    if (transferred <= 0f) { dispenser.blocked = true; continue; }
                    // Remove exactly what the conduit accepted, including germs.
                    int germs = (int)(element.DiseaseCount * transferred / element.Mass);
                    element.KeepZeroMassObject = true;
                    element.ModifyDiseaseCount(-germs, "FusionBatchDispenser");
                    element.Mass = Mathf.Max(0f, element.Mass - transferred);
                    dispenser.storage.Trigger((int)GameHashes.OnStorageChange, item);
                    dispenser.storage.Trigger(2051543657, element);
                    break;
                }
            }
        }

        private static bool Allowed(ConduitDispenser dispenser, SimHashes hash)
        {
            if (dispenser.elementFilter == null || dispenser.elementFilter.Length == 0) return true;
            bool found = false;
            foreach (SimHashes candidate in dispenser.elementFilter) if (candidate == hash) found = true;
            return dispenser.invertElementFilter ? !found : found;
        }

        protected override void OnCleanUp()
        {
            Game.Instance.gasConduitFlow.RemoveConduitUpdater(DispenseBatches);
            base.OnCleanUp();
        }
    }
}
