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
- The local build (`build/RaftMod.targets`, shared by `QoLChanges.csproj` and
  `DevTools/QoLDevTools.csproj`) mirrors this for compile checks and packs `src\` into the `.rmod`.
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
- **`Resources.FindObjectsOfTypeAll` also returns prefab assets.** Filter by
  `gameObject.scene.IsValid()` before adding or removing children, otherwise prefabs get modified.
- **Patches on hot game paths** (`Slot.RefreshComponents` runs after every inventory change) must not
  throw: catch, log once and keep the game running (see `DurabilityLabels.Refresh`).
- **Text added to game UI** that is removed and recreated in the same frame (feature reapply) must be
  destroyed with `DestroyImmediate`, otherwise lookups by name still find the old object.
- **Visual Studio** keeps its own copy of open files. If files are changed outside the editor,
  accept the reload prompt, otherwise the next build saves the stale copy over them.

## Architecture

```
QoLChangesMod      RML entry point: creates QoLRuntime and ModMenu, tears both down on unload
QoLRuntime         owns ModConfig + FeatureManager; every change from the menu goes through it and is saved
ModConfig          root of config.json: master "enabled" + one section per feature + "menu" (UI/MenuConfig:
                   features expanded on the settings page)
Core/
  Feature          base class: id, display name, config, own Harmony instance, Enable/Disable/Reapply
  FeatureConfig    base config section ("enabled"); override Normalize() to clamp values
  FeatureSetting   declarative options the settings page is generated from:
                   IntSliderSetting (slider), ToggleSetting (toggle), ChoiceSetting (dropdown, value = index)
  FeatureManager   registry, applies config, honours the master switch
  JsonConfigFile   load/merge/normalize/save with Newtonsoft.Json, atomic write
  ModLog           "[QoL Changes] " prefixed Unity log
  Percent          rounding of shown percentages: remaining (floor, at least 1% above zero),
                   progress (floor, at most 99% until done)
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

## Testing in game

Most features are tested in a **Creative** world. Creative has no item menu: it knows every recipe
and crafting costs nothing, but items without a recipe (cooked food, filled canteens, loot) still have
to be obtained the usual way. `DevTools/` is a second mod, **QoL Dev Tools**, with console commands
that give any item and set up the situations a survival world needs hours for. It is built and deployed together with QoL Changes (both projects
import `build/RaftMod.targets`), but is never attached to releases. It changes game state directly,
so it is only for testing; disable it in the RML mod manager for normal play.

Open the console with `F10`. Commands work on the local player in a loaded world:

| Command | Effect |
|---|---|
| `qoldev.find <text>` | Lists items whose internal or English name contains the text: `Cooked_Catfish - Cooked catfish`. |
| `qoldev.give <item> [amount]` | Adds items (default 1) by internal name or exact English name, as many as fit; stacks are never overfilled and nothing is dropped. |
| `qoldev.dur <uses\|percent%>` | Sets the durability of the item in the selected hotbar slot. |
| `qoldev.dur.all <uses\|percent%\|random>` | Same for every item with a durability bar in the hotbar, inventory, equipment and the open storage. `random` picks 1..max per item. |
| `qoldev.wear <on\|off>` | Creative only: tools lose durability as in Normal mode. Reverted on `off`, when returning to the main menu and when the mod is unloaded. |

- Values are uses (`44`) or a share of max uses (`37%`); they are clamped to the item's max.
- Durability is changed through `Slot.SetUses`, like the game does: `0` breaks a tool, turns a
  canteen into an empty one and eats one piece of a food stack.
- Storage slots changed while the storage is open are synchronised when it is closed, as usual.
- Creative tool settings live in the shared `SO_GameModeValue` asset (`toolVariables`:
  `areToolsIndestructible = true`, `toolDurabilityLossMultiplier = 0`); `qoldev.wear` swaps them and
  restores the originals.

- `qoldev.give` reads the last argument as the amount when it is a number; for items whose English
  name ends with a number use the internal name.
- Vanilla `Inventory.AddItem(string, int)` is not used: it puts the whole amount into one empty slot
  regardless of the stack size and drops everything when no slot is free.

