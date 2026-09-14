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
        public const string ModVersion = "1.4.9";
        public const string ModAuthor = "Morda";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }
        internal static ModConfig Settings { get; private set; }
        internal static bool Active { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Settings = new ModConfig(Config);

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            ConfigWatch.Start();

            Logger.LogInfo(ModName + " v" + ModVersion + " by " + ModAuthor
                + " loaded (dedicated server only — vanilla clients OK).");
        }

        private void Update()
        {
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
            if (net == null || !net.IsDedicated())
            {
                Active = false;
                if (net != null && net.IsServer() && !net.IsDedicated())
                    Log.LogWarning(ModName + ": listen/host worlds are not supported. Use a dedicated server.");
                return;
            }

            Active = true;
            Log.LogInfo(ModName + ": dedicated server detected — rates enabled.");
            RateApplier.ApplyGlobalKeys();
            CategoryGroundAmp.LogStatus();
            // Dedicated often never hits Terminal.InitTerminal — register here for remote/F5/chat.
            RatesCommands.Register();
        }
    }
}
