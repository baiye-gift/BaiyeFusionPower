using HarmonyLib;

namespace Baiye.FusionPower
{
    // U59 CircuitManager spends WattsUsed * 0.2 joules before calling this
    // method. Powered means that entire demand was met; Unpowered may mean
    // partial payment. Never grant a full recipe on partial or idle payment.
    [HarmonyPatch(typeof(EnergyConsumer), nameof(EnergyConsumer.SetConnectionStatus))]
    internal static class FusionPaidPowerPatch
    {
        private static void Prefix(EnergyConsumer __instance, out float __state)
        {
            __state = __instance.GetComponent<FusionProcessBase>() != null ? __instance.WattsUsed : 0f;
        }

        private static void Postfix(EnergyConsumer __instance,
            CircuitManager.ConnectionStatus connection_status, float __state)
        {
            if (__state <= 0f) return;
            __instance.GetComponent<FusionProcessBase>()?.OnPowerSettlement(__state,
                connection_status == CircuitManager.ConnectionStatus.Powered);
        }
    }
}
