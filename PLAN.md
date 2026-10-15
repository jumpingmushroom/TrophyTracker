# TrophyTracker — Technical Plan

**Idea:** a HUD panel that tells you how many more kills of a creature you need before its trophy
drops, read from the game's own pity counter rather than estimated. Pin the trophies you are
hunting from the Compendium; the creature you last killed shows up for a minute on its own.

**Replaces:** Combifightet-TrophyPercent 1.2.0 (Thunderstore). Its panel is hard-coded top-right
(`anchoredPosition (-30,-260)`, font 18, 250 px), it has no configuration beyond the pin list,
its "estimate" has no relation to the real counter (§1.2), and it misses trophies such as Bear
and Vile (§1.6). Its code was read only to find which game hooks exist; nothing is copied.

**Target build:** Valheim 1.0.20 (`Version.CurrentVersion = new GameVersion(1, 0, 20)`). The
findings below come from `assembly_valheim.dll` pulled from the rig's `valheim_Data/Managed` on
2026-10-15 and decompiled with `ilspycmd` into the gitignored `decomp/`. 1.0.20 rewrote the pity
counter (§1.1, "Changed in 1.0.20"); the mod targets 1.0.20 and later only.

**Scope:** client-side only. No RPCs, nothing on the server, works on vanilla servers.
Thunderstore namespace `Jumpingmushroom`, package `TrophyTracker`, GUID
`com.jumpingmushroom.trophytracker`, repo `github.com/jumpingmushroom/TrophyTracker`. BepInEx +
Harmony only, no Jotunn. Repo layout, build scripts, publicizer setup and pure-model test project
are the same as Earshot's and Forewarned's. No publish tooling: releases end at
`package.sh` → tag → `gh release create`, and the user uploads to Thunderstore.

**Decisions agreed (2026-10-12 design review):**
- **Name:** TrophyTracker.
- **Counter display** (revised 2026-10-15 for 1.0.20, spelled out in words 2026-10-15 fix wave
  7): the main count is the 0★ countdown, `in {0} kills` (`next kill` at 1) when the game has a
  counter for it, otherwise `within {0} kills` with `{0}` the worst case. After it, every starred
  tier that still uses pity and has a counter of its own, joined with ` · `: `1★ in 3 · 2★ next`.
  `random roll` when pity does not apply, `always drops` for guaranteed drops (both decided on
  the base chance).
- **Star tiers have separate, saved counters** (1.0.20): killing a ★1 no longer touches the 0★
  countdown, and every counter is saved with the character.
- **Multiplayer:** exact counts are dimmed while other players are online, because the counter
  belongs to whichever client owns the creature (§1.4).
- **Positioning:** config (anchor preset, X/Y offset, scale) **and** a drag mode (inventory open
  + hold Left Alt + drag) that writes the offset back to config. Default anchor MiddleLeft, clear
  of MyLittleUI's top-right stack.
- **Rows:** pinned trophies, plus the trophy of the creature you last killed until the next creature with a trophy is killed (configurable via `RecentKillSeconds`; 0 = never expire).
- **Pinning:** click a trophy in the Compendium to toggle; a pin badge marks pinned ones. The pin
  list is also a config string. Start fresh: no import from TrophyPercent's config.
- **Every trophy is pinnable** (taken from `ObjectDB`, not from drop tables), but only for
  creatures you have **discovered**: health bar seen, killed, or trophy collected. Nothing you
  have not met appears anywhere.
- **Compendium:** discovered-but-uncollected trophies appear as dark silhouettes in their normal
  slot, so they can be pinned. No global count in the title, no "(Missing)" text.
- **Row content:** icon, creature name, kills-left, base drop chance, thin progress bar.
- **Collected:** the row shows `✓ collected` for 5 s, then the trophy is unpinned (config toggle).
- **Visibility:** F7 toggles for the session. Hidden with the vanilla HUD, on the large map, in
  build mode and with the inventory open (each a config toggle, all on), except while dragging.

---

## 1. How vanilla trophy drops work

### 1.1 The pity counter: `CharacterDrop.GenerateDropList`

`CharacterDrop.OnDeath` (subscribed to `Character.m_onDeath`) calls `GenerateDropList()` and
drops the result. For each `Drop` in `m_drops` (CharacterDrop.cs:72-160, 1.0.20):

