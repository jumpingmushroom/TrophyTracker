# Changelog

## 0.1.0 — first cut

- Requires Valheim 1.0.20 or later.
- A HUD panel showing kills left until a trophy drops, read from the game's own drop counter,
  which is saved with your character: exact once you've killed that creature, a worst-case bound
  before that.
- Each star level has its own countdown; starred levels you've killed are shown after the 0★
  count, e.g. `1★ in 3`.
- A thin progress bar under exact rows that fills as the guaranteed drop gets closer.
- A row for the trophy of the creature you last killed, even if it isn't pinned or you already
  own it. By default it stays until you kill another creature that drops a trophy;
  `RecentKillSeconds` turns it into a timer.
- Every trophy in the game is pinnable from the Compendium — click to toggle — not just trophies
  with an obvious "first" creature source. Only creatures you've discovered (seen, killed, or
  collected) ever show up, in the panel or the Compendium.
- The panel is movable (hold Left Alt with the inventory open and drag) and configurable: anchor,
  offset, scale, max rows shown.
- Text settings: font (any font the game has loaded; Norsebold by default), size, bold, outline,
  and the opacity of the plate behind the rows.
- Options to hide icon, chance, or the progress bar per row, and to hide the whole panel with the
  vanilla HUD, the large map, build mode, or the inventory.
- Exact counts are dimmed while other players are online, since the counter belongs to whichever
  client controls the creature.
- F7 toggles the panel for the session.
