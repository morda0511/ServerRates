using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;

namespace ServerRates
{
    internal enum RateKind
    {
        Percent,    // 100 = vanilla
        Multiplier, // 1 = vanilla
        Number,     // plain number
        Toggle,     // Unchanged | On | Off
        Switch,     // true / false
        Choice      // one of Options
    }

    /// <summary>One tunable value: config entry + chat command + (optional) vanilla world key.</summary>
    internal sealed class Rate
    {
        public string Short;
        public string Key;
        public string Section;
        public string Group;
        public string Label;
        public string Description;
        public RateKind Kind;
        public GlobalKeys? GlobalKey;
        public DropClassifier.Kind? Drop;

        public float Min;
        public float Max = 10000f;
        public bool Integer;
        public float DefaultValue;
        public bool DefaultFlag;
        public string DefaultText = "Unchanged";
        public string[] Options;

        private ConfigEntry<float> _num;
        private ConfigEntry<string> _text;
        private ConfigEntry<bool> _flag;

        public bool IsNumeric
        {
            get { return Kind == RateKind.Percent || Kind == RateKind.Multiplier || Kind == RateKind.Number; }
        }

        public float Value { get { return _num != null ? _num.Value : 0f; } }
        public bool Flag { get { return _flag != null && _flag.Value; } }
        public string Text { get { return _text != null ? _text.Value : ""; } }

        internal void Bind(ConfigFile file)
        {
            switch (Kind)
            {
                case RateKind.Percent:
                case RateKind.Multiplier:
                case RateKind.Number:
                    string hint = Kind == RateKind.Percent ? " 100 = vanilla."
                        : Kind == RateKind.Multiplier ? " 1 = vanilla." : "";
                    _num = file.Bind(Section, Key, DefaultValue,
                        new ConfigDescription(Description + hint, new AcceptableValueRange<float>(Min, Max)));
                    break;
                case RateKind.Switch:
                    _flag = file.Bind(Section, Key, DefaultFlag, Description);
                    break;
                case RateKind.Toggle:
                    Options = new[] { "Unchanged", "On", "Off" };
                    _text = file.Bind(Section, Key, "Unchanged",
                        new ConfigDescription(Description + " Values: Unchanged | On | Off",
                            new AcceptableValueList<string>(Options)));
                    break;
                default:
                    _text = file.Bind(Section, Key, DefaultText,
                        new ConfigDescription(Description + " Values: " + string.Join(" | ", Options),
                            new AcceptableValueList<string>(Options)));
                    break;
            }
        }

        public static string Fmt(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public string Display()
        {
            switch (Kind)
            {
                case RateKind.Percent: return Fmt(Value) + "%";
                case RateKind.Multiplier: return "x" + Fmt(Value);
                case RateKind.Number: return Fmt(Value);
                case RateKind.Switch: return Flag ? "On" : "Off";
                default: return Text;
            }
        }

        public bool IsDefault()
        {
            switch (Kind)
            {
                case RateKind.Switch: return Flag == DefaultFlag;
                case RateKind.Toggle:
                case RateKind.Choice: return string.Equals(Text, DefaultText, StringComparison.OrdinalIgnoreCase);
                default: return Math.Abs(Value - DefaultValue) < 0.0001f;
            }
        }

        public void Reset()
        {
            switch (Kind)
            {
                case RateKind.Switch: _flag.Value = DefaultFlag; break;
                case RateKind.Toggle:
                case RateKind.Choice: _text.Value = DefaultText; break;
                default: _num.Value = DefaultValue; break;
            }
        }

        public string RangeHint()
        {
            switch (Kind)
            {
                case RateKind.Switch: return "on/off";
                case RateKind.Toggle: return "on/off/unchanged";
                case RateKind.Choice: return string.Join("/", Options).ToLowerInvariant();
                default: return Fmt(Min) + " - " + Fmt(Max);
            }
        }

        public bool TrySet(string raw, out string error)
        {
            error = null;
            string s = (raw ?? "").Trim().ToLowerInvariant();

            if (IsNumeric)
            {
                s = s.TrimStart('x', '*').TrimEnd('x', '%').Replace(',', '.');
                if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                {
                    error = Short + ": number expected (" + RangeHint() + ")";
                    return false;
                }
                if (Integer)
                    v = (float)Math.Round(v);
                if (v < Min || v > Max)
                {
                    error = Short + ": allowed " + RangeHint();
                    return false;
                }
                _num.Value = v;
                return true;
            }

            bool on = s == "on" || s == "true" || s == "yes" || s == "1";
            bool off = s == "off" || s == "false" || s == "no" || s == "0";

            if (Kind == RateKind.Switch)
            {
                if (!on && !off)
                {
                    error = Short + ": use on or off";
                    return false;
                }
                _flag.Value = on;
                return true;
            }

            if (Kind == RateKind.Toggle)
            {
                if (on) { _text.Value = "On"; return true; }
                if (off) { _text.Value = "Off"; return true; }
                if (s == "unchanged" || s == "default" || s == "-")
                {
                    _text.Value = "Unchanged";
                    return true;
                }
                error = Short + ": use on, off or unchanged";
                return false;
            }

            foreach (string opt in Options)
            {
                if (string.Equals(opt, s, StringComparison.OrdinalIgnoreCase))
                {
                    _text.Value = opt;
                    return true;
                }
            }
            error = Short + ": use " + RangeHint();
            return false;
        }
    }

