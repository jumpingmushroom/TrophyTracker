# TrophyTracker — notes for Claude

Kills left until each pinned trophy drops, read from Valheim's pity counter. Research, decisions and
design: `PLAN.md`. Implementation plan: `docs/superpowers/plans/2026-10-12-trophytracker.md`.

## Commits

- **Never add AI attribution** (`Co-Authored-By`, "Generated with", footers) to commits, PRs, the
  README, the changelog or release notes. The user is the only author.
- Commit and push after every change.
- Version bumps touch three places together: `PluginVersion` in `src/TrophyTracker/Plugin.cs`,
  `<Version>` in the csproj, and `version_number` in `thunderstore/manifest.json`.
  `build/package.sh` refuses to package if `Plugin.cs` and `manifest.json` disagree.
- Never commit the rig's address, user or paths. Write `<rig>` in tracked files; the real target
  lives only in the gitignored `build/deploy.sh`, `logs.sh`, `shot.sh`, `crop.sh`.

## Building and testing

- `dotnet test tests/TrophyTracker.Tests` runs the model tests (pure C#, net8.0). Everything under
  `src/TrophyTracker/Core/Model/` must stay free of UnityEngine and game types.
- The build box's dotnet SDK needs `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`; the scripts set it.
  `ilspycmd` also needs `DOTNET_ROOT=$HOME/.dotnet`.
- Reference assemblies live in `lib/` (gitignored), pulled from the rig's `valheim_Data/Managed`
  plus `BepInEx.dll` and `0Harmony.dll`. Decompiled game code is in `decomp/` (gitignored). No Jotunn.
- `./build/deploy.sh` builds Release and swaps the DLL atomically into the r2modman **Default**
  profile on the rig over SSH. A running game keeps the old DLL until relaunch.
- The rig's login shell is fish: wrap anything non-trivial in `bash -c '...'`. Brace expansion in
  scp paths fails there; use `tar` over ssh.
- `./build/logs.sh` shows TrophyTracker lines from the rig's BepInEx log. `./build/shot.sh <name>`
  captures the game window into `docs/images/`; `./build/crop.sh` cuts release images.
- In game: devcommands (`spawn Boar 10 1`, `god`, `ghost`, `killall`) and the `trophytracker`
  console command (catalog, counters, discovered set, unknown pins).
- **Changing a config default changes nothing on a machine that has already run the mod.** Delete
  `BepInEx/config/com.jumpingmushroom.trophytracker.cfg` in the rig's profile after changing one.

## Releasing

`./build/package.sh` → `dist/TrophyTracker-X.Y.Z.zip`. Tag `vX.Y.Z`, push, `gh release create`, then
copy the zip to `~/Downloads` on the rig (`scp dist/TrophyTracker-X.Y.Z.zip <rig>:Downloads/`). The
user uploads it to Thunderstore themselves. No publish tooling.
