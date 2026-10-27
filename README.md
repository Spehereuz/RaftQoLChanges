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
| Durability indicator | `durabilityIndicator` | on, `Percent` for every group | Writes the durability of items that have a durability bar on their slot: `73%`, `88` or `88/120`, coloured like the bar. |

### Durability indicator

The text appears in the top-left corner of the slot, where the amount of a stack is shown, in the
hotbar, inventory, equipment slots and storages. Items are split into three groups, each with its
own mode (Off, Percent, Remaining, Remaining / max):

| Group | Items | Unit |
|---|---|---|
| Tools and equipment | axes, hooks, weapons, fishing rods, armor, flippers, head lights, oxygen bottle and other tools | uses |
| Water containers | canteens and bottles, fresh or salt water | sips |
| Food portions | cooked catfish, cooked salmon, watermelon | portions |

- Items from other mods are grouped by their properties: drinkable → water, edible → food, anything
  else → tools.
- Batteries are not shown: their charge is planned for the hover tooltip of placed batteries.
- A stack of more than one item keeps showing its amount; the indicator appears when one item is left.
- Items that have not been used yet show nothing unless **Show at full durability** is on.
- Percent is rounded down, so a used item never shows `100%` and a nearly broken one shows at least `1%`.

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

Durability indicator only adds text to the inventory UI and leaves nothing behind when disabled.

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
    "showAtFullDurability": false,
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
Durability modes are `Off`, `Percent`, `Remaining` and `RemainingOfMax`. `menu.expandedFeatures` lists
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
  UI/                pause menu "Mods" button and window; widgets are cloned from Raft's own
                     pause and settings menus at runtime, so no game assets are shipped
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
