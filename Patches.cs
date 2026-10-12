using HarmonyLib;

namespace ServerRates
{
    [HarmonyPatch(typeof(ZNet), "Awake")]
    internal static class ZNetAwakePatch
    {
        private static void Postfix(ZNet __instance)
        {
            Plugin.TryActivate(__instance);
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    internal static class ZoneSystemStartPatch
    {
        private static void Postfix()
        {
            if (ZNet.instance != null)
                Plugin.TryActivate(ZNet.instance);
            RateApplier.ApplyGlobalKeys();
        }
    }

    [HarmonyPatch(typeof(Game), "UpdateWorldRates")]
    internal static class UpdateWorldRatesPatch
    {
        private static void Postfix()
        {
            RateApplier.ApplyGlobalKeys();
        }
    }
}
