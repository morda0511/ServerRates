using System;
using System.IO;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Picks up edits of com.morda.serverrates.cfg (Valheim Server Manager, text editor, panel)
    /// and applies them live. Polls the file's timestamp on the main thread: FileSystemWatcher
    /// callbacks run on a pool thread where Unity's Time API throws, and they are missed entirely
    /// on network shares and in some containers.
    /// </summary>
    internal static class ConfigWatch
    {
        private const float PollSeconds = 1f;
        private const float SettleSeconds = 0.8f;
        private const int MaxRetries = 5;

        private static string _path;
        private static DateTime _lastWrite;
        private static float _nextPoll;
        private static float _reloadAt = -1f;
        private static int _retries;

        public static void Start()
        {
            _path = Plugin.Instance != null ? Plugin.Instance.Config.ConfigFilePath : null;
            _lastWrite = Stamp();
            if (_path != null)
                Plugin.Log.LogInfo("ServerRates watching config: " + _path);
        }

        /// <summary>Call after the mod saved the file itself so that save is not treated as an external edit.</summary>
        public static void NotifySaved()
        {
            _lastWrite = Stamp();
            _reloadAt = -1f;
        }

        public static void Tick()
        {
            if (_path == null)
                return;

            float now = Time.unscaledTime;

            if (_reloadAt < 0f)
            {
                if (now < _nextPoll)
                    return;
                _nextPoll = now + PollSeconds;

                DateTime stamp = Stamp();
                if (stamp == _lastWrite)
                    return;

                _lastWrite = stamp;
                _reloadAt = now + SettleSeconds;
                _retries = 0;
                return;
            }

            if (now < _reloadAt)
                return;

            // Another edit while we were waiting: wait until the writer is done.
            DateTime again = Stamp();
            if (again != _lastWrite)
            {
                _lastWrite = again;
                _reloadAt = now + SettleSeconds;
                return;
            }

            _reloadAt = -1f;
            Reload();
        }

        private static DateTime Stamp()
        {
            try
            {
                return _path != null && File.Exists(_path) ? File.GetLastWriteTimeUtc(_path) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static void Reload()
        {
            try
            {
                Plugin.Instance.Config.Reload();
            }
            catch (Exception ex)
            {
                // Typically the file is still locked by the writer: try again shortly.
                if (++_retries <= MaxRetries)
                {
                    _reloadAt = Time.unscaledTime + 1f;
                    return;
                }
                Plugin.Log.LogWarning("ServerRates: config reload failed: " + ex.Message);
                return;
            }

            var changed = Rates.TakeChanges();
            RateApplier.ApplyGlobalKeys();
            CategoryGroundAmp.LogStatus();

            if (changed.Count == 0)
            {
                Plugin.Log.LogInfo("ServerRates: config reloaded, nothing changed.");
                return;
            }

            Plugin.Log.LogInfo("ServerRates: config reloaded, " + changed.Count + " rate(s) changed.");
            if (Plugin.Active)
                RatesFeedback.AnnounceChanges(changed, "config");
        }
    }
}
