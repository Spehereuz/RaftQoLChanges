# Development guide

Technical notes for working on QoL Changes. Setup, build and release steps are in the [README](../README.md).

## Target environment

| | |
|---|---|
| Game | Raft 1.1.01, Unity 2021.3.45f2, Mono |
| Loader | RaftModLoader 2.8.x (`%APPDATA%\RaftModLoader`) |
| Harmony | 2.3.6 (`binaries\0Harmony.dll`) |
| Language | C# 7.3 |

## How RML loads a mod

- A `.rmod` is a zip of `src\`. RML compiles the `.cs` files itself with Roslyn when the mod is loaded;
  the DLL built by the project is only used for compile checks. Subfolders are compiled.
- Highest supported language version is **C# 7.3**: no nullable reference types, switch expressions,
  `??=`, file-scoped namespaces, records, `init`, target-typed `new`. The project enforces this with
  `<LangVersion>7.3</LangVersion>`.
- Compile references: every DLL in `Raft_Data\Managed` **except** `Newtonsoft.Json.dll`, plus
  publicized `Assembly-CSharp`, `PlayFab`, `FMODUnity`, `com.rlabrecque.steamworks.net` and RML's
  `AssemblyLoader` (`HMLCoreLibrary.dll`), which also bundles Newtonsoft.Json.
- RML prepends `using RaftModLoader; using HMLLibrary; using Semih_Network = Raft_Network;` to every
  file. Avoid declaring types that clash with names in those namespaces.
- Assembly-CSharp is publicized for mods, so private members compile. Private fields are still read
  through `AccessTools.FieldRefAccess` in this project, so the code does not depend on that.
- `Mod.DataFolder` is `<Raft>\mods\ModData\<slug>`; the slug is set in `modinfo.json`.
- RML calls `Start()` on load and `OnModUnload()` on unload; both can happen in the main menu or in a world.

## Known pitfalls

- **`UnityEngine.JsonUtility` silently drops nested classes** of mod types (the assembly is loaded
  from memory and unknown to Unity's serializer). Use `Core/JsonConfigFile` (Newtonsoft.Json).
- **Do not Harmony-patch trivial getters** such as `ItemInstance_Inventory.StackSize`: Mono inlines
  them into callers and the patch is bypassed. Change data or patch a non-trivial caller instead.
- **Item instances copy their settings.** `ItemInstance` clones `Item_Base.settings_*` on creation;
  the game reads both the base item and the instance copy.
- **Save format:** slot amounts are stored as `Int16` (max 32767) and are not clamped on load.
- **Mono and `UnityEngine.Object` null checks:** use `== null` / `!= null`, not `??` or `?.`.
- **UI callbacks:** never destroy the element that raised a callback inside it; request a rebuild
  and perform it in `LateUpdate` (see `UI/ModMenu`).
- **Visual Studio** keeps its own copy of open files. If files are changed outside the editor,
  accept the reload prompt, otherwise the next build saves the stale copy over them.

## Architecture

```
QoLChangesMod      RML entry point: creates QoLRuntime and ModMenu, tears both down on unload
QoLRuntime         owns ModConfig + FeatureManager; every change from the menu goes through it and is saved
ModConfig          root of config.json: master "enabled" + one section per feature
Core/
  Feature          base class: id, display name, config, own Harmony instance, Enable/Disable/Reapply
  FeatureConfig    base config section ("enabled"); override Normalize() to clamp values
  FeatureSetting   declarative options (IntSliderSetting) the settings page is generated from
  FeatureManager   registry, applies config, honours the master switch
  JsonConfigFile   load/merge/normalize/save with Newtonsoft.Json, atomic write
  ModLog           "[QoL Changes] " prefixed Unity log
