<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/server-rates-banner.png" alt="ServerRates Banner" width="100%"></p>

<h1 align="center"><strong>ServerRates</strong></h1>

<p align="center"><strong>Your World. Your Rules. Your Rates.</strong></p>

<p align="center">Take control of your Valheim server. Customize combat, skills, loot, survival, building, world mechanics and more through a flexible server side configuration system.</p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-gold.png" width="100%" alt=""></p>

<h2 align="center"><strong>Overview</strong></h2>

<p align="center">ServerRates gives server owners the freedom to shape their own Valheim experience, from relaxed cooperative worlds to challenging survival adventures.</p>

<p align="center">Configure gameplay rates, customize world rules, manage item drops, change skill progression and adjust crafting restrictions without manually editing game files.</p>

<p align="center"><strong>Server side configuration</strong></p>
<p align="center"><strong>Flexible gameplay settings</strong></p>
<p align="center"><strong>In game chat commands</strong></p>
<p align="center"><strong>Presets for quick configuration</strong></p>
<p align="center"><strong>Detailed status and inspection tools</strong></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-norse.png" width="100%" alt=""></p>

<h2 align="center"><strong>Installation</strong></h2>

<p align="center"><strong>1. Install BepInEx</strong></p>
<p align="center">Install BepInExPack for Valheim on your dedicated server.</p>

<p align="center"><strong>2. Install ServerRates</strong></p>
<p align="center">Install ServerRates on the dedicated server using your preferred mod manager or manual installation.</p>

<p align="center"><strong>3. Start the server</strong></p>
<p align="center">Launch the server once to generate the configuration file.</p>

<p align="center"><strong>4. Configure your world</strong></p>
<p align="center">Edit the configuration or use the available chat commands to adjust your settings.</p>

<p align="center"><strong>Configuration file</strong></p>
<p align="center"><code>BepInEx/config/com.morda.serverrates.cfg</code></p>

<p align="center"><strong>Players do not need to install ServerRates on their clients when using it as a server side mod.</strong></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-gold.png" width="100%" alt=""></p>

<h2 align="center"><strong>Requirements</strong></h2>

<p align="center">Valheim Dedicated Server</p>
<p align="center">BepInExPack for Valheim</p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-norse.png" width="100%" alt=""></p>

<h2 align="center"><strong>Configuration Categories</strong></h2>

<p align="center">Use the categories below to find the settings you want to customize.</p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-combat.png" width="90" alt="Combat"></p>
<h3 align="center"><strong>Combat</strong></h3>
<p align="center">Customize player damage, enemy damage, enemy speed, enemy stars, world difficulty and event behavior.</p>
<p align="center"><code>sr_playerdmg</code> &nbsp; <code>sr_enemydmg</code> &nbsp; <code>sr_enemyspeed</code></p>
<p align="center"><code>sr_enemystars</code> &nbsp; <code>sr_events</code> &nbsp; <code>sr_worldlevel</code></p>
<p align="center"><code>sr_passivemobs</code> &nbsp; <code>sr_playerevents</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-skills.png" width="90" alt="Skills"></p>
<h3 align="center"><strong>Skills</strong></h3>
<p align="center">Control how quickly players gain skills and how much skill progress is lost.</p>
<p align="center"><code>sr_skillgain</code> &nbsp; <code>sr_skillloss</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-loot.png" width="90" alt="Loot"></p>
<h3 align="center"><strong>Loot and Resources</strong></h3>
<p align="center">Adjust resource rates, ground item behavior, extra loot per drop and loot generation.</p>
<p align="center"><code>sr_resourcerate</code> &nbsp; <code>sr_groundamp</code> &nbsp; <code>sr_maxpiles</code></p>
<p align="center"><code>sr_lootspawn</code> &nbsp; <code>sr_other</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-survival.png" width="90" alt="Survival"></p>
<h3 align="center"><strong>Survival</strong></h3>
<p align="center">Change stamina, Eitr, adrenaline, food, durability and carry weight related settings.</p>
<p align="center"><code>sr_stamina</code> &nbsp; <code>sr_movestamina</code> &nbsp; <code>sr_staminaregen</code></p>
<p align="center"><code>sr_eitr</code> &nbsp; <code>sr_adrenaline</code> &nbsp; <code>sr_food</code></p>
<p align="center"><code>sr_durability</code> &nbsp; <code>sr_carry</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-death.png" width="90" alt="Death"></p>
<h3 align="center"><strong>Death Settings</strong></h3>
<p align="center">Configure equipment retention, inventory loss and skill penalties on death.</p>
<p align="center"><code>sr_keepequip</code> &nbsp; <code>sr_keepinv</code></p>
<p align="center"><code>sr_deleteitems</code> &nbsp; <code>sr_deleteunequipped</code></p>
<p align="center"><code>sr_skillreset</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-building.png" width="90" alt="Building"></p>
<h3 align="center"><strong>Building and Crafting</strong></h3>
<p align="center">Customize building costs, crafting costs, available pieces, recipes and station restrictions.</p>
<p align="center"><code>sr_nobuildcost</code> &nbsp; <code>sr_nocraftcost</code></p>
<p align="center"><code>sr_allpieces</code> &nbsp; <code>sr_allrecipes</code></p>
<p align="center"><code>sr_noworkbench</code> &nbsp; <code>sr_toollocks</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-world.png" width="90" alt="World"></p>
<h3 align="center"><strong>World and Exploration</strong></h3>
<p align="center">Change map visibility, portal restrictions, dungeon building and world behavior.</p>
<p align="center"><code>sr_nomap</code> &nbsp; <code>sr_noportals</code></p>
<p align="center"><code>sr_nobossportals</code> &nbsp; <code>sr_teleportall</code></p>
<p align="center"><code>sr_dungeonbuild</code></p>
<p align="center"><code>sr_nopseudo</code> &nbsp; <code>sr_nobuildfall</code></p>
<p align="center"><code>sr_noheavysnow</code> &nbsp; <code>sr_allheavysnow</code> &nbsp; <code>sr_fire</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/icon-stations.png" width="90" alt="Stations"></p>
<h3 align="center"><strong>Production Stations</strong></h3>
<p align="center">Adjust processing behavior for smelters, cooking stations, fermenters and plant related systems.</p>
<p align="center"><code>sr_smelter</code> &nbsp; <code>sr_cooking</code> &nbsp; <code>sr_fermenter</code> &nbsp; <code>sr_plants</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-gold.png" width="100%" alt=""></p>