```
n    = m_character ? max(1, (int)2^(level-1)) : 1                  // level 1 = 0★
p    = drop.m_chance × (drop.m_levelMultiplier ? n : 1)
pity = p <= 0.3 && !GlobalKey(NoPseudoDrops)
key  = drop.m_prefab.name.GetStableHashCode() + (int)(p * 1000f)   // one counter per (trophy, tier)
if pity:
    if s_pseudoCounter[key] exists:
        c = s_pseudoCounter[key] - 1                                // decrement
    else:
        c = Random.Range(0, (int)(1/p * 2))                         // seed, int, max exclusive
        s_pseudoCounter[key] = c
    if c <= 0:
        drop
        s_pseudoCounter[key] = Random.Range(0, (int)(1/p * 2) + 2)  // reseed
    else:
        s_pseudoCounter[key] = c
else:
    drop if Random.value <= p                                       // plain roll
amount × n only if m_levelMultiplier && !m_levelMultiplierNotAmount
```

`s_pseudoCounter` is `public static Dictionary<int, int>`: key = the drop prefab name's
`GetStableHashCode()` (a string extension in `assembly_utils`) plus the effective chance in
thousandths; value = the remaining count. Each star tier of a level-multiplied drop has its own
entry (`TrophyBoar` at 0★ and 1★ are two keys). `PityMath.CounterKey` writes the key expression
exactly as the game does, so it truncates the same float product in the same runtime.

`Player.Save` writes the whole dictionary into the character file and `Player.Load` merges it back
(Player.cs ~4778 / ~5031), so **counters survive a restart**.

**Changed in 1.0.20** (from 1.0.17):
- Was `Dictionary<string, Tuple<float, int>>` keyed by trophy name, holding the chance it was
  seeded for; a kill at another star tier reseeded the one entry. Now one entry per (trophy,
  effective chance): other tiers no longer touch it.
- Reseed after a drop was `Random.Range(0, (int)(1/p*2 + 1))`; now `(int)(1/p*2) + 2`, one wider.
- Not saved before; now saved with the character.
- New `m_levelMultiplierNotAmount`: stars raise the chance but not the amount. Chance and counter
  are unaffected.

### 1.2 What that means for a display

- **Exact once seeded.** With a stored `c`, the k-th kill from now drops when `c - k <= 0`, so
  kills left = `max(1, c)`.
- **Unknowable before the first kill of that creature at that tier.** The seed is uniform on
  `[0, M-1]` with `M = (int)(2/p)`. Seed 0 drops on that kill; seed `s ≥ 1` stores `s` and drops `s`
  kills later. So from an unseeded state the drop lands on kill 1..M, worst case `M`, mean about
  `1/p`. For 10% that is ≤ 20. TrophyPercent's `round(1/p) - kills % round(1/p)` is unrelated to
  the real counter; we show `within M kills` instead. Because counters are saved, this only
  happens once per character and tier (for kills made since 1.0.20).
- **In game (Mono)** `(int)(1f / 0.1f * 2f)` evaluates to 19, not 20: Mono evaluates the float
  expression at higher precision, and 1/0.1f is a hair under 10. The game evaluates the same
  expression in the same runtime, so the displayed worst case matches the game; the unit tests
  (net8, strict float) see 20.
- **After a drop** the reseed is uniform on `[0, M+1]` (`PityMath.ReseedMax`); kills left is again
  `max(1, c)` and exact.
- **Star tiers are separate counters.** A ★1 kill advances only the ★1 entry; the 0★ countdown is
  untouched. The row shows the 0★ count and appends each starred tier that has an entry.
- **No pity above 30%.** A 10% trophy at 2★ is a plain 40% roll and has no counter at all.
- **World modifier** `GlobalKeys.NoPseudoDrops` disables the counter entirely.
- **Lifetime.** The dictionary is a static that is never cleared; it is saved with the character
  and merged back on load. Loading a second character in the same session therefore merges both
  characters' counters (a game quirk; documented, not worked around).
- **Shared counters.** Creatures that drop the same trophy at the same effective chance share one
  entry; at different chances they have separate entries.

### 1.3 Kill stats

`PlayerProfile.m_playerStats` is `PlayerStats[10]`; index 0 is raw stats. `m_enemyStats` is
`Dictionary<string, float>[5]` indexed by `KillModifiers` (`MixedAndTotal` = 0). Total kills of a
creature: `m_playerStats[0].m_enemyStats[0][character.m_name]`. The key is the **`m_name`
localisation token** (`$enemy_boar`), not the prefab name (Character.cs ~2950,
`Game.RPC_RegisterKill`). Stats are saved with the character.

