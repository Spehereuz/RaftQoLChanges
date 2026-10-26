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

### In-game menu

Pause menu → **Mods** lists every mod known to RaftModLoader with its state. QoL Changes can be
switched off as a whole from there, and **Settings** opens its page where each feature has its own
toggle and options. Changes apply immediately and are saved to the config file. Esc or the close
button goes one level back.

### Disabling a feature

Stacks larger than the vanilla limit are saved as they are and are not clamped by the game on load.
After disabling Stack size they stay intact: you can take items out of them, but cannot add more until
they drop below the vanilla limit.

## Configuration

Settings are stored in `<Raft>\mods\ModData\qol-changes\config.json`, created on first start:

```json
{
    "enabled": true,
    "stackSize": {
        "enabled": true,
        "multiplier": 5
    }
}
```

The top-level `enabled` switches the whole mod off while keeping each feature's own settings.
The in-game menu is the intended way to change settings; when editing the file by hand, do it while
the game is closed or reload the mod in the RML mod manager. `multiplier` is clamped to 1–5. An
invalid file is left untouched and defaults are used until it is fixed.

## Installation

1. Install [RaftModLoader](https://www.raftmodding.com/loader) and start Raft through it once.
2. Download `QoLChanges.rmod` from [Releases](../../releases) and put it into `<Raft>\mods`.
3. Enable the mod in the RML mod manager.

## Development

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

or **Build** in Visual Studio. Each build produces `bin\<Config>\QoLChanges.rmod` and copies it
into `<Raft>\mods` (set `DeployToGame` to `false` in `local.props` to disable).

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
3. Tag `vX.Y.Z` and attach `bin\Release\QoLChanges.rmod` to a GitHub release.

## License

[MIT](LICENSE). Raft and RaftModLoader assemblies are property of their respective owners and are
not distributed with this repository.
