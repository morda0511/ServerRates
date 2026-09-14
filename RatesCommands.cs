using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;

namespace ServerRates
{
    /// <summary>
    /// Dedicated / admin console:
    ///   rates … (meta)
    ///   sr_&lt;rate&gt; [value]  — one command per config rate
    /// </summary>
    internal static class RatesCommands
    {
        private static bool _added;
        private static string[] _keyCache;

        /// <summary>short cmd (without sr_) → ModConfig property name</summary>
        private static readonly Dictionary<string, string> ShortToKey =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> KeyToShort =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static bool _mapBuilt;

        public static void EnsureMaps()
        {
            if (_mapBuilt)
                return;
            BuildShortNameMap();
            _mapBuilt = true;
        }

        public static bool IsKnownShortOrKey(string token)
        {
            EnsureMaps();
            if (string.IsNullOrEmpty(token))
                return false;
            if (ShortToKey.ContainsKey(token))
                return true;
            if (token.StartsWith("sr_", StringComparison.OrdinalIgnoreCase)
                && ShortToKey.ContainsKey(token.Substring(3)))
                return true;
            return FindProp(token) != null;
        }

        /// <summary>Parse one command line (no prefix). Used by Terminal and chat.</summary>
        public static void ProcessLine(string line, Action<string> reply)
        {
            if (reply == null)
                return;
            EnsureMaps();

            if (!Plugin.Active || ZNet.instance == null || !ZNet.instance.IsDedicated())
            {
                reply("ServerRates: commands only work on the dedicated server.");
                return;
            }

            string[] parts = SplitArgs(line);
            if (parts.Length == 0)
            {
                PrintHelp(reply);
                return;
            }

            string head = parts[0];
            string headLower = head.ToLowerInvariant();

            // sr_wood [value]  OR  wood [value]
            if (headLower.StartsWith("sr_") || ShortToKey.ContainsKey(head) || FindProp(head) != null)
            {
                string prop = ResolveKey(head);
                if (prop == null)
                {
                    reply("ServerRates: unknown '" + head + "' — try /rates cmds");
                    return;
                }

                string shortName = KeyToShort.TryGetValue(prop, out string sh) ? sh : head;
                if (parts.Length < 2)
                {
                    if (TryGet(prop, out string text, out string err))
                        reply("ServerRates: " + text + "  (set: !" + "sr_" + shortName + " <value>)");
                    else
                        reply("rates get failed: " + err);
                    return;
                }

                ApplySet(reply, prop, JoinParts(parts, 1));
                return;
            }

            if (headLower == "rates")
            {
                ProcessRatesMeta(parts, reply);
                return;
            }

            PrintHelp(reply);
        }

        private static void ProcessRatesMeta(string[] parts, Action<string> reply)
        {
            if (parts.Length < 2)
            {
                PrintHelp(reply);
                return;
            }

            string sub = parts[1].ToLowerInvariant();

            if (ShortToKey.TryGetValue(sub, out string propFromShort)
                || KeyToShort.ContainsKey(parts[1]))
            {
                string prop = ShortToKey.TryGetValue(sub, out string k) ? k : FindProp(parts[1])?.Name;
                if (prop == null)
                {
                    PrintHelp(reply);
                    return;
                }

                if (parts.Length < 3)
                {
                    if (TryGet(prop, out string text, out string err))
                        reply("ServerRates: " + text);
                    else
                        reply("rates get failed: " + err);
                    return;
                }

                ApplySet(reply, prop, JoinParts(parts, 2));
                return;
            }

            if (sub == "help" || sub == "?")
            {
                PrintHelp(reply);
                return;
            }
            if (sub == "status")
            {
                PrintStatus(reply, false);
                return;
            }
            if (sub == "list" || sub == "keys")
            {
                PrintStatus(reply, true);
                return;
            }
            if (sub == "cmds" || sub == "commands")
            {
                PrintAllCommands(reply);
                return;
            }
            if (sub == "get")
            {
                if (parts.Length < 3)
                {
                    reply("Usage: /rates get <Key|short>");
                    return;
                }
                string prop = ResolveKey(parts[2]);
                if (prop == null)
                {
                    reply("unknown key '" + parts[2] + "' (/rates cmds)");
                    return;
                }
                if (!TryGet(prop, out string text, out string err))
                {
                    reply("rates get failed: " + err);
                    return;
                }
                reply("ServerRates: " + text);
                return;
            }
            if (sub == "set" || sub == "apply")
            {
                if (parts.Length < 4)
                {
                    reply("Usage: /rates set <Key|short> <value>");
                    return;
                }
                string prop = ResolveKey(parts[2]);
                if (prop == null)
                {
                    reply("unknown key '" + parts[2] + "' (/rates cmds)");
                    return;
                }
                ApplySet(reply, prop, JoinParts(parts, 3));
                return;
            }

            PrintHelp(reply);
        }

