using System.Globalization;
using System.IO;
using BepInEx.Configuration;

namespace ServerRates
{
    public class ModConfig
    {
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<bool> ChatCommandsEnabled { get; }
        public ConfigEntry<string> ChatCommandPrefix { get; }

        // Skills
        public ConfigEntry<float> SkillGainPercent { get; }
        public ConfigEntry<float> SkillReductionPercent { get; }

        // Combat / world
        public ConfigEntry<float> PlayerDamagePercent { get; }
        public ConfigEntry<float> EnemyDamagePercent { get; }
        public ConfigEntry<float> EnemySpeedSizePercent { get; }
        public ConfigEntry<float> EnemyLevelUpPercent { get; }
        public ConfigEntry<float> EventRatePercent { get; }
        public ConfigEntry<int> WorldLevel { get; }
        public ConfigEntry<string> PassiveMobs { get; }
        public ConfigEntry<string> PlayerEvents { get; }

        // Survival
        public ConfigEntry<float> StaminaPercent { get; }
        public ConfigEntry<float> MoveStaminaPercent { get; }
        public ConfigEntry<float> StaminaRegenPercent { get; }
        public ConfigEntry<float> EitrPercent { get; }
        public ConfigEntry<float> AdrenalinePercent { get; }
        public ConfigEntry<float> FoodPercent { get; }
        public ConfigEntry<float> DurabilityPercent { get; }
        public ConfigEntry<float> CarryWeightPercent { get; }

        // Global resources
        public ConfigEntry<float> ResourceRatePercent { get; }

        // Loot amp master
        public ConfigEntry<bool> CategoryGroundAmp { get; }
        public ConfigEntry<bool> SnapExtraDropsToGround { get; }
        public ConfigEntry<float> ExtraDropScatterForce { get; }
        public ConfigEntry<float> OtherDropMultiplier { get; }

        // 6a Materials
        public ConfigEntry<float> WoodDropMultiplier { get; }
        public ConfigEntry<float> FineWoodDropMultiplier { get; }
        public ConfigEntry<float> CoreWoodDropMultiplier { get; }
        public ConfigEntry<float> SpecialWoodDropMultiplier { get; }
        public ConfigEntry<float> OreDropMultiplier { get; }
        public ConfigEntry<float> ScrapDropMultiplier { get; }
        public ConfigEntry<float> StoneDropMultiplier { get; }
        public ConfigEntry<float> FlintDropMultiplier { get; }
        public ConfigEntry<float> CrystalDropMultiplier { get; }
        public ConfigEntry<float> FuelDropMultiplier { get; }
        public ConfigEntry<float> GemDropMultiplier { get; }
        public ConfigEntry<float> BossDropMultiplier { get; }

        // 6b Consumables
        public ConfigEntry<float> CropDropMultiplier { get; }
        public ConfigEntry<float> SeedDropMultiplier { get; }
        public ConfigEntry<float> MushroomDropMultiplier { get; }
        public ConfigEntry<float> BerryHoneyDropMultiplier { get; }
        public ConfigEntry<float> FishDropMultiplier { get; }
        public ConfigEntry<float> PotionDropMultiplier { get; }

        // 6c Mob drops
        public ConfigEntry<float> HideDropMultiplier { get; }
        public ConfigEntry<float> TrophyDropMultiplier { get; }
        public ConfigEntry<float> MeatDropMultiplier { get; }
        public ConfigEntry<float> PartsDropMultiplier { get; }
        public ConfigEntry<float> FeatherDropMultiplier { get; }
        public ConfigEntry<float> SpecialPartsDropMultiplier { get; }

        // Death
        public ConfigEntry<string> DeathKeepEquip { get; }
        public ConfigEntry<string> DeathKeepInventory { get; }
        public ConfigEntry<string> DeathDeleteItems { get; }
        public ConfigEntry<string> DeathDeleteUnequipped { get; }
        public ConfigEntry<string> DeathSkillsReset { get; }

