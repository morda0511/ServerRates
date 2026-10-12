# ServerRates

**Dedicated-server rates for Valheim, built for crossplay.**
PC, PlayStation and Xbox players all get the same rates. **Clients install nothing.** Tune loot, combat, skills, stamina, portals and more from the dedicated server. Console players change rates live in the normal text chat.

**Dedicated server only** · Valheim 1.0 · BepInExPack 5.4.2350+. Listen servers / host-from-client are not supported.

**Bug reports:** https://github.com/morda0511/ServerRates/issues
**Discord:** https://discord.gg/aVKVVmyzj

---

## Features

- **Loot:** global Resource Rate plus per-category multipliers (wood, ore, meat, hide, trophies, crops, fish, potions, ...). Extra piles spawn on the ground, not in the air.
- **Combat & world:** player/enemy damage, enemy speed/size/stars, raids, world level, passive mobs.
- **Skills & survival:** skill gain/loss, stamina, eitr, adrenaline, food duration, durability, carry weight.
- **Building, travel, death:** free build/craft, unlock pieces/recipes, portals, map, keep inventory/equipment.
- **Live:** edit the config file or type a chat command, rates apply instantly. Everybody sees a yellow message when a rate changes.

---

## Chat commands (console + PC)

Type in the **normal chat box**, **without a slash** (Valheim handles `/` locally and never sends it to the server):

```text
sr_wood 9          set wood to x9
sr_wood            show wood
sr_loot 5          every loot category x5
sr_mobs 3          all mob drops x3
rates preset x5    loot x5, skill gain 500%, plants + fermenter x5
rates status       what differs from vanilla
rates reset all    back to vanilla
```

- Names can be abbreviated: `sr_fine` = `sr_finewood`. If a short name fits several, the server lists them.
- Values: `5`, `x5`, `500%` all work. Toggles take `on` / `off` / `unchanged`.
- Replies go **only to you**. Rate changes are announced to everybody.
- PC players with the DLL on their client may also use `/sr_wood 9`.
- Who may change rates is set by `ChatAccess` in the config:
  `Everyone` (default, no adminlist needed, good for console) or `AdminOnly` (sender must be in the server adminlist). Anyone can always *read* values.
- Turn chat commands off with `ChatCommandsEnabled = false`.
- Server console / F5 (admins): the same commands, e.g. `sr_wood 9`, `rates status`.


### Station info

When you stand within 3 m of a smelter, kiln, furnace, cooking station or fermenter, the server shows a short message at the top left with the time **per item** (with your multiplier and the vanilla value for comparison). It is a plain message sent to you only, nothing is placed in the world. Switch it off with `sr_stationinfo off`, back on with `sr_stationinfo on`. `rates inspect` shows the same for the station next to you plus its stored values.

### How chat reaches the server

Valheim sends chat from client to client, the server is normally not a recipient. ServerRates therefore lists the server as a pseudo player named **ServerRates** in the player list (option `ChatBridge`, on by default). That makes every client send its chat to the server as well, so commands work even when you are the only player online. Commands are swallowed by the server and never shown to other players. If another mod already adds the server as a player (e.g. Server Devcommands "Server chat"), ServerRates does not add a second entry.

### Meta commands

| Command | Description |
|---|---|
| `rates help` | Short help |
| `rates status` | Everything that differs from vanilla |
| `rates list <group>` | Values of a group |
| `rates cmds` | All groups and command names |
| `rates get wood` / `rates set wood 9` | Read / write one value |
| `rates preset vanilla\|x2\|x5\|x10` | Quick presets (any number 1-100) |
| `rates reset <name\
| `rates inspect` | Shows the stations within 8 m of you: stored values (owner, fuel, queue, ...) and the time **per item** with and without your multiplier |
| `rates repair` | Like `inspect`, and sets a smelter's work-time store back to 0 ||group\|all>` | Back to vanilla |

### Groups (set several rates at once: `sr_<group> <value>`)

| Group | Contains |
|---|---|
| `loot` | `materials` + `consumables` + `mobs` |
| `materials` | wood, finewood, corewood, specialwood, ore, scrap, stone, flint, crystal, fuel, gems, boss |
| `consumables` | crops, seeds, mushrooms, berries, fish, potions |
| `mobs` | hide, trophy, meat, parts, feathers, specialparts |
| `skills` | skillgain, skillloss |
| `combat` | playerdmg, enemydmg, enemyspeed, enemystars, events, worldlevel, passivemobs, playerevents |
| `survival` | stamina, movestamina, staminaregen, eitr, adrenaline, food, durability, carry |
| `resources` | resourcerate, groundamp, maxpiles, other |
| `death` | keepequip, keepinv, deleteitems, deleteunequipped, skillreset |
| `build` | nobuildcost, nocraftcost, allpieces, allrecipes, noworkbench, toollocks |
| `map` | nomap, noportals, nobossportals, teleportall, dungeonbuild |
| `world` | nopseudo, nobuildfall, noheavysnow, allheavysnow, fire |
| `stations` | smelter, fermenter, cooking, plants |

Every single rate is also its own command `sr_<name>`; `rates cmds` prints them all.

---

## How values work

| Type | Meaning |
|---|---|
| **Percent** | `100` = vanilla, `200` = x2, `50` = half |
| **Multiplier** | `1` = vanilla, `2` = x2, `10` = x10 |
| **Toggle** | `Unchanged` / `On` / `Off` (`Off` removes the world flag, `Unchanged` leaves it alone) |

Category loot multipliers stack on top of Resource Rate.

### How loot multipliers work

The dedicated watches item drops near players. When a fresh drop has come to rest on the ground, the server asks the nearest player's game to spawn the extra items next to it (vanilla SpawnObject RPC, so they fall and land like normal drops). `LootSpawn = Server` lets the server create them itself instead. Items a player ever held or crafted are never multiplied, so dropping and picking up items cannot be abused. Drop *chance* stays vanilla, only the amount changes. `MaxExtraPiles` caps how many extra items one drop can create.

---

## Config

**One file:** `BepInEx/config/com.morda.serverrates.cfg`

Edit it by hand or with Valheim Server Manager: the server notices the change within about two seconds and applies it live, no restart. Chat commands write to the same file.

---

## Install

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) on your **Valheim dedicated server**.
2. Drop `ServerRates.dll` into `BepInEx/plugins/ServerRates/`.
3. Start the server once to generate `com.morda.serverrates.cfg`.
4. Edit the config, use Valheim Server Manager, or use chat commands.

Clients (vanilla PC, PS5, Xbox): no install.

---

## Limitations

- Per-skill XP needs a client mod. Vanilla only has a global skill rate.
- Loot that goes straight into the inventory (some pickables) never lands on the ground and is not multiplied.
- Station speed (`smelter`, `cooking`, `plants`, `fermenter`) is done by the server editing the stations' data, nothing is needed on the clients. `plants` and `fermenter` apply to plants planted and batches started after the change. `smelter` and `cooking` only speed things up (values below 1 are ignored) and only boost stations that are actually working (ore and fuel, fire lit). Cooked food is never pushed into burnt.
- Existing config files keep working. The old options `SnapExtraDropsToGround` and `ExtraDropScatterForce` are no longer used.
