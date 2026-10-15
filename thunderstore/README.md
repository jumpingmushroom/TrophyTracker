# TrophyTracker

Shows how many more kills you need before a trophy drops, read from the game's own drop counter
rather than estimated. Pin the trophies you're hunting from the Compendium; the trophy of whatever
you just killed shows up on its own for a minute too.

![TrophyTracker's HUD panel](https://raw.githubusercontent.com/jumpingmushroom/TrophyTracker/v0.1.0/docs/images/panel.jpg)

*The panel showing two pinned trophies and a recent kill, MiddleLeft at the default offset.*

## How it works

Requires **Valheim 1.0.20 or later** (the game's drop counter changed in 1.0.20).

Valheim doesn't drop low-chance trophies on a plain roll — for any trophy with a 30% or lower
chance, it keeps a counter and guarantees the drop within a bounded number of kills.
TrophyTracker reads that counter instead of guessing:

- **The count is saved with your character.** It survives death, logging out and restarting the
  game, so once you've killed a creature (since 1.0.20) its count is exact, falling by one with
  every kill.
- **Before your first kill of a creature** (at that star level), the game hasn't started its
  counter yet, so the exact count isn't knowable. The row shows `within N kills`, the worst
  case — the drop is guaranteed by the N-th kill, and usually comes sooner.
- **Each star level has its own countdown.** The main count is for 0★ creatures; a 1★ kill
  doesn't touch it. Starred levels you've killed are shown after it, e.g. `1★ in 3` — 3 more 1★
  kills.
- **Above a 30% chance, there's no counter at all** — it's a plain roll every kill, shown as
  `random roll`. Each star level doubles the chance, so a 10% trophy is already a 40% roll at
  2★, and that level never gets a countdown.
- **In multiplayer, the counter belongs to whichever client controls the creature** — usually
  whoever was nearest when it loaded, not necessarily whoever lands the killing blow. Your own
  count may not move on a kill you made, or may move on a kill you didn't. Exact counts are
  dimmed while other players are online, as a reminder they may not be "yours".
- **Only creatures you've discovered appear** — seen a health bar, killed, or collected the
  trophy. Nothing you haven't met shows up anywhere, including the Compendium.

Reading a row:

```
Greydwarf      in 14 kills · 1★ in 3     5% drop
Surtling       within 39 kills           5% drop
Serpent        random roll              33% drop
```

`in 14 kills` is the exact count of kills until the guaranteed drop, for a 0★ Greydwarf.
`1★ in 3` is the same, for 1★ Greydwarfs, which have their own countdown. `within 39 kills` is
the worst case, before you've killed a Surtling since 1.0.20. `random roll` means above a 30%
chance there's no countdown — every kill rolls. `5% drop` / `33% drop` is the base chance per
kill of a 0★ creature, which doubles per star.

One game quirk: switching to another character without closing the game carries the first
character's counts over and merges them into the second one's.

## Features

- Exact kills-left once a creature has been killed, worst-case bound before that, and a separate
  countdown for each star level you've killed.
- A thin progress bar under exact rows that fills as the guaranteed drop gets closer.
- Every trophy in the game is pinnable from the Compendium, not just the ones with a matching
  "first" source — Bear, Vile and friends included.
- A row for the creature you last killed, shown for a while even if its trophy isn't pinned.
- The panel is movable, scalable and configurable, and hides itself with the vanilla HUD, the
  large map, build mode and the inventory.
- Dims exact counts while other players are online, since the counter may belong to someone
  else's client.

## Pinning

Click a trophy in the Compendium to pin or unpin it — collected or not. Pinned trophies get a
small badge on their icon. A trophy is unpinned a few seconds after you collect it for the first
time, unless `UnpinWhenCollected` is turned off.

![The Compendium with an undiscovered trophy pinned](https://raw.githubusercontent.com/jumpingmushroom/TrophyTracker/v0.1.0/docs/images/compendium.jpg)

*A discovered-but-not-yet-collected trophy shown as a silhouette, pinned.*

On a gamepad, trophy entries in the Compendium aren't selectable (a vanilla limitation), so
pinning is config-only: edit the `Pinned` setting in F1 by hand (comma-separated prefab names,
e.g. `TrophyBoar,TrophyNeck`).

## Moving the panel

With the inventory open, hold **Left Alt** (configurable as `DragModifier`) and drag the panel.
The new position is saved to config automatically. You can also set the anchor, offset and scale
directly in F1 → Layout.

**F7** shows or hides the panel for the session.

## Configuration

Everything below is live in the BepInEx configuration manager (F1).

| Section | Key | Default | Description |
| --- | --- | --- | --- |
| General | Enabled | `true` | Master switch. |
| General | ToggleKey | `F7` | Show or hide the panel for this session. |
| Pins | Pinned | *(empty)* | Pinned trophies, comma-separated prefab names (TrophyBoar,TrophyNeck). Click a trophy in the Compendium to pin or unpin it. |
| Pins | UnpinWhenCollected | `true` | Unpin a trophy a few seconds after you collect it for the first time. |
| Pins | ShowRecentKill | `true` | Show the trophy of the creature you last killed for a while, if it isn't pinned. |
| Pins | RecentKillSeconds | `0` | How long the recent-kill row stays, in seconds. 0 keeps it until you kill another creature that drops a trophy. |
| Layout | Anchor | `MiddleLeft` | Screen corner or edge the panel is attached to. |
| Layout | OffsetX | `20` | Distance from the anchored edge, in HUD pixels (positive is inward; right for centre anchors). Set by dragging too. |
| Layout | OffsetY | `0` | Distance from the anchored edge, in HUD pixels (positive is inward; up for middle anchors). Set by dragging too. |
| Layout | Scale | `1.0` | Size of the panel and its text. |
| Layout | MaxRows | `8` | Rows shown at once; the rest collapse into '+N more'. |
| Layout | DragModifier | `LeftAlt` | With the inventory open, hold this and drag the panel to move it. |
| Rows | ShowIcon | `true` | Trophy icon. |
| Rows | ShowChance | `true` | Base drop chance. |
| Rows | ShowProgressBar | `true` | A thin bar that fills as the guaranteed drop gets closer (exact counts only). |
| Rows | DimInMultiplayer | `true` | Dim exact counts while other players are online: the counter belongs to whichever client controls the creature. |
| Visibility | HideWithVanillaHud | `true` | Hide when the game hides its HUD (Ctrl+F3, cutscenes). |
| Visibility | HideOnLargeMap | `true` | Hide while the large map is open. |
| Visibility | HideInBuildMode | `true` | Hide while placing buildings. |
| Visibility | HideWithInventory | `true` | Hide while the inventory is open (it still shows while you hold the drag key). |
| Visibility | HideWhenEmpty | `true` | Hide when there is nothing to show. |
| Text | FontSize | `22` | Text size of the panel's rows, before Scale. |
| Text | Bold | `true` | Bold text. Thicker strokes read better on the HUD. |
| Text | Outline | `0.25` | Thickness of the dark outline around the text. |
| Text | BackgroundOpacity | `0.72` | Opacity of the dark plate behind the rows. |
| Text | Font | `Valheim-Norsebold` | Font of the panel's text. The list holds every font the game has loaded; '(HUD font)' follows the game's own HUD text. Default: Valheim-Norsebold. |

## Translating

Copy `src/TrophyTracker/translations/English.txt` from this repository to
`BepInEx/config/TrophyTracker/translations/<Language>.txt`, using the game's own name for the
language (`German.txt`, `Norwegian.txt`, ...), and translate the right-hand side of each line.
Missing lines fall back to English. Creature names come from the game's own translations. Pull
requests with translations are welcome.

## Compatibility

Client-side, safe to add or remove at any time, works on vanilla servers, needs nothing on the
server. Built to sit alongside MyLittleUI and similar HUD mods — TrophyTracker defaults to
MiddleLeft, clear of the usual top-right HUD stack, and its position is configurable if you still
overlap.

**Replaces Combifightet-TrophyPercent.** Don't run both at once — they cover the same job, and
TrophyPercent's "estimate" isn't the game's real counter.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
