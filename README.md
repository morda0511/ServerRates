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
| Command | Description |
|---|---|
| `/sr_enabled` | Master switch (true/false) |
| `/sr_chatcommandsenabled` | Allow chat rate commands |
| `/sr_chatcommandprefix` | Chat prefix (default `/`) |

### Skills
| Command | Description |
|---|---|
| `/sr_skillgain` | Skill XP gain percent (100 = vanilla) |
| `/sr_skillloss` | Skill loss on death percent |

### Combat
| Command | Description |
|---|---|
| `/sr_playerdmg` | Player damage percent |
| `/sr_enemydmg` | Enemy damage percent |
| `/sr_enemyspeed` | Enemy speed / size percent |
| `/sr_enemystars` | Enemy star-up rate percent |
| `/sr_events` | Raid / event frequency percent |
| `/sr_worldlevel` | World level 0–10 |
| `/sr_passivemobs` | Passive: Unchanged / On / Off |
| `/sr_playerevents` | Toggle: Unchanged / On / Off |

### Survival
| Command | Description |
|---|---|
| `/sr_stamina` | Stamina drain percent |
| `/sr_movestamina` | Move stamina drain percent |
| `/sr_staminaregen` | Stamina regen percent |
| `/sr_eitr` | Eitr rate percent |
| `/sr_adrenaline` | Adrenaline rate percent |
| `/sr_food` | Food duration percent |
| `/sr_durability` | Durability loss percent |
| `/sr_carry` | Carry weight percent |

### Resources & loot master
| Command | Description |
|---|---|
| `/sr_resourcerate` | Global Resource Rate percent (synced) |
| `/sr_groundamp` | Category ground loot amp on/off |
| `/sr_snapground` | Snap extra piles to ground |
| `/sr_scatter` | Scatter force on extra piles |
| `/sr_other` | Multiplier for uncategorized ground loot |

### Materials
| Command | Description |
|---|---|
| `/sr_wood` | Basic wood drop multiplier |
| `/sr_finewood` | Fine wood multiplier |
| `/sr_corewood` | Core wood (RoundLog) multiplier |
| `/sr_specialwood` | Elder / Ygg / Ash / Black wood |
| `/sr_ore` | Raw ore multiplier |
| `/sr_scrap` | Scrap metal multiplier |
| `/sr_stone` | Stone multiplier |
| `/sr_flint` | Flint multiplier |
| `/sr_crystal` | Crystal / obsidian multiplier |
| `/sr_fuel` | Coal / resin / tar multiplier |
| `/sr_gems` | Coins / amber / rubies multiplier |
| `/sr_boss` | Boss materials multiplier |

### Consumables
| Command | Description |
|---|---|
| `/sr_crops` | Crops multiplier |
| `/sr_seeds` | Seeds / cones multiplier |
| `/sr_mushrooms` | Mushrooms multiplier |
| `/sr_berries` | Berries / honey multiplier |
| `/sr_fish` | Fish multiplier |
| `/sr_potions` | Meads / potions multiplier |

### Mob drops
| Command | Description |
|---|---|
| `/sr_hide` | Hides / pelts / scales multiplier |
| `/sr_trophy` | Mob trophies multiplier |
| `/sr_meat` | Meat multiplier |
| `/sr_parts` | Bones / entrails / glands multiplier |
| `/sr_feathers` | Feathers multiplier |
| `/sr_specialparts` | Guck / ooze / soft tissue multiplier |

### Death
| Command | Description |
|---|---|
| `/sr_keepequip` | Keep equipped gear (Unchanged/On/Off) |
| `/sr_keepinv` | Keep full inventory |
| `/sr_deleteitems` | Delete items on death |
| `/sr_deleteunequipped` | Delete unequipped items |
| `/sr_skillreset` | Reset skills on death |

### Build / craft
| Command | Description |
|---|---|
| `/sr_nobuildcost` | Free building |
| `/sr_nocraftcost` | Free crafting |
| `/sr_allpieces` | Unlock all build pieces |
| `/sr_allrecipes` | Unlock all recipes |
| `/sr_noworkbench` | No workbench range required |
| `/sr_toollocks` | World-level tool locks |

### Map / portals
| Command | Description |
|---|---|
| `/sr_nomap` | Disable map |
| `/sr_noportals` | Disable portals |
| `/sr_nobossportals` | Disable boss portals |
| `/sr_teleportall` | Teleport with ores / metals |
| `/sr_dungeonbuild` | Allow building in dungeons |

### World flags
| Command | Description |
|---|---|
| `/sr_nopseudo` | Disable pseudo drops |
| `/sr_nobuildfall` | Buildings do not fall |
| `/sr_noheavysnow` | No heavy snow |
| `/sr_allheavysnow` | Force heavy snow rules |
| `/sr_fire` | Fire world flag |

### Stations
| Command | Description |
|---|---|
| `/sr_smelter` | Smelter / kiln speed multiplier |
| `/sr_fermenter` | Fermenter speed multiplier |
| `/sr_cooking` | Cooking station speed multiplier |
| `/sr_plants` | Plant grow speed multiplier |

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