<h2 align="center"><strong>Loot Configuration</strong></h2>

<p align="center">Loot settings are divided into resource materials, consumables and creature drops.</p>

<h3 align="center"><strong>Materials</strong></h3>
<p align="center"><code>sr_wood</code> &nbsp; <code>sr_finewood</code> &nbsp; <code>sr_corewood</code></p>
<p align="center"><code>sr_specialwood</code> &nbsp; <code>sr_ore</code> &nbsp; <code>sr_scrap</code></p>
<p align="center"><code>sr_stone</code> &nbsp; <code>sr_flint</code> &nbsp; <code>sr_crystal</code></p>
<p align="center"><code>sr_fuel</code> &nbsp; <code>sr_gems</code> &nbsp; <code>sr_boss</code></p>

<h3 align="center"><strong>Consumables</strong></h3>
<p align="center"><code>sr_crops</code> &nbsp; <code>sr_seeds</code> &nbsp; <code>sr_mushrooms</code></p>
<p align="center"><code>sr_berries</code> &nbsp; <code>sr_fish</code> &nbsp; <code>sr_potions</code></p>

<h3 align="center"><strong>Creature Drops</strong></h3>
<p align="center"><code>sr_hide</code> &nbsp; <code>sr_trophy</code> &nbsp; <code>sr_meat</code></p>
<p align="center"><code>sr_parts</code> &nbsp; <code>sr_feathers</code> &nbsp; <code>sr_specialparts</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-norse.png" width="100%" alt=""></p>

<h2 align="center"><strong>Chat Commands</strong></h2>

<p align="center">Use the available commands in game to inspect and manage your server configuration.</p>

<p align="center">Type commands in the normal chat without a slash, for example <code>sr_wood 5</code>.</p>

<h3 align="center"><strong>General Commands</strong></h3>

<p align="center"><strong>Help and Status</strong></p>
<p align="center"><code>rates help</code> &nbsp; <code>rates status</code> &nbsp; <code>rates cmds</code></p>

<p align="center"><strong>Configuration</strong></p>
<p align="center"><code>rates list &lt;group&gt;</code> &nbsp; <code>rates get &lt;name&gt;</code></p>
<p align="center"><code>rates set &lt;name&gt; &lt;value&gt;</code></p>

<p align="center"><strong>Presets</strong></p>
<p align="center"><code>rates preset vanilla</code> &nbsp; <code>rates preset x2</code></p>
<p align="center"><code>rates preset x5</code> &nbsp; <code>rates preset x10</code></p>

<p align="center"><strong>Reset and Maintenance</strong></p>
<p align="center"><code>rates reset &lt;name|group|all&gt;</code> &nbsp; <code>rates inspect</code></p>
<p align="center"><code>rates repair</code></p>

