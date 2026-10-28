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
GameState/         read-only views of game state shared by features: RaftAnchors, RaftEngines (engines, power, fuel),
                   ItemNets (fill state of collection nets), Scarecrows (zones, parts, sitting seagulls)
UI/                pause menu "Mods" button, window and pages; HudPanel (shared lines above the stat bars,
                   one section per feature), StatusText (coloured rich-text values for hover info and the HUD),
                   ToolDurabilityColor (red-yellow-green of tool bars for fuel and scarecrow parts)
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
| `qoldev.fuel <units\|percent%>` | Host only: sets the fuel of every engine on the raft (a tank holds 50 units) and sends it to other players like the game does. |
| `qoldev.scarecrow [parts]` | Host only: removes that many parts (default 1) of the basic scarecrow under the crosshair through `Scarecrow.DamageScarecrow`, like a pecking seagull, sent to other players like the game does. After the last part the game removes the scarecrow 3 seconds later. |
| `qoldev.net <count\|full\|clear>` | Host only: empties the collection net under the crosshair and fills it with that many pieces of debris (`full`: up to its limit, `clear`: empty), sent to other players like the game does. Nothing goes into the inventory. |
| `qoldev.chest [slots]` | Empties the open storage and puts random items into that many random slots (default: 3/4 of the slots): items of every Storage sort group, several part stacks of the same item, used tools and drinks, partly eaten food and sometimes one stack above the stack size. Synchronised when the storage is closed. |

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
out and remove the battery to see `no fuel` / `no power`. Aim at the body, the tank and both levers of an engine:
`Fuel 50%` while it is off, `Fuel 50% · 25 min` while it runs, `· paused` with an anchor down, `Fuel Empty` after
`qoldev.fuel 0`; with `qoldev.fuel 30%` and another engine at 80%, each engine shows its own value. For multiplayer, a
second player compares the cooking and purifying percentages with the host; nothing is sent over the network.

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

Raft status check: in Creative the raft never moves (`isRaftAlwaysAnchored`), but anchors, engines and fuel work, which is
all the HUD reads. Stand on the raft and build a stationary anchor: `Free`; use it: `Dropping`, then `Anchored`; use it
again: `Raising`, then `Free`. Add an advanced anchor with Engine controls and a throwable anchor stand and throw the
anchor: `Dropping`, `Anchored`, `2/3 down` and so on. Build two engines, `qoldev.fuel 50%`: `0/2 running`, fuel `50%`
without time. Switch one on: `1/2 running`, an arrow, `50% · 25 min`; turn around and look up and down, the arrow must
keep pointing where the engine pushes; press its direction lever and the Engine controls reverse button: the arrow flips.
Turn the second engine the other way and switch it on: `No thrust`. Drop an anchor: `Held by anchor`, fuel `paused`.
`qoldev.fuel 0` with both on: `0/2 running · 2 no fuel`, fuel `Empty`. With one engine running build more than 110
foundations: `2 needed · Too heavy` and fuel `paused`; remove foundations down to 101..110: `Weak`. Swim off the raft
and onto an island: the HUD disappears. Switch off each line, the arrow and the power warnings, change **HUD size**
and restart the game: the settings stay. In a Normal world, check that the raft really moves where the arrow points and
that the fuel time goes down by a minute per minute. For multiplayer, a second player compares the lines with the host.

Full net check: build a basic and an advanced collection net. Aim at each: `Items 0/10` / `Items 0/15`. `qoldev.net 9` on the
basic net: `Items 9/10`, no glow; `qoldev.net full`: `Full 10/10` in red and the net glows dark red; `qoldev.net full` on the
advanced net: `Full 15/15`. If the paint brush can paint nets, paint one before filling it; aim the hammer at a full net to remove
it: paint and the hammer outline stay, the glow stays with them. Collect the items with the interact key: the glow goes away at once;
`qoldev.net clear` does the same. Fill a net, save, return to the main menu and load the world: the net glows again. Switch the
feature off and on with a full net in sight, and switch off **Collection nets** in Hover info. Compare the glow with the red
preview of a block that cannot be placed: the net must look darker and redder; at night the glow must still be visible. In a
Normal world, let a net fill up by itself while sailing. For multiplayer, the host fills a net and a second player sees the same
count and glow, then collects the items; the glow goes away for both.

