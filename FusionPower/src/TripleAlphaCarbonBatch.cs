namespace Baiye.FusionPower
{
    // Native SolidConduitDispenser pulls only from the dispatch store. Transfer
    // one full 20 kg batch there; keep partial product in the production store.
    public sealed class TripleAlphaCarbonBatch : KMonoBehaviour, ISim200ms
    {
        private Storage production, dispatch;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            Storage[] stores = GetComponents<Storage>();
            production = stores[1]; dispatch = stores[4];
        }
        public void Sim200ms(float dt)
        {
            if (production == null || dispatch == null || dispatch.ExactMassStored() > 0f) return;
            float packet = TripleAlphaBalance.CarbonPacketKg;
            if (production.GetMassAvailable(SimHashes.RefinedCarbon) < packet || dispatch.capacityKg < packet) return;
            production.TransferMass(dispatch, SimHashes.RefinedCarbon.CreateTag(), packet,
                flatten: true, hide_popups: true);
        }
    }
}