Hover info check: build a simple and an advanced purifier, an electric purifier and a few crop plots. Give
batteries (`qoldev.give Battery`, `qoldev.give Battery_Advanced`), set their charge with `qoldev.dur 37%`
while selected and put them into machines; the text and its colour must match the inventory text of the same
battery, also after switching the battery format; in the inventory and storages the same batteries show their
charge as durability text. Plant seeds and water only some of them; aim at each plant (also its centre) with
empty hands and with an item, and at the soil around them for the summary of the plot. Pour salt water into
the advanced purifier several times with fuel burning to see mixed portions. Cook on every grill, both
smelters, the paint mill, the pot and the juicer; aim at single items on multi-item grills, let the fire go
out and remove the battery to see `no fuel` / `no power`. For multiplayer, a second player compares the
cooking and purifying percentages with the host; nothing is sent over the network.

Auto-replace check: in Creative run `qoldev.wear on`, then `qoldev.give Hook_Plastic`, `qoldev.give Hook_Scrap`,
`qoldev.give Hook_Titanium`, `qoldev.give Axe 3`, `qoldev.give Axe_Titanium`. Keep the scrap hook and one axe in the hotbar
and move everything else into the inventory. Select the scrap hook, `qoldev.dur 1` and use it until it breaks: the titanium
hook (nearest better tier) takes its place; break that one too: the plastic hook comes. Run `qoldev.dur.all random`, note
the durability of the two axes in the inventory and break the selected axe: the more worn metal axe comes; with no metal
axe left, the titanium one. Give two stacks of `Cooked_Potato` and of a seed, eat or plant the last item of the selected
stack: the smaller stack from the inventory moves in. Place the last storage or grill of a stack, put the last battery into
a purifier, throw the last shark bait. Drink the last sip from a canteen: the empty canteen stays. Drop the last item with
`Q`, move it with the mouse, put it into a storage or craft with it: the slot stays empty. Switch off each group and
**Use other tiers** and repeat one case of it.

Durability indicator check: in Creative run `qoldev.give Axe`, `qoldev.give Canteen_Water`,
`qoldev.give Cooked_Catfish`, `qoldev.give Flipper`, `qoldev.give HeadLight`, then
`qoldev.dur.all random`, then compare the text with the bars; `qoldev.dur 1` and
`qoldev.dur 0` cover the near-broken and broken cases, `qoldev.wear on` and a few hits with a tool
cover the update after real use.

## Game UI reference

Templates are cloned at runtime from the game scene (`UI/RaftUiTemplates`), so no game assets are
distributed. Game scripts are stripped from clones (`UI/RaftUi.Clone`), keeping `ButtonSound`,
`TextChangeButton` and `UiMagnet`; serialized `Button`, `Toggle`, `Slider` and `Dropdown` listeners
are dropped. Row templates: "Quick build" (toggle), "MasterVolume" (slider), "Language" (dropdown,
`GeneralSettingsBox.dropdown_language`; its `Localize` and `SetLanguageDropdown` are stripped). The
dropdown's `Arrow` image (points down) is the expand arrow of feature headers, turned 90° when collapsed.

- Native rows carry a background `Image` on the row root that is enabled on some rows and disabled
  on others ("MasterVolume" has none). `RaftUiRows` sets it explicitly: `RowStyle.Header` (feature or
  mod) has the background, `RowStyle.Option` has none and is indented by 60 px.
- Header row: label (left edge at x 8.4, shifted 30 px for the arrow), toggle at 45 px from the right
  edge. A transparent button covers the row except the right 80 px, so the name and the arrow expand
  the feature and the toggle stays separate.

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

### Item slots

```
_CanvasGame_New/InventoryParent
  Hotbar/Hotslot parent             10 x Slot          (GridLayoutGroup, cell 70x70)
  Inventory_Player/SlotParent       30 x Slot          (cell 70x70)
    Equipslot parent                 5 x Slot_Equip    (cell 70x70)
  Inventory_ResearchTable/...                          (cell 60x60)
Slot_Hotbar / Slot_Inventory_*: ItemImage, AmountText, Slider{Background, Fill Area/Fill}
```

- `AmountText` (`Slot.textComponent`): top-left corner, 74.1x31.5 at (39.9, -17.5), font 24,
  UpperLeft, `Shadow` (2, -2). Shown only when `Amount > 1`. In equipment slots it is a copy scaled by
  ~0.137 with font 200 and shadow (15, -13.63), so neither its geometry nor its shadow offset can be
  copied as is: the offset has to be multiplied by that scale.
