using System;
using HarmonyLib;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// In-game chat commands for console players (no Terminal, no admin list / SteamID).
    /// Example: /sr_wood 9   /rates status   /rates help
    /// No admin check — anyone on the dedicated can use when enabled.
    /// </summary>
    internal static class RatesChat
    {
        [HarmonyPatch(typeof(Chat), "RPC_ChatMessage")]
        private static class ChatRpcPatch
        {
            private static bool Prefix(long sender, Vector3 position, int type, UserInfo userInfo, string text)
            {
                if (!Plugin.Active || Plugin.Settings == null)
                    return true;
                if (!Plugin.Settings.ChatCommandsEnabled.Value)
                    return true;
                if (string.IsNullOrWhiteSpace(text))
                    return true;

                string prefix = Plugin.Settings.ChatCommandPrefix.Value ?? "/";
                if (string.IsNullOrEmpty(prefix))
                    prefix = "/";

                string trimmed = text.Trim();
                if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;

                string body = trimmed.Substring(prefix.Length).Trim();
                if (body.Length == 0)
                    return true;

                // Only treat as ServerRates if it looks like our commands (avoid eating !hello)
                if (!LooksLikeRatesCommand(body))
                    return true;

                string who = userInfo != null && !string.IsNullOrEmpty(userInfo.Name) ? userInfo.Name : "player";
                Plugin.Log?.LogInfo("ServerRates chat cmd from " + who + ": " + body);

                RatesCommands.EnsureMaps();
                RatesCommands.ProcessLine(body, s => RatesFeedback.Say(s, false));

                // Hide the raw command from public chat
                return false;
            }
        }

        private static bool LooksLikeRatesCommand(string body)
        {
            string lower = body.ToLowerInvariant();
            if (lower.StartsWith("rates") || lower.StartsWith("sr_") || lower.StartsWith("sr "))
                return true;

            // !wood 9  (short name without sr_)
            int space = body.IndexOf(' ');
            string first = space > 0 ? body.Substring(0, space) : body;
            return RatesCommands.IsKnownShortOrKey(first);
        }
    }
}
