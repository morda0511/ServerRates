# Changelog

## 1.4.5

- Register `sr_*` / `rates` on dedicated activate (InitTerminal often never runs headless)
- Fixes remote admin: `'sr_wood' is not a recognized command`

## 1.4.4

- Chat commands for console players: `!sr_wood 9`, `!rates status` (no admin / SteamID check)
- Config: `ChatCommandsEnabled`, `ChatCommandPrefix`

## 1.4.3

- Clarify: `sr_*` / `rates set` apply **live** (no restart). Restart only needed after installing a new DLL.

## 1.4.2

- One console command per rate: `sr_wood`, `sr_meat`, `sr_teleportall`, … (`rates cmds` lists all)
- Also `rates wood 9` short form

## 1.4.1

- In-game / dedicated console: `rates list`, `rates get`, `rates set` for **all** config keys (admin + remote)
- Tab-complete for rate keys; toggles accept On/Off/Unchanged

## 1.4.0

- Fine loot categories: Materials / Consumables / Mob Drops
- Ground amp via ZDO scan near players (vanilla/PS5 clients OK)
- Snap extra piles to ground + scatter force
- Split wood/ore/stone and mob loot multipliers

## 1.3.8

- Category ground amp for dedicated (wood etc. actually multiplies)

## 1.1.0

- All server-side rate keys: combat, survival, resources, death, build/craft, map/portals, world flags
- Drop category multipliers + station/plant speed

## 1.0.0

- Skill gain/reduction + wood/ore drop multipliers
