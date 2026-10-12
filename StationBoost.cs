using System;
using System.Collections.Generic;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Faster / slower stations without anything on the clients.
    ///
    /// Every station keeps its progress in its ZDO (the object's server-side data) and the player's game
    /// only calculates from those values. The server edits the values and bumps the data revision above
    /// the owner's, so the owner accepts them (ZDOMan.RPC_ZDOData accepts any newer revision, ZDOPeer.ShouldSend
    /// also sends to the owner). No ownership change is needed, so nothing can get lost while a player
    /// interacts with the station.
    ///
    ///  Plant      one-time shift of "plantTime" when planted:      ready after growTime / x
    ///  Fermenter  one-time shift of "startTime" when filled:       ready after duration / x
    ///  Smelter    extra seconds added to "accTime" while it works: the game processes them in 1 s steps
    ///             (same fuel use per product as normal)
    ///  Cooking    cooked time of a slot advanced while it works, never past "done" (no burnt food)
    /// </summary>
    internal static class StationBoost
    {
        private const double FreshSeconds = 120.0;   // only react to things started just now
        private const uint RevisionJump = 64;        // beats the owner's own edits made while ours was on its way
        private const float SmelterPeriod = 5f;
        private const float CookingPeriod = 2f;
        private const float MaxElapsed = 15f;

        private enum Kind { Plant, Fermenter, Smelter, Cooking }

        private sealed class StationInfo
        {
            public Kind Kind;
            public string Name;
            public float Min, Max;          // plant: grow time range
            public float Duration;          // fermenter: fermentation duration
            public int MaxFuel, MaxOre;     // smelter
            public float SecPerProduct;     // smelter: seconds per item
            public int Slots;               // cooking
            public Dictionary<string, float> CookTime; // cooking: item prefab name -> seconds
        }

        private sealed class Track
        {
            public float NextCheck;
            public float LastTime;
            public float LastBake, LastFuel;
            public float[] LastCooked;
        }

        private static readonly Dictionary<int, StationInfo> Known = new Dictionary<int, StationInfo>();
        private static readonly HashSet<int> NotAStation = new HashSet<int>();
        private static readonly HashSet<ZDOID> DonePlants = new HashSet<ZDOID>();
        private static readonly Dictionary<ZDOID, long> LastStart = new Dictionary<ZDOID, long>();
        private static readonly Dictionary<ZDOID, Track> Tracks = new Dictionary<ZDOID, Track>();
        private static readonly List<ZDOID> Scratch = new List<ZDOID>();
        private static int _logged;

        private static StationInfo GetInfo(int hash)
        {
            StationInfo info;
            if (Known.TryGetValue(hash, out info))
                return info;
            if (NotAStation.Contains(hash))
                return null;

            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(hash) : null;
            if (prefab != null)
            {
                Plant plant = prefab.GetComponent<Plant>();
                Fermenter fermenter = plant == null ? prefab.GetComponent<Fermenter>() : null;
                Smelter smelter = prefab.GetComponent<Smelter>();
                CookingStation cooking = prefab.GetComponent<CookingStation>();

                if (plant != null)
                    info = new StationInfo { Kind = Kind.Plant, Min = plant.m_growTime, Max = plant.m_growTimeMax };
                else if (fermenter != null)
                    info = new StationInfo { Kind = Kind.Fermenter, Duration = fermenter.m_fermentationDuration };
                else if (smelter != null)
                    info = new StationInfo { Kind = Kind.Smelter, MaxFuel = smelter.m_maxFuel, MaxOre = smelter.m_maxOre, SecPerProduct = smelter.m_secPerProduct };
                else if (cooking != null && cooking.m_slots != null)
                {
                    info = new StationInfo
                    {
                        Kind = Kind.Cooking,
                        Slots = cooking.m_slots.Length,
                        CookTime = new Dictionary<string, float>()
                    };
                    foreach (CookingStation.ItemConversion c in cooking.m_conversion)
                        if (c != null && c.m_from != null)
                            info.CookTime[c.m_from.name] = c.m_cookTime;
                }

                if (info != null)
                    info.Name = prefab.name;
            }

            if (info == null)
            {
                NotAStation.Add(hash);
                return null;
            }

            Known[hash] = info;
            return info;
        }

        /// <summary>Called for every ZDO near a player, a few times per second.</summary>
        public static void Observe(ZDO zdo, float now)
        {
            int hash = zdo.GetPrefab();
            if (NotAStation.Contains(hash))
                return;

            // Nothing configured: do not even look at the object.
            if (Mathf.Approximately(Rates.Plants.Value, 1f) && Mathf.Approximately(Rates.Fermenter.Value, 1f)
                && Mathf.Approximately(Rates.Smelter.Value, 1f) && Mathf.Approximately(Rates.Cooking.Value, 1f))
                return;

            StationInfo info = GetInfo(hash);
            if (info == null)
                return;

            switch (info.Kind)
            {
                case Kind.Plant: BoostPlant(zdo, info, Rates.Plants.Value); break;
                case Kind.Fermenter: BoostFermenter(zdo, info, Rates.Fermenter.Value); break;
                case Kind.Smelter: BoostSmelter(zdo, info, Rates.Smelter.Value, now); break;
                case Kind.Cooking: BoostCooking(zdo, info, Rates.Cooking.Value, now); break;
            }
        }

        // ------------------------------------------------------------------ plants / fermenters

        private static void BoostPlant(ZDO zdo, StationInfo info, float mult)
        {
            mult = Mathf.Max(0.01f, mult);
            if (Mathf.Approximately(mult, 1f) || DonePlants.Contains(zdo.m_uid))
                return;

            long planted = zdo.GetLong(ZDOVars.s_plantTime, 0L);
            if (planted == 0L)
                return; // not set yet, look again next scan

            if (!IsFresh(planted))
            {
                DonePlants.Add(zdo.m_uid);
                return;
            }

            // Same grow time the game uses for this plant (seeded random between the two values).
            UnityEngine.Random.State state = UnityEngine.Random.state;
            UnityEngine.Random.InitState(zdo.GetInt(ZDOVars.s_seed, 0));
            float t = UnityEngine.Random.value;
            UnityEngine.Random.state = state;
            double growTime = Mathf.Lerp(info.Min, info.Max, t);

            ShiftStart(zdo, ZDOVars.s_plantTime, planted, growTime, mult, info.Name);
            DonePlants.Add(zdo.m_uid);
        }

        private static void BoostFermenter(ZDO zdo, StationInfo info, float mult)
        {
            mult = Mathf.Max(0.01f, mult);
            if (Mathf.Approximately(mult, 1f))
                return;

            long start = zdo.GetLong(ZDOVars.s_startTime, 0L);
            if (start == 0L)
                return; // empty

            long last;
            if (LastStart.TryGetValue(zdo.m_uid, out last) && last == start)
                return; // this batch is already handled

            if (!IsFresh(start))
            {
                LastStart[zdo.m_uid] = start;
                return;
            }

            LastStart[zdo.m_uid] = ShiftStart(zdo, ZDOVars.s_startTime, start, info.Duration, mult, info.Name);
        }

        private static bool IsFresh(long ticks)
        {
            if (ZNet.instance == null)
                return false;
            double age = new TimeSpan(ZNet.instance.GetTime().Ticks - ticks).TotalSeconds;
            return age >= 0.0 && age < FreshSeconds;
        }

        /// <summary>Moves a start timestamp so that the thing needs duration / mult instead of duration.</summary>
        private static long ShiftStart(ZDO zdo, int key, long startTicks, double duration, float mult, string name)
        {
            double backSeconds = duration * (1.0 - 1.0 / mult); // positive = earlier start (faster)
            long moved = startTicks - (long)(backSeconds * TimeSpan.TicksPerSecond);
            zdo.Set(key, moved);
            Publish(zdo);

            Log(name + " x" + Rate.Fmt(mult) + ": needs " + Rate.Fmt((float)(duration / mult)) + "s instead of "
                + Rate.Fmt((float)duration) + "s");
            return moved;
        }

        // ------------------------------------------------------------------ smelters

        private static void BoostSmelter(ZDO zdo, StationInfo info, float mult, float now)
        {
            if (mult <= 1.001f)
                return; // speeding up only: extra time cannot be taken away

            Track t;
            if (!Tracks.TryGetValue(zdo.m_uid, out t))
            {
                Tracks[zdo.m_uid] = t = new Track
                {
                    NextCheck = now + SmelterPeriod,
                    LastTime = now,
                    LastBake = zdo.GetFloat(ZDOVars.s_bakeTimer, 0f),
                    LastFuel = zdo.GetFloat(ZDOVars.s_fuel, 0f)
                };
                return;
            }

            if (now < t.NextCheck)
                return;

            float elapsed = Mathf.Min(now - t.LastTime, MaxElapsed);
            t.NextCheck = now + SmelterPeriod;
            t.LastTime = now;

            float bake = zdo.GetFloat(ZDOVars.s_bakeTimer, 0f);
            float fuel = zdo.GetFloat(ZDOVars.s_fuel, 0f);
            int queued = zdo.GetInt(ZDOVars.s_queued, 0);

            // Only boost what is demonstrably working: ore queued, fuel there, progress or fuel use since last look.
            bool working = queued > 0 && (info.MaxFuel == 0 || fuel > 0f)
                && (Mathf.Abs(bake - t.LastBake) > 0.001f || fuel < t.LastFuel - 0.0001f);
            t.LastBake = bake;
            t.LastFuel = fuel;
            if (!working)
                return;

            // The game works off "accTime" in 1 second steps (max 3600), each with normal fuel use and progress.
            float acc = zdo.GetFloat(ZDOVars.s_accTime, 0f) + (mult - 1f) * elapsed;
            zdo.Set(ZDOVars.s_accTime, Mathf.Min(acc, 3000f));
            Publish(zdo);

            Log(info.Name + " x" + Rate.Fmt(mult) + ": +" + Rate.Fmt((mult - 1f) * elapsed) + "s of work");
        }

        // ------------------------------------------------------------------ cooking stations

        private static void BoostCooking(ZDO zdo, StationInfo info, float mult, float now)
        {
            if (mult <= 1.001f)
                return;

            Track t;
            if (!Tracks.TryGetValue(zdo.m_uid, out t))
            {
                t = new Track { NextCheck = now + CookingPeriod, LastTime = now, LastCooked = new float[info.Slots] };
                for (int i = 0; i < info.Slots; i++)
                    t.LastCooked[i] = zdo.GetFloat("slot" + i, 0f);
                Tracks[zdo.m_uid] = t;
                return;
            }

            if (now < t.NextCheck || t.LastCooked == null)
                return;

            float elapsed = Mathf.Min(now - t.LastTime, MaxElapsed);
            t.NextCheck = now + CookingPeriod;
            t.LastTime = now;

            bool changed = false;
            for (int i = 0; i < info.Slots; i++)
            {
                string item = zdo.GetString("slot" + i, "");
                int status = zdo.GetInt("slotstatus" + i, 0);
                float cooked = zdo.GetFloat("slot" + i, 0f);
                float before = t.LastCooked[i];
                t.LastCooked[i] = cooked;

                // raw food that the station advanced since the last look (so it really has fire / fuel)
                float cookTime;
                if (string.IsNullOrEmpty(item) || status != 0 || cooked <= before + 0.001f)
                    continue;
                if (!info.CookTime.TryGetValue(item, out cookTime) || cookTime <= 0f || cooked >= cookTime)
                    continue;

                float boosted = Mathf.Min(cooked + (mult - 1f) * elapsed, cookTime); // never into burnt
                zdo.Set("slot" + i, boosted);
                t.LastCooked[i] = boosted;
                changed = true;
            }

            if (changed)
            {
                Publish(zdo);
                Log(info.Name + " x" + Rate.Fmt(mult) + ": cooking advanced");
            }
        }

        // ------------------------------------------------------------------ info message

        private const float InfoRange = 3f;
        private const float InfoCooldown = 30f;
        private static readonly Dictionary<string, float> InfoShown = new Dictionary<string, float>();

        /// <summary>
        /// Tells a player how long the station next to them needs per item. A plain top-left message
        /// sent to that player only; nothing is placed in the world.
        /// </summary>
        public static void Info(ZDO zdo, float now)
        {
            if (!Rates.StationInfo.Flag)
                return;

            int hash = zdo.GetPrefab();
            if (NotAStation.Contains(hash))
                return;
            StationInfo info = GetInfo(hash);
            if (info == null || info.Kind == Kind.Plant)
                return;

            List<ZNetPeer> peers = ZNet.instance.GetPeers();
            Vector3 at = zdo.GetPosition();
            for (int i = 0; peers != null && i < peers.Count; i++)
            {
                ZNetPeer peer = peers[i];
                if (peer == null || !peer.IsReady() || (peer.GetRefPos() - at).sqrMagnitude > InfoRange * InfoRange)
                    continue;

                string key = peer.m_uid + ":" + zdo.m_uid;
                float next;
                if (InfoShown.TryGetValue(key, out next) && now < next)
                    continue;
                InfoShown[key] = now + InfoCooldown;

                string text = InfoText(info);
                if (text != null && ZRoutedRpc.instance != null)
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "ShowMessage",
                        new object[] { (int)MessageHud.MessageType.TopLeft, text });
            }
        }

        private static string InfoText(StationInfo info)
        {
            string name = Pretty(info.Name);
            switch (info.Kind)
            {
                case Kind.Smelter:
                    return name + ": " + PerItem(info.SecPerProduct, Rates.Smelter.Value) + " per item";
                case Kind.Fermenter:
                    return name + ": " + PerItem(info.Duration, Rates.Fermenter.Value, true) + " per batch";
                case Kind.Cooking:
                {
                    float min = float.MaxValue, max = 0f;
                    foreach (float t in info.CookTime.Values)
                    {
                        min = Mathf.Min(min, t);
                        max = Mathf.Max(max, t);
                    }

                    if (max <= 0f)
                        return null;
                    float m = Mathf.Max(1f, Rates.Cooking.Value);
                    string range = Mathf.Approximately(min, max)
                        ? Rate.Fmt(min / m) + "s" : Rate.Fmt(min / m) + "s - " + Rate.Fmt(max / m) + "s";
                    return name + ": " + range + " per item" + (m > 1.001f ? " (x" + Rate.Fmt(m) + ")" : "");
                }
                default:
                    return null;
            }
        }

        /// <summary>"charcoal_kiln" -> "Charcoal Kiln"</summary>
        private static string Pretty(string prefabName)
        {
            string s = (prefabName ?? "").Replace("piece_", "").Replace('_', ' ').Trim();
            s = s.Replace("blastfurnace", "blast furnace").Replace("cookingstation", "cooking station")
                .Replace("eitrrefinery", "eitr refinery").Replace("spinningwheel", "spinning wheel")
                .Replace("charcoalkiln", "charcoal kiln");
            var parts = s.Split(' ');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }

        // ------------------------------------------------------------------ diagnosis

        /// <summary>
        /// "rates inspect": shows the stored values of the stations next to the sender.
        /// "rates repair": additionally sets a smelter's accumulated work time back to 0.
        /// </summary>
        public static void Inspect(CommandContext ctx, bool repair)
        {
            if (!ctx.Position.HasValue || ZDOMan.instance == null || ZNet.instance == null)
            {
                ctx.Reply("Stand next to the station and use this in the game chat.");
                return;
            }

            Vector3 pos = ctx.Position.Value;
            var near = new List<ZDO>();
            var distant = new List<ZDO>();
            ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(pos), ZNet.instance.GetSyncedSimulationDistance(), near, distant);

            var found = new List<KeyValuePair<float, ZDO>>();
            foreach (ZDO zdo in near)
            {
                float d = Vector3.Distance(zdo.GetPosition(), pos);
                if (d <= 8f && GetInfo(zdo.GetPrefab()) != null)
                    found.Add(new KeyValuePair<float, ZDO>(d, zdo));
            }

            found.Sort((a, b) => a.Key.CompareTo(b.Key));
            if (found.Count == 0)
            {
                ctx.Reply("No station within 8 m of you.");
                return;
            }

            long me = ZDOMan.GetSessionID();
            for (int n = 0; n < found.Count && n < 3; n++)
            {
                ZDO z = found[n].Value;
                StationInfo info = GetInfo(z.GetPrefab());
                long owner = z.GetOwner();
                string head = info.Name + " " + Rate.Fmt(found[n].Key) + "m owner="
                    + (owner == 0L ? "none" : owner == me ? "server" : "player") + " rev=" + z.DataRevision;

                switch (info.Kind)
                {
                    case Kind.Smelter:
                    {
                        float acc = z.GetFloat(ZDOVars.s_accTime, 0f);
                        ctx.Reply(head + " fuel=" + Rate.Fmt(z.GetFloat(ZDOVars.s_fuel, 0f)) + " queued=" + z.GetInt(ZDOVars.s_queued, 0)
                            + " bake=" + Rate.Fmt(z.GetFloat(ZDOVars.s_bakeTimer, 0f)) + " acc=" + Rate.Fmt(acc));
                        ctx.Reply("  time per item: " + PerItem(info.SecPerProduct, Rates.Smelter.Value));
                        if (repair)
                        {
                            z.Set(ZDOVars.s_accTime, 0f);
                            Publish(z);
                            ctx.Reply("  -> work time store set to 0");
                        }
                        break;
                    }
                    case Kind.Fermenter:
                        ctx.Reply(head + " content='" + z.GetString(ZDOVars.s_content, "") + "' started "
                            + AgeText(z.GetLong(ZDOVars.s_startTime, 0L)) + " ago");
                        ctx.Reply("  time per batch: " + PerItem(info.Duration, Rates.Fermenter.Value, true));
                        break;
                    case Kind.Cooking:
                    {
                        var slots = new List<string>();
                        for (int i = 0; i < info.Slots; i++)
                        {
                            string item = z.GetString("slot" + i, "");
                            if (item.Length > 0)
                                slots.Add(i + ":" + item + " t=" + Rate.Fmt(z.GetFloat("slot" + i, 0f)) + " st=" + z.GetInt("slotstatus" + i, 0));
                        }
                        ctx.Reply(head + " slots: " + (slots.Count == 0 ? "empty" : string.Join(", ", slots.ToArray())));
                        var times = new List<string>();
                        foreach (KeyValuePair<string, float> c in info.CookTime)
                            times.Add(c.Key + " " + PerItem(c.Value, Rates.Cooking.Value));
                        ctx.Reply("  time per item: " + string.Join(" | ", times.ToArray()));
                        break;
                    }
                    default:
                        ctx.Reply(head + " planted " + AgeText(z.GetLong(ZDOVars.s_plantTime, 0L)) + " ago");
                        ctx.Reply("  grow time: " + PerItem(info.Min, Rates.Plants.Value, true)
                            + (Mathf.Approximately(info.Min, info.Max) ? "" : " to " + PerItem(info.Max, Rates.Plants.Value, true)));
                        break;
                }
            }
        }

        /// <summary>"30s" at vanilla speed, "6s (vanilla 30s, x5)" with a multiplier.</summary>
        private static string PerItem(float vanillaSeconds, float mult, bool canSlowDown = false)
        {
            if (!canSlowDown)
                mult = Mathf.Max(1f, mult); // smelters and cooking stations are only ever sped up
            mult = Mathf.Max(0.01f, mult);
            if (Mathf.Approximately(mult, 1f))
                return Rate.Fmt(vanillaSeconds) + "s";
            return Rate.Fmt(vanillaSeconds / mult) + "s (vanilla " + Rate.Fmt(vanillaSeconds) + "s, x" + Rate.Fmt(mult) + ")";
        }

        private static string AgeText(long ticks)
        {
            if (ticks == 0L || ZNet.instance == null)
                return "-";
            return Rate.Fmt((float)new TimeSpan(ZNet.instance.GetTime().Ticks - ticks).TotalSeconds) + "s";
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Makes the owner accept the edit: its copy must have a lower data revision than ours.</summary>
        private static void Publish(ZDO zdo)
        {
            zdo.DataRevision = zdo.DataRevision + RevisionJump;
        }

        private static void Log(string text)
        {
            if (_logged++ < 12)
                Plugin.Log.LogInfo("ServerRates stations: " + text);
        }

        public static void Prune()
        {
            if (DonePlants.Count > 2000)
                DonePlants.Clear(); // old plants are not fresh anymore, so clearing is safe
            if (LastStart.Count > 2000)
                LastStart.Clear();

            if (InfoShown.Count > 200)
            {
                float cutoff = Time.time - 60f;
                var old = new List<string>();
                foreach (KeyValuePair<string, float> pair in InfoShown)
                    if (pair.Value < cutoff)
                        old.Add(pair.Key);
                foreach (string k in old)
                    InfoShown.Remove(k);
            }

            Scratch.Clear();
            foreach (ZDOID id in Tracks.Keys)
                if (ZDOMan.instance.GetZDO(id) == null)
                    Scratch.Add(id);
            for (int i = 0; i < Scratch.Count; i++)
                Tracks.Remove(Scratch[i]);
        }
    }
}