    /// <summary>Every rate in one table. Config, chat commands, applier and help are generated from it.</summary>
    internal static class Rates
    {
        public static readonly List<Rate> All = new List<Rate>();
        private static readonly Dictionary<string, Rate> ByShort = new Dictionary<string, Rate>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Rate> ByKey = new Dictionary<string, Rate>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<DropClassifier.Kind, Rate> ByDrop = new Dictionary<DropClassifier.Kind, Rate>();

        /// <summary>Chat group → real groups it expands to.</summary>
        public static readonly Dictionary<string, string[]> Groups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "general", new[] { "general" } },
            { "skills", new[] { "skills" } },
            { "combat", new[] { "combat" } },
            { "survival", new[] { "survival" } },
            { "resources", new[] { "resources" } },
            { "loot", new[] { "materials", "consumables", "mobs" } },
            { "materials", new[] { "materials" } },
            { "consumables", new[] { "consumables" } },
            { "mobs", new[] { "mobs" } },
            { "death", new[] { "death" } },
            { "build", new[] { "build" } },
            { "map", new[] { "map" } },
            { "world", new[] { "world" } },
            { "stations", new[] { "stations" } },
        };

        public static Rate Enabled, ChatEnabled, ChatAccess, ChatBridge, StationInfo, GroundAmp, MaxPiles, LootSpawn;
        public static Rate Smelter, Fermenter, Cooking, Plants;

        private static ConfigFile _file;
        private static Dictionary<string, string> _applied = new Dictionary<string, string>();

        public static bool Ready { get { return Plugin.Active && Enabled != null && Enabled.Flag; } }

        public static Rate Find(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            Rate r;
            if (ByShort.TryGetValue(name, out r))
                return r;
            return ByKey.TryGetValue(name, out r) ? r : null;
        }

        public static float DropMultiplier(DropClassifier.Kind kind)
        {
            Rate r;
            return ByDrop.TryGetValue(kind, out r) ? r.Value : 1f;
        }

        public static List<Rate> InGroup(string group)
        {
            var list = new List<Rate>();
            string[] parts;
            if (!Groups.TryGetValue(group, out parts))
                return list;
            foreach (Rate r in All)
                if (Array.IndexOf(parts, r.Group) >= 0)
                    list.Add(r);
            return list;
        }

        public static void Save()
        {
            if (_file != null)
                _file.Save();
        }

