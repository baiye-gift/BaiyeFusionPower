using HarmonyLib;
using KMod;

namespace Baiye.FusionPower
{
    public sealed class FusionMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            FusionStrings.Register();
            FusionStrings.RefreshLoadedElements();
        }
    }
}
