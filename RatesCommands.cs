using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace ServerRates
{
    /// <summary>Who typed the command and how to answer / what they may do.</summary>
    internal sealed class CommandContext
    {
        public string Who = "console";
        public bool CanWrite = true;
        public Action<string> Reply;
        public Vector3? Position; // where the sender stands (chat only)
    }

    /// <summary>
    /// One parser for chat (console + PC), F5 and the server console.
    ///   sr_wood 9          set       sr_wood           show
    ///   sr_loot 5          set every loot category (loot / materials / consumables / mobs)
    ///   rates status | list [group] | cmds | reset [name|all] | preset x5 | get/set name [value]
    /// Names may be abbreviated (sr_spec = specialwood is ambiguous, sr_fine = finewood).
    /// </summary>
    internal static class RatesCommands
    {
        private static readonly Dictionary<string, string> GroupAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "mob", "mobs" }, { "mobdrops", "mobs" }, { "drops", "loot" }, { "mats", "materials" },
            { "station", "stations" }, { "resource", "resources" }, { "res", "resources" }
        };

        private static bool _registered;

        // ---------------------------------------------------------------- entry point

        public static void Process(string line, CommandContext ctx)
        {
            if (!Plugin.Active || ZNet.instance == null || !ZNet.instance.IsDedicated())
            {
                ctx.Reply("ServerRates: commands only work on the dedicated server.");
                return;
            }

            string[] t = Tokens(line);
            if (t.Length == 0)
            {
                Help(ctx);
                return;
            }

            string head = t[0].ToLowerInvariant();

            if (head.StartsWith("sr_"))
            {
                Target(head.Substring(3), t, 1, ctx);
                return;
            }

            if (head != "rates" && head != "sr")
            {
                Help(ctx);
                return;
            }

            if (t.Length == 1)
            {
                Help(ctx);
                return;
            }

            string sub = t[1].ToLowerInvariant();
            switch (sub)
            {
                case "help":
                case "?":
                    Help(ctx);
                    return;
                case "status":
                    Status(ctx);
                    return;
                case "list":
                case "keys":
                    ListRates(ctx, t.Length > 2 ? t[2] : null);
                    return;
                case "cmds":
                case "commands":
                    Commands(ctx);
                    return;
                case "get":
                    if (t.Length < 3) ctx.Reply("Usage: rates get <name>");
                    else Target(t[2].ToLowerInvariant(), t, 3, ctx, true);
                    return;
                case "set":
                case "apply":
                    if (t.Length < 4) ctx.Reply("Usage: rates set <name> <value>");
                    else Target(t[2].ToLowerInvariant(), t, 3, ctx);
                    return;
                case "reset":
                    Reset(ctx, t.Length > 2 ? t[2].ToLowerInvariant() : null);
                    return;
                case "inspect":
                    StationBoost.Inspect(ctx, false);
                    return;
                case "repair":
                    if (!ctx.CanWrite) ctx.Reply("Only server admins can do this (ChatAccess=AdminOnly).");
                    else StationBoost.Inspect(ctx, true);
                    return;
                case "preset":
                    Preset(ctx, t.Length > 2 ? t[2].ToLowerInvariant() : null);
                    return;
                default:
                    Target(sub, t, 2, ctx);
                    return;
            }
        }

        private static string[] Tokens(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return new string[0];
            return line.Replace('=', ' ').Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Join(string[] t, int from)
        {
            return from >= t.Length ? "" : string.Join(" ", t, from, t.Length - from);
        }

        // ---------------------------------------------------------------- name resolving

        /// <summary>Resolves a name to one rate or a group of rates. Replies and returns false when it cannot.</summary>
        private static bool Resolve(string name, CommandContext ctx, out List<Rate> rates, out string group)
        {
            rates = null;
            group = null;
            name = name.ToLowerInvariant();
            if (name.StartsWith("sr_"))
                name = name.Substring(3);

            Rate exact = Rates.Find(name);
            if (exact != null)
            {
                rates = new List<Rate> { exact };
                return true;
            }

            string g = name;
            string alias;
            if (GroupAliases.TryGetValue(name, out alias))
                g = alias;
            if (Rates.Groups.ContainsKey(g))
            {
                group = g.ToLowerInvariant();
                rates = Rates.InGroup(group);
                return true;
            }

            var starts = new List<Rate>();
            var contains = new List<Rate>();
            foreach (Rate r in Rates.All)
            {
                if (r.Short.StartsWith(name, StringComparison.OrdinalIgnoreCase)) starts.Add(r);
                else if (r.Short.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) contains.Add(r);
            }

            List<Rate> found = starts.Count > 0 ? starts : contains;
            if (found.Count == 1)
            {
                rates = found;
                return true;
            }

            if (found.Count > 1)
            {
                var names = new List<string>();
                for (int i = 0; i < found.Count && i < 8; i++)
                    names.Add(found[i].Short);
                ctx.Reply("'" + name + "' could be: " + string.Join(", ", names.ToArray()));
                return false;
            }

            ctx.Reply("Unknown '" + name + "'. Try: rates cmds");
            return false;
        }

        // ---------------------------------------------------------------- get / set

        private static void Target(string name, string[] t, int valueFrom, CommandContext ctx, bool readOnly = false)
        {
            List<Rate> rates;
            string group;
            if (!Resolve(name, ctx, out rates, out group))
                return;

            string value = Join(t, valueFrom);
            if (readOnly || value.Length == 0)
            {
                Show(ctx, rates, group);
                return;
            }

            if (!ctx.CanWrite)
            {
                ctx.Reply("Only server admins can change rates here (ChatAccess=AdminOnly).");
                return;
            }

            string error = null;
            int ok = 0;
            foreach (Rate r in rates)
            {
                string err;
                if (r.TrySet(value, out err))
                    ok++;
                else if (error == null)
                    error = err;
            }

            if (ok == 0)
            {
                ctx.Reply(error ?? "Nothing changed.");
                return;
            }

            Commit(ctx, group != null ? Cap(group) : null);
            if (error != null && rates.Count > 1)
                ctx.Reply("Some rates were skipped: " + error);
        }

        private static void Show(CommandContext ctx, List<Rate> rates, string group)
        {
            if (rates.Count == 1)
            {
                Rate r = rates[0];
                ctx.Reply(r.Label + ": " + r.Display() + "  (sr_" + r.Short + " <" + r.RangeHint() + ">)");
                return;
            }

            Reply(ctx, Cap(group ?? "rates"), rates);
        }

        /// <summary>Save, apply to the world and tell everybody what changed.</summary>
        private static void Commit(CommandContext ctx, string label)
        {
            Rates.Save();
            ConfigWatch.NotifySaved();

            List<Rate> changed = Rates.TakeChanges();
            RateApplier.ApplyGlobalKeys();
            CategoryGroundAmp.LogStatus();

            if (changed.Count == 0)
            {
                ctx.Reply("Already set, nothing changed.");
                return;
            }

            RatesFeedback.AnnounceChanges(changed, ctx.Who, label);
        }

        // ---------------------------------------------------------------- reset / preset

        private static void Reset(CommandContext ctx, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                ctx.Reply("Usage: rates reset <name|group|all>");
                return;
            }

            if (!ctx.CanWrite)
            {
                ctx.Reply("Only server admins can change rates here (ChatAccess=AdminOnly).");
                return;
            }

            if (name == "all")
            {
                ResetAll();
                Commit(ctx, "All rates");
                return;
            }

            List<Rate> rates;
            string group;
            if (!Resolve(name, ctx, out rates, out group))
                return;

            foreach (Rate r in rates)
                r.Reset();
            Commit(ctx, group != null ? Cap(group) : null);
        }

        private static void ResetAll()
        {
            foreach (Rate r in Rates.All)
                if (r.Group != "general")
                    r.Reset();
        }

        private static void Preset(CommandContext ctx, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                ctx.Reply("Usage: rates preset vanilla | x2 | x3 | x5 | x10 (any number 1-100)");
                return;
            }

            if (!ctx.CanWrite)
            {
                ctx.Reply("Only server admins can change rates here (ChatAccess=AdminOnly).");
                return;
            }

            if (name == "vanilla" || name == "default" || name == "off")
            {
                ResetAll();
                Commit(ctx, "Vanilla");
                return;
            }

            string num = name.TrimStart('x');
            float n;
            if (!float.TryParse(num, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out n) || n < 1f || n > 100f)
            {
                ctx.Reply("Unknown preset '" + name + "'. Try vanilla, x2, x5, x10");
                return;
            }

            string err;
            foreach (Rate r in Rates.InGroup("loot"))
                r.TrySet(num, out err);
            Rates.Find("skillgain").TrySet((n * 100f).ToString(System.Globalization.CultureInfo.InvariantCulture), out err);
            foreach (Rate r in Rates.InGroup("stations"))
                r.TrySet(num, out err);

            Commit(ctx, "Preset x" + Rate.Fmt(n));
        }

        // ---------------------------------------------------------------- info output

        private static void Help(CommandContext ctx)
        {
            ctx.Reply("ServerRates (type without a slash):");
            ctx.Reply("sr_wood 5 = set | sr_wood = show | sr_loot 5 = all loot");
            ctx.Reply("rates status | rates list loot | rates cmds");
            ctx.Reply("rates preset x5 | rates reset all | rates reset wood");
            ctx.Reply("rates inspect | rates repair  (station next to you)");
        }

        private static void Status(CommandContext ctx)
        {
            ctx.Reply("ServerRates v" + Plugin.ModVersion + " " + Rates.Enabled.Display()
                + " | chat: " + (Rates.ChatEnabled.Flag ? Rates.ChatAccess.Display() : "off"));

            var changed = new List<Rate>();
            foreach (Rate r in Rates.All)
                if (r.Group != "general" && !r.IsDefault())
                    changed.Add(r);

            if (changed.Count == 0)
                ctx.Reply("All rates are vanilla.");
            else
                Reply(ctx, "Changed", changed);
        }

        private static void ListRates(CommandContext ctx, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                ctx.Reply("Groups: " + string.Join(" ", GroupNames()));
                ctx.Reply("Example: rates list loot");
                return;
            }

            List<Rate> rates;
            string group;
            if (!Resolve(name, ctx, out rates, out group))
                return;
            Show(ctx, rates, group);
        }

        private static void Commands(CommandContext ctx)
        {
            ctx.Reply("Groups (set all at once): " + string.Join(" ", GroupNames()));
            foreach (string g in GroupNames())
            {
                if (g == "loot")
                    continue;
                var names = new List<string>();
                foreach (Rate r in Rates.InGroup(g))
                    names.Add(r.Short);
                Pack(ctx, g, names);
            }
        }

        private static List<string> GroupNames()
        {
            var list = new List<string>();
            foreach (string g in Rates.Groups.Keys)
                list.Add(g);
            return list;
        }

        /// <summary>Compact "name value" lines, short enough for a console chat box.</summary>
        private static void Reply(CommandContext ctx, string title, List<Rate> rates)
        {
            var items = new List<string>();
            foreach (Rate r in rates)
                items.Add(r.Short + " " + r.Display());
            Pack(ctx, title, items);
        }

        private static void Pack(CommandContext ctx, string title, List<string> items)
        {
            const int max = 90;
            var sb = new StringBuilder(title + ":");
            foreach (string item in items)
            {
                if (sb.Length + item.Length + 3 > max)
                {
                    ctx.Reply(sb.ToString());
                    sb.Clear();
                    sb.Append("  ");
                }
                sb.Append(sb.Length > 2 && sb[sb.Length - 1] != ':' ? " | " : " ").Append(item);
            }
            if (sb.Length > 2)
                ctx.Reply(sb.ToString());
        }

        private static string Cap(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        // ---------------------------------------------------------------- F5 / server console

        public static void Register()
        {
            try
            {
                var cmds = Traverse.Create(typeof(Terminal)).Field("commands").GetValue<System.Collections.IDictionary>();
                if (_registered && cmds != null && cmds.Contains("rates"))
                    return;

                // Same flags as before: onlyServer + remoteCommand so F5 admins reach the dedicated.
                new Terminal.ConsoleCommand(
                    "rates", "ServerRates: status | list | cmds | preset | reset | help",
                    OnConsole, false, true, true, false, true, false, FetchOptions, true, true, false);

                foreach (Rate r in Rates.All)
                    RegisterOne("sr_" + r.Short, r.Key + " [value]");
                foreach (string g in Rates.Groups.Keys)
                    RegisterOne("sr_" + g, "all " + g + " rates [value]");

                _registered = true;
                Plugin.Log?.LogInfo("ServerRates: registered F5/console commands (rates, sr_*).");
            }
            catch (Exception ex)
            {
                _registered = false;
                Plugin.Log?.LogError("ServerRates: failed to register console commands: " + ex);
            }
        }

        private static void RegisterOne(string cmd, string help)
        {
            new Terminal.ConsoleCommand(
                cmd, "ServerRates " + help, OnConsole,
                false, true, true, false, true, false, null, false, true, false);
        }

        private static List<string> FetchOptions()
        {
            var opts = new List<string> { "status", "list", "cmds", "preset", "reset", "help" };
            foreach (Rate r in Rates.All)
                opts.Add(r.Short);
            return opts;
        }

        private static void OnConsole(Terminal.ConsoleEventArgs args)
        {
            if (args == null || args.Context == null || args.Args == null || args.Args.Length == 0)
                return;

            Process(string.Join(" ", args.Args), new CommandContext
            {
                Who = "admin",
                CanWrite = true,
                Reply = RatesFeedback.Say
            });
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
