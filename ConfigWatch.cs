using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Reload com.morda.serverrates.cfg from disk (Valheim Server Manager / manual edits)
    /// and re-apply global keys without restarting the dedicated server.
    /// </summary>
    internal static class ConfigWatch
    {
        private static FileSystemWatcher _watcher;
        private static float _reloadAt;
        private static bool _pending;
        private static string _cfgFileName;

        public static void Start()
        {
            if (_watcher != null)
                return;

            try
            {
                _cfgFileName = Plugin.Instance != null
                    ? Path.GetFileName(Plugin.Instance.Config.ConfigFilePath)
                    : "com.morda.serverrates.cfg";

                _watcher = new FileSystemWatcher(Paths.ConfigPath);
                _watcher.Filter = _cfgFileName;
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
                _watcher.Changed += OnChanged;
                _watcher.Created += OnChanged;
                _watcher.EnableRaisingEvents = true;
                Plugin.Log.LogInfo("ServerRates config watcher: " + Path.Combine(Paths.ConfigPath, _cfgFileName));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("ServerRates config watcher failed: " + ex.Message);
            }
        }

        /// <summary>Skip FileSystemWatcher reloads briefly after an in-game save (avoid race).</summary>
        private static float _suppressUntil;

        public static void SuppressReloadBriefly(float seconds = 2f)
        {
            _suppressUntil = Time.unscaledTime + Mathf.Max(0.1f, seconds);
            _pending = false;
        }

        public static void Tick()
        {
            if (Time.unscaledTime < _suppressUntil)
            {
                _pending = false;
                return;
            }

            if (!_pending || Time.unscaledTime < _reloadAt)
                return;

            _pending = false;
            Reload();
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            _pending = true;
            _reloadAt = Time.unscaledTime + 0.5f;
        }

        private static void Reload()
        {
            try
            {
                if (Plugin.Instance != null)
                    Plugin.Instance.Config.Reload();

                RateApplier.NotifyConfigReloaded();
                RateApplier.ApplyGlobalKeys();
                Plugin.Log.LogInfo("ServerRates config reloaded from disk and applied.");
                CategoryGroundAmp.LogStatus();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("ServerRates reload failed: " + ex.Message);
            }
        }
    }
}
