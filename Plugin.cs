using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ServerRates
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.morda.serverrates";
        public const string ModName = "ServerRates";
        public const string ModVersion = "1.7.0";
        public const string ModAuthor = "Morda";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal static bool Active { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Rates.Init(Config);

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            LogPatchOwners(typeof(Chat), "RPC_ChatMessage");
            LogPatchOwners(typeof(ZRoutedRpc), "RPC_RoutedRPC");
            LogPatchOwners(typeof(ZRoutedRpc), "RouteRPC");
            LogPatchOwners(typeof(ZRoutedRpc), "HandleRoutedRPC");


            ConfigWatch.Start();

            Logger.LogInfo(ModName + " v" + ModVersion + " by " + ModAuthor
                + " loaded (dedicated server only, vanilla clients OK).");
        }


        /// <summary>Shows which mods patch a method, to spot conflicts that swallow chat commands.</summary>
        private static void LogPatchOwners(System.Type type, string method)
        {
            try
            {
                var info = Harmony.GetPatchInfo(AccessTools.Method(type, method));
                if (info == null)
                {
                    Log.LogWarning(ModName + ": " + type.Name + "." + method + " is not patched!");
                    return;
                }

                var owners = new System.Collections.Generic.List<string>();
                foreach (var p in info.Prefixes)
                    owners.Add(p.owner + "(prio " + p.priority + ")");
                Log.LogInfo(ModName + ": " + type.Name + "." + method + " prefixes: " + string.Join(", ", owners.ToArray()));
            }
            catch (System.Exception ex)
            {
                Log.LogWarning(ModName + ": patch check failed for " + method + ": " + ex.Message);
            }
        }
        private float _nextActivationCheck;
        private static bool _warned;

        private void Update()
        {
            // Safety net: if the ZNet / ZoneSystem hooks were missed (plugin loaded late), activate here.
            if (!Active && ZNet.instance != null && Time.unscaledTime >= _nextActivationCheck)
            {
                _nextActivationCheck = Time.unscaledTime + 2f;
                TryActivate(ZNet.instance);
            }

            ConfigWatch.Tick();
            if (Active)
                CategoryGroundAmp.Tick();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            if (Instance == this)
                Instance = null;
            Active = false;
        }

        internal static void TryActivate(ZNet net)
        {
            // F5 / server console commands (InitTerminal may already have run).
            RatesCommands.Register();

            if (net == null || !net.IsDedicated())
            {
                Active = false;
                if (!_warned && net != null)
                {
                    _warned = true;
                    Log.LogWarning(ModName + ": not a dedicated server (IsServer=" + net.IsServer()
                        + ", IsDedicated=" + net.IsDedicated() + "). Rates are inactive.");
                }
                return;
            }

            if (!Active)
                Log.LogInfo(ModName + ": dedicated server detected, rates enabled.");
            Active = true;
            RateApplier.ApplyGlobalKeys();
            CategoryGroundAmp.LogStatus();
        }
    }
}
