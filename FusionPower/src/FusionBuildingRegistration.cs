using HarmonyLib;

namespace Baiye.FusionPower
{
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    internal static class FusionBuildingRegistration
    {
        private static void Postfix()
        {
            ModUtil.AddBuildingToPlanScreen((HashedString)"Refining", FusionIds.Separator);
            ModUtil.AddBuildingToPlanScreen((HashedString)"HEP", FusionIds.Breeder);
            ModUtil.AddBuildingToPlanScreen((HashedString)"Power", FusionIds.Reactor);
            ModUtil.AddBuildingToPlanScreen((HashedString)"Power", FusionIds.TripleAlpha);

            var tech = Db.Get().Techs.Get("AdvancedNuclearResearch");
            if (tech != null)
            {
                tech.unlockedItemIDs.Add(FusionIds.Separator);
                tech.unlockedItemIDs.Add(FusionIds.Breeder);
                tech.unlockedItemIDs.Add(FusionIds.Reactor);
                tech.unlockedItemIDs.Add(FusionIds.TripleAlpha);
            }
        }
    }
}
