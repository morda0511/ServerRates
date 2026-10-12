using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Everything the server tells people:
    ///  - private chat reply to the one who typed a command (works on console, no client mod)
    ///  - yellow center message + one chat line for everybody when a rate changes
    ///  - F5 / server console output
    /// </summary>
    internal static class RatesFeedback
    {
        /// <summary>Set while ZNet runs a remote admin (F5) command.</summary>
        internal static ZRpc CurrentRemoteRpc { get; private set; }

        private static string[] SplitLines(string msg)
        {
            return msg.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Server console / BepInEx log / remote F5 terminal.</summary>
        public static void Say(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            foreach (string line in SplitLines(msg))
            {
                Plugin.Log?.LogInfo(line);

                ZRpc rpc = CurrentRemoteRpc;
                if (rpc != null && rpc.IsConnected() && ZNet.instance != null)
                {
                    try
                    {
                        ZNet.instance.RemotePrint(rpc, line);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogWarning("ServerRates RemotePrint failed: " + ex.Message);
                    }
                }
                else if (Console.instance != null)
                {
                    Console.instance.Print(line);
                }
            }
        }

        /// <summary>Chat message to a single player (peer uid = chat sender id).</summary>
        public static void ReplyChat(long peer, string text, Vector3 near = default(Vector3))
        {
            foreach (string line in SplitLines(text ?? ""))
                SendChat(peer, line, near);
        }

        /// <summary>One line in everybody's chat + yellow center message.</summary>
        public static void Announce(string hud, string chatLine)
        {
            Plugin.Log?.LogInfo(chatLine);
            SendHud(hud);
            SendChat(ZRoutedRpc.Everybody, chatLine);
        }

        public static void AnnounceChanges(List<Rate> changed, string who, string label = null)
        {
            if (changed == null || changed.Count == 0)
                return;

            string text;
            if (changed.Count <= 3)
            {
                var parts = new List<string>();
                foreach (Rate r in changed)
                    parts.Add(r.Label + " " + r.Display());
                text = string.Join(" | ", parts.ToArray());
            }
            else if (!string.IsNullOrEmpty(label))
            {
                text = label + " " + changed[0].Display();
            }
            else
            {
                text = changed.Count + " rates changed";
            }

            string by = string.IsNullOrEmpty(who) ? "" : " (" + who + ")";
            Announce(text, "[ServerRates] " + text + by);
        }

        private static void SendHud(string text)
        {
            if (string.IsNullOrEmpty(text) || ZRoutedRpc.instance == null)
                return;
            try
            {
                // The dedicated has no MessageHud: use the routed RPC every client understands.
                ZRoutedRpc.instance.InvokeRoutedRPC(
                    ZRoutedRpc.Everybody, "ShowMessage",
                    new object[] { (int)MessageHud.MessageType.Center, text });
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("ServerRates HUD failed: " + ex.Message);
            }
        }

        private static void SendChat(long target, string text, Vector3 at = default(Vector3))
        {
            if (string.IsNullOrEmpty(text) || ZRoutedRpc.instance == null)
                return;
            try
            {
                // A valid user id keeps clients from logging "PlatformUserID failed to parse" for every reply.
                UserInfo from = new UserInfo { Name = "ServerRates" };
                Splatform.PlatformUserID? id = ChatBridge.ServerUser();
                if (id.HasValue)
                    from.UserId = id.Value;

                ZRoutedRpc.instance.InvokeRoutedRPC(
                    target, "ChatMessage",
                    at, (int)Talker.Type.Normal, from, text);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("ServerRates chat failed: " + ex.Message);
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
