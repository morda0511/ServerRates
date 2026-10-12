using System;
using HarmonyLib;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Rate commands in the normal text chat.
    ///   Console / vanilla client:  sr_wood 9     (NO slash: Valheim handles a leading / locally)
    ///   PC with this DLL:          /sr_wood 9    (forwarded as real chat)
    /// Runs on the dedicated server only; replies go privately to whoever typed the command.
    /// </summary>
    internal static class RatesChat
    {
        internal static bool TryGetRatesBody(string text, out string body)
        {
            body = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string candidate = text.Trim();
            if (candidate[0] == '/' || candidate[0] == '!')
                candidate = candidate.Substring(1).Trim();
            if (candidate.Length == 0)
                return false;

            // Only our own commands, never normal talk like "fish" or the server's own announcements.
            string lower = candidate.ToLowerInvariant();
            bool ours = lower.StartsWith("sr_") || lower.StartsWith("sr ")
                || lower == "rates" || lower.StartsWith("rates ");
            if (!ours)
                return false;

            body = candidate;
            return true;
        }

        private static bool CanChange(UserInfo user)
        {
            if (!string.Equals(Rates.ChatAccess.Text, "AdminOnly", StringComparison.OrdinalIgnoreCase))
                return true;
            try
            {
                return user != null && ZNet.instance != null && ZNet.instance.PlayerIsAdmin(user.UserId);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("ServerRates admin check failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>PC client with this DLL: /sr_* in the chat box goes out as real chat instead of a local console command.</summary>
        [HarmonyPatch(typeof(Chat), "InputText")]
        private static class ChatInputPatch
        {
            private static bool Prefix(Chat __instance)
            {
                if (__instance == null)
                    return true;

                var input = Traverse.Create(__instance).Field("m_input").GetValue();
                if (input == null)
                    return true;

                string text = Traverse.Create(input).Property("text").GetValue<string>();
                if (string.IsNullOrWhiteSpace(text))
                    return true;

                string trimmed = text.Trim();
                if (trimmed[0] != '/' || !TryGetRatesBody(trimmed, out _))
                    return true;

                if (Player.m_localPlayer != null)
                    __instance.SendText(Talker.Type.Normal, trimmed);

                Traverse.Create(input).Property("text").SetValue("");
                var go = Traverse.Create(input).Property("gameObject").GetValue<GameObject>();
                if (go != null)
                    go.SetActive(false);

                return false;
            }
        }
        /// <summary>
        /// Runs a chat line as a command if it is one. Returns true when it was handled
        /// (the vanilla handler is then skipped on the server).
        /// </summary>
        internal static bool TryHandle(long sender, Vector3 position, UserInfo userInfo, string text)
        {
            string body;
            if (!TryGetRatesBody(text, out body))
                return false;

            if (!Rates.Ready)
            {
                Plugin.Log?.LogWarning("ServerRates: chat command ignored, mod inactive (Active="
                    + Plugin.Active + ", Enabled=" + Rates.Enabled.Display() + ").");
                return false;
            }

            if (!Rates.ChatEnabled.Flag)
            {
                Plugin.Log?.LogWarning("ServerRates: chat command ignored, ChatCommandsEnabled=false in the config.");
                RatesFeedback.ReplyChat(sender, "ServerRates: chat commands are switched off (ChatCommandsEnabled=false).", position);
                return false;
            }

            string who = userInfo != null && !string.IsNullOrEmpty(userInfo.Name) ? userInfo.Name : "player";
            Plugin.Log?.LogInfo("ServerRates chat command from " + who + ": " + body);

            try
            {
                RatesCommands.Process(body, new CommandContext
                {
                    Who = who,
                    CanWrite = CanChange(userInfo),
                    Position = position,
                    Reply = line => RatesFeedback.ReplyChat(sender, line, position)
                });
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError("ServerRates command failed: " + ex);
                RatesFeedback.ReplyChat(sender, "ServerRates: command failed, see server log.", position);
            }

            return true;
        }

        // Valheim 1.0 sends normal chat / whispers as a per-player ZNetView RPC "Say" (type, user, text)
        // and shouts as the routed RPC "ChatMessage" (position, type, user, text). The dedicated only
        // forwards them, so both are read in the forwarding step.
        private static readonly int SayHash = "Say".GetStableHashCode();
        private static readonly int ChatMessageHash = "ChatMessage".GetStableHashCode();

        private static long _lastSender;
        private static string _lastText;
        private static float _lastTime = -10f;
        private static bool _lastHandled;
        private static int _seen;

        private static bool IsFromServer(long sender)
        {
            return ZNet.instance != null && (sender == ZNet.GetUID()
                || (ZDOMan.instance != null && sender == ZDOMan.GetSessionID()));
        }

        private static bool TryParse(ZRoutedRpc.RoutedRPCData data, out Vector3 position, out UserInfo user, out string text)
        {
            position = Vector3.zero;
            user = null;
            text = null;

            if (data == null || data.m_parameters == null)
                return false;
            if (data.m_methodHash != SayHash && data.m_methodHash != ChatMessageHash)
                return false;

            // Our own replies: nothing to read (and their parsing only produces log noise).
            if (IsFromServer(data.m_senderPeerID))
                return false;

            try
            {
                ZPackage pkg = new ZPackage(data.m_parameters.GetArray());
                if (data.m_methodHash == ChatMessageHash)
                    position = pkg.ReadVector3();
                pkg.ReadInt(); // talker type
                user = new UserInfo();
                user.Deserialize(ref pkg);
                text = pkg.ReadString();
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("ServerRates: could not read chat RPC: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Called for every chat message passing the server. Returns true when it was a command that we
        /// answered, so the raw text is dropped. The same message arrives once per recipient, so repeats
        /// within a moment get the same verdict without running the command again.
        /// </summary>
        internal static bool Intercept(string via, long sender, Vector3 position, UserInfo user, string text)
        {
            // Never react to our own replies (they are routed through here too).
            if (ZNet.instance != null && (sender == ZNet.GetUID()
                || (ZDOMan.instance != null && sender == ZDOMan.GetSessionID())))
                return false;

            if (_seen < 8)
            {
                _seen++;
                Plugin.Log?.LogInfo("ServerRates saw chat via " + via + " from " + sender + ": " + text);
            }

            string body;
            if (!TryGetRatesBody(text, out body))
                return false;

            float now = Time.unscaledTime;
            if (sender == _lastSender && text == _lastText && now - _lastTime < 0.5f)
            {
                _lastTime = now;
                return _lastHandled;
            }

            _lastSender = sender;
            _lastText = text;
            _lastTime = now;
            _lastHandled = TryHandle(sender, position, user, text);
            return _lastHandled;
        }

        private static Vector3 PositionOf(long peerUid)
        {
            try
            {
                ZNetPeer peer = ZNet.instance != null ? ZNet.instance.GetPeer(peerUid) : null;
                return peer != null ? peer.GetRefPos() : Vector3.zero;
            }
            catch
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Earliest hook: the packet exactly as it arrives from the client. Other mods (e.g. a networking
        /// mod with its own area-of-interest routing) may never call the vanilla RouteRPC for chat.
        /// </summary>
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ZRoutedRpc), "RPC_RoutedRPC")]
        private static class RoutedRpcEntryPatch
        {
            private static bool Prefix(ZRpc rpc, ZPackage pkg)
            {
                if (pkg == null || !Plugin.Active || ZNet.instance == null || !ZNet.instance.IsServer())
                    return true;

                try
                {
                    ZRoutedRpc.RoutedRPCData data = new ZRoutedRpc.RoutedRPCData();
                    data.Deserialize(new ZPackage(pkg.GetArray()));

                    Vector3 pos;
                    UserInfo user;
                    string text;
                    if (!TryParse(data, out pos, out user, out text))
                        return true;

                    if (data.m_methodHash == SayHash)
                        pos = PositionOf(data.m_senderPeerID);

                    return !Intercept("RPC_RoutedRPC", data.m_senderPeerID, pos, user, text);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning("ServerRates: could not inspect routed RPC: " + ex.Message);
                    return true;
                }
            }
        }

        /// <summary>Main hook: every chat message goes through the server's routing step.</summary>
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ZRoutedRpc), "RouteRPC")]
        private static class RouteRpcPatch
        {
            private static bool Prefix(ZRoutedRpc.RoutedRPCData rpcData)
            {
                if (!Plugin.Active || ZNet.instance == null || !ZNet.instance.IsServer())
                    return true;

                Vector3 pos;
                UserInfo user;
                string text;
                if (!TryParse(rpcData, out pos, out user, out text))
                    return true;

                if (rpcData.m_methodHash == SayHash)
                    pos = PositionOf(rpcData.m_senderPeerID);

                return !Intercept("RouteRPC", rpcData.m_senderPeerID, pos, user, text);
            }
        }

        /// <summary>Shouts addressed to everybody are also handled by the server itself.</summary>
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ZRoutedRpc), "HandleRoutedRPC")]
        private static class HandleRoutedRpcPatch
        {
            private static bool Prefix(ZRoutedRpc.RoutedRPCData data)
            {
                if (data == null || data.m_methodHash != ChatMessageHash)
                    return true;
                if (!Plugin.Active || ZNet.instance == null || !ZNet.instance.IsServer())
                    return true;

                Vector3 pos;
                UserInfo user;
                string text;
                if (!TryParse(data, out pos, out user, out text))
                    return true;

                return !Intercept("HandleRoutedRPC", data.m_senderPeerID, pos, user, text);
            }
        }

        /// <summary>Fallback on the chat handler itself.</summary>
        [HarmonyPatch(typeof(Chat), "RPC_ChatMessage")]
        private static class ChatRpcPatch
        {
            private static bool Prefix(long sender, Vector3 position, int type, UserInfo userInfo, string text)
            {
                return !Intercept("RPC_ChatMessage", sender, position, userInfo, text);
            }
        }
    }
}
