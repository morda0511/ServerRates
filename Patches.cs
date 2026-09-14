using HarmonyLib;
using UnityEngine;

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

    internal static class DropScale
    {
        public static bool Ready()
        {
            return Plugin.Active
                && Plugin.Settings != null
                && Plugin.Settings.Enabled.Value
                && ZNet.instance != null
                && ZNet.instance.IsDedicated();
        }
    }

    [HarmonyPatch(typeof(Smelter), "GetDeltaTime")]
    internal static class SmelterSpeedPatch
    {
        private static void Postfix(ref double __result)
        {
            float m = StationRates.Mult(Plugin.Settings != null ? Plugin.Settings.SmelterSpeedMultiplier.Value : 1f);
            if (m > 0f && !Mathf.Approximately(m, 1f))
                __result *= m;
        }
    }

    [HarmonyPatch(typeof(Fermenter), "GetFermentationTime")]
    internal static class FermenterSpeedPatch
    {
        private static void Postfix(ref double __result)
        {
            float m = StationRates.Mult(Plugin.Settings != null ? Plugin.Settings.FermenterSpeedMultiplier.Value : 1f);
            if (m > 0f && !Mathf.Approximately(m, 1f))
                __result *= m;
        }
    }

    [HarmonyPatch(typeof(CookingStation), "GetDeltaTime")]
    internal static class CookingSpeedPatch
    {
        private static void Postfix(ref float __result)
        {
            float m = StationRates.Mult(Plugin.Settings != null ? Plugin.Settings.CookingSpeedMultiplier.Value : 1f);
            if (m > 0f && !Mathf.Approximately(m, 1f))
                __result *= m;
        }
    }

    [HarmonyPatch(typeof(Plant), "GetGrowTime")]
    internal static class PlantGrowSpeedPatch
    {
        private static void Postfix(ref float __result)
        {
            float m = StationRates.Mult(Plugin.Settings != null ? Plugin.Settings.PlantGrowSpeedMultiplier.Value : 1f);
            if (m > 0f && !Mathf.Approximately(m, 1f))
                __result /= m;
        }
    }

    internal static class StationRates
    {
        public static float Mult(float value)
        {
            if (!DropScale.Ready())
                return 1f;
            return Mathf.Max(0.01f, value);
        }
    }
}