Scarecrow radius check: build crop plots of every size (small, medium, shoe, wall, ceiling, large) around the raft, a basic and
an advanced scarecrow. Select the hammer: a yellow line around the basic scarecrow, a green one around the advanced one, at the
height of the crop plots and hidden behind walls; crop plots inside a line glow green, outside red, the large ones never, empty
ones too. Select a scarecrow from the hotbar: its preview has a half-transparent green line while it can be placed and a red one
where it cannot, and crop plots it would protect turn green only while the preview is green. Open the inventory, the build menu
and the pause menu: lines and glow disappear and come back after closing. Put the hammer away: the crop plots get their own look
back, paint and the hammer outline included. Build crop plots one and two floors above and below and switch on **Neighbouring
floors**: fainter lines there, smaller further away; the glow matches the lines on every floor. Change **Line width** from 2 to
10 cm, switch off **With hammer**, **With scarecrow** and **Highlight crop plots** one by one and restart the game: the settings
stay. Aim at the basic scarecrow: `Parts 3/3`; `qoldev.scarecrow`: `Parts 2/3`, the head or an arm falls off; `qoldev.scarecrow 2`:
destroyed, its line disappears and the plots around it turn red. Aim at the advanced one: `Indestructible`; `qoldev.scarecrow`
refuses. In a Normal world, wait for a seagull on a basic scarecrow: `Parts 3/3 · seagull`, then one part less. Plant crops just
outside a line and just inside it and watch which ones seagulls attack. For multiplayer, a second player compares the lines, the
glow and `Parts` with the host while the host runs `qoldev.scarecrow`.

Storage sort check: build a small, a medium and a large storage, a cupboard and a fridge if available. Open the small storage and
run `qoldev.chest`: with **By category** press the sort button: resources first, then tools, weapons, equipment, food, drinks, seeds,
placeables and other, each group in alphabetical order, the empty slots at the end. Part stacks of one item are merged into full
stacks plus one remainder; used tools, canteens and the partly eaten catfish stay after the whole stacks of their item, the least
used first; the stack above the stack size stays whole in front of the other stacks of its item. Run `qoldev.chest` again and try
**Alphabetical** and **Vanilla**; Vanilla must look exactly like the game without the mod (used items at the end). Repeat in the
large storage with `qoldev.chest 40`. Put planks and nails into the hotbar, switch on **Sort after storing** and press the store
button and its key: the planks and nails go into the storage and it is sorted. Switch the feature off: the sort button sorts the
vanilla way. Restart the game: **Sort order** and **Sort after storing** stay. Sorting an empty storage changes nothing. In
multiplayer, the host fills and sorts a storage and closes it; a second player without the mod opens it and sees the same order.
Then the second player with the mod sorts a storage as a client and closes it; the host opens it and sees the same order, also
after saving and loading the world.

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

### HUD

```
_CanvasGame_New (CanvasHelper)
  Stat sliders          CanvasHelper.StatSliderParent, bottom-left corner, 364x257
    Health (16, 43), HungerBar (16, 97), Thirst (16, 151): UISlider_Stat, 276x47, pivot (0, 0.5);
                        StatIconBG 43x47 at the left edge of each bar
    QoLChanges_HudPanel  HudPanel: sections stacked upwards from 12 px above the highest bar
  BuffDisplayer         bottom centre above the hotbar
  InventoryPickup       bottom right, picked up items
  Notification*         top right
```

- HUD lines use the font and shadow of the interaction prompt (`DisplayTextMiddle`) and centre their 32 px icons on the
  stat icons. Arrows use the sprite of the settings dropdown arrow, which points down.

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

## Raft: anchors and engines

