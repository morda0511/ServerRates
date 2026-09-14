using System.Collections.Generic;
using UnityEngine;

namespace ServerRates
{
    internal static class DropClassifier
    {
        public enum Kind
        {
            Other,
            // Materials
            Wood,
            FineWood,
            CoreWood,
            SpecialWood,
            Ore,
            Scrap,
            Stone,
            Flint,
            Crystal,
            Fuel,
            Gems,
            BossMats,
            // Consumables
            Crops,
            Seeds,
            Mushrooms,
            BerriesHoney,
            Fish,
            Potions,
            // Mob drops
            Hide,
            Trophy,
            Meat,
            Parts,
            Feathers,
            SpecialParts
        }

        private static readonly HashSet<string> Wood = new HashSet<string> { "wood" };
        private static readonly HashSet<string> FineWood = new HashSet<string> { "finewood" };
        private static readonly HashSet<string> CoreWood = new HashSet<string> { "roundlog" };
        private static readonly HashSet<string> SpecialWood = new HashSet<string>
        {
            "elderbark", "yggdrasilwood", "ashwood", "blackwood"
        };

        private static readonly HashSet<string> Ore = new HashSet<string>
        {
            "copperore", "tinore", "ironore", "silverore", "flametalore", "flametalorenew"
        };

        private static readonly HashSet<string> Scrap = new HashSet<string>
        {
            "ironscrap", "blackmetalscrap", "copperscrap"
        };

        private static readonly HashSet<string> Stone = new HashSet<string>
        {
            "stone", "grausten", "sharpeningstone", "thunderstone"
        };

        private static readonly HashSet<string> Flint = new HashSet<string> { "flint" };

        private static readonly HashSet<string> Crystal = new HashSet<string>
        {
            "crystal", "obsidian"
        };

        private static readonly HashSet<string> Fuel = new HashSet<string>
        {
            "coal", "resin", "tar"
        };

        private static readonly HashSet<string> Gems = new HashSet<string>
        {
            "coins", "amber", "amberpearl", "ruby", "silvernecklace"
        };

        private static readonly HashSet<string> BossMats = new HashSet<string>
        {
            "surtlingcore", "dragontear", "ancientseed", "ymirremains", "queenbee",
            "yagluthdrop", "vegvisirshard_bonemass", "blackcore", "refinedeitr",
            "mechanicalspring", "dvergrkeyfragment", "trophybonemass", "trophyeikthyr",
            "trophytheelder", "trophydragonqueen", "trophygoblinking", "trophyseekerqueen",
            "trophyfader"
        };

        private static readonly HashSet<string> Crops = new HashSet<string>
        {
            "barley", "barleyflour", "flax", "carrot", "onion", "turnip", "thistle",
            "dandelion", "sap", "royaljelly", "jotunpuffs", "smokepuffs"
        };

        private static readonly HashSet<string> Seeds = new HashSet<string>
        {
            "carrotseeds", "onionseeds", "turnipseeds", "beechseeds", "birchseeds",
            "fircone", "pinecone", "acorn", "oakseeds"
        };

        private static readonly HashSet<string> Mushrooms = new HashSet<string>
        {
            "mushroom", "mushroomyellow", "mushroomblue", "magecap"
        };

        private static readonly HashSet<string> BerriesHoney = new HashSet<string>
        {
            "raspberry", "blueberry", "cloudberry", "honey", "bukeperries"
        };

        private static readonly HashSet<string> Fish = new HashSet<string>
        {
            "fishraw", "fish", "perch", "pike", "tuna", "trollfish", "pufferfish",
            "anglerfish", "coralcod", "northernsalmon", "magmapander", "voltureegg"
        };

        private static readonly HashSet<string> Potions = new HashSet<string>
        {
            "meadbasehealth", "meadbasehealthminor", "meadbasestamina", "meadbasetasty",
            "meadbasepoisonresist", "meadbasefrostresist", "meadbaseeitr", "meadbaselight",
            "potion_health_minor", "potion_health_medium", "potion_stamina_minor",
            "potion_stamina_medium", "potion_eitr_minor", "poisonbomb"
        };

        private static readonly HashSet<string> Hide = new HashSet<string>
        {
            "deerhide", "leatherscraps", "trollhide", "wolfpelt", "loxpelt",
            "serpentscale", "chitin", "scalehide", "asksvinhide", "harepelt", "hare_pelt",
            "wolfhairbundle"
        };

        private static readonly HashSet<string> Meat = new HashSet<string>
        {
            "rawmeat", "necktail", "boarmeat", "deer", "deermeat", "wolfmeat", "loxmeat",
            "serpentmeat", "chickenmeat", "haremeat", "asksvinmeat", "bugmeat", "meatrotten",
            "necktailgrilled", "cookedmeat", "cookedbackmeat", "serpentmeatcooked",
            "cookeddeer", "cookedwolf", "loxpie", "fishcooked"
        };

        private static readonly HashSet<string> Parts = new HashSet<string>
        {
            "bonefragments", "witheredbone", "entrails", "bloodbag", "needle",
            "greydwarfeye", "freezegland", "hardantler", "wolffang", "root",
            "mandible", "chain"
        };

        private static readonly HashSet<string> Feathers = new HashSet<string>
        {
            "feathers"
        };

