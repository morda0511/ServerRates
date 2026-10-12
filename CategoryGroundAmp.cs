using System.Collections.Generic;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Category loot multipliers, server side only.
    ///
    /// Loot is rolled on the client that owns the tree / creature, so the dedicated cannot hook the roll.
    /// Instead it watches item ZDOs near connected players. Once an item lies still on the ground it
    /// creates the extra piles as plain ZDOs (no server-side GameObject, no server physics). Nearby clients
    /// instantiate them like any other item and let them rest on the real ground.
    ///
    /// Why wait until the item rests: the ZDO position of a drop that is still flying is mid-air. Extra piles
    /// created there hang in the air for everybody.
    /// </summary>
    internal static class CategoryGroundAmp
    {
        private const float ScanInterval = 0.25f;
        private const float PruneInterval = 30f;
        private const float MaxWait = 8f;       // amplify even if the item never settles
        private const int StillScansNeeded = 2; // consecutive scans without movement
        private const float MoveEpsilonSqr = 0.0004f;

        private static readonly int CatFlag = "SR.Cat".GetStableHashCode();

        private sealed class ItemInfo
        {
            public int Hash;
            public GameObject Prefab;
            public ItemDrop Template;
            public DropClassifier.Kind Kind;
            public int MaxStack;
        }

        private sealed class Pending
        {
            public ItemInfo Info;
            public Vector3 Pos;
            public float Since;
            public int Still;
        }

        private static readonly Dictionary<int, ItemInfo> Items = new Dictionary<int, ItemInfo>();
        private static readonly HashSet<int> Ignored = new HashSet<int>();
        private static readonly Dictionary<ZDOID, Pending> Waiting = new Dictionary<ZDOID, Pending>();
        private static readonly HashSet<ZDOID> Done = new HashSet<ZDOID>();
        private static readonly List<ZDO> Near = new List<ZDO>(512);
        private static readonly List<ZDO> Distant = new List<ZDO>(512);
        private static readonly List<ZDOID> Scratch = new List<ZDOID>();

        private static float _nextScan;
        private static float _nextPrune;
        private static int _amplified;

        public static void LogStatus()
        {
            if (Plugin.Log == null || Rates.GroundAmp == null)
                return;
            Plugin.Log.LogInfo("ServerRates loot: GroundAmp=" + Rates.GroundAmp.Display()
                + " MaxPiles=" + Rates.MaxPiles.Display()
                + " (rates changed: " + CountNonDefaultLoot() + " of "
                + Rates.InGroup("loot").Count + " categories)");
        }

        private static int CountNonDefaultLoot()
        {
            int n = 0;
            foreach (Rate r in Rates.InGroup("loot"))
                if (!r.IsDefault())
                    n++;
            return n;
        }

        public static void Tick()
        {
            if (!Rates.Ready)
                return;
            if (ZDOMan.instance == null || ZNet.instance == null || ZNetScene.instance == null)
                return;

            float now = Time.time;
            if (now < _nextScan)
                return;
            _nextScan = now + ScanInterval;

            List<ZNetPeer> peers = ZNet.instance.GetPeers();
            if (peers != null && peers.Count > 0)
            {
                Near.Clear();
                Distant.Clear();

                SimulationDistance sim = ZNet.instance.GetSyncedSimulationDistance();
                for (int i = 0; i < peers.Count; i++)
                {
                    ZNetPeer peer = peers[i];
                    if (peer == null || !peer.IsReady())
                        continue;
                    ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(peer.GetRefPos()), sim, Near, Distant);
                }

                bool loot = Rates.GroundAmp.Flag;
                for (int i = 0; i < Near.Count; i++)
                {
                    StationBoost.Observe(Near[i], now);
                    StationBoost.Info(Near[i], now);
                    if (loot)
                        Observe(Near[i], now);
                }
                for (int i = 0; i < Distant.Count; i++)
                {
                    StationBoost.Observe(Distant[i], now);
                    StationBoost.Info(Distant[i], now);
                    if (loot)
                        Observe(Distant[i], now);
                }
            }

            ProcessWaiting(now);

            if (now >= _nextPrune)
            {
                _nextPrune = now + PruneInterval;
                Prune();
                StationBoost.Prune();
            }
        }

        private static ItemInfo GetInfo(int hash)
        {
            ItemInfo info;
            if (Items.TryGetValue(hash, out info))
                return info;
            if (Ignored.Contains(hash))
                return null;

            GameObject prefab = ZNetScene.instance.GetPrefab(hash);
            ItemDrop template = prefab != null ? prefab.GetComponent<ItemDrop>() : null;

            // Wild fish are ItemDrops too: never duplicate them in the water.
            if (template == null || template.m_itemData == null || prefab.GetComponent<Fish>() != null)
            {
                Ignored.Add(hash);
                return null;
            }

            info = new ItemInfo
            {
                Hash = hash,
                Prefab = prefab,
                Template = template,
                Kind = DropClassifier.Classify(prefab),
                MaxStack = template.m_itemData.m_shared != null
                    ? Mathf.Max(1, template.m_itemData.m_shared.m_maxStackSize) : 1
            };
            Items[hash] = info;
            return info;
        }

        private static void Observe(ZDO zdo, float now)
        {
            if (zdo == null)
                return;

            int hash = zdo.GetPrefab();
            if (Ignored.Contains(hash))
                return;

            ZDOID id = zdo.m_uid;
            if (Done.Contains(id) || Waiting.ContainsKey(id))
                return;

            ItemInfo info = GetInfo(hash);
            if (info == null)
                return;

            // Items we asked a client to spawn show up as new drops: never amplify them again.
            if (IsOwnSpawn(hash, zdo.GetPosition(), zdo, now))
            {
                Done.Add(id);
                return;
            }

            if (zdo.GetInt(CatFlag, 0) != 0)
            {
                Done.Add(id);
                return;
            }

            // Anything a player ever held or crafted is not fresh loot (stops dupe by drop & re-pickup).
            ItemDrop.ItemData data = info.Template.m_itemData.Clone();
            ItemDrop.LoadFromZDO(data, zdo, 0);
            if (data.m_pickedUp || data.m_crafterID != 0)
            {
                Done.Add(id);
                return;
            }

            // Items already on the ground before the rate was raised stay untouched.
            if (Rates.DropMultiplier(info.Kind) <= 1.001f)
            {
                Done.Add(id);
                return;
            }

            Pending pending = new Pending { Info = info, Pos = zdo.GetPosition(), Since = now };

            // Client spawns need no resting item: they fall with real physics. Amplify right away so
            // the extras appear while the original is still in the air, even if it is picked up at once.
            if (ViaClient())
            {
                Amplify(zdo, pending);
                Done.Add(id);
                return;
            }

            Waiting[id] = pending;
        }

        private static bool ViaClient()
        {
            return string.Equals(Rates.LootSpawn.Text, "Client", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void ProcessWaiting(float now)
        {
            if (Waiting.Count == 0)
                return;

            Scratch.Clear();
            foreach (KeyValuePair<ZDOID, Pending> pair in Waiting)
            {
                ZDO zdo = ZDOMan.instance.GetZDO(pair.Key);
                if (zdo == null) // picked up / despawned while waiting
                {
                    Scratch.Add(pair.Key);
                    continue;
                }

                Pending p = pair.Value;
                Vector3 pos = zdo.GetPosition();
                bool moved = (pos - p.Pos).sqrMagnitude > MoveEpsilonSqr;
                p.Pos = pos;
                p.Still = moved ? 0 : p.Still + 1;

                if (p.Still >= StillScansNeeded || now - p.Since > MaxWait)
                {
                    Amplify(zdo, p);
                    Scratch.Add(pair.Key);
                }
            }

            for (int i = 0; i < Scratch.Count; i++)
            {
                Waiting.Remove(Scratch[i]);
                Done.Add(Scratch[i]);
            }
        }

        private static void Amplify(ZDO zdo, Pending p)
        {
            ItemInfo info = p.Info;
            float mult = Mathf.Max(0f, Rates.DropMultiplier(info.Kind));

            ItemDrop.ItemData data = info.Template.m_itemData.Clone();
            ItemDrop.LoadFromZDO(data, zdo, 0);

            int before = Mathf.Max(1, data.m_stack);
            int extra = Mathf.RoundToInt(before * mult) - before;

            try
            {
                zdo.Set(CatFlag, 1);
            }
            catch
            {
                // flag is best effort, Done already protects this session
            }

            if (extra <= 0)
                return;

            int cap = Mathf.Max(1, Mathf.RoundToInt(Rates.MaxPiles.Value));
            Quaternion rot = zdo.GetRotation();
            bool viaClient = ViaClient();

            int piles = 0;
            int added = 0;
            if (viaClient)
            {
                // One vanilla item per request; the game stacks neighbours on its own.
                int count = Mathf.Min(extra, cap);
                piles = SpawnViaClient(info, p.Pos, rot, count);
                added = piles;
            }
            else
            {
                while (extra > 0 && piles < cap)
                {
                    int stack = Mathf.Min(extra, info.MaxStack);
                    extra -= stack;
                    added += stack;
                    piles++;
                    SpawnPile(info, p.Pos, rot, data, stack);
                }
            }

            if (_amplified++ < 10)
                Plugin.Log.LogInfo("ServerRates loot: " + info.Prefab.name + " (" + info.Kind + ") x"
                    + Rate.Fmt(mult) + ", " + before + " -> " + (before + added)
                    + " via " + (viaClient ? "player client" : "server object") + ", " + piles + " spawn(s)");
        }

        private sealed class OwnSpawn
        {
            public int Hash;
            public Vector3 Pos;
            public float Until;
            public int Left;
            public long SentTicks; // network time when the spawn request was sent
        }

        private static readonly List<OwnSpawn> OwnSpawns = new List<OwnSpawn>();

        // A spawned item carries its creation time (network time). Items created before our request are
        // other drops (e.g. the 2nd and 3rd wood of the same tree) and must be amplified on their own.
        private const long SpawnTimeSlackTicks = 1000000; // 0.1 s, covers client/server clock difference

        private static bool IsOwnSpawn(int hash, Vector3 pos, ZDO zdo, float now)
        {
            for (int i = OwnSpawns.Count - 1; i >= 0; i--)
            {
                OwnSpawn s = OwnSpawns[i];
                if (now > s.Until || s.Left <= 0)
                {
                    OwnSpawns.RemoveAt(i);
                    continue;
                }

                if (s.Hash != hash)
                    continue;

                Vector3 d = pos - s.Pos;
                if (d.x * d.x + d.z * d.z > 1f || Mathf.Abs(d.y) > 3f)
                    continue;

                long created = zdo.GetLong(ZDOVars.s_spawnTime, 0L);
                if (created != 0L && created < s.SentTicks - SpawnTimeSlackTicks)
                    continue; // older than our request: a genuine drop

                s.Left--;
                return true;
            }

            return false;
        }

        private static ZNetPeer NearestPeer(Vector3 at)
        {
            ZNetPeer best = null;
            float bestSqr = float.MaxValue;
            List<ZNetPeer> peers = ZNet.instance.GetPeers();
            for (int i = 0; peers != null && i < peers.Count; i++)
            {
                ZNetPeer peer = peers[i];
                if (peer == null || !peer.IsReady())
                    continue;
                float d = (peer.GetRefPos() - at).sqrMagnitude;
                if (d < bestSqr)
                {
                    bestSqr = d;
                    best = peer;
                }
            }

            return best;
        }

        /// <summary>
        /// Asks the nearest player's game to spawn plain copies of the item with vanilla's "SpawnObject" RPC.
        /// The client creates them like any other drop: real ground, real physics, normal replication.
        /// </summary>
        private static int SpawnViaClient(ItemInfo info, Vector3 at, Quaternion rot, int count)
        {
            ZNetPeer peer = NearestPeer(at);
            if (peer == null || ZRoutedRpc.instance == null || count <= 0)
                return 0;

            for (int i = 0; i < count; i++)
            {
                Vector2 jitter = Random.insideUnitCircle * 0.5f;
                Vector3 pos = at + new Vector3(jitter.x, 0.5f, jitter.y);
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "SpawnObject", new object[] { pos, rot, info.Hash });
            }

            OwnSpawns.Add(new OwnSpawn { Hash = info.Hash, Pos = at, Until = Time.time + 6f, Left = count, SentTicks = ZNet.instance.GetTime().Ticks });
            return count;
        }

        /// <summary>Creates the pile as a bare ZDO next to the resting original. Clients spawn and settle it.</summary>
        private static void SpawnPile(ItemInfo info, Vector3 at, Quaternion rot, ItemDrop.ItemData template, int stack)
        {
            Vector2 jitter = Random.insideUnitCircle * 0.4f;
            Vector3 pos = at + new Vector3(jitter.x, 0.2f, jitter.y);

            ZDO z = ZDOMan.instance.CreateNewZDO(pos, info.Hash);
            z.SetRotation(rot);

            ZNetView view = info.Prefab.GetComponent<ZNetView>();
            if (view != null)
            {
                z.Persistent = view.m_persistent;
                z.Distant = view.m_distant;
                z.SetType(view.m_type);
            }

            z.Set(CatFlag, 1);

            ItemDrop.ItemData data = template.Clone();
            data.m_stack = stack;
            ItemDrop.SaveToZDO(data, z, 0);

            Done.Add(z.m_uid);
        }

        /// <summary>Forget ids whose ZDO is gone so the sets stay small on long-running servers.</summary>
        private static void Prune()
        {
            if (Done.Count < 500)
                return;

            Scratch.Clear();
            foreach (ZDOID id in Done)
                if (ZDOMan.instance.GetZDO(id) == null)
                    Scratch.Add(id);
            for (int i = 0; i < Scratch.Count; i++)
                Done.Remove(Scratch[i]);
        }
    }
}