- `Raft.AnchorCount` / `IsAnchored` are counted on the host: `Anchor_Stationary.DropAnchor` adds an anchor as soon as it
  starts dropping, `WeighAnchor` removes it when it starts rising, a thrown anchor is added when it reaches the bottom
  (`Anchor_Throwable_Stand.OnAnchorHit`). Other players do not keep the count up to date, so the HUD derives the state
  from the anchors: stationary `AtBottom` / `DroppingAnchor` / `weighingAnchor`, throwable stand `FullyAnchored` /
  `HasThrownAnchor`. The game keeps no list of anchors; they are found with `FindObjectsOfType` once a second and
  filtered by `Block.hasBeenPlaced` (the block preview has the same components).
- Advanced anchors are `Anchor_Stationary` with `remoteControlled` (`Raft.remoteAnchors`); Engine controls toggle them
  through the static `EngineControls.AnchorsAreDown`. Its power button calls `OnButtonPressed_ToggleEngine` on every
  engine, so it inverts each engine separately and a mixed state stays mixed.
- Engines: `RaftVelocityManager.Motors` (placed engines, on every player). `MotorWheel.MotorState` is
  `engineSwitchOn && FuelTank.CurrentTankAmount > 0`, but its getter fires `OnMotorDirty` on a change, so the HUD reads
  the fields. `PushDirection` is the engine's own `transform.forward` (flattened), negated when `rotatesForward`: the
  direction depends on how each engine was placed.
- Strength: `_motorStrenght` 100 and `_extraMotorStrength` 10 foundations per running engine;
  `RaftWeightManager.FoundationWeight` is the foundation count capped at 600. `RaftVelocityManager.MotorWheelWeightStrength`:
  anchored or weight > strength + extra → `NotStrongEnough` (`MotorDirection` is zero, engines burn no fuel),
  weight > strength → `Weak` (half speed), else `StrongEnough`.
- Fuel: `MotorwheelFuelTank` (a `Tank`) holds `maxCapacity` 50; a running engine burns 1 unit per `timePerFuel` (60 s)
  unless `NotStrongEnough`. The host sends the level to other players when the gauge changes
  (`Tank.SendTankSetValueMessage`).
- Without engine thrust the raft drifts with open sails (`Sail.AllSails`) and the current; Creative sets
  `raftSpecificVariables.isRaftAlwaysAnchored` and keeps the raft in place.

## Collection nets

- A net is a `Block_Foundation_ItemNet` with `ItemNet` (the interactable: "Collect N items" prompt, `itemCollector`) and an
  `ItemCollector` on its child `_netObjectCollider` with `playerControlled = false`; the hook has an `ItemCollector` too, with
  `playerControlled` and no limit.
- Limit: private `maxNumberOfItems`, `Placeable_CollectionNet_Basic` 10, `Placeable_CollectionNet_Advanced` 15, 0 = no limit.
  `OnTriggerEnter` ignores debris while `collectedItems.Count >= maxNumberOfItems` and items that `requiresHands` (barrels).
- Only the host collects: it sends `ItemCollectorCollect` and calls `CollectItem`, other players run `CollectItem` from the
  message. Collecting by hand (`AddCollectedItemsToPlayer`) sends `ItemCollectorClear` with the player; every game calls
  private `ClearCollectedItems(player)`, which removes the items and gives them to that player only on that player's game.
  Loading a world or joining (`Message_ItemCollectorCreate`) restores the items through `CollectItem`, so postfixes on
  `CollectItem` and `ClearCollectedItems` see every change on every player.
- Collected items are parented to the collector and keep their models, so the net's own renderer is
  `OccupyingComponent.renderers` (found in `Awake`, before anything is collected), not every renderer below the block.
- The net's material uses the `BlockPaint` shader (`GameManager.blockPaintShader`). The game changes placed blocks through
  property blocks on `OccupyingComponent.renderers`, always reading the block first: paint (`_Side1_*`, `_Side2_*`,
  `_PaintSide`), the hammer outline (`_Destroy_Overlay`). Block previews keep their materials and get `_BuildingEmission`
  through `MaterialRendConnection`: `GameManager.shaderGreen` (0.54, 0.75, 0.27) or `shaderRed` (0.86, 0.16, 0.24).
  Full net indicator sets `_BuildingEmission` (0.4, 0.06, 0.04) on placed nets and restores the material's value.