### 1.4 Multiplayer

- `CheckDeath` is only called from the owner branch of `Character.CustomFixedUpdate`
  (Character.cs:825-845), so `OnDeath`, `m_onDeath` and with it `CharacterDrop.OnDeath` /
  `GenerateDropList` run **only on the creature's ZDO owner** (usually the client nearest when it
  loaded). So **the counter that advances is the owner's static**, not the killer's.
- Kill credit goes to **every player who damaged it** (`ZDOVars.s_attackers + playerName`), via
  `Game.RegisterKill` → routed `RPC_RegisterKill` on each such player's client.
- Consequence: with others online, your exact count may not move when a friend's client owns
  the creature, and may move from kills you did not make. We dim exact counts while
  `ZNet.instance.GetNrOfPlayers() > 1`.

### 1.5 Trophies and the Compendium

- Collected trophies: `Player.m_trophies` (`HashSet<string>`, saved), read via
  `Player.GetTrophies()`; added by `Player.AddTrophy(ItemData)` (private, Player.cs:5313) from
  `AddKnownItem`.
- `InventoryGui.UpdateTrophyList()` (InventoryGui.cs:2389) destroys every element in
  `m_trophyList`, then for each collected trophy instantiates `m_trophieElementPrefab` under
  `m_trophieListRoot` at `m_trophyPos × m_trophieListSpace` (30), sets `icon_bkg/icon`, `name`
  (localised name minus " trophy") and `description` (`<name>_lore`), and finally sizes the
  root to `max(m_trophieListBaseSize, -(lowest y - space))`. It is rebuilt every time the page
  opens.

### 1.6 Why TrophyPercent misses Bear, Vile and others

It only offers trophies that are collected, or "killed but missing". It builds trophy → creature
by scanning `ZNetScene` prefabs with `CharacterDrop` and keeps the **first** creature found per
trophy, then looks up kills by that creature's `m_name`. If the first match is a variant you
never killed, or the trophy has no `CharacterDrop` source, it never becomes pinnable. To be
confirmed in game with the `trophytracker` console command (§7), which lists every trophy and
all of its sources. TrophyTracker takes trophies from `ObjectDB` (`ItemType.Trophy`) and maps
**all** creatures per trophy.

### 1.7 Seeing a creature

The game does not record which creatures a player has seen. `EnemyHud.ShowHud(Character, bool)`
(EnemyHud.cs:126) runs when a creature's health bar is shown (within `m_maxShowDistance` 10 m,
bosses 100 m), which is a good "met it" signal. Per-character mod data can be saved in
`Player.m_customData` (`Dictionary<string, string>`, saved with the character, Player.cs:581).

### 1.8 HUD visibility

`Hud.SetVisible(bool)` hides the HUD by moving `m_rootObject` to `s_notVisiblePosition`
(Hud.cs:436) for Ctrl+F3, cutscenes and the like; children of `m_rootObject` follow for free.
`Minimap.IsOpen()`, `Player.InPlaceMode()`, `InventoryGui.IsVisible()`, `Chat`/`Console`/
`TextInput` visibility give the other states.

---

## 2. Architecture

```
src/TrophyTracker/
  Plugin.cs                  BepInEx entry, Harmony, console command
  PluginConfig.cs            all ConfigEntry bindings (§4)
  ConfigurationManagerAttributes.cs
  Core/                       game-facing runtime
    Runtime.cs
    CatalogBuilder.cs
    CounterReader.cs
    Discovery.cs
    Keys.cs
    TrophyConsole.cs
    Model/                    pure C#: no UnityEngine, no game types
      TrophyCatalog.cs        trophy id → sources (creature prefab, m_name token, chance, levelMultiplier)
      PityMath.cs             effective chance per tier, pity flag, ≤M bound, counter key, pity tiers, kills left
      RowBuilder.cs           inputs → ordered list of RowView structs (§3)
      RowView.cs              plain display row: icon id, name, count text, state, chance, bar fill, dim
      PinList.cs              parse/serialise the Pinned config string
      DiscoveredSet.cs        parse/serialise the m_customData value
      PanelLayout.cs          anchor presets, offset direction, drag delta → offset
      BarTracker.cs
      RowText.cs
      Translations.cs
      Visibility.cs
  Patches/                   Hud, EnemyHud.ShowHud, CharacterDrop.GenerateDropList,
                             Game.RPC_RegisterKill, Player.AddTrophy, InventoryGui.UpdateTrophyList
  UI/
    TrophyPanel.cs           panel root, row pool, rebinding, visibility
    TrophyRow.cs             one pooled row
    PanelDrag.cs             drag handling, writes Offset
    CompendiumDecorator.cs   silhouettes, pin badges, click toggles
    PinToggle.cs
    Sprites.cs / UiUtil.cs
  translations/English.txt
tests/TrophyTracker.Tests/   net8.0, dotnet test
```

