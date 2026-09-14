using System;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Client feedback: F5 via RemotePrint, chat once, optional yellow HUD.
    /// Dedicated AddString alone only hits BepInEx.
    /// </summary>
    internal static class RatesFeedback
    {
        private static bool _guard;

        /// <summary>Active while ZNet runs a remote admin command.</summary>
        internal static ZRpc CurrentRemoteRpc { get; private set; }

        public static void Say(string msg, bool yellowHud)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            // Always log full text server-side
            foreach (string line in SplitLines(msg))
                Plugin.Log?.LogInfo(line);

            if (_guard)
                return;
            _guard = true;
            try
            {
                // F5 terminal on the admin client — one RemotePrint per line (reliable)
                ZRpc rpc = CurrentRemoteRpc;
                if (rpc != null && rpc.IsConnected() && ZNet.instance != null)
                {
                    foreach (string line in SplitLines(msg))
                    {
                        try
                        {
                            ZNet.instance.RemotePrint(rpc, line);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.LogWarning("ServerRates RemotePrint failed: " + ex.Message);
                            break;
                        }
                    }
                }
                else if (Console.instance != null)
                {
                    foreach (string line in SplitLines(msg))
                        Console.instance.Print(line);
                }

                if (yellowHud)
                {
                    // Center HUD: first line only
                    string first = SplitLines(msg)[0];
                    ShowYellowHud(first);
                }

                // Chat: entire block once (trimmed if huge)
                BroadcastChat(TrimForChat(msg));
            }
            finally
            {
                _guard = false;
            }
        }

        private static string[] SplitLines(string msg)
        {
            return msg.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string TrimForChat(string msg)
        {
            const int max = 450;
            string compact = msg.Replace("\r\n", " | ").Replace('\n', '|').Replace('\r', '|');
            if (compact.Length <= max)
                return compact;
            return compact.Substring(0, max - 3) + "...";
        }

        public static void ShowYellowHud(string text)
        {
            if (string.IsNullOrEmpty(text) || ZRoutedRpc.instance == null)
                return;
            try
            {
                if (MessageHud.instance != null)
                {
                    MessageHud.instance.MessageAll(MessageHud.MessageType.Center, text);
                    return;
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(
                    ZRoutedRpc.Everybody,
                    "ShowMessage",
                    new object[] { (int)MessageHud.MessageType.Center, text });
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("ServerRates HUD failed: " + ex.Message);
            }
        }

        public static void BroadcastChat(string text)
        {
            if (string.IsNullOrEmpty(text) || ZRoutedRpc.instance == null)
                return;
            try
            {
                UserInfo info = new UserInfo { Name = "ServerRates" };
                ZRoutedRpc.instance.InvokeRoutedRPC(
                    ZRoutedRpc.Everybody,
                    "ChatMessage",
                    Vector3.zero,
                    (int)Talker.Type.Normal,
                    info,
                    text);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("ServerRates chat broadcast failed: " + ex.Message);
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_RemoteCommand")]
        private static class RemoteCommandPatch
        {
            private static void Prefix(ZRpc rpc)
            {
                CurrentRemoteRpc = rpc;
            }
        }

        [HarmonyPatch(typeof(ZNet), "InternalCommand")]
        private static class InternalCommandPatch
        {
            private static void Prefix(ZRpc rpc)
            {
                CurrentRemoteRpc = rpc;
            }

            private static void Postfix()
            {
                CurrentRemoteRpc = null;
            }

            private static Exception Finalizer(Exception __exception)
            {
                CurrentRemoteRpc = null;
                return __exception;
            }
        }
    }
}