        // Build / craft
        public ConfigEntry<string> NoBuildCost { get; }
        public ConfigEntry<string> NoCraftCost { get; }
        public ConfigEntry<string> AllPiecesUnlocked { get; }
        public ConfigEntry<string> AllRecipesUnlocked { get; }
        public ConfigEntry<string> NoWorkbench { get; }
        public ConfigEntry<string> WorldLevelLockedTools { get; }

        // Map / portals
        public ConfigEntry<string> NoMap { get; }
        public ConfigEntry<string> NoPortals { get; }
        public ConfigEntry<string> NoBossPortals { get; }
        public ConfigEntry<string> TeleportAll { get; }
        public ConfigEntry<string> DungeonBuild { get; }

        // World flags
        public ConfigEntry<string> NoPseudoDrops { get; }
        public ConfigEntry<string> NoBuildingFall { get; }
        public ConfigEntry<string> NoHeavySnow { get; }
        public ConfigEntry<string> AllHeavySnow { get; }
        public ConfigEntry<string> Fire { get; }

        // Stations / plants (higher = faster)
        public ConfigEntry<float> SmelterSpeedMultiplier { get; }
        public ConfigEntry<float> FermenterSpeedMultiplier { get; }
        public ConfigEntry<float> CookingSpeedMultiplier { get; }
        public ConfigEntry<float> PlantGrowSpeedMultiplier { get; }

        private static readonly AcceptableValueList<string> ToggleValues =
            new AcceptableValueList<string>("Unchanged", "On", "Off");