- Debris spawners: `ObjectSpawnerManager.plankSpawner` and `itemSpawner` (both `ObjectSpawner_RaftDirection`);
  `ObjectSpawner.SpawnItemNetwork` spawns on the host and sends the item to other players.

## Seagulls and scarecrows

- Seagulls (`Seagull`, prefab in `sharedassets1.assets`) are controlled by the host: every `searchCropInterval` (4) minutes
  `FindCropplotAndNest` goes through `PlantManager.allCropplots` (placed crop plots) while
  `seagullVariables.attacksCrops` is set. A plot is a target when it is `peckable` (the large plots are not), `ContainsCrops`,
  visible from the seagull and no player is within `searchPlayerRadius` (2.25 m) of it.
- `GetScarecrowInVicinity`: `Physics.OverlapSphere` around the centre of the plot's `mainCollider` with `searchScarecrowRadius`
  (6 m) and `itemSearchMask` (Block, Item, BakedBlocks, RemotePlayer, LocalPlayer), colliders tagged `Scarecrow`. A scarecrow that is
  `Occupied` (a seagull is on its way to it or sits on it) stops the whole search; one with `Health > 0` protects the plot: an
  invulnerable one is skipped, a basic one becomes the target instead of the crops. Only the host sets `Occupied` and
  `targetScarecrow`; every player gets the seagull's `currentState` (`Peck` while it sits).
- Tagged colliders: basic `Placeable_ScareCrow` — child `BlockCollider`, box 0.25 x 1.4 x 0.6 at (0, 0.745, 0.01); advanced
  `Placeable_Scarecrow_Advanced` — box on the root, 0.46 x 1.92 x 1.2 at (-0.1, 1.0, -0.06). The sphere around the plot touches the
  box, so the zone is the box grown by 6 m: a rounded rectangle on the scarecrow's floor, a smaller one on the floors around it
  (`BlockCreator.FullFloorHeight` 2.42 m). Crop plot centres are 0.1–0.45 m above their floor (wall and ceiling plots differ).
- Basic scarecrow: `parts` are its head and arms (3); `Seagull.Peck` → `DamageScarecrow` (host: RPC + `RemovePart`, others ask
  the host), `RemovePart` runs on every player; at 0 parts the host removes the block after 3 s. `invurnerable` scarecrows have no
  parts and `Health` 1. A seagull lands on the child tagged `BirdLanding`.
- Scarecrows are not interactables. The preview of a placeable is `BlockCreator.selectedBlock` (`hasBeenPlaced` false); the local
  `BlockCreator.Update` calls `OccupyingComponent.SetNewMaterial` with `GameManager.ghostMaterialGreen` / `ghostMaterialRed` every
  frame, which is how Scarecrow radius knows whether the preview can be placed.
- Lines use `Sprites/Default` (always included: vertex colour, alpha blending, depth test, no depth write). Crop plot models use
  `BlockPaint`; Scarecrow radius lights them with `_BuildingEmission` (0.1, 0.3, 0.05) / (0.4, 0.06, 0.04) like Full net indicator.

## Storages and sorting

```
_CanvasGame_New/InventoryParent (Inventory.Parent)
  Inventory_Storage_Small / _Medium / _Large / _Cupboard / _Fridge   (Inventory), created per storage from
                                            Storage_Small.inventoryPrefab in OnFinishedPlacement
    BrownBackground (1)
    DescriptionBox
      IconBackground: Icon, ChestText,
        Sort button                    (Inventory.sortButton) sprite InventorySortIcon, onClick Inventory.Sort
        Sort Player Inventory button   (Inventory.dumpButton) sprite InventorySortIntoStorage,
                                       onClick Inventory.SortPlayerInventoryIntoStorage
    SlotParent (GridLayoutGroup): Slot_Inventory x N
  Inventory_Storage_Trashcan (Inventory_Trashcan): only a "Sort button" that empties the trash
```

