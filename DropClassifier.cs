using System.Collections.Generic;
using UnityEngine;

namespace ServerRates
{
    /// <summary>Maps an item (prefab / shared name) to a loot category.</summary>
    internal static class DropClassifier
    {
        public enum Kind
        {
            Other,
            // Materials
            Wood, FineWood, CoreWood, SpecialWood, Ore, Scrap, Stone, Flint, Crystal, Fuel, Gems, BossMats,
            // Consumables
            Crops, Seeds, Mushrooms, BerriesHoney, Fish, Potions,
            // Mob drops
            Hide, Trophy, Meat, Parts, Feathers, SpecialParts
        }

        private static readonly Dictionary<string, Kind> Exact = new Dictionary<string, Kind>();

        static DropClassifier()
        {
            // Order = priority when a name is listed twice (first wins).
            Add(Kind.FineWood, "finewood");
            Add(Kind.CoreWood, "roundlog");
            Add(Kind.SpecialWood, "elderbark", "yggdrasilwood", "ashwood", "blackwood");
            Add(Kind.Wood, "wood");
            Add(Kind.Ore, "copperore", "tinore", "ironore", "silverore", "flametalore", "flametalorenew");
            Add(Kind.Scrap, "ironscrap", "blackmetalscrap", "copperscrap");
            Add(Kind.Flint, "flint");
            Add(Kind.Crystal, "crystal", "obsidian");
            Add(Kind.Stone, "stone", "grausten", "sharpeningstone", "thunderstone");
            Add(Kind.Fuel, "coal", "resin", "tar");
            Add(Kind.Gems, "coins", "amber", "amberpearl", "ruby", "silvernecklace");
            Add(Kind.BossMats,
                "surtlingcore", "dragontear", "ancientseed", "ymirremains", "queenbee", "yagluthdrop",
                "vegvisirshardbonemass", "blackcore", "refinedeitr", "mechanicalspring", "dvergrkeyfragment",
                "trophybonemass", "trophyeikthyr", "trophytheelder", "trophydragonqueen", "trophygoblinking",
                "trophyseekerqueen", "trophyfader");
            Add(Kind.Seeds, "carrotseeds", "onionseeds", "turnipseeds", "beechseeds", "birchseeds", "fircone",
                "pinecone", "acorn", "oakseeds");
            Add(Kind.Mushrooms, "mushroom", "mushroomyellow", "mushroomblue", "magecap");
            Add(Kind.BerriesHoney, "raspberry", "blueberry", "cloudberry", "honey", "bukeperries");
            Add(Kind.Meat,
                "rawmeat", "necktail", "boarmeat", "deer", "deermeat", "wolfmeat", "loxmeat", "serpentmeat",
                "chickenmeat", "haremeat", "asksvinmeat", "bugmeat", "meatrotten", "necktailgrilled",
                "cookedmeat", "cookedbackmeat", "serpentmeatcooked", "cookeddeer", "cookedwolf", "loxpie",
                "fishcooked");
            Add(Kind.Fish, "fishraw", "fish", "perch", "pike", "tuna", "trollfish", "pufferfish", "anglerfish",
                "coralcod", "northernsalmon", "magmapander", "voltureegg");
            Add(Kind.Potions, "meadbasehealth", "meadbasehealthminor", "meadbasestamina", "meadbasetasty",
                "meadbasepoisonresist", "meadbasefrostresist", "meadbaseeitr", "meadbaselight",
                "potionhealthminor", "potionhealthmedium", "potionstaminaminor", "potionstaminamedium",
                "potioneitrminor", "poisonbomb");
            Add(Kind.Crops, "barley", "barleyflour", "flax", "carrot", "onion", "turnip", "thistle", "dandelion",
                "sap", "royaljelly", "jotunpuffs", "smokepuffs");
            Add(Kind.Hide, "deerhide", "leatherscraps", "trollhide", "wolfpelt", "loxpelt", "serpentscale",
                "chitin", "scalehide", "asksvinhide", "harepelt", "wolfhairbundle");
            Add(Kind.Feathers, "feathers");
            Add(Kind.SpecialParts, "guck", "ooze", "softtissue", "bilebag", "seekerchitin", "carapace",
                "moltencore", "charredskull", "flametal", "proustitepowder");
            Add(Kind.Parts, "bonefragments", "witheredbone", "entrails", "bloodbag", "needle", "greydwarfeye",
                "freezegland", "hardantler", "wolffang", "root", "mandible", "chain");
        }

        private static void Add(Kind kind, params string[] names)
        {
            foreach (string n in names)
                if (!Exact.ContainsKey(n))
                    Exact[n] = kind;
        }

        public static Kind Classify(GameObject prefab)
        {
            if (prefab == null)
                return Kind.Other;

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null)
            {
                Kind fromShared = ClassifyKey(Normalize(drop.m_itemData.m_shared.m_name));
                if (fromShared != Kind.Other)
                    return fromShared;
            }

            return ClassifyKey(Normalize(prefab.name));
        }

        private static Kind ClassifyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Kind.Other;

            Kind exact;
            if (Exact.TryGetValue(key, out exact))
                return exact;

            // Fuzzy fallbacks for modded / unlisted items.
            if (key.EndsWith("wood") && !key.Contains("yggdrasil")) return Kind.Wood;
            if (key.EndsWith("ore") && !key.EndsWith("core")) return Kind.Ore;
            if (key.EndsWith("scrap")) return Kind.Scrap;
            if (key.EndsWith("seeds") || key.EndsWith("cone")) return Kind.Seeds;
            if (key.Contains("mushroom")) return Kind.Mushrooms;
            if (key.Contains("berry")) return Kind.BerriesHoney;
            if (key.StartsWith("fish") || key.Contains("salmon") || key.Contains("puffer")) return Kind.Fish;
            if (key.Contains("mead") || key.Contains("potion")) return Kind.Potions;
            if (key.Contains("trophy")) return Kind.Trophy;
            if (key.Contains("hide") || key.Contains("pelt") || key.Contains("leather") || key.Contains("scale")) return Kind.Hide;
            if (key.Contains("meat") || key.Contains("necktail")) return Kind.Meat;
            if (key.Contains("feather")) return Kind.Feathers;
            if (key.Contains("bone") || key.Contains("entrails") || key.Contains("gland") || key.Contains("fang") || key.Contains("eye"))
                return Kind.Parts;

            return Kind.Other;
        }

        private static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            string s = raw.Trim().ToLowerInvariant();
            int clone = s.IndexOf("(clone)");
            if (clone >= 0)
                s = s.Substring(0, clone).Trim();
            if (s.StartsWith("$item_"))
                s = s.Substring(6);
            return s.Replace(" ", "").Replace("-", "").Replace("_", "");
        }
    }
}
