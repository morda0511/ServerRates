using UnityEngine;

namespace ServerRates
{
    internal static class RateApplier
    {
        private static bool _busy;
        private static bool _logged;

        public static void ApplyGlobalKeys()
        {
            if (_busy)
                return;
            if (!Plugin.Active || Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                return;
            if (ZoneSystem.instance == null)
                return;

            ModConfig c = Plugin.Settings;
            _busy = true;
            try
            {
                bool changed = false;

                changed |= SetScalar(GlobalKeys.SkillGainRate, c.SkillGainPercent.Value);
                changed |= SetScalar(GlobalKeys.SkillReductionRate, c.SkillReductionPercent.Value);

                changed |= SetScalar(GlobalKeys.PlayerDamage, c.PlayerDamagePercent.Value);
                changed |= SetScalar(GlobalKeys.EnemyDamage, c.EnemyDamagePercent.Value);
                changed |= SetScalar(GlobalKeys.EnemySpeedSize, c.EnemySpeedSizePercent.Value);
                changed |= SetScalar(GlobalKeys.EnemyLevelUpRate, c.EnemyLevelUpPercent.Value);
                changed |= SetScalar(GlobalKeys.EventRate, c.EventRatePercent.Value);
                changed |= SetWorldLevel(c.WorldLevel.Value);

                changed |= SetScalar(GlobalKeys.StaminaRate, c.StaminaPercent.Value);
                changed |= SetScalar(GlobalKeys.MoveStaminaRate, c.MoveStaminaPercent.Value);
                changed |= SetScalar(GlobalKeys.StaminaRegenRate, c.StaminaRegenPercent.Value);
                changed |= SetScalar(GlobalKeys.EitrRate, c.EitrPercent.Value);
                changed |= SetScalar(GlobalKeys.AdrenalineRate, c.AdrenalinePercent.Value);
                changed |= SetScalar(GlobalKeys.FoodRate, c.FoodPercent.Value);
                changed |= SetScalar(GlobalKeys.DurabilityRate, c.DurabilityPercent.Value);
                changed |= SetScalar(GlobalKeys.CarryWeightRate, c.CarryWeightPercent.Value);

                changed |= SetScalar(GlobalKeys.ResourceRate, c.ResourceRatePercent.Value);

                changed |= SetToggle(GlobalKeys.PassiveMobs, c.PassiveMobs.Value);
                changed |= SetToggle(GlobalKeys.PlayerEvents, c.PlayerEvents.Value);

                changed |= SetToggle(GlobalKeys.DeathKeepEquip, c.DeathKeepEquip.Value);
                changed |= SetToggle(GlobalKeys.DeathKeepInventory, c.DeathKeepInventory.Value);
                changed |= SetToggle(GlobalKeys.DeathDeleteItems, c.DeathDeleteItems.Value);
                changed |= SetToggle(GlobalKeys.DeathDeleteUnequipped, c.DeathDeleteUnequipped.Value);
                changed |= SetToggle(GlobalKeys.DeathSkillsReset, c.DeathSkillsReset.Value);

                changed |= SetToggle(GlobalKeys.NoBuildCost, c.NoBuildCost.Value);
                changed |= SetToggle(GlobalKeys.NoCraftCost, c.NoCraftCost.Value);
                changed |= SetToggle(GlobalKeys.AllPiecesUnlocked, c.AllPiecesUnlocked.Value);
                changed |= SetToggle(GlobalKeys.AllRecipesUnlocked, c.AllRecipesUnlocked.Value);
                changed |= SetToggle(GlobalKeys.NoWorkbench, c.NoWorkbench.Value);
                changed |= SetToggle(GlobalKeys.WorldLevelLockedTools, c.WorldLevelLockedTools.Value);

                changed |= SetToggle(GlobalKeys.NoMap, c.NoMap.Value);
                changed |= SetToggle(GlobalKeys.NoPortals, c.NoPortals.Value);
                changed |= SetToggle(GlobalKeys.NoBossPortals, c.NoBossPortals.Value);
                changed |= SetToggle(GlobalKeys.TeleportAll, c.TeleportAll.Value);
                changed |= SetToggle(GlobalKeys.DungeonBuild, c.DungeonBuild.Value);

                changed |= SetToggle(GlobalKeys.NoPseudoDrops, c.NoPseudoDrops.Value);
                changed |= SetToggle(GlobalKeys.NoBuildingFall, c.NoBuildingFall.Value);
                changed |= SetToggle(GlobalKeys.NoHeavySnow, c.NoHeavySnow.Value);
                changed |= SetToggle(GlobalKeys.AllHeavySnow, c.AllHeavySnow.Value);
                changed |= SetToggle(GlobalKeys.Fire, c.Fire.Value);

                if (!_logged)
                {
                    _logged = true;
                    Plugin.Log.LogInfo("ServerRates global keys applied (percent keys: 100 = vanilla).");
                }
                else if (changed)
                {
                    Plugin.Log.LogInfo("ServerRates global keys refreshed live (no restart).");
                }
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>Allow one Info log after a disk reload / admin command.</summary>
        public static void NotifyConfigReloaded()
        {
            _logged = false;
        }

        private static bool SetScalar(GlobalKeys key, float value)
        {
            value = Mathf.Max(0f, value);
            float current;
            if (ZoneSystem.instance.GetGlobalKey(key, out current) && Mathf.Abs(current - value) < 0.001f)
                return false;
            ZoneSystem.instance.SetGlobalKey(key, value);
            return true;
        }

        private static bool SetWorldLevel(int level)
        {
            level = Mathf.Clamp(level, 0, 10);
            float current;
            if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.WorldLevel, out current)
                && Mathf.Abs(current - level) < 0.001f)
                return false;
            ZoneSystem.instance.SetGlobalKey(GlobalKeys.WorldLevel, level);
            return true;
        }

        private static bool SetToggle(GlobalKeys key, string mode)
        {
            string m = (mode ?? "Unchanged").Trim();
            if (m.Equals("Unchanged", System.StringComparison.OrdinalIgnoreCase))
                return false;

            bool wantOn = m.Equals("On", System.StringComparison.OrdinalIgnoreCase)
                || m.Equals("True", System.StringComparison.OrdinalIgnoreCase)
                || m.Equals("1", System.StringComparison.OrdinalIgnoreCase);

            bool wantOff = m.Equals("Off", System.StringComparison.OrdinalIgnoreCase)
                || m.Equals("False", System.StringComparison.OrdinalIgnoreCase)
                || m.Equals("0", System.StringComparison.OrdinalIgnoreCase);

            if (!wantOn && !wantOff)
                return false;

            bool has = ZoneSystem.instance.GetGlobalKey(key);
            if (wantOn)
            {
                if (has)
                    return false;
                ZoneSystem.instance.SetGlobalKey(key);
                return true;
            }

            if (!has)
                return false;
            ZoneSystem.instance.RemoveGlobalKey(key);
            return true;
        }
    }
}
