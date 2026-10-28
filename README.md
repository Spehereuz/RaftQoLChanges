# QoL Changes

Modular quality-of-life mod for [Raft](https://store.steampowered.com/app/648800/Raft/) built on
[RaftModLoader](https://www.raftmodding.com/loader) and [Harmony](https://github.com/pardeike/Harmony).

The goal is to remove routine (UI, inventory handling, range visualisation) without touching the
survival loop: stamina, resource spawn rates and damage values stay vanilla. Every feature is
self-contained and can be switched on or off independently.

| | |
|---|---|
| Game | Raft 1.1.01 (Unity 2021.3, Mono) |
| Loader | RaftModLoader 2.8.x |
| Language | C# 7.3 (limit of RML's runtime compiler) |
| Multiplayer | Client-side; not required by all players |

## Features

| Feature | Config section | Default | Description |
|---|---|---|---|
| Stack size | `stackSize` | on, `multiplier: 5` | Multiplies the stack size of every stackable item by 1–5 (at 5: 20 → 100, 5 → 25, arrows 30 → 150). Items that never stack (tools, equipment, water containers) are unchanged. |
| Durability indicator | `durabilityIndicator` | on, `Percent` for every group | Writes the durability of items that have a durability bar on their slot, and the charge of batteries: `73%`, `88` or `88/120`, coloured like the bar. |
| Hover info | `hoverInfo` | on, every target | Shows the state of the object under the crosshair in a line below it: battery charge, plant growth, water purifiers, engine fuel, cooking and smelting. |
| Auto-replace tool | `autoReplace` | on, every group | When the item in the selected hotbar slot breaks or is used up, moves the same item from the inventory into the slot; a broken tool can be replaced by another tier of the same tool. |
| Raft status HUD | `raftStatus` | on, every line, 100% | While you are on the raft, lines above the health bar show whether the raft is anchored, how many engines run and where they push it, whether they are strong enough for the raft and how long their fuel lasts. |
| Full net indicator | `fullNet` | on | Collection nets glow red while they are full and catch nothing more. |
| Scarecrow radius | `scarecrowRadius` | on, crop plots lit, 4 cm lines | While the hammer or a scarecrow is selected, outlines where scarecrows keep seagulls away and lights crop plots green inside and red outside of it. |
| Storage sort | `storageSort` | on, by category | The sort button of a storage groups the items by category or puts them in alphabetical order instead of the game's order. |
| Pinned recipes | `pinnedRecipes` | on, ingredients shown, 100% | Up to three recipes pinned from the crafting menu stay on the HUD with the amounts of their ingredients and how many times they can be crafted. |

### Durability indicator

The text appears in the top-left corner of the slot, where the amount of a stack is shown, in the
hotbar, inventory, equipment slots and storages. Items are split into four groups, each with its
own mode (Off, Percent, Remaining, Remaining / max):

| Group | Items | Unit |
|---|---|---|
| Tools and equipment | axes, hooks, weapons, fishing rods, armor, flippers, head lights, oxygen bottle and other tools | uses |
| Water containers | canteens and bottles, fresh or salt water | sips |
| Food portions | cooked catfish, cooked salmon, watermelon | portions |
| Batteries | batteries, advanced batteries | charge |

- Items from other mods are grouped by their properties: drinkable → water, edible → food, a name
  containing "Battery" → batteries, anything else → tools.
- The charge of a battery placed in a machine is shown by [Hover info](#hover-info).
- A stack of more than one item keeps showing its amount; the indicator appears when one item is left.
- Items that have not been used yet show nothing unless **Show at full durability** is on.
- Percent is rounded down, so a used item never shows `100%` and a nearly broken one shows at least `1%`.

### Hover info

When you aim at one of the objects below within interaction distance, a line under the crosshair shows its
state, mirroring the game's own prompt above the crosshair. Labels are white, values are coloured.

| Target | Shows |
|---|---|
| Battery slot of any machine (purifier, grill, smelter, juicer, receiver, sprinkler, recycler, battery charger) | `Battery 68%`, coloured like its charge bar in the inventory. Format: Off, Percent, Remaining or Remaining / max. Nothing when the slot is empty. |
| Plant or tree | `Growing 63%`, `Ready to harvest` or `Needs water` |
| Soil of a crop plot | `2 growing (41%) · 1 ready · 1 needs water`; the percentage is the plant closest to being ready. A plot with a single plant reads like the plant. |
| Simple or advanced water purifier | `Salt water · purifying 45%` or `Fresh water ready`; with several portions `3 purifying (45%) · 2 fresh` |
| Electric water purifier | `Fresh water 12/20 · Pumping`, `Full` or `No power` |
| Grill (simple, advanced, electric) | `Raw salmon · cooking 45%` or `Cooked salmon ready`; with several items `2 cooking (45%) · 1 ready`. Aiming at one of the items shows only that item. |
| Smelter, electric smelter, paint mill | the same with `smelting` / `grinding`: `Metal ore · smelting 45%` |
| Cooking pot, juicer | `Fish stew · cooking 45%` / `Smoothie · blending 45%`, then `Fish stew · 3 portions` |
| Engine (any part of it) | `Fuel 72% · 36 min` while it burns fuel, `Fuel 72% · paused` while it runs but an anchor holds the raft or the raft is too heavy, `Fuel 72%` when switched off, `Fuel Empty`. Same text and colours as the fuel line of the [Raft status HUD](#raft-status-hud), which shows only the emptiest engine. |
| Collection net | `Items 7/10` (also `Items 0/10` for an empty net), `Full 10/10` in red once it catches nothing more. A net without a limit (from another mod) shows only the count. |
| Scarecrow | `Parts 2/3` for a basic scarecrow, coloured like a tool's durability: a seagull pecks off one part per attack and the scarecrow breaks after the last one. `Indestructible` for an advanced scarecrow. `· seagull` while a seagull sits on it. |

- A plant grows only after it has been watered once and then grows until it is harvested. The game asks
  for water only while you hold an item; `Needs water` is shown with empty hands as well.
- Plants are found the same way as the game's harvest prompt, so a plant reads the same wherever it is
  aimed at; the soil around plants shows the summary of the plot.
- Grills, smelters and pots add `· no fuel` / `· no power` while something waits because the fire is out or
  the battery is off or empty. Ingredients in a pot that has not started cooking show nothing.
- Each target group can be switched off separately: Batteries, Plants, Water purifiers, Engine fuel, Cooking
  (grills, cooking pot, juicer), Smelters and paint mill, Collection nets, Scarecrows. The battery slot is described on its own, aiming at the
  rest of a machine does not show its battery.
- Client-side and read-only. Plant timers run on every player's game and the host announces when a plant is
  ready. Cooking and purifying timers run only on the host; other players' games advance their own copy with
  the same rules. Either way another player may see a slightly different percentage than the host until the
  host announces that it is done.

### Auto-replace tool

When the item in your hand is used up, the same item is taken from the inventory and put into the empty hotbar
slot, so you keep working without opening the inventory. Nothing is shown or played: the item is simply back in
your hand.

| Group | When |
|---|---|
| Tools and weapons | A tool or weapon breaks: hooks, axes, fishing rods, spears, the machete, the sword, the bow, the shovel, the paddle, the metal detector, the sweep net, the net gun and others. |
| Food and drinks | The last portion of food or a drink is eaten. |
| Other consumables | The last item of a stack is placed (storages, grills, decorations and other placeables), planted (seeds, saplings), put into a machine (batteries, fuel, raw food, ores, pot and juicer ingredients, trophies, cassettes), used (healing salves, buff items, mystery packages) or thrown (shark bait, throwable items, fireworks). |

- The replacement comes only from the main inventory, including an equipped backpack; other hotbar slots and
  equipment are never touched.
- Several copies of a tool: the most worn one is taken first. Several stacks: the smallest one is taken first.
- **Use other tiers**: when no copy of a broken tool is left, another tier of the same tool is used: the nearest
  better tier first, then the nearest worse one. Only tools of one group replace each other:

  | Group | Tiers, worst to best |
  |---|---|
  | Hooks | plastic, scrap, titanium |
  | Axes | stone, metal, titanium |
  | Fishing rods | fishing rod, metal fishing rod |
  | Melee weapons | wooden spear, metal spear, machete, titanium sword |

  Food, consumables, single-tier tools and items of other mods are replaced only by the same item.
- Only an empty slot is refilled. Water containers and bowls that turn into an empty container after the last sip
  or bite stay in the hand, and armor, flippers, head lights, oxygen bottles and zipline tools in the equipment
  slots are not replaced.
- Dropping, moving or storing an item, and crafting with it, never refills the slot.
- Client-side: only your own inventory changes, the same way as moving the item by hand. Other players do not need
  the mod.

### Raft status HUD

While you stand on the raft, up to three lines appear above the health, hunger and thirst bars, each with the icon of
the matching item. A line is shown only when the raft has the thing it describes.

```
[anchor]  Anchored · 2/3 down
[engine]  3/3 running  ->
[fuel]    72% · 36 min
```

| Line | Shows |
|---|---|
| Anchor | `Anchored` (green) when an anchor is at the bottom, `Dropping` or `Raising` (yellow) while one moves, `Free` otherwise. With several anchors also `2/3 down`. Stationary, advanced and thrown anchors are counted. |
| Engines | `2/3 running`: engines that are switched on and have fuel, out of all engines. Engines switched on with an empty tank add `1 no fuel` (red). An arrow shows where the running engines push the raft, relative to where you look. |
| Fuel | The emptiest tank of the switched-on engines, coloured like a tool's durability, and how long it lasts: `72% · 36 min`. `Empty` when one of them has no fuel left. With every engine switched off, the emptiest tank of all engines without the time. |

- The raft stops as soon as an anchor starts dropping. While an anchor holds the raft, running engines only strain and
  burn no fuel: the engine line reads `Held by anchor` and the fuel line `paused`.
- One engine moves 100 foundations at full speed and 10 more at half speed. **Engine power** adds `Weak` (yellow) when
  the raft is slightly too heavy and `4 needed · Too heavy` (red) when the engines cannot move it at all; such engines
  burn no fuel either (`paused`). Nothing is added while the engines are strong enough.
- `No thrust` means the running engines push in opposite directions and cancel each other out.
- A tank holds 50 units of fuel and a running engine burns one unit a minute, so a full tank lasts 50 minutes.
- Each line, the arrow and the power warnings can be switched off; **HUD size** scales the lines from 75% to 150%.
- Client-side and read-only. Engine levers, fuel and anchors are synchronised by the game, so every player sees the
  same raft; the fuel of other players' engines may differ slightly from the host until the host sends the tank level
  again.
- In Creative the raft never moves, but the HUD still shows what the anchors and engines do.

### Full net indicator

A collection net catches debris until it holds 10 items (basic net) or 15 items (advanced net); after that the debris
floats past. While a net is full, it glows dark red, day and night, until you collect the items.

- The glow is darker and redder than the red preview the game shows where a block cannot be placed, and it does not
  change paint or the outline of the hammer.
- The limit is read from the net itself, so nets whose limit another mod changes are shown correctly; nets without a
  limit never glow. The hook is not a net and is never tinted.
- To see how many items a net holds, aim at it: [Hover info](#hover-info) shows `Items 7/10` or `Full 10/10`.
- Client-side and purely visual, no settings and no key. The host collects the debris and tells every player, so every
  player with the mod sees the same nets glow; other players do not need the mod.

### Scarecrow radius

Seagulls look for crop plots every few minutes and leave a plot alone while a scarecrow stands within 6 m of its centre:
they peck a basic scarecrow instead, one part per attack, and ignore the crops near an advanced one. While the hammer or a
scarecrow is selected and no menu is open, a line on the floor shows this zone around every placed scarecrow: yellow for basic
scarecrows, which break, green for advanced ones. A crop plot whose centre is inside the line is protected.

- The distance is measured from the scarecrow's pole (basic) or body (advanced), so the line is a rectangle with rounded corners,
  about 6.1–6.3 m from the centre of a basic scarecrow and 6.1–6.7 m of an advanced one.
- The scarecrow you are placing gets a half-transparent line in the green or red of the game's preview.
- **Highlight crop plots** lights crop plots that seagulls may attack: green inside a zone, red outside. Empty plots are lit too;
  large crop plots are never attacked and never lit. The preview of a scarecrow counts while it can be placed, so you see which
  plots it would protect.
- The zone is a sphere, so it also reaches crop plots on the floors around the scarecrow: about 5.9 m one floor up, 4.7 m (advanced:
  5.1 m) two floors up, 5.6 m one floor down and 3.9 m two floors down. **Neighbouring floors** draws those lines too, fainter. The crop
  plot highlight always counts every floor.
- Lines are hidden behind walls and blocks like the rest of the world. **Line width** sets their width from 2 to 10 cm.
- The radius is read from the seagulls, so a radius changed by another mod is shown as well.
- Client-side and purely visual. Seagulls are controlled by the host, but every player has the same scarecrows and crop
  plots, so every player sees the same zones; other players do not need the mod.

### Storage sort

Every storage has a sort button next to its name; the game shows it only while you play with keyboard and mouse.
Storage sort lets this button put the items in another order, chosen with **Sort order** on the settings page:

| Order | Result |
|---|---|
| By category | Items grouped as below, each group in alphabetical order. Default. |
| Alphabetical | Every item by its English name. |
| Vanilla | The game's own sort: the order in which the items first appear in the storage, used items at the end. |

| Group | Items |
|---|---|
| Resources | planks, plastic, scrap, rope, nails, ores, ingots, clay, sand, glass, leather, feathers, flowers, paint, biofuel, batteries, trash cubes |
| Tools | hooks, axes, fishing rods and bait, shovel, shears, sweep net, metal detector, binoculars, hammer, paddle, paint brush, compass, shark bait, fireworks |
| Weapons | spears, machete, sword, bow, arrows, net gun and canisters |
| Equipment | armor, flippers, backpacks, head lights, oxygen bottle, zipline tools, hats and heads |
| Food | raw and cooked food, fruit, dishes, spices and milk for the cooking pot, healing salves |
| Drinks | water, smoothies, head broth, empty canteens, cups, bottles, bowls and glasses, the bucket |
| Seeds | seeds, saplings, potatoes and beets |
| Placeables | storages, grills, purifiers, crop plots, furniture, decorations, sails, anchors, engines |
| Other | blueprints, recipe notes, fish trophies, cassettes, trade coins, packages |

- Like the vanilla sort, stacks of the same unused item are merged and filled up to the stack size, and the empty slots end
  up at the end.
- Used items (worn tools, partly eaten food, partly drunk canteens) are never merged. They stay next to the same item,
  after its whole stacks, the least used first.
- Stacks larger than the stack size, left over after lowering the **Stack size** multiplier, stay as they are, in front of
  the other stacks of the same item.
- Unlike the vanilla sort, items are not created anew, so every item keeps all of its own data.
- Items of other mods are grouped by their crafting category and their properties: wearable items go to Equipment, food to
  Food and so on; anything that fits no group goes to Other.
- **Sort after storing** also sorts the storage after the button (or key) that stores the items of your inventory that the
  storage already holds. Off by default.
- Small, medium and large storages, cupboards and fridges are sorted; the trash can is left alone. The gamepad sort action
  uses the same order.
- The storage is locked to you while it is open, and the game sends its slots to the host when you close it, the same way
  as after the vanilla sort. Other players do not need the mod.

### Pinned recipes

Select a recipe in the crafting menu and press **Pin to HUD** under its box to keep it on the screen while you gather the
ingredients. Up to three recipes can be pinned; while three are pinned the button reads `3/3 pinned` and is inactive, and on a
pinned recipe it reads **Unpin**. The recipes appear above the health, hunger and thirst bars (above the
[Raft status HUD](#raft-status-hud)), in the order they were pinned:

```
[metal axe]  Metal axe  ×2
             [plank] 12/4  [scrap] 3/6  [rope] 2/2
```

- `×2` is how many times the recipe can be crafted now, green; `×0` is red. A recipe that crafts several items at once still
  counts crafts, not items.
- Every ingredient shows the amount you have and the amount one craft needs, like the crafting menu; the amount you have is red
  while it is not enough for one craft. An ingredient that accepts several items (any of them will do) adds them all up and
  shows the icon of the one you have most of.
- Items are counted where the crafting menu takes them from: the hotbar, the inventory and the backpack, plus the storage that is
  open at the moment. Storages nearby are not counted yet.
- A recipe with skins is pinned with the skin selected when you pin it, with that skin's icon and cost.
- In Creative, where crafting costs nothing, the real amounts are still shown.
- The pinned recipes are kept in the config and are the same in every world. A pinned recipe that is not learned in the current
  world, or comes from a mod that is not loaded, stays pinned but is not shown.
- **Show ingredients** switches the ingredient lines off, leaving the name and `×2`. **Unpin after crafting** (off by default)
  unpins a recipe as soon as it has been crafted once with the craft button; crafting another skin of it keeps it pinned.
  **HUD size** scales the lines from 75% to 150%.
- Client-side and read-only: nothing is crafted, moved or sent. Other players do not need the mod.

### In-game menu

Pause menu → **Mods** lists every mod known to RaftModLoader with its state. QoL Changes can be
switched off as a whole from there, and **Settings** opens its page where each feature has its own
toggle. Click the arrow or the name of a feature to show or hide its options; all features start
collapsed and the expanded ones are remembered. Changes apply immediately and are saved to the config
file. Esc or the close button goes one level back.

### Disabling a feature

Stacks larger than the vanilla limit are saved as they are and are not clamped by the game on load.
After disabling Stack size they stay intact: you can take items out of them, but cannot add more until
they drop below the vanilla limit.

Durability indicator, Hover info and Raft status HUD only add text to the game's UI and leave nothing behind when
disabled. Full net indicator removes the glow from every net when disabled. Scarecrow radius removes its lines and the
light of crop plots as soon as the hammer or scarecrow is put away or the feature is disabled.
Auto-replace tool only moves items that you could move by hand; disabling it changes nothing. Storage sort only changes
the order of the sort button; disabled, the button sorts the vanilla way again. Disabling Pinned recipes removes its HUD lines
and the pin button; the pinned recipes stay in the config for the next time it is enabled.

## Configuration

Settings are stored in `<Raft>\mods\ModData\qol-changes\config.json`, created on first start:

```json
{
  "enabled": true,
  "stackSize": {
    "multiplier": 5,
    "enabled": true
  },
  "durabilityIndicator": {
    "tools": "Percent",
    "water": "Percent",
    "food": "Percent",
    "batteries": "Percent",
    "showAtFullDurability": false,
    "enabled": true
  },
  "hoverInfo": {
    "battery": "Percent",
    "plants": true,
    "purifiers": true,
    "engineFuel": true,
    "cooking": true,
    "smelters": true,
    "collectionNets": true,
    "scarecrows": true,
    "enabled": true
  },
  "autoReplace": {
    "tools": true,
    "useOtherTiers": true,
    "food": true,
    "consumables": true,
    "enabled": true
  },
  "raftStatus": {
    "anchor": true,
    "engines": true,
    "thrustDirection": true,
    "enginePower": true,
    "fuel": true,
    "scale": 100,
    "enabled": true
  },
  "fullNet": {
    "enabled": true
  },
  "scarecrowRadius": {
    "withHammer": true,
    "withScarecrow": true,
    "neighbouringFloors": false,
    "cropPlots": true,
    "lineWidth": 4,
    "enabled": true
  },
  "storageSort": {
    "order": "ByCategory",
    "sortAfterStoring": false,
    "enabled": true
  },
  "pinnedRecipes": {
    "recipes": [],
    "showIngredients": true,
    "unpinAfterCrafting": false,
    "scale": 100,
    "enabled": true
  },
  "menu": {
    "expandedFeatures": []
  }
}
```

The top-level `enabled` switches the whole mod off while keeping each feature's own settings.
The in-game menu is the intended way to change settings; when editing the file by hand, do it while
the game is closed or reload the mod in the RML mod manager. `multiplier` is clamped to 1–5.
Durability modes and the hover info `battery` format are `Off`, `Percent`, `Remaining` and `RemainingOfMax`.
The raft status `scale` is a percentage, rounded to 75, 100, 125 or 150. The scarecrow `lineWidth` is in centimetres,
clamped to 2–10. The storage sort `order` is `Vanilla`, `ByCategory` or `Alphabetical`. Pinned `recipes` are internal item names
(`Axe`), at most three; the pinned recipes `scale` is rounded like the raft status one. `menu.expandedFeatures` lists
the features whose options are shown in the menu. An invalid file is left
untouched and defaults are used until it is fixed.

## Installation

1. Install [RaftModLoader](https://www.raftmodding.com/loader) and start Raft through it once.
2. Download `QoLChanges.rmod` from [Releases](../../releases) and put it into `<Raft>\mods`.
3. Enable the mod in the RML mod manager.

## Development

Architecture, RML specifics and known pitfalls are described in [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).

### Requirements

- Windows with Raft (Steam) and RaftModLoader
- Visual Studio 2022+ or Rider with the .NET SDK (any version that builds SDK-style `net48` projects)

No .NET Framework targeting pack is needed: the project compiles against the game's own
`mscorlib` from `lib\Managed`.

### First-time setup

```powershell
git clone https://github.com/spehereuz/RaftQoLChanges.git
cd RaftQoLChanges
powershell -ExecutionPolicy Bypass -File tools\Sync-Libs.ps1
```

`Sync-Libs.ps1` finds Raft through Steam, copies the assemblies into `lib\` and creates
`local.props` with your paths. Both are ignored by git. Re-run it after every game or RML update.
To set paths manually, copy `local.props.example` to `local.props` and edit it.

### Build

```powershell
dotnet build -c Release
```

or **Build** in Visual Studio. The solution builds two mods and copies both into `<Raft>\mods`
(set `DeployToGame` to `false` in `local.props` to disable):

- `bin\<Config>\QoLChanges.rmod` — the mod itself;
- `DevTools\bin\<Config>\QoLDevTools.rmod` — console commands for testing, never published
  (see [Testing in game](docs/DEVELOPMENT.md#testing-in-game)).

RML compiles the `.cs` files from the `.rmod` itself when the game starts. The local build is
there for IntelliSense, compile-time checks against the real game assemblies and packaging.

### Layout

```
src/                 everything here is packed into the .rmod
  modinfo.json       RML metadata
  QoLChangesMod.cs   entry point (HMLLibrary.Mod)
  QoLRuntime.cs      owns config and features; every change from the menu goes through it
  ModConfig.cs       root of config.json, one section per feature
  Core/              feature base class (own Harmony instance per feature), declarative feature
                     settings, JSON config, logging
  Features/<Name>/   one folder per feature: feature class, config section, Harmony patches
  GameState/         game state read by several features (raft anchors, engines and fuel, collection nets,
                     scarecrow zones, items available for crafting)
  UI/                pause menu "Mods" button and window; widgets are cloned from Raft's own
                     pause and settings menus at runtime, so no game assets are shipped; the
                     shared HUD panel above the stat bars
  icon.png           512x512
  banner.png         660x200
DevTools/            QoL Dev Tools: separate development-only mod with its own src/ and modinfo.json
build/RaftMod.targets  shared build of both mods: references, Unity Mono runtime, .rmod packaging
tools/Sync-Libs.ps1  restores lib\ from the Raft and RML folders
lib/                 game and RML assemblies (not committed)
```

### Debugging

- `F10` in game opens the RML console with mod logs.
- Unity log: `%USERPROFILE%\AppData\LocalLow\Redbeet Interactive\Raft\Player.log`.
- Browse game code with [dnSpyEx](https://github.com/dnSpyEx/dnSpy) on `lib\Publicized\Assembly-CSharp.dll`.

### Release

1. Bump `version` in `src/modinfo.json` and `<Version>` in `QoLChanges.csproj`.
2. `dotnet build -c Release`
3. Tag `vX.Y.Z` and attach `bin\Release\QoLChanges.rmod` to a GitHub release. `QoLDevTools.rmod`
   is never attached.

## License

[MIT](LICENSE). Raft and RaftModLoader assemblies are property of their respective owners and are
not distributed with this repository.
