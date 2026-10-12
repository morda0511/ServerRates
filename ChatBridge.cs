using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace ServerRates
{
    /// <summary>
    /// Makes the dedicated server a chat recipient.
    ///
    /// Valheim 1.0 has no server-side chat: a client sends every chat line as an RPC to each OTHER entry in
    /// the player list. The server is not in that list, so with one player online no chat packet is sent at
    /// all and no server mod can ever see "sr_wood 5". Adding the server to the player list as a pseudo
    /// player (the same trick Server Devcommands uses for "server chat") makes clients send their chat to
    /// the server too, where RatesChat picks the commands out.
    /// </summary>
    internal static class ChatBridge
    {
        internal const string DisplayName = "ServerRates";

        private static string _userId;
        private static Splatform.PlatformUserID? _serverUser;
        private static bool _failed;

        /// <summary>The server's user id for chat messages it sends itself. Null if it cannot be built.</summary>
        internal static Splatform.PlatformUserID? ServerUser()
        {
            if (ZNet.instance != null)
                ServerUserId(ZNet.instance);
            return _serverUser;
        }

        /// <summary>Platform user id string of the server, e.g. "Steam_7656119...". Null if it cannot be built.</summary>
        private static string ServerUserId(ZNet net)
        {
            if (_userId != null)
                return _userId;
            if (_failed)
                return null;

            try
            {
                Type gs = Type.GetType("Steamworks.SteamGameServer, com.rlabrecque.steamworks.net", false);
                MethodInfo get = gs != null ? gs.GetMethod("GetSteamID") : null;
                object steamId = get != null ? get.Invoke(null, null) : null;
                string id = steamId != null ? steamId.ToString() : null;
                if (string.IsNullOrEmpty(id) || id == "0")
                    throw new InvalidOperationException("server Steam id not available");

                // Built directly: an unset Platform makes PlatformUserID.ToString() return "", which clients
                // reject ("PlatformUserID failed to parse" / "UserID was invalid").
                Splatform.PlatformUserID userId = new Splatform.PlatformUserID(new Splatform.Platform("Steam"), id);
                string text = userId.ToString();
                if (!userId.IsValid || string.IsNullOrEmpty(text))
                    throw new InvalidOperationException("server user id '" + id + "' is not valid");

                _userId = text;
                _serverUser = userId;
                Plugin.Log?.LogInfo("ServerRates: chat bridge lists the server as '" + DisplayName + "' (" + _userId + ").");
                return _userId;
            }
            catch (Exception ex)
            {
                _failed = true;
                Plugin.Log?.LogWarning("ServerRates: chat bridge disabled, cannot build the server user id: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Appends one player entry to a package written by ZNet.WritePlayerInfo and raises the count.
        /// Layout (see ZNet.RPC_PlayerList): int count, then per player: name, characterId, userId,
        /// displayName, serverAssignedDisplayName, playfabId, publicPosition [, position].
        /// Does nothing when another mod already added an entry.
        /// </summary>
        internal static bool AppendServerPlayer(ZPackage pkg, int realPlayers, long sessionId, string userId, string name)
        {
            int end = pkg.GetPos();
            int count = new ZPackage(pkg.GetArray()).ReadInt();
            if (count > realPlayers)
                return false;

            pkg.SetPos(0);
            pkg.Write(count + 1);
            pkg.SetPos(end);

            pkg.Write(name);
            pkg.Write(new ZDOID(sessionId, uint.MaxValue));
            pkg.Write(userId);
            pkg.Write(name);
            pkg.Write(name);
            pkg.Write("");
            pkg.Write(false);
            return true;
        }

        [HarmonyPatch(typeof(ZNet), "WritePlayerInfo")]
        private static class WritePlayerInfoPatch
        {
            private static void Postfix(ZNet __instance, ref ZPackage __result)
            {
                if (__result == null || !Plugin.Active || !Rates.Ready || !Rates.ChatBridge.Flag)
                    return;
                if (!__instance.IsServer())
                    return;

                try
                {
                    string userId = ServerUserId(__instance);
                    if (userId == null)
                        return;

                    ICollection players = Traverse.Create(__instance).Field("m_players").GetValue<ICollection>();
                    int real = players != null ? players.Count : 0;
                    AppendServerPlayer(__result, real, ZDOMan.GetSessionID(), userId, DisplayName);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning("ServerRates: chat bridge failed: " + ex.Message);
                }
            }
        }
    }
}