        private static readonly HashSet<string> SpecialParts = new HashSet<string>
        {
            "guck", "ooze", "softtissue", "bilebag", "refinedeitr", "blackcore",
            "dvergrkeyfragment", "mechanicalspring", "seekerchitin", "carapace",
            "moltencore", "charredskull", "flametal", "proustitepowder"
        };

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

        public static Kind Classify(ItemDrop.ItemData data)
        {
            if (data == null || data.m_shared == null)
                return Kind.Other;
            return ClassifyKey(Normalize(data.m_shared.m_name));
        }

        private static Kind ClassifyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Kind.Other;

            // More specific first.
            if (FineWood.Contains(key)) return Kind.FineWood;
            if (CoreWood.Contains(key)) return Kind.CoreWood;
            if (SpecialWood.Contains(key)) return Kind.SpecialWood;
            if (Wood.Contains(key) || key == "wood") return Kind.Wood;
            if (key.EndsWith("wood") && !key.Contains("yggdrasil")) return Kind.Wood;

            if (Ore.Contains(key) || (key.EndsWith("ore") && !key.Contains("flametalorenew")))
                return Kind.Ore;
            if (Scrap.Contains(key) || (key.EndsWith("scrap") && (key.Contains("iron") || key.Contains("metal") || key.Contains("copper") || key.Contains("black"))))
                return Kind.Scrap;

            if (Flint.Contains(key)) return Kind.Flint;
            if (Crystal.Contains(key)) return Kind.Crystal;
            if (Stone.Contains(key)) return Kind.Stone;

            if (Fuel.Contains(key)) return Kind.Fuel;
            if (Gems.Contains(key)) return Kind.Gems;
            if (BossMats.Contains(key)) return Kind.BossMats;

            if (Seeds.Contains(key) || key.EndsWith("seeds") || key.EndsWith("cone") || key == "acorn")
                return Kind.Seeds;
            if (Mushrooms.Contains(key) || key.Contains("mushroom") || key == "magecap")
                return Kind.Mushrooms;
            if (BerriesHoney.Contains(key) || key.Contains("berry") || key == "honey")
                return Kind.BerriesHoney;
            if (Fish.Contains(key) || key.StartsWith("fish") || key.Contains("salmon") || key.Contains("puffer"))
                return Kind.Fish;
            if (Potions.Contains(key) || key.Contains("mead") || key.Contains("potion"))
                return Kind.Potions;
            if (Crops.Contains(key)) return Kind.Crops;

            if (key.Contains("trophy")) return Kind.Trophy;
            if (Hide.Contains(key) || key.Contains("hide") || key.Contains("pelt") || key.Contains("leather") || key.Contains("scale"))
                return Kind.Hide;
            if (Meat.Contains(key) || key.Contains("meat") || key.Contains("necktail"))
                return Kind.Meat;
            if (Feathers.Contains(key) || key.Contains("feather"))
                return Kind.Feathers;
            if (SpecialParts.Contains(key))
                return Kind.SpecialParts;
            if (Parts.Contains(key) || key.Contains("bone") || key.Contains("entrails") || key.Contains("gland") || key.Contains("fang") || key.Contains("eye"))
                return Kind.Parts;

            return Kind.Other;
        }

        public static float MultiplierFor(Kind kind, ModConfig c)
        {
            if (c == null)
                return 1f;
            switch (kind)
            {
                case Kind.Wood: return c.WoodDropMultiplier.Value;
                case Kind.FineWood: return c.FineWoodDropMultiplier.Value;
                case Kind.CoreWood: return c.CoreWoodDropMultiplier.Value;
                case Kind.SpecialWood: return c.SpecialWoodDropMultiplier.Value;
                case Kind.Ore: return c.OreDropMultiplier.Value;
                case Kind.Scrap: return c.ScrapDropMultiplier.Value;
                case Kind.Stone: return c.StoneDropMultiplier.Value;
                case Kind.Flint: return c.FlintDropMultiplier.Value;
                case Kind.Crystal: return c.CrystalDropMultiplier.Value;
                case Kind.Fuel: return c.FuelDropMultiplier.Value;
                case Kind.Gems: return c.GemDropMultiplier.Value;
                case Kind.BossMats: return c.BossDropMultiplier.Value;
                case Kind.Crops: return c.CropDropMultiplier.Value;
                case Kind.Seeds: return c.SeedDropMultiplier.Value;
                case Kind.Mushrooms: return c.MushroomDropMultiplier.Value;
                case Kind.BerriesHoney: return c.BerryHoneyDropMultiplier.Value;
                case Kind.Fish: return c.FishDropMultiplier.Value;
                case Kind.Potions: return c.PotionDropMultiplier.Value;
                case Kind.Hide: return c.HideDropMultiplier.Value;
                case Kind.Trophy: return c.TrophyDropMultiplier.Value;
                case Kind.Meat: return c.MeatDropMultiplier.Value;
                case Kind.Parts: return c.PartsDropMultiplier.Value;
                case Kind.Feathers: return c.FeatherDropMultiplier.Value;
                case Kind.SpecialParts: return c.SpecialPartsDropMultiplier.Value;
                default: return c.OtherDropMultiplier.Value;
            }
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