The model gets everything as plain values (strings, floats, ints, sets); the game layer does all
the reading and the UI layer only binds `RowView`s.

---

## 3. Rows

### 3.1 Layout

About 26 px tall at scale 1, with a 3 px progress bar under the text:

```
[icon] Fenring ............  in 7 kills · 1★ in 3     10%
       ▓▓▓▓▓▓▓▓░░░░░░
[icon] Surtling ...........  within 40 kills           5%
[icon] Deer ...............  random roll              50%
```

- **Icon:** the trophy item's icon (`ShowIcon`).
- **Name:** the creature's localised name. When several creatures drop the trophy, the one with
  the most kills; ties and zero kills fall back to the first discovered source.
- **Chance:** the base 0★ chance of that source (`ShowChance`).
- **Font:** the vanilla HUD font asset; size follows `Scale`.
- If the HUD font lacks ★, ✓ or · (checked once with `TMP_FontAsset.HasCharacter(c, true)`),
  ASCII stand-ins are used: `*`, `+`, ` - `.

### 3.2 States

| State | When | Count text |
|---|---|---|
| Exact | the 0★ key has an entry | `in {0} kills` (`next kill` at 1), then the extras |
| Unknown | no 0★ entry yet | `within {0} kills`, `{0} = (int)(2 / p0)`, then the extras |
| Roll | base chance > 0.3, or `NoPseudoDrops` is set | `random roll` |
| Always | base chance ≥ 1 | `always drops` |
| No source | trophy has no creature drop in the catalog | `—` |
| Collected | trophy just entered `m_trophies` | `✓ collected` for 5 s, then unpinned |
| Dimmed | Exact while `GetNrOfPlayers() > 1` (`DimInMultiplayer`) | same text, reduced opacity |

**Extras** (Exact and Unknown rows): every starred tier `s ≥ 1` whose effective chance is still
≤ 0.3 and whose key has an entry, in star order, joined with ` · `: `in 7 kills · 1★ in 3 · 2★
in 5`, `within 19 kills · 1★ in 3` (ASCII separator ` - `, star `*`). A tier whose own countdown
is 1 reads `{stars}★ next` instead of `{stars}★ in {n}`. Tiers without an entry are not shown; a
drop without a level multiplier has only the 0★ tier. Extras dim with the count.

**Progress bar** (`ShowProgressBar`): Exact rows only, 0★ counter only. Fill = `1 - left / start`, where `start` is
the highest value seen since the last reseed (a reseed is seen by the drop-roll hook, §5).
Hidden in every other state.

### 3.3 Which rows

1. The **recent-kill row** first, highlighted, if `ShowRecentKill` and you killed something in the
   last `RecentKillSeconds` (0) whose trophy is not pinned. Collected or not does not matter.
2. **Pinned rows** in pin order.
3. Rows for undiscovered creatures are never shown, even if pinned.
4. At most `MaxRows` (8); the rest collapse into `+N more`.
5. `HideWhenEmpty`: no rows → no panel.

---

## 4. Configuration

`com.jumpingmushroom.trophytracker.cfg`, everything live through ConfigurationManager (F1).