Features/<Name>/   feature class, its config section, Harmony patch classes
UI/                pause menu "Mods" button, window and pages
```

Settings always persist: every change made in the menu is written to `config.json` immediately and
restored on the next load.

### Adding a feature

1. Create `Features/<Name>/` with a `<Name>Config : FeatureConfig` (`[Serializable]`, public fields,
   `Normalize()` clamps values) and a `<Name>Feature : Feature`.
2. Implement `OnEnable()` / `OnDisable()`. `OnDisable()` must revert everything and tolerate a
   partial enable. Apply Harmony patches with `Patch(typeof(SomePatchClass))`; they are removed
   automatically on disable.
3. Expose options with `GetSettings()`. Add a new `FeatureSetting` type plus a row builder in
   `UI/RaftUiRows` and a case in `UI/QoLSettingsPage` when the existing ones are not enough.
4. Add the section to `ModConfig` (field + `Normalize()`), register the feature in `QoLRuntime`.
5. Document it in the README feature table and config example.

### Multiplayer rules

- Purely visual or local-inventory features need no networking.
- Storages are host-synchronised: when a storage closes its full slot list is sent
  (`Message_Storage_Close`) and applied without clamping; `StorageManager.OpenStorage/CloseStorage`
  lock a storage to one player. Changes to storages must go through that open/close cycle.
- Keep `requiredByAllPlayers` false unless a feature truly needs every player to run it.

## Game UI reference

Templates are cloned at runtime from the game scene (`UI/RaftUiTemplates`), so no game assets are
distributed. Game scripts are stripped from clones (`UI/RaftUi.Clone`), keeping `ButtonSound`,
`TextChangeButton` and `UiMagnet`.

```
_CanvasPausMenu (PauseMenu)
  Button holder (VerticalLayoutGroup, fixed height; PauseMenu.pausePanel == buttonHolderPanel)
    BrownBackground (ignoreLayout), BrownBackground_Unstuck (ignoreLayout)
    HostText, WorldCode, ReturnToGame / Lobby / Save Game / OptionsMenu / Report issue /
    Main menu / ExitGame buttons, GamepadSelectBack (ignoreLayout)
_SCP_3_Settings/_CanvasOptionMenu (Settings.optionsCanvas)
  OptionMenuParent: BrownBackground, CloseButton, OptionsText, LB, TabContainer, Divider,
                    TabContent/{General, Controls, Graphics, Audio, ...}, RB, GamepadBack
  General/Viewport/Content rows: Language (Dropdown), GDPR (button), Quick build (Toggle), ...
  Audio/Viewport/Content rows: MasterVolume (UISlider: Text, Slider, Slider value text), ...
```

- `PauseMenu.Update` closes the menu on cancel unless `ignoreCancel` is set; the flag is reset at
  the end of every `Update`.
- `CanvasHelper.ActiveMenu == MenuType.PauseMenu` while the pause menu is open.

## Useful game facts

| Area | Types and members |
|---|---|
| Items | `ItemManager.GetAllItems()`, `Item_Base.settings_Inventory`, `ItemInstance`, `Slot.RefreshComponents()` |
| Inventory | `PlayerInventory` (`secondInventory`, `GetItemCount`, `RemoveCostMultiple*`), `Inventory.Sort()`, `Inventory.SortPlayerInventoryIntoStorage()`, `Hotbar` |
| Storage | `Storage_Small.GetInventoryReference()`, `StorageManager` (`OpenStorage`, `CloseStorage`, `currentStorage`) |
| Crafting | `CraftingMenu`, `CostCollection` |
| Raft | `Anchor_Stationary`, `EngineControls`, `MotorWheel`, `Sail`, `SteeringWheel` |
| World | `ItemCollector` (`maxNumberOfItems`), `Scarecrow`, `Plant`, `Cropplot`, `Battery`, `Tank`, `Electric_Purifier` |
| Tools | `Hook`, `ChargeMeter`, `Throwable` |
| Notebook / research | `NoteBook`, `NoteBookUI`, `ResearchTable`, `Inventory_ResearchTable` |
| Input | Input System actions; vanilla storage actions `SortPlayerIntoChest`, `SortChest` |