        public ModConfig(ConfigFile file)
        {
            Enabled = file.Bind("1 - General", "Enabled", true,
                "Master switch. Dedicated server only; vanilla clients need no install.");
            ChatCommandsEnabled = file.Bind("1 - General", "ChatCommandsEnabled", true,
                "Allow rate commands in chat (for console players). No admin check — anyone can use. Prefix below.");
            ChatCommandPrefix = file.Bind("1 - General", "ChatCommandPrefix", "/",
                "Chat prefix for console/PC chat commands, e.g. /sr_wood 9 or /rates status");

            SkillGainPercent = Percent(file, "2 - Skills", "SkillGainPercent", 100f,
                "XP for ALL skills. 100 = vanilla, 200 = x2, 1000 = x10.");
            SkillReductionPercent = Percent(file, "2 - Skills", "SkillReductionPercent", 100f,
                "Skill loss on death. 100 = vanilla, 0 = none.");

            PlayerDamagePercent = Percent(file, "3 - Combat", "PlayerDamagePercent", 100f,
                "Damage players deal. 100 = vanilla.");
            EnemyDamagePercent = Percent(file, "3 - Combat", "EnemyDamagePercent", 100f,
                "Damage enemies deal. 100 = vanilla.");
            EnemySpeedSizePercent = Percent(file, "3 - Combat", "EnemySpeedSizePercent", 100f,
                "Enemy move speed / size scaling. 100 = vanilla.");
            EnemyLevelUpPercent = Percent(file, "3 - Combat", "EnemyLevelUpPercent", 100f,
                "How fast enemies star-up. 100 = vanilla.");
            EventRatePercent = Percent(file, "3 - Combat", "EventRatePercent", 100f,
                "Raid / event frequency. 100 = vanilla, 0 = none.");
            WorldLevel = file.Bind("3 - Combat", "WorldLevel", 0,
                new ConfigDescription("World level 0-10. 0 = vanilla start.", new AcceptableValueRange<int>(0, 10)));
            PassiveMobs = Toggle(file, "3 - Combat", "PassiveMobs",
                "On = mobs do not attack. Unchanged = leave vanilla/world setting.");
            PlayerEvents = Toggle(file, "3 - Combat", "PlayerEvents",
                "Player-based raid events.");

            StaminaPercent = Percent(file, "4 - Survival", "StaminaPercent", 100f,
                "Stamina drain. Lower = less drain. 100 = vanilla.");
            MoveStaminaPercent = Percent(file, "4 - Survival", "MoveStaminaPercent", 100f,
                "Movement stamina drain. 100 = vanilla.");
            StaminaRegenPercent = Percent(file, "4 - Survival", "StaminaRegenPercent", 100f,
                "Stamina regen. Higher = faster. 100 = vanilla.");
            EitrPercent = Percent(file, "4 - Survival", "EitrPercent", 100f, "Eitr rate. 100 = vanilla.");
            AdrenalinePercent = Percent(file, "4 - Survival", "AdrenalinePercent", 100f, "Adrenaline rate. 100 = vanilla.");
            FoodPercent = Percent(file, "4 - Survival", "FoodPercent", 100f,
                "Food duration scaling. 100 = vanilla.");
            DurabilityPercent = Percent(file, "4 - Survival", "DurabilityPercent", 100f,
                "Tool/weapon durability loss. Lower = lasts longer. 100 = vanilla.");
            CarryWeightPercent = Percent(file, "4 - Survival", "CarryWeightPercent", 100f,
                "Carry weight. 200 = x2 capacity. 100 = vanilla.");

            ResourceRatePercent = Percent(file, "5 - Resources", "ResourceRatePercent", 100f,
                "GLOBAL drop rate (vanilla World Modifier ResourceRate). 100 = vanilla, 200 = x2. Synced to all clients.");

            CategoryGroundAmp = file.Bind("6 - Loot", "CategoryGroundAmp", true,
                "Server amplifies categorized ground loot after it spawns (vanilla/PS5 clients OK). Player-thrown items are skipped.");
            SnapExtraDropsToGround = file.Bind("6 - Loot", "SnapExtraDropsToGround", true,
                "Place amplified extra piles on the ground (fixes floating server extras).");
            ExtraDropScatterForce = file.Bind("6 - Loot", "ExtraDropScatterForce", 4f,
                new ConfigDescription("Physics push on extra piles (vanilla mob loot uses ~5). 0 = no push.", new AcceptableValueRange<float>(0f, 20f)));
            OtherDropMultiplier = Mult(file, "6 - Loot", "OtherDropMultiplier", LegacyFloat(file, "OtherDropMultiplier", 1f),
                "Anything not matched below. Keep at 1 unless you want everything amplified.");

            float woodLegacy = LegacyFloat(file, "WoodDropMultiplier", 1f);
            WoodDropMultiplier = Mult(file, "6a - Materials", "WoodDropMultiplier", woodLegacy, "Basic wood.");
            FineWoodDropMultiplier = Mult(file, "6a - Materials", "FineWoodDropMultiplier", woodLegacy, "Fine wood.");
            CoreWoodDropMultiplier = Mult(file, "6a - Materials", "CoreWoodDropMultiplier", woodLegacy, "Core wood (RoundLog).");
            SpecialWoodDropMultiplier = Mult(file, "6a - Materials", "SpecialWoodDropMultiplier", woodLegacy,
                "Elder bark, Yggdrasil, Ash, Black wood.");
            float oreLegacy = LegacyFloat(file, "OreDropMultiplier", 1f);
            OreDropMultiplier = Mult(file, "6a - Materials", "OreDropMultiplier", oreLegacy, "Raw ores.");
            ScrapDropMultiplier = Mult(file, "6a - Materials", "ScrapDropMultiplier", oreLegacy, "Iron/black metal/copper scrap.");
            float stoneLegacy = LegacyFloat(file, "StoneDropMultiplier", 1f);
            StoneDropMultiplier = Mult(file, "6a - Materials", "StoneDropMultiplier", stoneLegacy, "Stone and similar.");
            FlintDropMultiplier = Mult(file, "6a - Materials", "FlintDropMultiplier", stoneLegacy, "Flint.");
            CrystalDropMultiplier = Mult(file, "6a - Materials", "CrystalDropMultiplier", stoneLegacy, "Crystal, obsidian.");
            FuelDropMultiplier = Mult(file, "6a - Materials", "FuelDropMultiplier", LegacyFloat(file, "FuelDropMultiplier", 1f),
                "Coal, resin, tar.");
            GemDropMultiplier = Mult(file, "6a - Materials", "GemDropMultiplier", LegacyFloat(file, "GemDropMultiplier", 1f),
                "Coins, amber, rubies, necklace.");
            BossDropMultiplier = Mult(file, "6a - Materials", "BossDropMultiplier", LegacyFloat(file, "BossDropMultiplier", 1f),
                "Boss materials and boss trophies.");

            float cropLegacy = LegacyFloat(file, "CropDropMultiplier", 1f);
            CropDropMultiplier = Mult(file, "6b - Consumables", "CropDropMultiplier", cropLegacy,
                "Crops / plants when they spawn as ground ItemDrops.");
            SeedDropMultiplier = Mult(file, "6b - Consumables", "SeedDropMultiplier", cropLegacy, "Seeds and cones.");
            MushroomDropMultiplier = Mult(file, "6b - Consumables", "MushroomDropMultiplier", 1f, "Mushrooms / magecap.");
            BerryHoneyDropMultiplier = Mult(file, "6b - Consumables", "BerryHoneyDropMultiplier", 1f, "Berries, honey.");
            FishDropMultiplier = Mult(file, "6b - Consumables", "FishDropMultiplier", 1f, "Fish and similar.");
            PotionDropMultiplier = Mult(file, "6b - Consumables", "PotionDropMultiplier", 1f, "Meads / potions if they land as ground loot.");

            HideDropMultiplier = Mult(file, "6c - Mob Drops", "HideDropMultiplier", LegacyFloat(file, "HideDropMultiplier", 1f),
                "Hides, leather, pelts, scales.");
            TrophyDropMultiplier = Mult(file, "6c - Mob Drops", "TrophyDropMultiplier", 1f, "Normal mob trophies.");
            MeatDropMultiplier = Mult(file, "6c - Mob Drops", "MeatDropMultiplier", 1f, "Raw / cooked meat drops.");
            PartsDropMultiplier = Mult(file, "6c - Mob Drops", "PartsDropMultiplier", LegacyFloat(file, "PartsDropMultiplier", 1f),
                "Bones, entrails, eyes, glands, fangs.");
            FeatherDropMultiplier = Mult(file, "6c - Mob Drops", "FeatherDropMultiplier", 1f, "Feathers.");
            SpecialPartsDropMultiplier = Mult(file, "6c - Mob Drops", "SpecialPartsDropMultiplier", 1f,
                "Guck, ooze, soft tissue, carapace, bile, …");

            DeathKeepEquip = Toggle(file, "7 - Death", "DeathKeepEquip", "Keep equipped gear on death.");
            DeathKeepInventory = Toggle(file, "7 - Death", "DeathKeepInventory", "Keep full inventory on death.");
            DeathDeleteItems = Toggle(file, "7 - Death", "DeathDeleteItems", "Delete items on death.");
            DeathDeleteUnequipped = Toggle(file, "7 - Death", "DeathDeleteUnequipped", "Delete unequipped items on death.");
            DeathSkillsReset = Toggle(file, "7 - Death", "DeathSkillsReset", "Reset skills on death.");

            NoBuildCost = Toggle(file, "8 - Build Craft", "NoBuildCost", "Building costs nothing.");
            NoCraftCost = Toggle(file, "8 - Build Craft", "NoCraftCost", "Crafting costs nothing.");
            AllPiecesUnlocked = Toggle(file, "8 - Build Craft", "AllPiecesUnlocked", "All build pieces unlocked.");
            AllRecipesUnlocked = Toggle(file, "8 - Build Craft", "AllRecipesUnlocked", "All recipes unlocked.");
            NoWorkbench = Toggle(file, "8 - Build Craft", "NoWorkbench", "No workbench range required.");
            WorldLevelLockedTools = Toggle(file, "8 - Build Craft", "WorldLevelLockedTools", "World-level tool locks.");

            NoMap = Toggle(file, "9 - Map Portals", "NoMap", "Disable map.");
            NoPortals = Toggle(file, "9 - Map Portals", "NoPortals", "Disable portals.");
            NoBossPortals = Toggle(file, "9 - Map Portals", "NoBossPortals", "Disable boss portals.");
            TeleportAll = Toggle(file, "9 - Map Portals", "TeleportAll", "Teleport with ores/metals.");
            DungeonBuild = Toggle(file, "9 - Map Portals", "DungeonBuild", "Allow building in dungeons.");

            NoPseudoDrops = Toggle(file, "10 - World Flags", "NoPseudoDrops", "Disable pseudo drops.");
            NoBuildingFall = Toggle(file, "10 - World Flags", "NoBuildingFall", "Buildings do not fall.");
            NoHeavySnow = Toggle(file, "10 - World Flags", "NoHeavySnow", "No heavy snow.");
            AllHeavySnow = Toggle(file, "10 - World Flags", "AllHeavySnow", "Force heavy snow rules.");
            Fire = Toggle(file, "10 - World Flags", "Fire", "Fire-related world flag.");

            SmelterSpeedMultiplier = Mult(file, "11 - Stations", "SmelterSpeedMultiplier", 1f,
                "Smelter / kiln / similar. 2 = twice as fast.");
            FermenterSpeedMultiplier = Mult(file, "11 - Stations", "FermenterSpeedMultiplier", 1f,
                "Fermenter speed. 2 = twice as fast.");
            CookingSpeedMultiplier = Mult(file, "11 - Stations", "CookingSpeedMultiplier", 1f,
                "Cooking station speed. 2 = twice as fast.");
            PlantGrowSpeedMultiplier = Mult(file, "11 - Stations", "PlantGrowSpeedMultiplier", 1f,
                "Plant grow speed. 2 = twice as fast (half grow time).");
        }