| Section | Key | Default |
|---|---|---|
| General | `Enabled` | true |
| General | `ToggleKey` | F7 |
| Pins | `Pinned` | empty (comma-separated trophy prefab names) |
| Pins | `UnpinWhenCollected` | true |
| Pins | `ShowRecentKill` | true |
| Pins | `RecentKillSeconds` | 0 |
| Layout | `Anchor` | MiddleLeft (TopLeft … BottomRight, 9 presets) |
| Layout | `OffsetX` | 20 |
| Layout | `OffsetY` | 0 |
| Layout | `Scale` | 1.0 (0.5–2.0) |
| Layout | `MaxRows` | 8 |
| Layout | `DragModifier` | LeftAlt |
| Rows | `ShowIcon` / `ShowChance` / `ShowProgressBar` | true |
| Rows | `DimInMultiplayer` | true |
| Visibility | `HideWithVanillaHud` | true |
| Visibility | `HideOnLargeMap` | true |
| Visibility | `HideInBuildMode` | true |
| Visibility | `HideWithInventory` | true |
| Visibility | `HideWhenEmpty` | true |
| Text | `FontSize` | 22 (12–40) |
| Text | `Bold` | true |
| Text | `Outline` | 0.25 (0–0.5) |
| Text | `BackgroundOpacity` | 0.72 (0–1) |
| Text | `Font` | "Valheim-Norsebold" (dropdown of every loaded TMP font plus "(HUD font)"; bound once the panel is first built) |

---

## 5. Data flow

Nothing runs per frame except the visibility check and an active drag.

1. **Catalog**, built once per world load when `ObjectDB` and `ZNetScene` are ready: every
   `ObjectDB` item with `ItemType.Trophy` is a trophy; every `ZNetScene` prefab with `Character`
   and `CharacterDrop` adds `trophy → (prefab name, m_name, m_chance, m_levelMultiplier)` for each
   trophy-type drop. Null prefabs and broken entries are skipped and counted.
2. **Discovered set**, keyed by creature prefab name, stored in
   `Player.m_customData["TrophyTracker.Discovered"]`. Loaded when the local player spawns, then
   back-filled from kill stats (any source whose `m_name` has kills > 0) and collected trophies
   (all sources of that trophy). Saved by the game with the character.
3. **`EnemyHud.ShowHud` postfix**: if the creature's prefab is not yet discovered, add it and mark
   the panel and Compendium dirty. One HashSet lookup in the common case.
4. **`CharacterDrop.GenerateDropList` prefix/postfix**: compute this creature's counter key per
   trophy exactly like the game (level from `m_character`, effective chance, `CounterKey`),
   snapshot those entries before and after; detect reseeds (for the bar's `start`, per key) and
   mark dirty.
5. **`Game.RPC_RegisterKill` postfix**: record `(m_name, time)` as the recent kill. This runs on
   your client for every creature you damaged, owner or not.
6. **`Player.AddTrophy` postfix**: start the 5 s collected flash; afterwards unpin if
   `UnpinWhenCollected` and the trophy was not collected before.
7. **Panel**: when dirty, or on a 1 s tick (recent-row expiry, player count), `RowBuilder` builds
   the `RowView` list and the pooled row objects are re-bound in place. Rows are created once
   (`MaxRows` + 1) and never destroyed until the HUD is.

---

## 6. UI

### 6.1 Panel and positioning

- Child of `Hud.m_rootObject` (follows Ctrl+F3 and cutscene hiding); if `HideWithVanillaHud` is
  off, child of the HUD canvas instead.
- `PanelLayout` sets anchor and pivot to the same preset corner, so `Offset` always points inward,
  and rows grow away from the anchor (Bottom* presets stack upward).
- Default MiddleLeft (20, 0): clear of MyLittleUI's top-right clock, forecast, winds and status
  list (−25 to −290 px on the rig), its bottom-left weight and slot readouts, ExtraSlots' slots
  and the vanilla health and food bars.

### 6.2 Drag mode

- Inventory open + `DragModifier` held: the panel shows even when `HideWithInventory` is on,
  gets a faint outline and accepts a drag.
- The drag delta is converted to the parent's local space with
  `RectTransformUtility.ScreenPointToLocalPointInRectangle`, so canvas scaling is handled; the
  panel's own `Scale` does not affect its position. On release (or if the modifier is released
  mid-drag) `Offset` is written to config and saved.
- Dragging never changes `Anchor`.
- In drag mode the panel gets a Canvas with `overrideSorting` so it draws above the inventory.

### 6.3 Toggle key

`ToggleKey` flips a session-only flag; it is ignored while chat, console or a text input has
focus.

### 6.4 Visibility

Shown when: `Enabled`, toggled on, a local player exists, and none of the enabled hide conditions
hold (large map, build mode, inventory open unless dragging, no rows).

---