- Both buttons are 94.8 x 96 scaled by 0.44 (about 42 px), anchored to the top-right corner of `IconBackground` 46 px apart.
  `Inventory.Update` shows them only without a gamepad. The small storage and the cupboard have little room left between
  `ChestText` and the buttons.
- `StorageManager.Update` (local player, storage open): the keyboard binding `SortStorage` (action `SortPlayerIntoChest`) calls
  `SortPlayerInventoryIntoStorage` (or empties a trash can), the gamepad action `SortChest` calls `Inventory.Sort`. Nothing else
  calls `Inventory.Sort`.
- `Inventory.Sort`: items with full uses and `Amount <= StackSize` are summed per `UniqueIndex` (in the order they first appear,
  by dictionary order) and added again with `AddItem(name, amount)` in full stacks plus a remainder: new instances, so
  `exclusiveString` is lost. The rest (used items, stacks above the stack size) is cloned and added at the end ordered by
  `UniqueIndex`. `AddItem` plays the UI move sound for every stack.
- `SortPlayerInventoryIntoStorage` (storage inventory, `secondInventory` = player inventory): moves the same kinds of whole
  items that the storage already holds from the player inventory, hotbar included, into it.
- Storage contents are synchronised only when the storage closes (`Message_Storage_Close` with all slots), so any change made
  while it is open by the local player reaches the host and the other players.
- Storage sort patches `Inventory.Sort` with a prefix (the original runs for Vanilla, non-storage inventories and on errors) and
  writes all slots with `Slot.SetItem` from a finished plan; if a write fails the original instances are written back.
- `CraftingCategory` is `Nothing` for 274 of 584 vanilla items (materials, food, water, blueprints, trophies, building blocks);
  seeds are `CreativeMode`, some decorations `Skin`. Storage sort groups by its own rules (`StorageGroups`): crafting
  ingredients from `settings_recipe.NewCost`, cooking pot ingredients from `Resources.LoadAll<SO_CookingTable_Recipe>("SO_CookingRecipes")`,
  seeds from `PlantManager.allPlants` (`Plant.item`, includes raw potato and beet), containers from `ItemAfterUse` of food and
  drinks, smeltable ores from `settings_cookable.CookingResult`. `settings_buildable.Placeable` marks real placeables;
  `blockPrefabs` is also set on ingots, spices, bait and trophies, so it is not used.
- Display names are localised at runtime (`Settings` rewrites `settings_Inventory.DisplayName` when the language changes);
  the serialized value of some items is Swedish.

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
| Storage | `Storage_Small.GetInventoryReference()`, `StorageManager` (`OpenStorage`, `CloseStorage`, `currentStorage`, static `allStorages`), `Inventory.Sort()`, `Inventory.SortPlayerInventoryIntoStorage()` |
| Crafting | `CraftingMenu`, `CostCollection` |
| Raft | `Raft` (`AnchorCount`, `remoteAnchors`), `Anchor_Stationary`, `Anchor_Throwable_Stand`, `EngineControls`, `MotorWheel` (`FuelTank`, `engineSwitchOn`, `rotatesForward`), `RaftVelocityManager`, `RaftWeightManager`, `Sail`, `SteeringWheel` |
| World | `ItemCollector` (`maxNumberOfItems`, `collectedItems`), `ItemNet`, `Scarecrow` (`parts`, `invurnerable`, `Occupied`), `Seagull` (`searchScarecrowRadius`, `currentState`), `PlantManager.allCropplots`, `Plant`, `Cropplot` (`peckable`, `mainCollider`), `Battery`, `Tank`, `Electric_Purifier` |
| Interaction | `Pickup.RaycastForRayInteractables`, `RaycastInteractable`, `IRaycastable.OnIsRayed`, `DisplayTextManager`, `Player.UseDistance` |
| Tools | `Hook`, `ChargeMeter`, `Throwable` |
| Notebook / research | `NoteBook`, `NoteBookUI`, `ResearchTable`, `Inventory_ResearchTable` |
| Input | Input System actions; vanilla storage actions `SortPlayerIntoChest` (keyboard binding `SortStorage`), `SortChest` (gamepad only) |