<h3 align="center"><strong>Configuration Groups</strong></h3>
<p align="center">Use <code>rates list &lt;group&gt;</code> to view the settings available in a group.</p>
<p align="center"><code>loot</code> &nbsp; <code>materials</code> &nbsp; <code>consumables</code> &nbsp; <code>mobs</code></p>
<p align="center"><code>skills</code> &nbsp; <code>combat</code> &nbsp; <code>survival</code> &nbsp; <code>resources</code></p>
<p align="center"><code>death</code> &nbsp; <code>build</code> &nbsp; <code>map</code> &nbsp; <code>world</code></p>
<p align="center"><code>stations</code> &nbsp; <code>general</code></p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-gold.png" width="100%" alt=""></p>

<h2 align="center"><strong>Command Examples</strong></h2>

<p align="center"><strong>Check the current configuration</strong></p>
<p align="center"><code>rates status</code> &nbsp; <code>rates list loot</code></p>

<p align="center"><strong>Change a setting</strong></p>
<p align="center"><code>rates set sr_wood 2</code></p>

<p align="center"><strong>Apply a preset</strong></p>
<p align="center"><code>rates preset x5</code></p>

<p align="center"><strong>Reset a setting or group</strong></p>
<p align="center"><code>rates reset sr_wood</code> &nbsp; <code>rates reset loot</code></p>

<p align="center"><strong>View detailed information</strong></p>
<p align="center"><code>rates inspect</code> &nbsp; <code>rates repair</code></p>

<p align="center">The exact commands available to players may depend on your configured chat command access and prefix settings.</p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-norse.png" width="100%" alt=""></p>

<h2 align="center"><strong>Presets</strong></h2>

<p align="center">Quickly change your server's gameplay style using the built in presets.</p>

<p align="center"><code>rates preset vanilla</code></p>
<p align="center">Return supported settings to their vanilla values.</p>

<p align="center"><code>rates preset x2</code></p>
<p align="center">Apply the supported double rate preset.</p>

<p align="center"><code>rates preset x5</code></p>
<p align="center">Apply the supported five times rate preset.</p>

<p align="center"><code>rates preset x10</code></p>
<p align="center">Apply the supported ten times rate preset.</p>

<p align="center">Presets apply to supported loot, skill gain and station settings (smelter, cooking, fermenter and plants).</p>

<h2 align="center"><strong>Inspection and Repair</strong></h2>

<p align="center"><code>rates inspect</code></p>
<p align="center">Displays saved configuration values and station item processing times for supported stations within inspection range.</p>

<p align="center"><code>rates repair</code></p>
<p align="center">Displays relevant configuration and station information and resets the stored smelter timer to zero.</p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-gold.png" width="100%" alt=""></p>

<h2 align="center"><strong>General Settings</strong></h2>

<p align="center">The following keys control general ServerRates behavior.</p>

<p align="center"><code>sr_enabled</code> &nbsp; Enable or disable ServerRates.</p>
<p align="center"><code>sr_chatcmds</code> &nbsp; Enable or disable chat commands.</p>
<p align="center"><code>sr_chataccess</code> &nbsp; Configure chat command access.</p>
<p align="center"><code>sr_chatbridge</code> &nbsp; Configure chat command bridge behavior.</p>
<p align="center"><code>sr_stationinfo</code> &nbsp; Configure station information output.</p>

<h2 align="center"><strong>Notes and Limitations</strong></h2>

<p align="center">Available settings and behavior depend on the installed ServerRates version and the underlying Valheim mechanics.</p>

<p align="center">Some changes may only affect newly generated drops, newly crafted items or future processing cycles. Existing items and ongoing processes may not be recalculated automatically.</p>

<p align="center">Smelter and cooking station speed can be raised above vanilla and set back to normal. Values below 1 (slower than vanilla) are ignored.</p>

<p align="center">Always keep a backup of your world and configuration before making major gameplay changes.</p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/divider-norse.png" width="100%" alt=""></p>

<h2 align="center"><strong>Support and Community</strong></h2>

<p align="center">Found a bug, have a suggestion or need help configuring ServerRates?</p>

<p align="center"><strong>Join the ServerRates Discord Community</strong></p>

<p align="center"><a href="https://discord.gg/KGM8rjAfT">Join our Discord</a></p>

<p align="center">Feedback and bug reports are always welcome.</p>

<p align="center"><img src="https://raw.githubusercontent.com/morda0511/ServerRates/main/thunderstore-assets/server-rates/server-rates-footer.png" alt="ServerRates Footer" width="100%"></p>

<p align="center"><strong>ServerRates</strong></p>
<p align="center"><em>Your World. Your Rules. Your Rates.</em></p>