## 7. Compendium

A postfix on `InventoryGui.UpdateTrophyList`:

1. **Silhouettes.** For each discovered trophy not in `GetTrophies()`, instantiate
   `m_trophieElementPrefab` as vanilla does at its `m_trophyPos` slot; icon tinted near-black,
   name = creature name, description = `Not yet collected · 10% drop · click to pin` (translated
   through `translations/English.txt`). Added to `m_trophyList` so vanilla's next rebuild destroys
   them. The root height is recalculated with vanilla's formula.
2. Undiscovered trophies stay absent (vanilla already leaves gaps for uncollected ones).
3. **Pin toggle** on every element: a `Button` (no transition) on the element root toggles the
   trophy in `Pinned`. Pinned elements get a small badge on the icon's top-right — a small drawn
   dot sprite (no game sprite lookup). Clicks are ignored while `DragModifier` is held.
4. Collected trophies keep vanilla's name and lore and stay pinnable for farming duplicates.
5. Not carried over from TrophyPercent: title count, red "(Missing)", flavour line, checkboxes.
6. Gamepad: trophy elements are not gamepad-selectable in vanilla, so on a controller pins are
   config-only in 0.1.0 (README).

Console command `trophytracker`: catalog (every trophy with all sources, chances and `lm` /
`notAmount` flags), the primary source's pity tiers with their saved counters (`★0 n=7  ★1 -`),
discovered set, unknown pins, skipped catalog entries.

---

## 8. Error handling

- Every patch body is wrapped in try/catch, logs once per patch and key, and returns. Prefixes
  never skip the original.
- Private members are reached through the publicizer, not string reflection; a rename after a
  game update fails at patch time with a clear log line and the rest keeps working.
- If `s_pseudoCounter` changes type, all rows fall back to Unknown/Roll and one warning is logged
  (the field is read in a `NoInlining` helper so the failure lands inside the try).
- Unknown pins are kept in config, not shown, and listed by the console command.
- A corrupt `m_customData` value is parsed as far as possible and rebuilt from the back-fill.

---

## 9. Testing

**Unit tests** (`tests/TrophyTracker.Tests`, net8.0, `dotnet test`):

- `PityMath`: effective chance per tier, 0.3 threshold, `NoPseudoDrops`, `≤M`, counter key,
  reseed maximum, pity tiers, `max(1, n)`.
- **Simulation:** re-implement the 1.0.20 §1.1 loop (counter dictionary keyed by `CounterKey`
  with a fixed fake hash) with a seeded RNG and check the predicted kills-left equals the kill that
  actually drops, over 10k cycles per chance value; that the unseeded worst case is reached and
  never exceeds `M`; that ★1 kills between 0★ kills leave the 0★ countdown unchanged; and that a
  reseed never exceeds `M + 1`.
- `RowBuilder`: every §3.2 state, ordering, `MaxRows` + `+N more`, recent-row lifetime and
  exclusions, undiscovered filtering, collected flash then unpin, dimming.
- `PanelLayout`: all 9 anchors, offset sign per corner, drag → offset at several scales.
- `PinList`, `DiscoveredSet`: round trips, whitespace, duplicates, unknown ids.
- `TranslationsTests`: every key used in code exists in `English.txt`.

**In game** (rig, Default profile, `devcommands`):

- `trophytracker`: every vanilla trophy present and mapped, Bear and Vile included.
- Spawned creatures: Unknown → Exact, count reaches 1 and the drop lands on the predicted kill,
  a ★1 kill adds `★1:n` and leaves the 0★ count alone, counts survive a restart, a Roll trophy,
  a boss (`always`).
- Discovery from a health bar; persists across relog; back-fill on an existing character.
- Compendium silhouettes, badge, click toggle; recent-kill row and expiry; collected flash.
- Drag and save, each anchor, scale, every hide state, F7.
- README screenshots via `shot.sh` / `crop.sh`.

---

## 10. Release

0.1.0. Version in `Plugin.cs`, the csproj and `thunderstore/manifest.json` together;
`build/package.sh` refuses to package if they disagree. `package.sh` → tag `v0.1.0` → push →
`gh release create` → copy the zip to `~/Downloads` on the rig. The user uploads to Thunderstore.
After the user is happy in game, Combifightet-TrophyPercent is removed from the Default profile
through r2modman (it is already gone from `plugins/`; only its cfg remains).