        private static ConfigEntry<float> Percent(ConfigFile file, string section, string key, float def, string desc)
        {
            return file.Bind(section, key, def, new ConfigDescription(desc, new AcceptableValueRange<float>(0f, 10000f)));
        }

        private static ConfigEntry<float> Mult(ConfigFile file, string section, string key, float def, string desc)
        {
            return file.Bind(section, key, def, new ConfigDescription(desc + " 1 = vanilla.", new AcceptableValueRange<float>(0f, 100f)));
        }

        /// <summary>Read orphaned values from the old "6 - Drop Categories" section if present.</summary>
        private static float LegacyFloat(ConfigFile file, string key, float fallback)
        {
            try
            {
                string path = file.ConfigFilePath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return fallback;

                string section = null;
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#"))
                        continue;
                    if (t.StartsWith("[") && t.EndsWith("]"))
                    {
                        section = t.Substring(1, t.Length - 2);
                        continue;
                    }
                    if (section != "6 - Drop Categories")
                        continue;
                    int eq = t.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    string k = t.Substring(0, eq).Trim();
                    if (!string.Equals(k, key, System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    string raw = t.Substring(eq + 1).Trim();
                    int hash = raw.IndexOf('#');
                    if (hash >= 0)
                        raw = raw.Substring(0, hash).Trim();
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                        return v;
                }
            }
            catch
            {
                // ignore — fall back to default
            }

            return fallback;
        }

        private static ConfigEntry<string> Toggle(ConfigFile file, string section, string key, string desc)
        {
            return file.Bind(section, key, "Unchanged",
                new ConfigDescription(desc + " Values: Unchanged | On | Off", ToggleValues));
        }
    }
}
