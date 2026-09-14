using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ServerRates
{
    /// <summary>
    /// Category multipliers without stealing ZDO ownership.
    /// Tree/rock loot usually rolls on the nearby client; the dedicated often never
    /// CreateObject()s ItemDrops (reference pos is not the player). We scan ZDOs
    /// near connected peers and spawn extra piles server-side.
    /// </summary>
    internal static class CategoryGroundAmp
    {
        private static readonly int CatFlag = "SR.Cat".GetStableHashCode();
        private static readonly int NoCatFlag = "SR.NoCat".GetStableHashCode();

        private static readonly HashSet<ZDOID> Done = new HashSet<ZDOID>();
        private static readonly List<ZDO> Near = new List<ZDO>(256);
        private static readonly List<ZDO> Distant = new List<ZDO>(256);

        private static float _nextScan;
        private static int _diag;
        private static int _seen;

        public static void LogStatus()
        {
            if (Plugin.Log == null || Plugin.Settings == null)
                return;
            ModConfig c = Plugin.Settings;
            Plugin.Log.LogInfo(
                "ServerRates loot: GroundAmp=" + c.CategoryGroundAmp.Value
                + " SnapGround=" + c.SnapExtraDropsToGround.Value
                + " Wood=x" + c.WoodDropMultiplier.Value
                + " Hide=x" + c.HideDropMultiplier.Value
                + " Meat=x" + c.MeatDropMultiplier.Value
                + " Ore=x" + c.OreDropMultiplier.Value
                + " (fine settings in 6a/6b/6c)");
        }

        public static void MarkPlayerDrop(ItemDrop drop)
        {
            ZNetView nv = drop != null ? drop.GetComponent<ZNetView>() : null;
            if (nv == null || !nv.IsValid())
                return;
            ZDO zdo = nv.GetZDO();
            if (zdo == null)
                return;
            zdo.Set(NoCatFlag, 1);
            Done.Add(zdo.m_uid);
        }

        public static void Tick()
        {
            if (!DropScale.Ready() || Plugin.Settings == null || !Plugin.Settings.CategoryGroundAmp.Value)
                return;
            if (ZDOMan.instance == null || ZNet.instance == null || ZNetScene.instance == null)
                return;
            if (Time.time < _nextScan)
                return;
            _nextScan = Time.time + 0.35f;

            Near.Clear();
            Distant.Clear();

            List<ZNetPeer> peers = ZNet.instance.GetPeers();
            if (peers == null || peers.Count == 0)
                return;

            SimulationDistance sim = ZNet.instance.GetSyncedSimulationDistance();
            for (int i = 0; i < peers.Count; i++)
            {
                ZNetPeer peer = peers[i];
                if (peer == null || !peer.IsReady())
                    continue;
                Vector2s zone = ZoneSystem.GetZone(peer.GetRefPos());
                ZDOMan.instance.FindSectorObjects(zone, sim, Near, Distant);
            }

            for (int i = 0; i < Near.Count; i++)
                TryAmplifyZdo(Near[i]);
            for (int i = 0; i < Distant.Count; i++)
                TryAmplifyZdo(Distant[i]);

            if (Done.Count > 4000)
                Done.Clear();
        }

        public static void TryAmplifyZdo(ZDO zdo)
        {
            if (zdo == null || !DropScale.Ready())
                return;
            if (Plugin.Settings == null || !Plugin.Settings.CategoryGroundAmp.Value)
                return;
            if (ZNetScene.instance == null)
                return;

            if (Done.Contains(zdo.m_uid))
                return;
            if (zdo.GetInt(NoCatFlag, 0) != 0)
            {
                Done.Add(zdo.m_uid);
                return;
            }
            if (zdo.GetInt(CatFlag, 0) != 0)
            {
                Done.Add(zdo.m_uid);
                return;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
            if (prefab == null)
                return;

            ItemDrop templateDrop = prefab.GetComponent<ItemDrop>();
            if (templateDrop == null || templateDrop.m_itemData == null)
                return;

            DropClassifier.Kind kind = DropClassifier.Classify(prefab);
            if (kind == DropClassifier.Kind.Other)
                kind = DropClassifier.Classify(templateDrop.m_itemData);

            float mult = Mathf.Max(0f, DropClassifier.MultiplierFor(kind, Plugin.Settings));

            ItemDrop.ItemData data = templateDrop.m_itemData.Clone();
            ItemDrop.LoadFromZDO(data, zdo, 0);
            int before = Mathf.Max(1, data.m_stack);

            if (_seen < 30 && Plugin.Log != null)
            {
                _seen++;
                Plugin.Log.LogInfo(
                    "ServerRates ground amp check: '" + prefab.name + "' -> " + kind
                    + " x" + mult.ToString("0.##") + " stack=" + before);
            }

            // Claim locally first so we never double-amp even if ZDO.Set is ignored.
            Done.Add(zdo.m_uid);
            try
            {
                zdo.Set(CatFlag, 1);
            }
            catch
            {
                // ignore — client-owned ZDOs may reject sets
            }

            if (mult <= 1.001f)
                return;

            int target = Mathf.Max(0, Mathf.RoundToInt(before * mult));
            if (target <= before)
                return;

            if (_diag < 40 && Plugin.Log != null)
            {
                _diag++;
                Plugin.Log.LogInfo(
                    "ServerRates ground amp: " + kind + " x" + mult.ToString("0.##")
                    + " '" + prefab.name + "' " + before + " -> " + target);
            }

            int maxStack = 1;
            if (data.m_shared != null)
                maxStack = Mathf.Max(1, data.m_shared.m_maxStackSize);

            int remain = target - before;
            Vector3 pos = zdo.GetPosition();
            Quaternion rot = zdo.GetRotation();

            while (remain > 0)
            {
                int pile = Mathf.Min(remain, maxStack);
                remain -= pile;
                SpawnExtra(prefab, pos, rot, data, pile);
            }
        }

        private static void SpawnExtra(
            GameObject prefab,
            Vector3 pos,
            Quaternion rot,
            ItemDrop.ItemData template,
            int stack)
        {
            Vector2 jitter = Random.insideUnitCircle * 0.45f;
            Vector3 at = pos + new Vector3(jitter.x, 0.15f, jitter.y);

            ModConfig cfg = Plugin.Settings;
            if (cfg != null && cfg.SnapExtraDropsToGround.Value && ZoneSystem.instance != null)
            {
                float ground = ZoneSystem.instance.GetSolidHeight(at);
                // Only pull down / lift into a sane band so we don't bury under cliffs.
                if (ground > -1000f && ground < 5000f)
                {
                    if (at.y > ground + 0.35f || at.y < ground - 0.1f)
                        at.y = ground + 0.2f;
                }
            }

            GameObject go = Object.Instantiate(prefab, at, rot);
            ItemDrop extra = go.GetComponent<ItemDrop>();
            if (extra == null)
            {
                Object.Destroy(go);
                return;
            }

            if (extra.m_itemData != null && template != null)
            {
                extra.m_itemData = template.Clone();
                extra.m_itemData.m_stack = stack;
            }

            ZNetView env = extra.GetComponent<ZNetView>();
            if (env != null && env.IsValid())
            {
                ZDO ez = env.GetZDO();
                if (ez != null)
                {
                    Done.Add(ez.m_uid);
                    ez.Set(CatFlag, 1);
                    ez.Set(NoCatFlag, 0);
                    ez.SetPosition(at);
                    if (extra.m_itemData != null)
                        ItemDrop.SaveToZDO(extra.m_itemData, ez, 0);
                }
            }

            ItemDrop.OnCreateNew(go, false);

            // Match CharacterDrop scatter so piles fall instead of hanging in the air.
            float force = cfg != null ? Mathf.Max(0f, cfg.ExtraDropScatterForce.Value) : 4f;
            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body != null && force > 0.01f)
            {
                Vector3 push = Random.insideUnitSphere * force;
                if (push.y < 0f)
                    push.y = -push.y;
                body.WakeUp();
                body.AddForce(push, ForceMode.VelocityChange);
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.OnPlayerDrop))]
    internal static class ItemDropOnPlayerDropPatch
    {
        private static void Postfix(ItemDrop __instance)
        {
            CategoryGroundAmp.MarkPlayerDrop(__instance);
        }
    }

    // Client-owned tree loot: when the dedicated does create the GO, amp immediately.
    [HarmonyPatch(typeof(ZNetScene), "CreateObject")]
    internal static class ZNetSceneCreateObjectPatch
    {
        private static void Postfix(ZDO zdo, GameObject __result)
        {
            if (zdo != null)
                CategoryGroundAmp.TryAmplifyZdo(zdo);
            else if (__result != null)
            {
                ZNetView nv = __result.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid())
                    CategoryGroundAmp.TryAmplifyZdo(nv.GetZDO());
            }
        }
    }
}
