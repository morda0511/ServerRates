using UnityEngine;

namespace ServerRates
{
    /// <summary>Pushes the configured rates into the vanilla world modifiers (global keys).</summary>
    internal static class RateApplier
    {
        private static bool _busy;
        private static float _lastLog = -100f;

        public static void ApplyGlobalKeys()
        {
            if (_busy || !Rates.Ready || ZoneSystem.instance == null)
                return;

            _busy = true;
            try
            {
                int changed = 0;
                foreach (Rate r in Rates.All)
                {
                    if (!r.GlobalKey.HasValue)
                        continue;
                    bool did = r.Kind == RateKind.Toggle
                        ? SetToggle(r.GlobalKey.Value, r.Text)
                        : SetScalar(r.GlobalKey.Value, r.Value);
                    if (did)
                        changed++;
                }

                if (changed > 0)
                {
                    // The world re-applies keys one by one while loading: log that quietly.
                    string msg = "ServerRates: " + changed + " world modifier(s) applied.";
                    if (Time.unscaledTime - _lastLog > 10f)
                        Plugin.Log.LogInfo(msg);
                    else
                        Plugin.Log.LogDebug(msg);
                    _lastLog = Time.unscaledTime;
                }
            }
            finally
            {
                _busy = false;
            }
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

        /// <summary>Unchanged = leave the world as is, On = set the key, Off = remove it.</summary>
        private static bool SetToggle(GlobalKeys key, string mode)
        {
            bool has = ZoneSystem.instance.GetGlobalKey(key);
            if (string.Equals(mode, "On", System.StringComparison.OrdinalIgnoreCase))
            {
                if (has)
                    return false;
                ZoneSystem.instance.SetGlobalKey(key);
                return true;
            }

            if (string.Equals(mode, "Off", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!has)
                    return false;
                ZoneSystem.instance.RemoveGlobalKey(key);
                return true;
            }

            return false;
        }
    }
}