- Durability bar (`Slot.sliderComponent`): shown when `Item_Base.MaxUses > 1`, value
  `ItemInstance.NormalizedUses`, fill colour `Item_Base.BarGradient.Evaluate(value)`. Tools use a
  red → yellow → green gradient, water containers flat cyan, food flat yellow.
- Storage slots are not part of the scene, they are created at runtime; patching
  `Slot.RefreshComponents` covers them without looking for their prefab.

### Interaction prompts and hover info

```
_CanvasGame_New
  DisplayTextManager    (DisplayTextManager) centred on the crosshair
    DisplayTextMiddle   displayTexts[0]: prompt above the crosshair, (0, 56.1), pivot (0.5, 0); Text, Shadow,
                        Localize, ContentSizeFitter, DisplayText; children: key hint, gamepad icons
  DisplayTextBottom     DisplayText 1..3 = displayTexts[1..3]: hints at the bottom of the screen
```

- Objects write their prompt from `IRaycastable.OnIsRayed` every frame, usually with `clearAllTexts`, and hide
  it with `HideDisplayTexts()`; writing into these texts would fight with the game. Hover info clones
  `DisplayTextMiddle` without its children and game scripts and places the copy mirrored below the crosshair.
- `Pickup.Update` of the local player calls `RaycastForRayInteractables()` once per frame
  (`Helper.FindInteractable`, `Player.UseDistance` x 1.1). It is skipped while a menu is open. The result is a
  `RaycastInteractable`; its `RaycastableObjects` are the `IRaycastable` components on it plus those added by
  `RaycastInteractable_Extension`. `OnIsRayed` implementations also check
  `Helper.LocalPlayerIsWithinDistance(transform.position, Player.UseDistance)`.
- Plants are not interactables: `PlantManager.HarvestPlants` finds them with
  `Helper.HitAtCursor(out hit, Player.UseDistance, LayerMasks.MASK_Item)` and tag `Plant` and shows the
  harvest prompt itself; aimed at a plant, the interactable raycast finds neither the plant nor its crop plot.
  `Cropplot.OnIsRayed` asks for water only while an item is selected in the hotbar.
- Plants: `Plant.GetGrowTimer()` / `growTimeSec` (`growTime` minutes x 60), `FullyGrown()`. A slot's
  `PlantationSlot.hasWater` is set once when watered and cleared only when the plant is removed;
  `Cropplot.Update` grows plants of slots that `CanGrow` (planted and watered).
- Cooking stands and fuel purifiers: `Block_CookingStand.cookingSlots`; a slot cooks while busy,
  `cookTimer` up to `CookingTime x cookTimeMultiplier`, then `IsComplete`. `FakeSlot` slots are occupied by an
  item of another slot. Simple purifier: 1 slot, advanced: 5. Grills: simple 1, advanced 3, electric 6;
  smelter 1, electric smelter 3, paint mill 5. Kind of stand by `buildableItem.UniqueName`
  (`Placeable_CookingStand_Food_*`, `*_Smelter*`, `*_PaintMill`, `*_Purifier_*`).
- Only the host advances cooking timers: `Block_CookingStand.Update` calls `UpdateSlot()` while
  `IsUsingEnergySource()` (fuel burning / battery `On`), `CookingTable.Update` adds to `cookTimer` while
  `IsCooking()` (recipe without portions; pot: fuel burning, juicer: battery has charge). Other players get
  the timers on join and the completion from the host. Hover info advances its own copies on those players
  (`CookingTimers`, postfixes on both `Update` methods) and never writes to the game objects.
- Slot colliders (item layer) are enabled only when the item is done (`CookingSlot.OnComplete`), for
  collecting; the raw and cooked models of a slot are `CookItemConnection.rawItem` / `cookedItem`.
- Cooking pot and juicer: `CookingTable.CurrentRecipe` (`SO_CookingTable_Recipe`: `Result`, `CookTime`,
  `Portions`), `CookTimer`, `Portions` left after cooking.
- Electric purifier pumps while `battery.CanGiveElectricity`, battery uses >= `batteryDrainPerPump` and the
  tank is not full (`Tank.CurrentTankAmount`, `maxCapacity`).
- Battery slots: `Battery.batteryInstance` (uses / `BaseItemMaxUses`). The slot lights switch colour at
  `greenLimit`, `yellowLimit`, `orangeLimit` and turn yellow early; hover info uses the item's `BarGradient`
  instead, like the durability text in the inventory.

