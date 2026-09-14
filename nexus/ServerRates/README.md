# ServerRates

**Run the rates. Keep the friends.**  
Dedicated-server rates mod built for **crossplay** — PC, **PS5**, and **Xbox** players all benefit. **Nobody on the client needs to install anything.**

Tune loot, combat, skills, stamina, portals, smelters, and more from the server. Console players change rates **in the chat box** with simple commands. Live apply. No world restart for rate tweaks.

---

## Why console players love it

- **No client mod** — vanilla / console joiners just play  
- **Chat commands** — type in the game chat (prefix `/`)  
- **No Steam admin list required** for chat commands (crossplay-friendly)  
- **Yellow HUD** when you change a rate — e.g. `Wood Multiplier x2`  
- **Fine loot categories** — wood, meat, hide, ore, trophies… not only one global multiplier  

Host on a dedicated server with BepInEx. Your PlayStation / Xbox squad gets the same rates as PC.

---

## What you can do

| Area | Examples |
|---|---|
| **Loot** | More wood, meat, hides, ore, scrap, gems, boss mats — per category |
| **Global drops** | Vanilla-style Resource Rate (synced to all clients) |
| **Combat** | Player / enemy damage, raid frequency, world level, passive mobs |
| **Skills & survival** | Faster XP, less stamina drain, more carry weight, food duration |
| **Building & travel** | Free build/craft, teleport with ores, no portal locks (toggles) |
| **Stations** | Faster smelter, fermenter, cooking, plant growth |
| **Death rules** | Keep gear / inventory, skill loss on death, and more |

Category loot is amplified **on the dedicated server** after items hit the ground — works with vanilla and console clients.

---

## Install (dedicated only)

1. Install **BepInEx** on your **Valheim dedicated server**  
2. Put `ServerRates.dll` in `BepInEx/plugins/ServerRates/`  
3. Start once → config: `BepInEx/config/com.morda.serverrates.cfg`  
4. Edit rates in the cfg, with **Valheim Server Manager**, or with **chat / F5 commands**

Clients (PC vanilla, PS5, Xbox): **install nothing.**

---

## Chat commands (console + PC)

Open chat and use the **`/`** prefix (Valheim chat style):

```text
/sr_wood 9
/sr_meat 5
/sr_resourcerate 200
/rates status
/rates help
/rates cmds
```

- **No value** → shows the current setting  
- **With value** → saves + applies **live** (yellow message)  
- **Multipliers:** `1` = vanilla, `2` = x2, `10` = x10  
- **Percents:** `100` = vanilla, `200` = x2  
- **Toggles:** `Unchanged` · `On` · `Off`  

PC admins can also use F5 without `/`: `sr_wood 9`, `rates cmds`, …

Disable chat commands anytime: `ChatCommandsEnabled = false` in the cfg.  
Prefix is configurable (`ChatCommandPrefix`, default `/`).

---

<details>
<summary><strong>All commands — click to expand</strong></summary>

### Meta
| Command | What it does |
|---|---|
| `/rates help` | Short help |
| `/rates status` | Overview |
| `/rates list` | Many current values |
| `/rates cmds` | List every `sr_*` command |
| `/rates get wood` | One value |
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

## Config sections (quick map)

1. General · 2 Skills · 3 Combat · 4 Survival · 5 Resources  
6 Loot · **6a Materials** · **6b Consumables** · **6c Mob Drops**  
7 Death · 8 Build/Craft · 9 Map/Portals · 10 World flags · 11 Stations  

---

## Notes

- **Dedicated server only** (not listen/host from the game client)  
- Rate changes apply **live** — restart only when replacing the DLL  
- Per-skill XP (e.g. only Axes x2) is not possible server-only  
- Chat commands are available to everyone when enabled — intentional for console / crossplay  

Made for servers that want a **perfect rate setup** without forcing mods on friends with controllers.