        private static string[] SplitArgs(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return Array.Empty<string>();
            return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string JoinParts(string[] parts, int startIndex)
        {
            if (parts == null || startIndex >= parts.Length)
                return "";
            if (startIndex == parts.Length - 1)
                return parts[startIndex];
            return string.Join(" ", parts, startIndex, parts.Length - startIndex);
        }

        public static void Register()
        {
            if (_added)
                return;
            _added = true;

            try
            {
                EnsureMaps();

                // No onlyAdmin — console / crossplay players often cannot be on adminlist.
                new Terminal.ConsoleCommand(
                    "rates",
                    "ServerRates: status | list | cmds | get <Key> | set <Key> <value> | help",
                    OnRatesConsole,
                    false, true, true, false, true, false,
                    FetchRatesTabOptions, true, true, false);

                foreach (KeyValuePair<string, string> pair in ShortToKey)
                {
                    string shortName = pair.Key;
                    string propName = pair.Value;
                    string cmd = "sr_" + shortName;

                    string keyCapture = propName;
                    string shortCapture = shortName;

                    new Terminal.ConsoleCommand(
                        cmd,
                        "ServerRates " + propName + " — sr_" + shortCapture + " [value]",
                        args => OnSingleRate(args, keyCapture, shortCapture),
                        false, true, true, false, true, false,
                        null, false, true, false);
                }

                Plugin.Log?.LogInfo("ServerRates: registered " + ShortToKey.Count
                    + " rate commands (sr_* + chat " + (Plugin.Settings?.ChatCommandPrefix?.Value ?? "/") + ").");
            }
            catch (Exception ex)
            {
                _added = false;
                Plugin.Log?.LogError("ServerRates: failed to register console commands: " + ex);
            }
        }

        private static void BuildShortNameMap()
        {
            ShortToKey.Clear();
            KeyToShort.Clear();

            // Preferred short names (unique, easy to type)
            AddShort("Enabled", "enabled");
            AddShort("SkillGainPercent", "skillgain");
            AddShort("SkillReductionPercent", "skillloss");
            AddShort("PlayerDamagePercent", "playerdmg");
            AddShort("EnemyDamagePercent", "enemydmg");
            AddShort("EnemySpeedSizePercent", "enemyspeed");
            AddShort("EnemyLevelUpPercent", "enemystars");
            AddShort("EventRatePercent", "events");
            AddShort("WorldLevel", "worldlevel");
            AddShort("PassiveMobs", "passivemobs");
            AddShort("PlayerEvents", "playerevents");
            AddShort("StaminaPercent", "stamina");
            AddShort("MoveStaminaPercent", "movestamina");
            AddShort("StaminaRegenPercent", "staminaregen");
            AddShort("EitrPercent", "eitr");
            AddShort("AdrenalinePercent", "adrenaline");
            AddShort("FoodPercent", "food");
            AddShort("DurabilityPercent", "durability");
            AddShort("CarryWeightPercent", "carry");
            AddShort("ResourceRatePercent", "resourcerate");
            AddShort("CategoryGroundAmp", "groundamp");
            AddShort("SnapExtraDropsToGround", "snapground");
            AddShort("ExtraDropScatterForce", "scatter");
            AddShort("OtherDropMultiplier", "other");
            AddShort("WoodDropMultiplier", "wood");
            AddShort("FineWoodDropMultiplier", "finewood");
            AddShort("CoreWoodDropMultiplier", "corewood");
            AddShort("SpecialWoodDropMultiplier", "specialwood");
            AddShort("OreDropMultiplier", "ore");
            AddShort("ScrapDropMultiplier", "scrap");
            AddShort("StoneDropMultiplier", "stone");
            AddShort("FlintDropMultiplier", "flint");
            AddShort("CrystalDropMultiplier", "crystal");
            AddShort("FuelDropMultiplier", "fuel");
            AddShort("GemDropMultiplier", "gems");
            AddShort("BossDropMultiplier", "boss");
            AddShort("CropDropMultiplier", "crops");
            AddShort("SeedDropMultiplier", "seeds");
            AddShort("MushroomDropMultiplier", "mushrooms");
            AddShort("BerryHoneyDropMultiplier", "berries");
            AddShort("FishDropMultiplier", "fish");
            AddShort("PotionDropMultiplier", "potions");
            AddShort("HideDropMultiplier", "hide");
            AddShort("TrophyDropMultiplier", "trophy");
            AddShort("MeatDropMultiplier", "meat");
            AddShort("PartsDropMultiplier", "parts");
            AddShort("FeatherDropMultiplier", "feathers");
            AddShort("SpecialPartsDropMultiplier", "specialparts");
            AddShort("DeathKeepEquip", "keepequip");
            AddShort("DeathKeepInventory", "keepinv");
            AddShort("DeathDeleteItems", "deleteitems");
            AddShort("DeathDeleteUnequipped", "deleteunequipped");
            AddShort("DeathSkillsReset", "skillreset");
            AddShort("NoBuildCost", "nobuildcost");
            AddShort("NoCraftCost", "nocraftcost");
            AddShort("AllPiecesUnlocked", "allpieces");
            AddShort("AllRecipesUnlocked", "allrecipes");
            AddShort("NoWorkbench", "noworkbench");
            AddShort("WorldLevelLockedTools", "toollocks");
            AddShort("NoMap", "nomap");
            AddShort("NoPortals", "noportals");
            AddShort("NoBossPortals", "nobossportals");
            AddShort("TeleportAll", "teleportall");
            AddShort("DungeonBuild", "dungeonbuild");
            AddShort("NoPseudoDrops", "nopseudo");
            AddShort("NoBuildingFall", "nobuildfall");
            AddShort("NoHeavySnow", "noheavysnow");
            AddShort("AllHeavySnow", "allheavysnow");
            AddShort("Fire", "fire");
            AddShort("SmelterSpeedMultiplier", "smelter");
            AddShort("FermenterSpeedMultiplier", "fermenter");
            AddShort("CookingSpeedMultiplier", "cooking");
            AddShort("PlantGrowSpeedMultiplier", "plants");

            // Any property not listed yet → auto short name
            foreach (PropertyInfo prop in typeof(ModConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!IsConfigEntry(prop.PropertyType))
                    continue;
                if (KeyToShort.ContainsKey(prop.Name))
                    continue;
                string auto = AutoShort(prop.Name);
                AddShort(prop.Name, auto);
            }
        }

        private static void AddShort(string propName, string shortName)
        {
            if (ShortToKey.ContainsKey(shortName))
            {
                Plugin.Log?.LogWarning("ServerRates: duplicate short name '" + shortName + "' for " + propName);
                shortName = shortName + "2";
            }
            ShortToKey[shortName] = propName;
            KeyToShort[propName] = shortName;
        }

        private static string AutoShort(string propName)
        {
            string s = propName;
            if (s.EndsWith("DropMultiplier", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - "DropMultiplier".Length);
            else if (s.EndsWith("SpeedMultiplier", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - "SpeedMultiplier".Length) + "speed";
            else if (s.EndsWith("Percent", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - "Percent".Length);
            else if (s.EndsWith("Multiplier", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - "Multiplier".Length);
            return s.ToLowerInvariant();
        }

        private static List<string> FetchRatesTabOptions()
        {
            var opts = new List<string> { "status", "list", "cmds", "get", "set", "help" };
            opts.AddRange(ShortToKey.Keys);
            opts.AddRange(GetAllKeys());
            return opts;
        }

        private static string[] GetAllKeys()
        {
            if (_keyCache != null)
                return _keyCache;
            var keys = new List<string>();
            foreach (PropertyInfo prop in typeof(ModConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsConfigEntry(prop.PropertyType))
                    keys.Add(prop.Name);
            }
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            _keyCache = keys.ToArray();
            return _keyCache;
        }

        private static bool IsConfigEntry(Type t)
        {
            return t != null && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ConfigEntry<>);
        }

        private static void OnSingleRate(Terminal.ConsoleEventArgs args, string propName, string shortName)
        {
            if (args?.Context == null)
                return;

            Action<string> reply = s => RatesFeedback.Say(s, false);
            if (args.Length < 2)
                ProcessLine("sr_" + shortName, reply);
            else
                ProcessLine("sr_" + shortName + " " + JoinArgsFrom(args, 1), reply);
        }

        private static void OnRatesConsole(Terminal.ConsoleEventArgs args)
        {
            if (args?.Context == null)
                return;

            Action<string> reply = s => RatesFeedback.Say(s, false);
            if (args.Length < 2)
            {
                ProcessLine("rates", reply);
                return;
            }

            ProcessLine("rates " + JoinArgsFrom(args, 1), reply);
        }

        private static string ResolveKey(string raw)
        {
            if (ShortToKey.TryGetValue(raw, out string fromShort))
                return fromShort;
            if (raw.StartsWith("sr_", StringComparison.OrdinalIgnoreCase)
                && ShortToKey.TryGetValue(raw.Substring(3), out string fromSr))
                return fromSr;
            PropertyInfo prop = FindProp(raw);
            return prop?.Name;
        }

        private static string JoinArgsFrom(Terminal.ConsoleEventArgs args, int startIndex)
        {
            if (args.Args == null || startIndex >= args.Args.Length)
                return args.Length > startIndex ? args[startIndex] : "";
            if (startIndex == args.Args.Length - 1)
                return args.Args[startIndex];
            return string.Join(" ", args.Args, startIndex, args.Args.Length - startIndex);
        }

        private static void ApplySet(Action<string> reply, string propName, string value)
        {
            if (!TrySet(propName, value, out string err, out string applied))
            {
                reply("rates set failed: " + err);
                return;
            }

            ConfigWatch.SuppressReloadBriefly(2f);
            if (Plugin.Instance != null)
                Plugin.Instance.Config.Save();
            RateApplier.NotifyConfigReloaded();
            RateApplier.ApplyGlobalKeys();
            CategoryGroundAmp.LogStatus();

            string shortName = KeyToShort.TryGetValue(propName, out string s) ? s : propName;
            string hud = FormatHudMessage(propName, applied);
            RatesFeedback.Say(hud, true);
            Plugin.Log?.LogInfo("ServerRates command live-applied: " + propName + "=" + applied
                + " (sr_" + shortName + ")");
        }

        private static string FormatHudMessage(string propName, string applied)
        {
            string label = FriendlyHudLabel(propName);
            if (propName.EndsWith("Percent", StringComparison.Ordinal))
                return label + " " + applied + "%";
            if (propName.EndsWith("Multiplier", StringComparison.Ordinal)
                || propName.Equals("ExtraDropScatterForce", StringComparison.OrdinalIgnoreCase)
                || propName.Equals("WorldLevel", StringComparison.OrdinalIgnoreCase))
                return label + " Multiplier x" + applied;
            if (propName.EndsWith("Enabled", StringComparison.Ordinal)
                || propName.Contains("Amp")
                || propName.Contains("Snap"))
                return label + ": " + applied;
            // toggles Unchanged/On/Off
            return label + ": " + applied;
        }

        private static string FriendlyHudLabel(string propName)
        {
            if (KeyToShort.TryGetValue(propName, out string sh))
            {
                switch (sh)
                {
                    case "wood": return "Wood";
                    case "finewood": return "Fine Wood";
                    case "corewood": return "Core Wood";
                    case "specialwood": return "Special Wood";
                    case "ore": return "Ore";
                    case "scrap": return "Scrap";
                    case "stone": return "Stone";
                    case "flint": return "Flint";
                    case "crystal": return "Crystal";
                    case "fuel": return "Fuel";
                    case "gems": return "Gems";
                    case "boss": return "Boss Mats";
                    case "crops": return "Crops";
                    case "seeds": return "Seeds";
                    case "mushrooms": return "Mushrooms";
                    case "berries": return "Berries";
                    case "fish": return "Fish";
                    case "potions": return "Potions";
                    case "hide": return "Hide";
                    case "trophy": return "Trophy";
                    case "meat": return "Meat";
                    case "parts": return "Parts";
                    case "feathers": return "Feathers";
                    case "specialparts": return "Special Parts";
                    case "resourcerate": return "Resource Rate";
                    case "skillgain": return "Skill Gain";
                    case "skillloss": return "Skill Loss";
                    case "playerdmg": return "Player Damage";
                    case "enemydmg": return "Enemy Damage";
                    case "groundamp": return "Ground Amp";
                    case "teleportall": return "Teleport All";
                    case "smelter": return "Smelter";
                    case "fermenter": return "Fermenter";
                    case "cooking": return "Cooking";
                    case "plants": return "Plant Grow";
                    case "other": return "Other Loot";
                    case "scatter": return "Drop Scatter";
                }
            }

            string n = propName;
            if (n.EndsWith("DropMultiplier")) n = n.Substring(0, n.Length - "DropMultiplier".Length);
            else if (n.EndsWith("SpeedMultiplier")) n = n.Substring(0, n.Length - "SpeedMultiplier".Length);
            else if (n.EndsWith("Percent")) n = n.Substring(0, n.Length - "Percent".Length);
            else if (n.EndsWith("Multiplier")) n = n.Substring(0, n.Length - "Multiplier".Length);
            return System.Text.RegularExpressions.Regex.Replace(n, "([a-z])([A-Z])", "$1 $2");
        }

        /// <summary>Vanilla yellow center HUD — works for all clients, no client mod.</summary>
        private static void ShowYellowHud(string text)
        {
            RatesFeedback.ShowYellowHud(text);
        }

        private static void PrintHelp(Action<string> reply)
        {
            string p = Plugin.Settings?.ChatCommandPrefix?.Value ?? "/";
            var sb = new StringBuilder();
            sb.AppendLine("ServerRates — chat or F5 (no admin needed):");
            sb.AppendLine("  " + p + "sr_wood 9   " + p + "sr_meat 5   " + p + "sr_teleportall On");
            sb.AppendLine("  " + p + "sr_resourcerate 200");
            sb.AppendLine("  " + p + "sr_wood            (no value = show current)");
            sb.AppendLine("  rates cmds | status | list | help");
            sb.AppendLine("  rates set wood 9");
            sb.AppendLine("Percent: 100=vanilla | Mult: 1=vanilla | Toggle: Unchanged|On|Off");
            sb.Append("Applies LIVE.");
            reply(sb.ToString());
        }

        private static void PrintAllCommands(Action<string> reply)
        {
            string p = Plugin.Settings?.ChatCommandPrefix?.Value ?? "/";
            var lines = new List<string>();
            foreach (KeyValuePair<string, string> pair in ShortToKey)
                lines.Add(p + "sr_" + pair.Key + "  ->  " + pair.Value);
            lines.Sort(StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.AppendLine("ServerRates commands (" + lines.Count + "):");
            foreach (string line in lines)
                sb.AppendLine(line);
            reply(sb.ToString().TrimEnd());
        }

        private static void PrintStatus(Action<string> reply, bool all)
        {
            if (Plugin.Settings == null)
            {
                reply("ServerRates: settings not ready");
                return;
            }

            ModConfig c = Plugin.Settings;
            var sb = new StringBuilder();
            sb.AppendLine("ServerRates Active=" + Plugin.Active + " Enabled=" + c.Enabled.Value);

            if (!all)
            {
                sb.AppendLine("ResourceRatePercent=" + c.ResourceRatePercent.Value + " (sr_resourcerate)");
                sb.AppendLine("WoodDropMultiplier=" + c.WoodDropMultiplier.Value + " (sr_wood)");
                sb.AppendLine("MeatDropMultiplier=" + c.MeatDropMultiplier.Value + " (sr_meat)");
                sb.AppendLine("HideDropMultiplier=" + c.HideDropMultiplier.Value + " (sr_hide)");
                sb.AppendLine("OreDropMultiplier=" + c.OreDropMultiplier.Value + " (sr_ore)");
                sb.AppendLine("CategoryGroundAmp=" + c.CategoryGroundAmp.Value + " (sr_groundamp)");
                sb.Append("rates list / rates cmds for more");
                reply(sb.ToString());
                return;
            }

            foreach (string key in GetAllKeys())
            {
                if (!TryGet(key, out string text, out _))
                    continue;
                string suffix = KeyToShort.TryGetValue(key, out string sh) ? "  [sr_" + sh + "]" : "";
                sb.AppendLine(text + suffix);
            }
            reply(sb.ToString().TrimEnd());
        }

        private static bool TryGet(string key, out string text, out string error)
        {
            text = null;
            error = null;
            if (Plugin.Settings == null)
            {
                error = "settings not ready";
                return false;
            }

            PropertyInfo prop = FindProp(key);
            if (prop == null)
            {
                error = "unknown key '" + key + "'";
                return false;
            }

            object entry = prop.GetValue(Plugin.Settings);
            PropertyInfo valueProp = entry?.GetType().GetProperty("Value");
            if (valueProp == null)
            {
                error = "no Value";
                return false;
            }

            text = prop.Name + "=" + FormatValue(valueProp.GetValue(entry, null));
            return true;
        }

        private static string FormatValue(object val)
        {
            if (val == null) return "";
            if (val is float f) return f.ToString("0.###", CultureInfo.InvariantCulture);
            if (val is bool b) return b ? "true" : "false";
            return Convert.ToString(val, CultureInfo.InvariantCulture) ?? "";
        }

        private static PropertyInfo FindProp(string key)
        {
            return typeof(ModConfig).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }

        private static bool TrySet(string key, string raw, out string error, out string applied)
        {
            error = null;
            applied = null;
            if (Plugin.Settings == null)
            {
                error = "settings not ready";
                return false;
            }

            PropertyInfo prop = FindProp(key);
            if (prop == null)
            {
                error = "unknown key '" + key + "'";
                return false;
            }

            object entry = prop.GetValue(Plugin.Settings);
            if (entry == null)
            {
                error = "null entry";
                return false;
            }

            Type entryType = entry.GetType();
            if (!IsConfigEntry(entryType))
            {
                error = "not a ConfigEntry";
                return false;
            }

            Type valueType = entryType.GetGenericArguments()[0];
            PropertyInfo valueProp = entryType.GetProperty("Value");
            if (valueProp == null)
            {
                error = "no Value";
                return false;
            }

            try
            {
                object parsed = ParseValue(valueType, raw, out error);
                if (parsed == null && error != null)
                    return false;
                valueProp.SetValue(entry, parsed, null);
                applied = FormatValue(parsed);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static object ParseValue(Type valueType, string raw, out string error)
        {
            error = null;
            string s = (raw ?? "").Trim();

            if (valueType == typeof(bool))
            {
                if (bool.TryParse(s, out bool b)) return b;
                if (s == "1" || s.Equals("on", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (s == "0" || s.Equals("off", StringComparison.OrdinalIgnoreCase) || s.Equals("no", StringComparison.OrdinalIgnoreCase))
                    return false;
                error = "bool expected (true/false/on/off)";
                return null;
            }
            if (valueType == typeof(int))
            {
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return i;
                error = "int expected";
                return null;
            }
            if (valueType == typeof(float))
            {
                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
                error = "number expected";
                return null;
            }
            if (valueType == typeof(string))
            {
                if (s.Equals("on", StringComparison.OrdinalIgnoreCase)) return "On";
                if (s.Equals("off", StringComparison.OrdinalIgnoreCase)) return "Off";
                if (s.Equals("unchanged", StringComparison.OrdinalIgnoreCase)) return "Unchanged";
                return s;
            }
            error = "unsupported type " + valueType.Name;
            return null;
        }
    }

    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class RatesTerminalPatch
    {
        private static void Postfix()
        {
            RatesCommands.Register();
        }
    }
}