## Using up items

Tools, food and consumables leave the hotbar in different ways; Auto-replace patches each of them instead of watching
slots, so moving items by hand never looks like using them up.

- Tools and weapons: `PlayerInventory.RemoveDurabillityFromHotSlot` → `RemoveStacksFromSlot(Slot, int)` (also used for
  equipment with its own slots) → `Slot.IncrementUses(-n)`. Creative skips it (`areToolsIndestructible`).
- `Slot.IncrementUses`: when the uses run out one item of the stack is removed; the last one is replaced by
  `ItemAfterUse` (empty canteen, bowl, glass) or the slot is emptied, then a player slot calls
  `Hotbar.ReselectCurrentSlot()`.
- Eating and drinking: `UseItemController.Use` → coroutine `ConsumeStacks(int, float)`, which captures the selected slot
  and calls `IncrementUses` after the animation delay.
- `Inventory.RemoveItem(string, int)` removes from the **first** slots holding the item (hotbar first), not from the
  selected one. It is used by seeds (`PlantManager.PlantSeed`), fuel (`Block_CookingStand.IncrementFuel`,
  `FuelNetwork.AddFuel`), salves, buffs, mystery packages, thrown items, fireworks, trophies, cassettes, but also by
  crafting (`RemoveCostMultiple`), trading, quests and `SortPlayerInventoryIntoStorage`.
- `PlayerInventory.RemoveSelectedHotSlotItem(int)`: batteries (`Battery.Insert`), shark bait
  (`SharkBaitThrower.OnHitWater`), containers filled at tanks and purifiers. Placeables: `BlockCreator.CreateBlock` takes
  one item from `hotslotIndex`.
- `Inventory.RemoveItemUses` is used only by the cooking pot / juicer (`CookingTable_Slot.InsertItem`) and tanks.
- Slot layout of `PlayerInventory.allSlots`: hotbar first (`hotslotCount`), then the inventory (`SlotType.Normal`) and the
  backpack (`SlotType.Backpack`, usable only while `backpackEquipped`); equipment slots are `Slot_Equip`.
- `Inventory.MoveSlotToEmpty` plays the UI move sound; Auto-replace moves items with `Slot.SetItem` (clones the instance)
  to stay silent. Player inventories are local: moving items sends nothing, `ReselectCurrentSlot` tells other players
  what is in the hand. The local player is `ComponentManager<Network_Player>.Value`.

## Useful game facts

| Area | Types and members |
|---|---|
| Items | `ItemManager.GetAllItems()`, `Item_Base.settings_Inventory`, `ItemInstance`, `Slot.RefreshComponents()` |
| Durability | `Item_Base.MaxUses`, `Item_Base.BarGradient`, `ItemInstance.Uses` / `NormalizedUses`, `Slot.IncrementUses()`; `settings_consumeable.FoodForm` (`Fluid` = drink, `Solid` = food) |
| Inventory | `PlayerInventory` (`secondInventory`, `GetItemCount`, `RemoveCostMultiple*`, `GetSelectedHotbarSlot`), `Inventory.Sort()`, `Inventory.SortPlayerInventoryIntoStorage()`, `Hotbar` (`GetSelectedSlotIndex`, `ReselectCurrentSlot`) |
| Storage | `Storage_Small.GetInventoryReference()`, `StorageManager` (`OpenStorage`, `CloseStorage`, `currentStorage`) |
| Crafting | `CraftingMenu`, `CostCollection` |
| Raft | `Anchor_Stationary`, `EngineControls`, `MotorWheel`, `Sail`, `SteeringWheel` |
| World | `ItemCollector` (`maxNumberOfItems`), `Scarecrow`, `Plant`, `Cropplot`, `Battery`, `Tank`, `Electric_Purifier` |
| Interaction | `Pickup.RaycastForRayInteractables`, `RaycastInteractable`, `IRaycastable.OnIsRayed`, `DisplayTextManager`, `Player.UseDistance` |
| Tools | `Hook`, `ChargeMeter`, `Throwable` |
| Notebook / research | `NoteBook`, `NoteBookUI`, `ResearchTable`, `Inventory_ResearchTable` |
| Input | Input System actions; vanilla storage actions `SortPlayerIntoChest`, `SortChest` |
