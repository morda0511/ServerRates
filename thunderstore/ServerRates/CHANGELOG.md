# Changelog

## 1.7.0

- **Station info:** standing next to a smelter, kiln, furnace, cooking station or fermenter shows a short top-left message with the time per item (your multiplier applied, vanilla for comparison). Sent to that player only, repeated at most every 30 s per station. `sr_stationinfo off` turns it off (option `StationInfo`, default on).

## 1.6.4

- `rates inspect` also shows the time per item of the station next to you (smelter / kiln per ore, cooking station per food, fermenter per batch, plant grow time), with your multiplier applied and the vanilla value for comparison. The hover text of a station is built in the player's game and cannot be changed server-side.

## 1.6.3

- New `rates inspect`: shows the values stored in the stations within 8 m of you (owner, data revision, fuel, queued ore, bake timer, work-time store `acc`, cooking slots, fermenter content). New `rates repair`: same, and sets a smelter's work-time store back to 0.

## 1.6.2

- Fix log noise: the server no longer tries to parse its own chat replies ("PlatformUserID failed to parse"), and its replies now carry a valid sender id so clients do not log it either.

## 1.6.1

- **Smelter, kiln, furnace and cooking station speed work server-only now** (`sr_smelter`, `sr_cooking`, back after being removed in 1.6.0). The server adds work time to the smelter's `accTime` store (the game processes it in 1 s steps with normal fuel use per product) and advances raw food in a cooking station, only while the station is demonstrably working and never past "done".
- No ownership change any more: the server edits the values and raises the data revision above the owner's (checked in ZDOMan.RPC_ZDOData / ShouldSend). Nothing can get lost while a player puts ore or food into a station.

## 1.6.0

- **Plants and fermenters now work server-only.** When a plant is planted or a fermenter is filled, the server moves its start timestamp once so it needs duration / multiplier (`sr_plants 5`, `sr_fermenter 5`). Applies to things started after the change. Needs nothing on the clients.
- Removed `sr_smelter` and `sr_cooking`: the old method only worked where the server itself simulates the station, which is almost never. Their progress is recalculated every second inside the player's game and cannot be changed by a server-only mod.
- `rates preset` now sets plants and fermenter instead of all four stations.

## 1.5.10

- Fix: the chat bridge entry had an empty user id, so clients logged "PlatformUserID failed to parse" on every chat message. The id is now built with an explicit Steam platform and validated before it is sent; the used id is logged once at startup.

## 1.5.9

- **Fix: multipliers were too low when a tree dropped several items.** The protection against amplifying our own extra items skipped every wood drop within 3 m, including the other genuine drops of the same tree. Own spawns are now recognised by their creation time (newer than the spawn request) and a tight radius, so every drop of a tree is multiplied.

## 1.5.8

- Loot: with `LootSpawn=Client` the extras are requested as soon as the drop is seen (scan every 0.25 s) instead of waiting until it rests on the ground. Picking up the wood right away no longer gives vanilla amounts.

## 1.5.7

- **Loot multipliers: extra items are now spawned by the nearest player's game** (vanilla "SpawnObject" RPC) instead of being created as server-side objects. They land on the real ground with normal physics and replicate normally, also with networking mods that rework ZDO sending. New option `LootSpawn` (`Client` default, `Server` = old behaviour). `MaxExtraPiles` now caps the number of extra items per drop.

## 1.5.6

- **Chat bridge: chat commands now reach the server.** Valheim 1.0 sends chat from client to client and the server is not a recipient, so with one player online the server never saw any chat. The server now lists itself as a pseudo player ("ServerRates") so clients send their chat to it. New option `ChatBridge` (default on).
- Chat hooks run before other networking mods.
- Verified against the game code: player list packet, chat packet parsing and the command logic.

## 1.5.5

- Chat commands also work with networking mods that route chat themselves (e.g. Fires Ghetto Networking): the command is read when the packet arrives. The first chat lines seen are logged once to help diagnose hook problems.

## 1.5.4

- **Chat commands now work in the normal chat.** Valheim 1.0 sends normal chat and whispers as a per-player "Say" RPC that the dedicated only forwards; the command is now read when the server forwards it. The raw command is no longer shown to other players.

## 1.5.3

- Chat commands are read straight from the routed chat RPC (works even if another mod interferes with the chat handler). Startup log shows which mods patch the chat methods.
- Less log spam when world modifiers are re-applied while the world loads.

## 1.5.2

- Chat replies are sent with the player position so area-of-interest RPC routers (e.g. Fires Ghetto Networking) do not drop them.

## 1.5.1

- Chat commands: the server now answers (and logs) when commands are ignored, e.g. ChatCommandsEnabled=false.
- Mod activates itself even if the ZNet hooks were missed.

## 1.5.0

- **Config edits from Valheim Server Manager apply live now.** The old file watcher ran on a background thread and failed silently; the file is now polled on the main thread. Edits are announced in-game.
- **Loot piles no longer hang in the air.** Extra piles are created as plain ZDOs once the original item rests on the ground (no server-side physics).
- Items a player ever held or crafted are never multiplied (stops drop-and-pickup duplication). Wild fish are never duplicated.
- Fixed loot classification: boss cores were counted as ore, some potion/boss names never matched.
- Chat: group commands (`sr_loot 5`, `sr_mobs 3`), presets (`rates preset x5`), `rates reset`, abbreviated names, `x5` / `500%` values, private replies, short lines for console chat.
- New `ChatAccess` option: `Everyone` (default) or `AdminOnly` (server adminlist).
- New `MaxExtraPiles` safety cap. Removed `SnapExtraDropsToGround` / `ExtraDropScatterForce`.
- Code: all rates now come from one table (config, commands, apply, help).

## 1.4.12

- Console chat works again: `sr_wood 9` (no slash, no adminlist). `/` cannot reach the dedicated from vanilla/console clients.
- PC may still use `/sr_wood 9` if the client DLL is installed (forwards as chat).
- F5 `sr_wood 9` still needs adminlist for remote.
## 1.4.11

- Chat: `/sr_wood 9` (slash). Client forwards `/sr_*` as real chat.
- F5: `sr_wood 9` (no slash). Remote needs adminlist.
- Yellow HUD via routed ShowMessage on dedicated.
## 1.4.10

- Chat commands work **without** / (e.g. `sr_wood 9`). Valheim treats leading `/` as a local console command and never sends it to the dedicated server.
- Default `ChatCommandPrefix` is empty
## 1.4.9

- `rates cmds` / status / help: full list to F5 via RemotePrint (per line) + one chat summary
- Keep yellow HUD only for rate set (e.g. Wood Multiplier x2)

## 1.4.8

- Feedback to clients: RemotePrint (F5), yellow ShowMessage HUD, chat shout — not only BepInEx log

## 1.4.7

- Fix Harmony Chat.RPC_ChatMessage patch (param name `userInfo`) — chat commands work again

## 1.4.6

- Yellow center HUD on rate change (`Wood Multiplier x9`) via vanilla `ShowMessage` RPC — no client mod

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