        public static Dictionary<string, string> Snapshot()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Rate r in All)
                d[r.Short] = r.Display();
            return d;
        }

        /// <summary>Rates whose value differs from the last time this was called.</summary>
        public static List<Rate> TakeChanges()
        {
            var now = Snapshot();
            var changed = new List<Rate>();
            foreach (Rate r in All)
            {
                string old;
                if (!_applied.TryGetValue(r.Short, out old) || old != now[r.Short])
                    changed.Add(r);
            }
            _applied = now;
            return changed;
        }

        public static void Init(ConfigFile file)
        {
            _file = file;
            file.SaveOnConfigSet = false;

            const string G = "1 - General", SK = "2 - Skills", CB = "3 - Combat", SV = "4 - Survival",
                RS = "5 - Resources", LT = "6 - Loot", MT = "6a - Materials", CS = "6b - Consumables",
                MB = "6c - Mob Drops", DT = "7 - Death", BC = "8 - Build Craft", MP = "9 - Map Portals",
                WF = "10 - World Flags", ST = "11 - Stations";

            // General
            Enabled = Sw("enabled", "Enabled", G, "general", "ServerRates", true,
                "Master switch. Dedicated server only; vanilla clients need no install.");
            ChatEnabled = Sw("chatcmds", "ChatCommandsEnabled", G, "general", "Chat commands", true,
                "Allow rate commands in chat (sr_wood 9 / rates status). Console players type WITHOUT a slash.");
            ChatAccess = Ch("chataccess", "ChatAccess", G, "general", "Chat access", "Everyone",
                new[] { "Everyone", "AdminOnly" },
                "Who may CHANGE rates in chat. Everyone = no adminlist needed (console players). "
                + "AdminOnly = sender must be in the server adminlist. Reading values is always allowed.");
            ChatBridge = Sw("chatbridge", "ChatBridge", G, "general", "Chat bridge", true,
                "Lists the server as a pseudo player so that clients send their chat to it. Needed for chat commands: "
                + "without it the server never sees chat while only one player is online.");
            StationInfo = Sw("stationinfo", "StationInfo", G, "general", "Station info", true,
                "Shows a short message (top left) with the time per item when you stand next to a smelter, kiln, cooking station or fermenter. Turn off with sr_stationinfo off.");

            // Skills
            Pct("skillgain", "SkillGainPercent", SK, "skills", "Skill Gain", GlobalKeys.SkillGainRate,
                "XP for ALL skills. 200 = x2, 1000 = x10.");
            Pct("skillloss", "SkillReductionPercent", SK, "skills", "Skill Loss", GlobalKeys.SkillReductionRate,
                "Skill loss on death. 0 = none.");

            // Combat
            Pct("playerdmg", "PlayerDamagePercent", CB, "combat", "Player Damage", GlobalKeys.PlayerDamage, "Damage players deal.");
            Pct("enemydmg", "EnemyDamagePercent", CB, "combat", "Enemy Damage", GlobalKeys.EnemyDamage, "Damage enemies deal.");
            Pct("enemyspeed", "EnemySpeedSizePercent", CB, "combat", "Enemy Speed", GlobalKeys.EnemySpeedSize, "Enemy move speed / size.");
            Pct("enemystars", "EnemyLevelUpPercent", CB, "combat", "Enemy Stars", GlobalKeys.EnemyLevelUpRate, "How fast enemies star-up.");
            Pct("events", "EventRatePercent", CB, "combat", "Raid Rate", GlobalKeys.EventRate, "Raid / event frequency. 0 = none.");
            Add(new Rate
            {
                Short = "worldlevel", Key = "WorldLevel", Section = CB, Group = "combat", Label = "World Level",
                Kind = RateKind.Number, Min = 0, Max = 10, Integer = true, DefaultValue = 0,
                GlobalKey = GlobalKeys.WorldLevel, Description = "World level 0-10. 0 = vanilla start."
            });
            Tg("passivemobs", "PassiveMobs", CB, "combat", "Passive Mobs", GlobalKeys.PassiveMobs, "On = mobs do not attack.");
            Tg("playerevents", "PlayerEvents", CB, "combat", "Player Events", GlobalKeys.PlayerEvents, "Player-based raid events.");

            // Survival
            Pct("stamina", "StaminaPercent", SV, "survival", "Stamina", GlobalKeys.StaminaRate, "Stamina drain. Lower = less drain.");
            Pct("movestamina", "MoveStaminaPercent", SV, "survival", "Move Stamina", GlobalKeys.MoveStaminaRate, "Movement stamina drain.");
            Pct("staminaregen", "StaminaRegenPercent", SV, "survival", "Stamina Regen", GlobalKeys.StaminaRegenRate, "Stamina regen. Higher = faster.");
            Pct("eitr", "EitrPercent", SV, "survival", "Eitr", GlobalKeys.EitrRate, "Eitr rate.");
            Pct("adrenaline", "AdrenalinePercent", SV, "survival", "Adrenaline", GlobalKeys.AdrenalineRate, "Adrenaline rate.");
            Pct("food", "FoodPercent", SV, "survival", "Food Duration", GlobalKeys.FoodRate, "Food duration scaling.");
            Pct("durability", "DurabilityPercent", SV, "survival", "Durability", GlobalKeys.DurabilityRate,
                "Tool/weapon durability loss. Lower = lasts longer.");
            Pct("carry", "CarryWeightPercent", SV, "survival", "Carry Weight", GlobalKeys.CarryWeightRate, "Carry weight. 200 = x2 capacity.");

            // Resources / loot master
            Pct("resourcerate", "ResourceRatePercent", RS, "resources", "Resource Rate", GlobalKeys.ResourceRate,
                "GLOBAL drop rate (vanilla world modifier ResourceRate). Synced to all clients. Category multipliers stack on top.");
            GroundAmp = Sw("groundamp", "CategoryGroundAmp", LT, "resources", "Ground Amp", true,
                "Server multiplies categorized ground loot after it dropped (works for vanilla/PS5/Xbox clients). Player-dropped items are skipped.");
            MaxPiles = new Rate
            {
                Short = "maxpiles", Key = "MaxExtraPiles", Section = LT, Group = "resources", Label = "Max Piles",
                Kind = RateKind.Number, Min = 1, Max = 200, Integer = true, DefaultValue = 20,
                Description = "Safety cap: max extra items (LootSpawn=Client) or piles (LootSpawn=Server) created per single drop."
            };
            Add(MaxPiles);
            LootSpawn = Ch("lootspawn", "LootSpawn", LT, "resources", "Loot spawn", "Client",
                new[] { "Client", "Server" },
                "How extra loot is created. Client = the server asks the nearest player's game to spawn it "
                + "(real ground and physics, recommended). Server = the server creates the item objects itself.");
            Drp("other", "OtherDropMultiplier", LT, "resources", "Other Loot", DropClassifier.Kind.Other,
                "Anything not matched below. Keep at 1 unless you want everything amplified.");

            // Materials
            Drp("wood", "WoodDropMultiplier", MT, "materials", "Wood", DropClassifier.Kind.Wood, "Basic wood.");
            Drp("finewood", "FineWoodDropMultiplier", MT, "materials", "Fine Wood", DropClassifier.Kind.FineWood, "Fine wood.");
            Drp("corewood", "CoreWoodDropMultiplier", MT, "materials", "Core Wood", DropClassifier.Kind.CoreWood, "Core wood (RoundLog).");
            Drp("specialwood", "SpecialWoodDropMultiplier", MT, "materials", "Special Wood", DropClassifier.Kind.SpecialWood,
                "Elder bark, Yggdrasil, Ash, Black wood.");
            Drp("ore", "OreDropMultiplier", MT, "materials", "Ore", DropClassifier.Kind.Ore, "Raw ores.");
            Drp("scrap", "ScrapDropMultiplier", MT, "materials", "Scrap", DropClassifier.Kind.Scrap, "Iron/black metal/copper scrap.");
            Drp("stone", "StoneDropMultiplier", MT, "materials", "Stone", DropClassifier.Kind.Stone, "Stone and similar.");
            Drp("flint", "FlintDropMultiplier", MT, "materials", "Flint", DropClassifier.Kind.Flint, "Flint.");
            Drp("crystal", "CrystalDropMultiplier", MT, "materials", "Crystal", DropClassifier.Kind.Crystal, "Crystal, obsidian.");
            Drp("fuel", "FuelDropMultiplier", MT, "materials", "Fuel", DropClassifier.Kind.Fuel, "Coal, resin, tar.");
            Drp("gems", "GemDropMultiplier", MT, "materials", "Gems", DropClassifier.Kind.Gems, "Coins, amber, rubies, necklace.");
            Drp("boss", "BossDropMultiplier", MT, "materials", "Boss Mats", DropClassifier.Kind.BossMats, "Boss materials and boss trophies.");

            // Consumables
            Drp("crops", "CropDropMultiplier", CS, "consumables", "Crops", DropClassifier.Kind.Crops, "Crops / plants that spawn as ground items.");
            Drp("seeds", "SeedDropMultiplier", CS, "consumables", "Seeds", DropClassifier.Kind.Seeds, "Seeds and cones.");
            Drp("mushrooms", "MushroomDropMultiplier", CS, "consumables", "Mushrooms", DropClassifier.Kind.Mushrooms, "Mushrooms / magecap.");
            Drp("berries", "BerryHoneyDropMultiplier", CS, "consumables", "Berries", DropClassifier.Kind.BerriesHoney, "Berries, honey.");
            Drp("fish", "FishDropMultiplier", CS, "consumables", "Fish", DropClassifier.Kind.Fish, "Fish and similar.");
            Drp("potions", "PotionDropMultiplier", CS, "consumables", "Potions", DropClassifier.Kind.Potions, "Meads / potions on the ground.");

            // Mob drops
            Drp("hide", "HideDropMultiplier", MB, "mobs", "Hide", DropClassifier.Kind.Hide, "Hides, leather, pelts, scales.");
            Drp("trophy", "TrophyDropMultiplier", MB, "mobs", "Trophy", DropClassifier.Kind.Trophy, "Normal mob trophies.");
            Drp("meat", "MeatDropMultiplier", MB, "mobs", "Meat", DropClassifier.Kind.Meat, "Raw / cooked meat.");
            Drp("parts", "PartsDropMultiplier", MB, "mobs", "Parts", DropClassifier.Kind.Parts, "Bones, entrails, eyes, glands, fangs.");
            Drp("feathers", "FeatherDropMultiplier", MB, "mobs", "Feathers", DropClassifier.Kind.Feathers, "Feathers.");
            Drp("specialparts", "SpecialPartsDropMultiplier", MB, "mobs", "Special Parts", DropClassifier.Kind.SpecialParts,
                "Guck, ooze, soft tissue, carapace, bile, ...");

            // Death
            Tg("keepequip", "DeathKeepEquip", DT, "death", "Keep Equipment", GlobalKeys.DeathKeepEquip, "Keep equipped gear on death.");
            Tg("keepinv", "DeathKeepInventory", DT, "death", "Keep Inventory", GlobalKeys.DeathKeepInventory, "Keep full inventory on death.");
            Tg("deleteitems", "DeathDeleteItems", DT, "death", "Delete Items", GlobalKeys.DeathDeleteItems, "Delete items on death.");
            Tg("deleteunequipped", "DeathDeleteUnequipped", DT, "death", "Delete Unequipped", GlobalKeys.DeathDeleteUnequipped,
                "Delete unequipped items on death.");
            Tg("skillreset", "DeathSkillsReset", DT, "death", "Skill Reset", GlobalKeys.DeathSkillsReset, "Reset skills on death.");

            // Build / craft
            Tg("nobuildcost", "NoBuildCost", BC, "build", "Free Building", GlobalKeys.NoBuildCost, "Building costs nothing.");
            Tg("nocraftcost", "NoCraftCost", BC, "build", "Free Crafting", GlobalKeys.NoCraftCost, "Crafting costs nothing.");
            Tg("allpieces", "AllPiecesUnlocked", BC, "build", "All Pieces", GlobalKeys.AllPiecesUnlocked, "All build pieces unlocked.");
            Tg("allrecipes", "AllRecipesUnlocked", BC, "build", "All Recipes", GlobalKeys.AllRecipesUnlocked, "All recipes unlocked.");
            Tg("noworkbench", "NoWorkbench", BC, "build", "No Workbench", GlobalKeys.NoWorkbench, "No workbench range required.");
            Tg("toollocks", "WorldLevelLockedTools", BC, "build", "Tool Locks", GlobalKeys.WorldLevelLockedTools, "World-level tool locks.");

            // Map / portals
            Tg("nomap", "NoMap", MP, "map", "No Map", GlobalKeys.NoMap, "Disable map.");
            Tg("noportals", "NoPortals", MP, "map", "No Portals", GlobalKeys.NoPortals, "Disable portals.");
            Tg("nobossportals", "NoBossPortals", MP, "map", "No Boss Portals", GlobalKeys.NoBossPortals, "Disable boss portals.");
            Tg("teleportall", "TeleportAll", MP, "map", "Teleport All", GlobalKeys.TeleportAll, "Teleport with ores/metals.");
            Tg("dungeonbuild", "DungeonBuild", MP, "map", "Dungeon Build", GlobalKeys.DungeonBuild, "Allow building in dungeons.");

            // World flags
            Tg("nopseudo", "NoPseudoDrops", WF, "world", "No Pseudo Drops", GlobalKeys.NoPseudoDrops, "Disable pseudo drops.");
            Tg("nobuildfall", "NoBuildingFall", WF, "world", "No Building Fall", GlobalKeys.NoBuildingFall, "Buildings do not fall.");
            Tg("noheavysnow", "NoHeavySnow", WF, "world", "No Heavy Snow", GlobalKeys.NoHeavySnow, "No heavy snow.");
            Tg("allheavysnow", "AllHeavySnow", WF, "world", "All Heavy Snow", GlobalKeys.AllHeavySnow, "Force heavy snow rules.");
            Tg("fire", "Fire", WF, "world", "Fire", GlobalKeys.Fire, "Fire-related world flag.");

            // Stations (higher = faster). Edited server-side in the stations' data, nothing needed on clients.
            Smelter = Mul("smelter", "SmelterSpeedMultiplier", ST, "stations", "Smelter", "Smelter / kiln / furnace speed. 2 = twice as fast. Values below 1 are ignored.");
            Cooking = Mul("cooking", "CookingSpeedMultiplier", ST, "stations", "Cooking", "Cooking station speed. 2 = twice as fast. Values below 1 are ignored.");
            Fermenter =Mul("fermenter", "FermenterSpeedMultiplier", ST, "stations", "Fermenter", "Fermenter speed. 2 = twice as fast, 0.5 = half as fast. Applies to batches started after the change.");
            Plants = Mul("plants", "PlantGrowSpeedMultiplier", ST, "stations", "Plant Grow", "Plant grow speed. 2 = twice as fast, 0.5 = half as fast. Applies to plants planted after the change.");

            foreach (Rate r in All)
                r.Bind(file);

            file.Save();
            _applied = Snapshot();
        }

        private static Rate Add(Rate r)
        {
            All.Add(r);
            ByShort[r.Short] = r;
            ByKey[r.Key] = r;
            if (r.Drop.HasValue)
                ByDrop[r.Drop.Value] = r;
            return r;
        }

        private static Rate Pct(string sh, string key, string sec, string grp, string label, GlobalKeys gk, string desc)
        {
            return Add(new Rate
            {
                Short = sh, Key = key, Section = sec, Group = grp, Label = label, Description = desc,
                Kind = RateKind.Percent, Min = 0, Max = 10000, DefaultValue = 100, GlobalKey = gk
            });
        }

        private static Rate Mul(string sh, string key, string sec, string grp, string label, string desc)
        {
            return Add(new Rate
            {
                Short = sh, Key = key, Section = sec, Group = grp, Label = label, Description = desc,
                Kind = RateKind.Multiplier, Min = 0, Max = 100, DefaultValue = 1
            });
        }

        private static Rate Drp(string sh, string key, string sec, string grp, string label, DropClassifier.Kind kind, string desc)
        {
            return Add(new Rate
            {
                Short = sh, Key = key, Section = sec, Group = grp, Label = label, Description = desc,
                Kind = RateKind.Multiplier, Min = 0, Max = 100, DefaultValue = 1, Drop = kind
            });
        }

        private static Rate Tg(string sh, string key, string sec, string grp, string label, GlobalKeys gk, string desc)
        {
            return Add(new Rate
            {
                Short = sh, Key = key, Section = sec, Group = grp, Label = label, Description = desc,
                Kind = RateKind.Toggle, GlobalKey = gk
            });
        }

        private static Rate Sw(string sh, string key, string sec, string grp, string label, bool def, string desc)
        {
            return Add(new Rate
            {
                Short = sh, Key = key, Section = sec, Group = grp, Label = label, Description = desc,
                Kind = RateKind.Switch, DefaultFlag = def
            });
        }

        private static Rate Ch(string sh, string key, string sec, string grp, string label, string def, string[] options, string desc)
        {
            return Add(new Rate
            {
                Short = sh, Key = key, Section = sec, Group = grp, Label = label, Description = desc,
                Kind = RateKind.Choice, DefaultText = def, Options = options
            });
        }
    }
}
