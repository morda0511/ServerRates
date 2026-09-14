# ServerRates

**Dedicated-server rates for Valheim — built for crossplay.**  
PC, PlayStation, and Xbox players all get the same rates. **Clients install nothing.**

Tune loot, combat, skills, stamina, portals, crafting, and stations from your dedicated server. Console players change rates live in the **chat box**. Perfect for friends who play with a controller and never open a mod folder.

[Thunderstore](https://thunderstore.io/) · Dedicated + BepInEx only

---

## Why this mod?

Most rate mods either need every player to install something, or only offer one blunt “more resources” slider. ServerRates is different:

- **Server-side only** — vanilla and console clients just join  
- **Fine loot categories** — wood, meat, hides, ore, trophies, and more, not only one global multiplier  
- **Chat commands for console** — `/sr_wood 9`, `/rates status`, …  
- **Live apply** — no world restart when you change a rate  
- **Yellow HUD feedback** — e.g. `Wood Multiplier x2` when a rate changes  
- **Full server toolkit** — skills, combat, survival, death rules, portals, smelters, plants  

If you host a dedicated server for a mixed PC + console group, this is for you.

---

## Features

### Loot & resources
- Global **Resource Rate** (vanilla world modifier, synced to all clients)  
- **Category ground amp** on the dedicated server after loot hits the ground (works with PS5/Xbox)  
- Materials: wood / fine / core / special wood, ore, scrap, stone, flint, crystal, fuel, gems, boss mats  
- Consumables: crops, seeds, mushrooms, berries, fish, potions  
- Mob drops: hide, trophy, meat, parts, feathers, special parts  

### Combat & world
- Player and enemy damage, enemy speed/size, star-up rate, raid frequency  
- World level, passive mobs, player events  

### Skills & survival
- Skill gain and skill loss on death  
- Stamina, move stamina, regen, eitr, adrenaline, food duration, durability, carry weight  

### Building, travel, death
- Free build/craft, unlock pieces/recipes, workbench range, tool locks (toggles)  
- Map / portals / boss portals / teleport-all / dungeon build  
- Keep equipment or inventory on death, delete items, skill reset  

### Stations
- Smelter, fermenter, cooking, and plant grow speed multipliers  

---

## Install (dedicated server only)

1. Install **BepInEx** on your **Valheim dedicated server**  
2. Drop `ServerRates.dll` into `BepInEx/plugins/ServerRates/`  
3. Start the server once → `BepInEx/config/com.morda.serverrates.cfg`  
4. Edit the config, use **Valheim Server Manager**, or use **chat / F5 commands**

**Do not put this mod in a client profile** unless you only want chat helpers on PC — rates activate only on a dedicated process.

Clients (vanilla PC, PS5, Xbox): **no install.**

---

## How values work

| Type | Meaning |
|---|---|
| **Percent** | `100` = vanilla · `200` = x2 · `50` = half · `0` = off where it makes sense |
| **Multiplier** | `1` = vanilla · `2` = x2 · `10` = x10 |
| **Toggle** | `Unchanged` · `On` · `Off` |

Category loot multipliers stack **on top of** Resource Rate. They amplify ground piles on the dedicated server after items spawn.

---

## Chat commands (console + PC)

Open the in-game chat and use the **`/`** prefix:

```text
/sr_wood 9
/sr_meat 5
/sr_resourcerate 200
/rates status
/rates help
/rates cmds
```

- No value → show current setting  
- With value → save + apply **live** (yellow center message)  
- Chat commands do **not** require a Steam admin list (crossplay-friendly)  
- Turn off with `ChatCommandsEnabled = false`  
- Prefix is configurable (`ChatCommandPrefix`, default `/`)  

PC players can also use the F5 console without `/`: `sr_wood 9`, `rates cmds`, …

<details>
<summary><strong>All commands (click to expand)</strong></summary>

### Meta
| Command | Description |
|---|---|
| `/rates help` | Short help |
| `/rates status` | Overview |
| `/rates list` | Many current values |
| `/rates cmds` | List every `sr_*` command |
| `/rates get wood` | Read one value |
| `/rates set wood 9` | Same as `/sr_wood 9` |

### General
`/sr_enabled` · `/sr_chatcommandsenabled` · `/sr_chatcommandprefix`

### Skills
`/sr_skillgain` · `/sr_skillloss`

### Combat
`/sr_playerdmg` · `/sr_enemydmg` · `/sr_enemyspeed` · `/sr_enemystars` · `/sr_events` · `/sr_worldlevel` · `/sr_passivemobs` · `/sr_playerevents`

### Survival
`/sr_stamina` · `/sr_movestamina` · `/sr_staminaregen` · `/sr_eitr` · `/sr_adrenaline` · `/sr_food` · `/sr_durability` · `/sr_carry`

### Resources & loot master
`/sr_resourcerate` · `/sr_groundamp` · `/sr_snapground` · `/sr_scatter` · `/sr_other`

### Materials
`/sr_wood` · `/sr_finewood` · `/sr_corewood` · `/sr_specialwood` · `/sr_ore` · `/sr_scrap` · `/sr_stone` · `/sr_flint` · `/sr_crystal` · `/sr_fuel` · `/sr_gems` · `/sr_boss`

### Consumables
`/sr_crops` · `/sr_seeds` · `/sr_mushrooms` · `/sr_berries` · `/sr_fish` · `/sr_potions`

### Mob drops
`/sr_hide` · `/sr_trophy` · `/sr_meat` · `/sr_parts` · `/sr_feathers` · `/sr_specialparts`

### Death
`/sr_keepequip` · `/sr_keepinv` · `/sr_deleteitems` · `/sr_deleteunequipped` · `/sr_skillreset`

### Build / craft
`/sr_nobuildcost` · `/sr_nocraftcost` · `/sr_allpieces` · `/sr_allrecipes` · `/sr_noworkbench` · `/sr_toollocks`

### Map / portals
`/sr_nomap` · `/sr_noportals` · `/sr_nobossportals` · `/sr_teleportall` · `/sr_dungeonbuild`

### World flags
`/sr_nopseudo` · `/sr_nobuildfall` · `/sr_noheavysnow` · `/sr_allheavysnow` · `/sr_fire`

### Stations
`/sr_smelter` · `/sr_fermenter` · `/sr_cooking` · `/sr_plants`

</details>

---

## Config sections

1. General  
2. Skills  
3. Combat  
4. Survival  
5. Resources  
6. Loot (master)  
6a. Materials  
6b. Consumables  
6c. Mob Drops  
7. Death  
8. Build / Craft  
9. Map / Portals  
10. World flags  
11. Stations  

The config reloads from disk while the server runs (file watcher). Rate commands also save and apply live.

---

## Requirements

- Valheim **dedicated server**  
- [BepInEx Pack for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)  
- Not supported: listen server / “host from game client”  

---

## Limitations

- Per-skill XP (e.g. only Axes x2) needs a client mod — vanilla only has a global skill rate  
- Direct inventory picks (some berries/pickables) may not go through ground ItemDrop amp  
- Drop **chance** stays vanilla; ServerRates multiplies amounts after something already dropped  

---

## Links

- Source: GitHub (see repository)  
- Optional companion: Valheim Server Manager (desktop cfg UI for dedicated)

---

Made for servers that want a **clean, crossplay-friendly rate setup** — without forcing mods onto friends with controllers.
