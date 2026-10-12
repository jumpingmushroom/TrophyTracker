# TrophyTracker 0.1.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A client-side Valheim mod that shows, on a movable HUD panel, how many kills are left until each pinned trophy drops, read from the game's own pity counter.

**Architecture:** A pure C# model (`src/TrophyTracker/Core/Model/`, no Unity or game types) holds every rule: pity arithmetic, the trophy catalog, pins, the discovered set, row building and text, panel layout and visibility. It is unit-tested on net8.0. A thin game layer (`Core/`, `Patches/`, `UI/`) reads game state into plain values, feeds the model, and binds the result to pooled uGUI rows under the vanilla HUD and to the Compendium trophy page.

**Tech Stack:** C# (LangVersion latest) targeting net472, BepInEx 5 + HarmonyX, `BepInEx.AssemblyPublicizer.MSBuild` 0.4.3, TextMeshPro, uGUI; xunit 2.9.2 on net8.0 for tests. No Jotunn.

**Spec:** `PLAN.md` (repo root). Read it first; section numbers below (§n) refer to it.

## Global Constraints

- Repo `/workspace/gamemods/Valheim/TrophyTracker`, GitHub `jumpingmushroom/TrophyTracker` (public, `main`).
- GUID `com.jumpingmushroom.trophytracker`, plugin name `TrophyTracker`, Thunderstore `Jumpingmushroom-TrophyTracker`, version `0.1.0` in `Plugin.cs`, the csproj and `thunderstore/manifest.json` together.
- Everything under `src/TrophyTracker/Core/Model/` must not reference `UnityEngine`, `BepInEx`, `HarmonyLib` or any game type.
- **Never add AI attribution** (`Co-Authored-By`, "Generated with", footers) to commits, PRs, README, changelog or release notes. Commit as the user (git's configured identity; `Johnny Dalen <johnny@jumpingmushroom.com>`). Commit **and push** after every task.
- Never commit the rig's address, user or paths. The rig is written `<rig>` in tracked files; the real target lives only in the gitignored `build/deploy.sh`, `logs.sh`, `shot.sh`, `crop.sh`.
- No publish tooling: no `build/publish.sh`, no `thunderstore.toml`.
- `lib/` (reference assemblies) and `decomp/` (decompiled game) are gitignored and already populated with the current game build (1.0.17). `lib/` additionally needs `BepInEx.dll` and `0Harmony.dll` (Task 1).
- Every dotnet command on the build box needs: `export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH`. Below, `$ENV` stands for that line.
- The rig's login shell is fish: wrap non-trivial remote commands in `bash -c '...'`.
- Every Harmony patch body is wrapped in try/catch → `TrophyTrackerPlugin.WarnOnce(key, e)`; prefixes return `void` (never skip the original).
- Code style: match Earshot (`/workspace/gamemods/Valheim/Earshot/src/Earshot`): explicit types, no `new()` target-typed, `/// <summary>` on types with a PLAN reference, `StringComparer.Ordinal` for id sets, `CultureInfo.InvariantCulture` for number formatting.

## File map

```
TrophyTracker.sln, Directory.Build.props, LICENSE, CHANGELOG.md, README.md, CLAUDE.md, .gitignore, PLAN.md
build/package.sh, build/make_icon.py                       (tracked)
build/deploy.sh, logs.sh, shot.sh, crop.sh                 (gitignored, local only)
thunderstore/manifest.json, thunderstore/README.md, thunderstore/icon.png
src/TrophyTracker/
  TrophyTracker.csproj, Plugin.cs, PluginConfig.cs, ConfigurationManagerAttributes.cs
  translations/English.txt                                  embedded resource
  Core/Model/  CounterEntry.cs PityMath.cs TrophyCatalog.cs PinList.cs DiscoveredSet.cs
               BarTracker.cs RowView.cs RowBuilder.cs RowText.cs Translations.cs
               PanelLayout.cs Visibility.cs
  Core/        Runtime.cs CatalogBuilder.cs CounterReader.cs Discovery.cs Keys.cs TrophyConsole.cs
  Patches/     EnemyHudPatch.cs DropRollPatch.cs KillPatch.cs TrophyPatch.cs CompendiumPatch.cs
  UI/          UiUtil.cs Sprites.cs TrophyPanel.cs CompendiumDecorator.cs PinToggle.cs
tests/TrophyTracker.Tests/
  TrophyTracker.Tests.csproj PityMathTests.cs PitySimulationTests.cs TrophyCatalogTests.cs
  PinListTests.cs DiscoveredSetTests.cs BarTrackerTests.cs RowBuilderTests.cs RowTextTests.cs
  PanelLayoutTests.cs VisibilityTests.cs TranslationsTests.cs TestData.cs
```

---

### Task 1: Scaffold the repo from the Earshot/Forewarned template

**Files:**
- Create: `Directory.Build.props`, `TrophyTracker.sln`, `LICENSE`, `CHANGELOG.md`, `README.md`, `CLAUDE.md`, `src/TrophyTracker/TrophyTracker.csproj`, `src/TrophyTracker/Plugin.cs`, `src/TrophyTracker/ConfigurationManagerAttributes.cs`, `src/TrophyTracker/translations/English.txt`, `tests/TrophyTracker.Tests/TrophyTracker.Tests.csproj`, `tests/TrophyTracker.Tests/SmokeTests.cs`, `thunderstore/manifest.json`, `build/package.sh`, `build/deploy.sh`, `build/logs.sh`, `build/shot.sh`, `build/crop.sh`
- Modify: `PLAN.md` (§2, §4, §6.2, §7 small corrections listed in Step 9)

**Interfaces:**
- Produces: `TrophyTracker.TrophyTrackerPlugin` with `internal static ManualLogSource Log` and `internal static void WarnOnce(string key, Exception e)`; embedded resource `TrophyTracker.English.txt`; test project compiling `src/TrophyTracker/Core/Model/*.cs`.

- [ ] **Step 1: Copy BepInEx/Harmony reference assemblies into lib/**

```bash
cp /workspace/gamemods/Valheim/Earshot/lib/BepInEx.dll /workspace/gamemods/Valheim/Earshot/lib/0Harmony.dll /workspace/gamemods/Valheim/TrophyTracker/lib/
ls /workspace/gamemods/Valheim/TrophyTracker/lib
```
Expected: the list includes `BepInEx.dll`, `0Harmony.dll`, `assembly_valheim.dll`, `Unity.TextMeshPro.dll`, `UnityEngine.UI.dll`.

- [ ] **Step 2: Copy and rename the template files**

```bash
cd /workspace/gamemods/Valheim/TrophyTracker
E=/workspace/gamemods/Valheim/Earshot; F=/workspace/gamemods/Valheim/Forewarned
cp $E/Directory.Build.props $E/LICENSE .
mkdir -p build tests/TrophyTracker.Tests thunderstore src/TrophyTracker/translations src/TrophyTracker/Core/Model src/TrophyTracker/Patches src/TrophyTracker/UI
cp $E/src/Earshot/ConfigurationManagerAttributes.cs src/TrophyTracker/
for s in package.sh deploy.sh logs.sh shot.sh crop.sh; do sed -e 's/Forewarned/TrophyTracker/g' -e 's/forewarned/trophytracker/g' $F/build/$s > build/$s; chmod +x build/$s; done
grep -n "TrophyTracker\|trophytracker" build/*.sh | head -20
```
Expected: every script names `TrophyTracker`; `deploy.sh` targets `.../profiles/Default/BepInEx/plugins/TrophyTracker`.

- [ ] **Step 3: Write the solution and project files**

`src/TrophyTracker/TrophyTracker.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>TrophyTracker</AssemblyName>
    <RootNamespace>TrophyTracker</RootNamespace>
    <Version>0.1.0</Version>
    <Description>Kills left until each pinned trophy drops, read from the game's own pity counter.</Description>
  </PropertyGroup>

  <ItemGroup>
    <!-- .NET Framework 4.7.2 reference assemblies; required to build net472 off-Windows. -->
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
    <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.3" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <!-- Publicized at build time only; the shipped DLL binds to the real members
         and runs against an unmodified game install. -->
    <Reference Include="assembly_valheim"  HintPath="$(ValheimManaged)/assembly_valheim.dll"  Publicize="true" Private="false" />
    <Reference Include="assembly_utils"    HintPath="$(ValheimManaged)/assembly_utils.dll"    Private="false" />
    <Reference Include="assembly_guiutils" HintPath="$(ValheimManaged)/assembly_guiutils.dll" Private="false" />

    <Reference Include="UnityEngine"                     HintPath="$(ValheimManaged)/UnityEngine.dll" Private="false" />
    <Reference Include="UnityEngine.CoreModule"          HintPath="$(ValheimManaged)/UnityEngine.CoreModule.dll" Private="false" />
    <Reference Include="UnityEngine.UI"                  HintPath="$(ValheimManaged)/UnityEngine.UI.dll" Private="false" />
    <Reference Include="UnityEngine.UIModule"            HintPath="$(ValheimManaged)/UnityEngine.UIModule.dll" Private="false" />
    <Reference Include="UnityEngine.TextRenderingModule" HintPath="$(ValheimManaged)/UnityEngine.TextRenderingModule.dll" Private="false" />
    <Reference Include="UnityEngine.InputLegacyModule"   HintPath="$(ValheimManaged)/UnityEngine.InputLegacyModule.dll" Private="false" />
    <Reference Include="Unity.TextMeshPro"               HintPath="$(ValheimManaged)/Unity.TextMeshPro.dll" Private="false" />

    <Reference Include="BepInEx"  HintPath="$(BepInExPath)/BepInEx.dll" Private="false" />
    <Reference Include="0Harmony" HintPath="$(BepInExPath)/0Harmony.dll" Private="false" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="translations/English.txt" LogicalName="TrophyTracker.English.txt" />
  </ItemGroup>

</Project>
```

`tests/TrophyTracker.Tests/TrophyTracker.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- Overrides Directory.Build.props (net472): the model is plain C#, so it is tested on the
       net8 runtime the build box has. Mono is not installed, so the plugin itself can't run here. -->
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>disable</Nullable>
    <!-- The invariant-globalization SDK flags the test host's localized resources; harmless. -->
    <NoWarn>$(NoWarn);NETSDK1188</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="../../src/TrophyTracker/Core/Model/*.cs" Link="Model/%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

Solution:

```bash
cd /workspace/gamemods/Valheim/TrophyTracker && $ENV
dotnet new sln -n TrophyTracker
dotnet sln TrophyTracker.sln add --solution-folder src src/TrophyTracker/TrophyTracker.csproj
dotnet sln TrophyTracker.sln add --solution-folder tests tests/TrophyTracker.Tests/TrophyTracker.Tests.csproj
```

- [ ] **Step 4: Write Plugin.cs (loads, logs, nothing else yet)**

`src/TrophyTracker/Plugin.cs`:

```csharp
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TrophyTracker
{
    /// <summary>
    /// How many kills are left until each pinned trophy drops, read from the game's own pity
    /// counter (PLAN.md §1). Reads game state and draws on the HUD; never changes drops. Client-side.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim.x86_64")]
    public sealed class TrophyTrackerPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jumpingmushroom.trophytracker";
        public const string PluginName = "TrophyTracker";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private static readonly HashSet<string> Warned = new HashSet<string>();
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(TrophyTrackerPlugin).Assembly);
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }

        /// <summary>Log an exception once per key, so a broken hook can't flood the log every frame.</summary>
        internal static void WarnOnce(string key, Exception e)
        {
            if (Warned.Add(key))
                Log.LogWarning(key + ": " + e);
        }
    }
}
```

`src/TrophyTracker/translations/English.txt` (final content; later tasks only read it):

```
# TrophyTracker's English strings: key = text.
# To translate, copy this file to BepInEx/config/TrophyTracker/translations/<Language>.txt, using the
# game's language name (German.txt, Norwegian.txt, ...), and translate the right-hand side. Keys you
# leave out fall back to English. {0} is replaced by a number. Creature names come from the game.

count_roll = roll
count_always = always
count_none = -
collected = collected
more = +{0} more
uncollected = Not yet collected. {0} drop. Click to pin.
uncollected_nochance = Not yet collected. Click to pin.
pinned = Pinned to the HUD. Click to unpin.
```

- [ ] **Step 5: Smoke test so the test project has something to run**

`tests/TrophyTracker.Tests/SmokeTests.cs`:

```csharp
using Xunit;

public class SmokeTests
{
    [Fact]
    public void TestProjectRuns()
    {
        Assert.True(true);
    }
}
```

(Delete this file in Task 2 once real tests exist.)

- [ ] **Step 6: Build and test**

```bash
cd /workspace/gamemods/Valheim/TrophyTracker && $ENV
dotnet build src/TrophyTracker/TrophyTracker.csproj -c Release --nologo -v minimal
dotnet test tests/TrophyTracker.Tests --nologo -v minimal
```
Expected: build succeeds with `TrophyTracker.dll` in `src/TrophyTracker/bin/Release/net472/`; 1 test passed. (The test project has no model files yet; the glob matching nothing is fine.)

- [ ] **Step 7: manifest, CHANGELOG, README stub, CLAUDE.md**

`thunderstore/manifest.json`:

```json
{
  "name": "TrophyTracker",
  "version_number": "0.1.0",
  "website_url": "https://github.com/jumpingmushroom/TrophyTracker",
  "dependencies": [
    "denikson-BepInExPack_Valheim-5.4.2350"
  ],
  "description": "Shows how many kills are left until a trophy drops, read from the game's own pity counter. Pin trophies from the Compendium; movable, configurable HUD panel. Client-side."
}
```

`CHANGELOG.md`:

```markdown
# Changelog

## 0.1.0 — first cut

- First release.
```

`README.md`: a one-paragraph stub (`# TrophyTracker` + the manifest description). Task 11 writes the full README.

`CLAUDE.md` — copy Forewarned's `CLAUDE.md` structure with these contents:

```markdown
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
```

- [ ] **Step 8: Deploy and confirm the plugin loads on the rig**

```bash
cd /workspace/gamemods/Valheim/TrophyTracker && ./build/deploy.sh
```
Expected: `==> done: TrophyTracker.dll ...`. Ask the user to launch (or relaunch) the game, then:

```bash
./build/logs.sh
```
Expected: a line `TrophyTracker 0.1.0 loaded.`

- [ ] **Step 9: Align PLAN.md with template conventions**

Edit `PLAN.md`:
- §2: replace the `Game/` block with `Core/` (Earshot's convention: `Core/` = game-facing runtime, `Core/Model/` = pure), listing `Runtime.cs CatalogBuilder.cs CounterReader.cs Discovery.cs Keys.cs TrophyConsole.cs`; add `PinToggle.cs` and `Sprites.cs`/`UiUtil.cs` under `UI/`; add `BarTracker.cs RowText.cs Translations.cs Visibility.cs CounterEntry.cs` under `Core/Model/`.
- §4: replace the `Offset` row with two rows `OffsetX` = 20 and `OffsetY` = 0 (floats, F1 sliders, like Earshot).
- §6.2: replace "divided by canvas scale and `Scale`" with "converted to the parent's local space with `RectTransformUtility.ScreenPointToLocalPointInRectangle`, so canvas scaling is handled; the panel's own `Scale` does not affect its position". Add: "In drag mode the panel gets a Canvas with `overrideSorting` so it draws above the inventory."
- §7 item 3: the badge is a small drawn dot sprite (no game sprite lookup).
- §3.1: add "If the HUD font lacks ★, ≤ or ✓ (checked once with `TMP_FontAsset.HasCharacter(c, true)`), ASCII stand-ins are used: `*`, `<=`, `+`."

- [ ] **Step 10: Commit and push**

```bash
cd /workspace/gamemods/Valheim/TrophyTracker
git add -A && git status --short
git commit -m "Scaffold from the Earshot/Forewarned template; plugin loads"
git push
```
Expected status lists no `build/deploy.sh`, `logs.sh`, `shot.sh`, `crop.sh`, `lib/`, `decomp/`, `bin/`, `obj/`.

---

### Task 2: PityMath and the simulation guard

**Files:**
- Create: `src/TrophyTracker/Core/Model/CounterEntry.cs`, `src/TrophyTracker/Core/Model/PityMath.cs`
- Test: `tests/TrophyTracker.Tests/PityMathTests.cs`, `tests/TrophyTracker.Tests/PitySimulationTests.cs`
- Delete: `tests/TrophyTracker.Tests/SmokeTests.cs`

**Interfaces:**
- Produces:
  - `struct CounterEntry { float Chance; int Remaining; CounterEntry(float chance, int remaining) }`
  - `static class PityMath { const float PityThreshold = 0.3f; const int MaxStars = 10; float EffectiveChance(float baseChance, bool levelMultiplier, int stars); bool UsesPity(float effectiveChance, bool noPseudoDrops); int SeedBound(float p); int WorstCaseUnseeded(float p); int KillsLeft(int remaining); int MatchTier(float stored, float baseChance, bool levelMultiplier) }`

- [ ] **Step 1: Write the failing tests**

`tests/TrophyTracker.Tests/PityMathTests.cs`:

```csharp
using TrophyTracker.Core.Model;
using Xunit;

public class PityMathTests
{
    [Theory]
    [InlineData(0.1f, true, 0, 0.1f)]
    [InlineData(0.1f, true, 1, 0.2f)]
    [InlineData(0.1f, true, 2, 0.4f)]
    [InlineData(0.1f, false, 2, 0.1f)]
    public void EffectiveChanceDoublesPerStarWithLevelMultiplier(float b, bool lm, int stars, float expected)
    {
        Assert.Equal(expected, PityMath.EffectiveChance(b, lm, stars));
    }

    [Theory]
    [InlineData(0.3f, false, true)]
    [InlineData(0.31f, false, false)]
    [InlineData(0.1f, true, false)]
    [InlineData(0f, false, false)]
    public void PityAppliesAtOrBelowThirtyPercentUnlessWorldDisablesIt(float p, bool noPseudo, bool expected)
    {
        Assert.Equal(expected, PityMath.UsesPity(p, noPseudo));
    }

    [Theory]
    [InlineData(0.1f, 20)]
    [InlineData(0.05f, 40)]
    [InlineData(0.2f, 10)]
    [InlineData(0.3f, 6)]
    public void WorstCaseFromAnUnseededCounterIsTheSeedBound(float p, int expected)
    {
        Assert.Equal(expected, PityMath.WorstCaseUnseeded(p));
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    public void KillsLeftIsAtLeastOne(int remaining, int expected)
    {
        Assert.Equal(expected, PityMath.KillsLeft(remaining));
    }

    [Fact]
    public void MatchTierFindsTheStarTierFromTheStoredChance()
    {
        Assert.Equal(0, PityMath.MatchTier(0.1f, 0.1f, true));
        Assert.Equal(1, PityMath.MatchTier(0.1f * 2f, 0.1f, true));
        Assert.Equal(-1, PityMath.MatchTier(0.15f, 0.1f, true));
        Assert.Equal(0, PityMath.MatchTier(0.1f, 0.1f, false));
        Assert.Equal(-1, PityMath.MatchTier(0.2f, 0.1f, false));
    }
}
```

`tests/TrophyTracker.Tests/PitySimulationTests.cs` — re-implements `CharacterDrop.GenerateDropList`'s counter loop (PLAN.md §1.1) and checks predictions against it:

```csharp
using System;
using TrophyTracker.Core.Model;
using Xunit;

public class PitySimulationTests
{
    /// <summary>The game's counter loop for one drop, PLAN.md §1.1. UnityEngine.Random.Range(int, int) is
    /// min-inclusive, max-exclusive, like System.Random.Next.</summary>
    private sealed class GameCounter
    {
        private readonly Random _rng;
        public CounterEntry? Entry;

        public GameCounter(int seed) { _rng = new Random(seed); }

        public bool Kill(float p)
        {
            int n;
            if (Entry.HasValue && Entry.Value.Chance == p)
            {
                n = Entry.Value.Remaining - 1;
            }
            else
            {
                n = _rng.Next(0, (int)(1f / p * 2f));
                Entry = new CounterEntry(p, n);
            }
            if (n <= 0)
            {
                Entry = new CounterEntry(p, _rng.Next(0, (int)(1f / p * 2f + 1f)));
                return true;
            }
            Entry = new CounterEntry(p, n);
            return false;
        }
    }

    [Theory]
    [InlineData(0.02f)]
    [InlineData(0.05f)]
    [InlineData(0.1f)]
    [InlineData(0.2f)]
    [InlineData(0.3f)]
    public void PredictedKillsLeftIsExactlyTheKillThatDrops(float p)
    {
        var game = new GameCounter(1234);
        // First cycle from an unseeded counter.
        int kills = 0;
        do { kills++; } while (!game.Kill(p));
        Assert.InRange(kills, 1, PityMath.WorstCaseUnseeded(p));

        for (int cycle = 0; cycle < 10000; cycle++)
        {
            int predicted = PityMath.KillsLeft(game.Entry.Value.Remaining);
            kills = 0;
            do { kills++; } while (!game.Kill(p));
            Assert.Equal(predicted, kills);
        }
    }

    [Fact]
    public void PredictionHoldsAfterEveryKillNotJustAfterDrops()
    {
        var game = new GameCounter(99);
        const float p = 0.1f;
        game.Kill(p);
        for (int i = 0; i < 10000; i++)
        {
            int predicted = PityMath.KillsLeft(game.Entry.Value.Remaining);
            bool dropped = game.Kill(p);
            Assert.Equal(predicted == 1, dropped);
        }
    }

    [Fact]
    public void UnseededWorstCaseIsReachedAndNeverExceeded()
    {
        const float p = 0.1f;
        int worst = 0;
        for (int trial = 0; trial < 10000; trial++)
        {
            var game = new GameCounter(trial);
            int kills = 0;
            do { kills++; } while (!game.Kill(p));
            worst = Math.Max(worst, kills);
        }
        Assert.Equal(PityMath.WorstCaseUnseeded(p), worst);
    }

    [Fact]
    public void KillingAnotherStarTierReseedsTheCounter()
    {
        var game = new GameCounter(7);
        float p0 = PityMath.EffectiveChance(0.1f, true, 0);
        float p1 = PityMath.EffectiveChance(0.1f, true, 1);
        game.Kill(p0);
        Assert.Equal(0, PityMath.MatchTier(game.Entry.Value.Chance, 0.1f, true));
        game.Kill(p1);
        Assert.Equal(1, PityMath.MatchTier(game.Entry.Value.Chance, 0.1f, true));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
cd /workspace/gamemods/Valheim/TrophyTracker && $ENV && rm tests/TrophyTracker.Tests/SmokeTests.cs
dotnet test tests/TrophyTracker.Tests --nologo -v minimal
```
Expected: compile errors `The type or namespace name 'TrophyTracker' could not be found` / `PityMath` missing.

- [ ] **Step 3: Implement**

`src/TrophyTracker/Core/Model/CounterEntry.cs`:

```csharp
namespace TrophyTracker.Core.Model
{
    /// <summary>One CharacterDrop.s_pseudoCounter value (PLAN.md §1.1): the effective chance the entry was
    /// seeded for (Item1) and the count it holds (Item2).</summary>
    public struct CounterEntry
    {
        public float Chance;
        public int Remaining;

        public CounterEntry(float chance, int remaining)
        {
            Chance = chance;
            Remaining = remaining;
        }
    }
}
```

`src/TrophyTracker/Core/Model/PityMath.cs`:

```csharp
using System;

namespace TrophyTracker.Core.Model
{
    /// <summary>
    /// PLAN.md §1.1-1.2: the arithmetic of CharacterDrop.GenerateDropList, mirrored. Float expressions are
    /// written exactly as the game writes them, because the game compares the stored chance with ==.
    /// </summary>
    public static class PityMath
    {
        public const float PityThreshold = 0.3f;
        public const int MaxStars = 10;

        /// <summary>The game: chance × max(1, (int)2^(level-1)) when the drop has a level multiplier. stars = level - 1.</summary>
        public static float EffectiveChance(float baseChance, bool levelMultiplier, int stars)
        {
            if (!levelMultiplier)
                return baseChance;
            int multiplier = Math.Max(1, (int)Math.Pow(2.0, stars));
            return baseChance * (float)multiplier;
        }

        /// <summary>Pity applies at or below 30% unless the world has the NoPseudoDrops modifier.</summary>
        public static bool UsesPity(float effectiveChance, bool noPseudoDrops)
        {
            return effectiveChance > 0f && effectiveChance <= PityThreshold && !noPseudoDrops;
        }

        /// <summary>Exclusive upper bound of the first seed: Random.Range(0, (int)(1/p × 2)).</summary>
        public static int SeedBound(float p)
        {
            return (int)(1f / p * 2f);
        }

        /// <summary>From an unseeded counter the drop lands on kill 1..SeedBound: seed 0 drops at once, seed s ≥ 1 drops on kill s + 1.</summary>
        public static int WorstCaseUnseeded(float p)
        {
            return Math.Max(1, SeedBound(p));
        }

        /// <summary>With a stored count n the k-th kill from now drops when n - k ≤ 0.</summary>
        public static int KillsLeft(int remaining)
        {
            return Math.Max(1, remaining);
        }

        /// <summary>The star tier whose effective chance equals the stored chance exactly, or -1.</summary>
        public static int MatchTier(float stored, float baseChance, bool levelMultiplier)
        {
            if (!levelMultiplier)
                return stored == baseChance ? 0 : -1;
            for (int stars = 0; stars <= MaxStars; stars++)
            {
                if (EffectiveChance(baseChance, true, stars) == stored)
                    return stars;
            }
            return -1;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test tests/TrophyTracker.Tests --nologo -v minimal
```
Expected: all PityMathTests and PitySimulationTests pass. If `PredictedKillsLeftIsExactlyTheKillThatDrops` fails, the reading of §1.1 is wrong: stop and re-read `decomp/valheim/CharacterDrop.cs:72-160` before touching the test.

- [ ] **Step 5: Commit and push**

```bash
git add -A && git commit -m "Model: pity arithmetic, checked against a simulation of the game's counter" && git push
```

---

### Task 3: Catalog, pins and the discovered set

**Files:**
- Create: `src/TrophyTracker/Core/Model/TrophyCatalog.cs`, `PinList.cs`, `DiscoveredSet.cs`
- Test: `tests/TrophyTracker.Tests/TestData.cs`, `TrophyCatalogTests.cs`, `PinListTests.cs`, `DiscoveredSetTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `class DropSource { string CreaturePrefab; string NameToken; float Chance; bool LevelMultiplier; }`
  - `class TrophyInfo { string Id; List<DropSource> Sources; }`
  - `class TrophyCatalog { IReadOnlyList<TrophyInfo> All; int SkippedEntries; TrophyInfo AddTrophy(string id); void AddSource(string trophyId, DropSource source); TrophyInfo Get(string id); IReadOnlyList<string> TrophiesForToken(string nameToken); IReadOnlyList<string> TrophiesForCreature(string creaturePrefab); bool IsCreature(string creaturePrefab) }`
  - `static class PinList { List<string> Parse(string text); string Serialize(IEnumerable<string> ids); string Toggle(string text, string id); string Remove(string text, string id); bool Contains(string text, string id) }`
  - `class DiscoveredSet { const string CustomDataKey = "TrophyTracker.Discovered"; int Count; bool Contains(string prefab); bool Add(string prefab); static DiscoveredSet Parse(string text); string Serialize(); bool Backfill(TrophyCatalog catalog, IDictionary<string, float> kills, ICollection<string> collected); bool IsTrophyDiscovered(TrophyInfo trophy, ICollection<string> collected); IEnumerable<string> Items }`
  - Test helper `TestData.Catalog()` used by later test files.

- [ ] **Step 1: Write the failing tests**

`tests/TrophyTracker.Tests/TestData.cs`:

```csharp
using TrophyTracker.Core.Model;

/// <summary>A small catalog shaped like the real one: one creature per trophy, a shared trophy, a bear
/// with variants, a high-chance trophy, a boss, and a trophy nobody drops.</summary>
internal static class TestData
{
    public static TrophyCatalog Catalog()
    {
        var c = new TrophyCatalog();
        c.AddSource("TrophyFenring", new DropSource { CreaturePrefab = "Fenring", NameToken = "$enemy_fenring", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophySurtling", new DropSource { CreaturePrefab = "Surtling", NameToken = "$enemy_surtling", Chance = 0.05f, LevelMultiplier = true });
        c.AddSource("TrophyDeer", new DropSource { CreaturePrefab = "Deer", NameToken = "$enemy_deer", Chance = 0.5f, LevelMultiplier = true });
        c.AddSource("TrophyEikthyr", new DropSource { CreaturePrefab = "Eikthyr", NameToken = "$enemy_eikthyr", Chance = 1f, LevelMultiplier = false });
        c.AddSource("TrophySkeleton", new DropSource { CreaturePrefab = "Skeleton", NameToken = "$enemy_skeleton", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophySkeleton", new DropSource { CreaturePrefab = "Skeleton_NoArcher", NameToken = "$enemy_skeleton", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophyBjorn", new DropSource { CreaturePrefab = "Bjorn_Variant", NameToken = "$enemy_bjorn_variant", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophyBjorn", new DropSource { CreaturePrefab = "Bjorn", NameToken = "$enemy_bjorn", Chance = 0.1f, LevelMultiplier = true });
        c.AddTrophy("TrophyOrphan");
        return c;
    }
}
```

`tests/TrophyTracker.Tests/TrophyCatalogTests.cs`:

```csharp
using System.Linq;
using TrophyTracker.Core.Model;
using Xunit;

public class TrophyCatalogTests
{
    [Fact]
    public void KeepsEveryTrophyInInsertionOrderIncludingOnesWithNoSource()
    {
        TrophyCatalog c = TestData.Catalog();
        Assert.Equal(new[] { "TrophyFenring", "TrophySurtling", "TrophyDeer", "TrophyEikthyr", "TrophySkeleton", "TrophyBjorn", "TrophyOrphan" },
            c.All.Select(t => t.Id).ToArray());
        Assert.Empty(c.Get("TrophyOrphan").Sources);
    }

    [Fact]
    public void MapsAllCreaturesThatDropATrophyNotJustTheFirst()
    {
        TrophyCatalog c = TestData.Catalog();
        Assert.Equal(new[] { "Bjorn_Variant", "Bjorn" }, c.Get("TrophyBjorn").Sources.Select(s => s.CreaturePrefab).ToArray());
    }

    [Fact]
    public void IgnoresTheSameCreatureTwice()
    {
        var c = new TrophyCatalog();
        c.AddSource("TrophyBoar", new DropSource { CreaturePrefab = "Boar", NameToken = "$enemy_boar", Chance = 0.15f });
        c.AddSource("TrophyBoar", new DropSource { CreaturePrefab = "Boar", NameToken = "$enemy_boar", Chance = 0.15f });
        Assert.Single(c.Get("TrophyBoar").Sources);
    }

    [Fact]
    public void LooksUpByTokenAndByCreature()
    {
        TrophyCatalog c = TestData.Catalog();
        Assert.Equal(new[] { "TrophySkeleton" }, c.TrophiesForToken("$enemy_skeleton").ToArray());
        Assert.Equal(new[] { "TrophyBjorn" }, c.TrophiesForCreature("Bjorn").ToArray());
        Assert.Empty(c.TrophiesForToken("$enemy_nobody"));
        Assert.True(c.IsCreature("Skeleton_NoArcher"));
        Assert.False(c.IsCreature("Greyling"));
        Assert.Null(c.Get("TrophyMissing"));
    }
}
```

`tests/TrophyTracker.Tests/PinListTests.cs`:

```csharp
using TrophyTracker.Core.Model;
using Xunit;

public class PinListTests
{
    [Fact]
    public void ParsesTrimsDropsEmptiesAndDuplicatesKeepingOrder()
    {
        Assert.Equal(new[] { "TrophyFenring", "TrophySerpent" }, PinList.Parse(" TrophyFenring, ,TrophySerpent,TrophyFenring ").ToArray());
        Assert.Empty(PinList.Parse(null));
        Assert.Empty(PinList.Parse(""));
    }

    [Fact]
    public void SerializeRoundTrips()
    {
        Assert.Equal("A,B,C", PinList.Serialize(PinList.Parse("A, B ,C")));
    }

    [Fact]
    public void ToggleAddsAtTheEndAndRemovesWherever()
    {
        Assert.Equal("A,B,C", PinList.Toggle("A,B", "C"));
        Assert.Equal("A,C", PinList.Toggle("A,B,C", "B"));
        Assert.Equal("A", PinList.Remove("A,B", "B"));
        Assert.Equal("A,B", PinList.Remove("A,B", "Z"));
        Assert.True(PinList.Contains("A, B", "B"));
        Assert.False(PinList.Contains("A,B", "b"));
    }
}
```

`tests/TrophyTracker.Tests/DiscoveredSetTests.cs`:

```csharp
using System.Collections.Generic;
using TrophyTracker.Core.Model;
using Xunit;

public class DiscoveredSetTests
{
    [Fact]
    public void ParsesAndSerializesSorted()
    {
        DiscoveredSet d = DiscoveredSet.Parse("Troll, Boar,,Boar");
        Assert.Equal(2, d.Count);
        Assert.Equal("Boar,Troll", d.Serialize());
        Assert.Equal(0, DiscoveredSet.Parse(null).Count);
    }

    [Fact]
    public void AddReportsWhetherItWasNew()
    {
        var d = new DiscoveredSet();
        Assert.True(d.Add("Boar"));
        Assert.False(d.Add("Boar"));
        Assert.True(d.Contains("Boar"));
    }

    [Fact]
    public void BackfillAddsKilledCreaturesAndEverySourceOfCollectedTrophies()
    {
        TrophyCatalog c = TestData.Catalog();
        var d = new DiscoveredSet();
        var kills = new Dictionary<string, float> { { "$enemy_fenring", 3f }, { "$enemy_deer", 0f } };
        var collected = new HashSet<string> { "TrophySkeleton" };

        Assert.True(d.Backfill(c, kills, collected));
        Assert.True(d.Contains("Fenring"));
        Assert.False(d.Contains("Deer"));
        Assert.True(d.Contains("Skeleton"));
        Assert.True(d.Contains("Skeleton_NoArcher"));
        Assert.False(d.Backfill(c, kills, collected));
    }

    [Fact]
    public void ATrophyIsDiscoveredWhenAnySourceIsOrItIsCollected()
    {
        TrophyCatalog c = TestData.Catalog();
        DiscoveredSet d = DiscoveredSet.Parse("Bjorn");
        var none = new HashSet<string>();
        Assert.True(d.IsTrophyDiscovered(c.Get("TrophyBjorn"), none));
        Assert.False(d.IsTrophyDiscovered(c.Get("TrophyFenring"), none));
        Assert.False(d.IsTrophyDiscovered(c.Get("TrophyOrphan"), none));
        Assert.True(d.IsTrophyDiscovered(c.Get("TrophyOrphan"), new HashSet<string> { "TrophyOrphan" }));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/TrophyTracker.Tests --nologo -v minimal`
Expected: compile errors for `TrophyCatalog`, `DropSource`, `PinList`, `DiscoveredSet`.

- [ ] **Step 3: Implement**

`src/TrophyTracker/Core/Model/TrophyCatalog.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>One creature that drops a trophy, with its base (0★) chance.</summary>
    public sealed class DropSource
    {
        public string CreaturePrefab;
        public string NameToken;
        public float Chance;
        public bool LevelMultiplier;
    }

    public sealed class TrophyInfo
    {
        public string Id;
        public readonly List<DropSource> Sources = new List<DropSource>();
    }

    /// <summary>
    /// PLAN.md §1.6, §5.1: every trophy item, and every creature that drops it. Built from ObjectDB (all
    /// trophies, even ones nobody drops) and ZNetScene (CharacterDrop on creature prefabs).
    /// </summary>
    public sealed class TrophyCatalog
    {
        private static readonly IReadOnlyList<string> None = new string[0];

        private readonly Dictionary<string, TrophyInfo> _byId = new Dictionary<string, TrophyInfo>(StringComparer.Ordinal);
        private readonly List<TrophyInfo> _all = new List<TrophyInfo>();
        private readonly Dictionary<string, List<string>> _byToken = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _byCreature = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public IReadOnlyList<TrophyInfo> All { get { return _all; } }

        /// <summary>Catalog entries skipped because a prefab was broken; reported by the console command.</summary>
        public int SkippedEntries;

        public TrophyInfo AddTrophy(string id)
        {
            TrophyInfo info;
            if (!_byId.TryGetValue(id, out info))
            {
                info = new TrophyInfo { Id = id };
                _byId[id] = info;
                _all.Add(info);
            }
            return info;
        }

        public void AddSource(string trophyId, DropSource source)
        {
            TrophyInfo info = AddTrophy(trophyId);
            foreach (DropSource existing in info.Sources)
            {
                if (existing.CreaturePrefab == source.CreaturePrefab)
                    return;
            }
            info.Sources.Add(source);
            Index(_byToken, source.NameToken, trophyId);
            Index(_byCreature, source.CreaturePrefab, trophyId);
        }

        public TrophyInfo Get(string id)
        {
            TrophyInfo info;
            return id != null && _byId.TryGetValue(id, out info) ? info : null;
        }

        public IReadOnlyList<string> TrophiesForToken(string nameToken)
        {
            List<string> list;
            return nameToken != null && _byToken.TryGetValue(nameToken, out list) ? list : None;
        }

        public IReadOnlyList<string> TrophiesForCreature(string creaturePrefab)
        {
            List<string> list;
            return creaturePrefab != null && _byCreature.TryGetValue(creaturePrefab, out list) ? list : None;
        }

        public bool IsCreature(string creaturePrefab)
        {
            return creaturePrefab != null && _byCreature.ContainsKey(creaturePrefab);
        }

        private static void Index(Dictionary<string, List<string>> map, string key, string trophyId)
        {
            if (key == null)
                return;
            List<string> list;
            if (!map.TryGetValue(key, out list))
            {
                list = new List<string>();
                map[key] = list;
            }
            if (!list.Contains(trophyId))
                list.Add(trophyId);
        }
    }
}
```

`src/TrophyTracker/Core/Model/PinList.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>The Pinned config string: comma-separated trophy prefab names, in pin order (PLAN.md §4).</summary>
    public static class PinList
    {
        public static List<string> Parse(string text)
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string raw in (text ?? "").Split(','))
            {
                string id = raw.Trim();
                if (id.Length > 0 && seen.Add(id))
                    list.Add(id);
            }
            return list;
        }

        public static string Serialize(IEnumerable<string> ids)
        {
            return string.Join(",", ids);
        }

        public static string Toggle(string text, string id)
        {
            List<string> list = Parse(text);
            if (!list.Remove(id))
                list.Add(id);
            return Serialize(list);
        }

        public static string Remove(string text, string id)
        {
            List<string> list = Parse(text);
            list.Remove(id);
            return Serialize(list);
        }

        public static bool Contains(string text, string id)
        {
            return Parse(text).Contains(id);
        }
    }
}
```

`src/TrophyTracker/Core/Model/DiscoveredSet.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrophyTracker.Core.Model
{
    /// <summary>
    /// PLAN.md §1.7, §5.2: creature prefabs this character has met (health bar seen, killed, or trophy
    /// collected), saved in Player.m_customData. Nothing undiscovered is shown anywhere.
    /// </summary>
    public sealed class DiscoveredSet
    {
        public const string CustomDataKey = "TrophyTracker.Discovered";

        private readonly HashSet<string> _set = new HashSet<string>(StringComparer.Ordinal);

        public int Count { get { return _set.Count; } }

        public IEnumerable<string> Items { get { return _set; } }

        public bool Contains(string prefab)
        {
            return prefab != null && _set.Contains(prefab);
        }

        public bool Add(string prefab)
        {
            return !string.IsNullOrEmpty(prefab) && _set.Add(prefab);
        }

        public static DiscoveredSet Parse(string text)
        {
            var d = new DiscoveredSet();
            foreach (string raw in (text ?? "").Split(','))
                d.Add(raw.Trim());
            return d;
        }

        public string Serialize()
        {
            return string.Join(",", _set.OrderBy(s => s, StringComparer.Ordinal));
        }

        /// <summary>Adds every source creature with kills, and every source of a collected trophy. True if anything was added.</summary>
        public bool Backfill(TrophyCatalog catalog, IDictionary<string, float> kills, ICollection<string> collected)
        {
            bool changed = false;
            foreach (TrophyInfo trophy in catalog.All)
            {
                bool owned = collected != null && collected.Contains(trophy.Id);
                foreach (DropSource s in trophy.Sources)
                {
                    float k;
                    bool killed = kills != null && s.NameToken != null && kills.TryGetValue(s.NameToken, out k) && k > 0f;
                    if ((owned || killed) && Add(s.CreaturePrefab))
                        changed = true;
                }
            }
            return changed;
        }

        public bool IsTrophyDiscovered(TrophyInfo trophy, ICollection<string> collected)
        {
            if (trophy == null)
                return false;
            if (collected != null && collected.Contains(trophy.Id))
                return true;
            foreach (DropSource s in trophy.Sources)
            {
                if (_set.Contains(s.CreaturePrefab))
                    return true;
            }
            return false;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/TrophyTracker.Tests --nologo -v minimal`
Expected: all pass.

- [ ] **Step 5: Commit and push**

```bash
git add -A && git commit -m "Model: trophy catalog with every source, pin list, discovered set" && git push
```

---

### Task 4: Rows: progress bars, row building, row text, translations

**Files:**
- Create: `src/TrophyTracker/Core/Model/BarTracker.cs`, `RowView.cs`, `RowBuilder.cs`, `RowText.cs`, `Translations.cs`
- Test: `tests/TrophyTracker.Tests/BarTrackerTests.cs`, `RowBuilderTests.cs`, `RowTextTests.cs`, `TranslationsTests.cs`

**Interfaces:**
- Consumes: `PityMath`, `CounterEntry`, `TrophyCatalog`, `TrophyInfo`, `DropSource`, `DiscoveredSet` (Tasks 2-3).
- Produces:
  - `class BarTracker { void Observe(string trophy, CounterEntry? before, CounterEntry? after); float Fill(string trophy, CounterEntry current); void Clear() }`
  - `enum RowState { Exact, Unknown, Roll, Always, NoSource, Collected }`
  - `struct RowView { string TrophyId; string NameToken; RowState State; int Count; int Stars; float ChancePercent; float BarFill; bool Dim; bool Recent; }`
  - `class RowList { List<RowView> Rows; int More; }`
  - `class RowInputs { TrophyCatalog Catalog; IList<string> Pins; DiscoveredSet Discovered; ICollection<string> Collected; IDictionary<string, CounterEntry> Counters; IDictionary<string, float> Kills; BarTracker Bars; IDictionary<string, float> FlashUntil; string RecentToken; float RecentTime; float RecentSeconds; bool ShowRecent; float Now; bool NoPseudoDrops; bool Dim; int MaxRows; }`
  - `static class RowBuilder { RowList Build(RowInputs i); DropSource PrimarySource(TrophyInfo t, IDictionary<string, float> kills, DiscoveredSet discovered) }`
  - `struct Glyphs { string Star; string AtMost; string Check; static Glyphs Unicode; static Glyphs Ascii }`
  - `static class RowText { string Count(RowView v, Translations tr, Glyphs g); string Chance(float percent); string More(int more, Translations tr) }`
  - `class Translations { static Translations Parse(string text); Translations WithFallback(Translations fallback); string Get(string key); IEnumerable<string> Keys }`

- [ ] **Step 1: Write the failing tests**

`tests/TrophyTracker.Tests/BarTrackerTests.cs`:

```csharp
using TrophyTracker.Core.Model;
using Xunit;

public class BarTrackerTests
{
    [Fact]
    public void FillsFromEmptyAtTheSeedToFullWhenTheNextKillDrops()
    {
        var b = new BarTracker();
        b.Observe("T", null, new CounterEntry(0.1f, 5));
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.1f, 5)));
        b.Observe("T", new CounterEntry(0.1f, 5), new CounterEntry(0.1f, 4));
        Assert.Equal(0.25f, b.Fill("T", new CounterEntry(0.1f, 4)));
        Assert.Equal(1f, b.Fill("T", new CounterEntry(0.1f, 1)));
    }

    [Fact]
    public void ADropOrATierChangeRestartsTheBar()
    {
        var b = new BarTracker();
        b.Observe("T", null, new CounterEntry(0.1f, 5));
        b.Observe("T", new CounterEntry(0.1f, 1), new CounterEntry(0.1f, 12));
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.1f, 12)));
        b.Observe("T", new CounterEntry(0.1f, 12), new CounterEntry(0.2f, 3));
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.2f, 3)));
    }

    [Fact]
    public void UnobservedOrSingleKillCountersAreHandled()
    {
        var b = new BarTracker();
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.1f, 6)));
        b.Observe("U", null, new CounterEntry(0.1f, 0));
        Assert.Equal(1f, b.Fill("U", new CounterEntry(0.1f, 0)));
    }
}
```

`tests/TrophyTracker.Tests/RowBuilderTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using TrophyTracker.Core.Model;
using Xunit;

public class RowBuilderTests
{
    private static RowInputs Inputs(params string[] pins)
    {
        return new RowInputs
        {
            Catalog = TestData.Catalog(),
            Pins = pins.ToList(),
            Discovered = DiscoveredSet.Parse("Fenring,Surtling,Deer,Eikthyr,Skeleton,Bjorn"),
            Collected = new HashSet<string>(),
            Counters = new Dictionary<string, CounterEntry>(),
            Kills = new Dictionary<string, float>(),
            Bars = new BarTracker(),
            FlashUntil = new Dictionary<string, float>(),
            RecentTime = float.NegativeInfinity,
            RecentSeconds = 60f,
            ShowRecent = true,
            Now = 100f,
            MaxRows = 8,
        };
    }

    private static RowView Only(RowInputs i)
    {
        return Assert.Single(RowBuilder.Build(i).Rows);
    }

    [Fact]
    public void UnknownBeforeTheFirstKillShowsTheWorstCase()
    {
        RowView v = Only(Inputs("TrophyFenring"));
        Assert.Equal(RowState.Unknown, v.State);
        Assert.Equal(20, v.Count);
        Assert.Equal(10f, v.ChancePercent, 3);
        Assert.Equal("$enemy_fenring", v.NameToken);
        Assert.Equal(-1f, v.BarFill);
    }

    [Fact]
    public void ExactWithTheTierItTracks()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Counters["TrophyFenring"] = new CounterEntry(PityMath.EffectiveChance(0.1f, true, 1), 7);
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(7, v.Count);
        Assert.Equal(1, v.Stars);
        Assert.Equal(0f, v.BarFill);
    }

    [Fact]
    public void ExactWithAnUnmatchedChanceHasNoStar()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Counters["TrophyFenring"] = new CounterEntry(0.123f, 0);
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(1, v.Count);
        Assert.Equal(-1, v.Stars);
    }

    [Fact]
    public void ExactIsDimmedInMultiplayer()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Dim = true;
        i.Counters["TrophyFenring"] = new CounterEntry(0.1f, 3);
        Assert.True(Only(i).Dim);
        i.Counters.Clear();
        Assert.False(Only(i).Dim);
    }

    [Fact]
    public void HighChanceRollsBossesAlwaysDropOrphansHaveNoSource()
    {
        Assert.Equal(RowState.Roll, Only(Inputs("TrophyDeer")).State);
        Assert.Equal(RowState.Always, Only(Inputs("TrophyEikthyr")).State);
        RowInputs i = Inputs("TrophyOrphan");
        i.Collected.Add("TrophyOrphan");
        RowView v = Only(i);
        Assert.Equal(RowState.NoSource, v.State);
        Assert.Null(v.NameToken);
    }

    [Fact]
    public void NoPseudoDropsTurnsEverythingIntoARoll()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.NoPseudoDrops = true;
        i.Counters["TrophyFenring"] = new CounterEntry(0.1f, 3);
        Assert.Equal(RowState.Roll, Only(i).State);
    }

    [Fact]
    public void UndiscoveredPinsAndUnknownPinsAreNotShown()
    {
        RowInputs i = Inputs("TrophySerpent", "TrophyFenring");
        i.Discovered = DiscoveredSet.Parse("Surtling");
        Assert.Empty(RowBuilder.Build(i).Rows);
    }

    [Fact]
    public void PrimarySourceIsTheMostKilledThenTheDiscoveredOne()
    {
        TrophyCatalog c = TestData.Catalog();
        TrophyInfo bear = c.Get("TrophyBjorn");
        var d = DiscoveredSet.Parse("Bjorn");
        Assert.Equal("Bjorn", RowBuilder.PrimarySource(bear, new Dictionary<string, float>(), d).CreaturePrefab);
        var kills = new Dictionary<string, float> { { "$enemy_bjorn_variant", 2f }, { "$enemy_bjorn", 1f } };
        Assert.Equal("Bjorn_Variant", RowBuilder.PrimarySource(bear, kills, d).CreaturePrefab);
    }

    [Fact]
    public void RecentKillComesFirstForSixtySecondsUnlessPinnedOrCollected()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.RecentToken = "$enemy_surtling";
        i.RecentTime = 50f;
        List<RowView> rows = RowBuilder.Build(i).Rows;
        Assert.Equal(new[] { "TrophySurtling", "TrophyFenring" }, rows.Select(r => r.TrophyId).ToArray());
        Assert.True(rows[0].Recent);
        Assert.False(rows[1].Recent);

        i.Now = 111f;
        Assert.Single(RowBuilder.Build(i).Rows);

        i.Now = 100f;
        i.Collected.Add("TrophySurtling");
        Assert.Single(RowBuilder.Build(i).Rows);

        i.Collected.Clear();
        i.ShowRecent = false;
        Assert.Single(RowBuilder.Build(i).Rows);

        i.ShowRecent = true;
        i.RecentToken = "$enemy_fenring";
        Assert.Single(RowBuilder.Build(i).Rows);
    }

    [Fact]
    public void CollectedFlashesThenFallsBackToTheNormalState()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Collected.Add("TrophyFenring");
        i.FlashUntil["TrophyFenring"] = 103f;
        Assert.Equal(RowState.Collected, Only(i).State);
        i.Now = 104f;
        Assert.Equal(RowState.Unknown, Only(i).State);
    }

    [Fact]
    public void MaxRowsCollapsesTheRestIntoMore()
    {
        RowInputs i = Inputs("TrophyFenring", "TrophySurtling", "TrophyDeer", "TrophyEikthyr");
        i.MaxRows = 2;
        RowList list = RowBuilder.Build(i);
        Assert.Equal(2, list.Rows.Count);
        Assert.Equal(2, list.More);
        Assert.Equal("TrophyFenring", list.Rows[0].TrophyId);
    }
}
```

`tests/TrophyTracker.Tests/RowTextTests.cs`:

```csharp
using TrophyTracker.Core.Model;
using Xunit;

public class RowTextTests
{
    private static readonly Translations Tr = Translations.Parse("count_roll = roll\ncount_always = always\ncount_none = -\ncollected = collected\nmore = +{0} more");

    [Fact]
    public void FormatsEveryState()
    {
        Assert.Equal("7 ★0", RowText.Count(new RowView { State = RowState.Exact, Count = 7, Stars = 0 }, Tr, Glyphs.Unicode));
        Assert.Equal("7", RowText.Count(new RowView { State = RowState.Exact, Count = 7, Stars = -1 }, Tr, Glyphs.Unicode));
        Assert.Equal("?  ≤20", RowText.Count(new RowView { State = RowState.Unknown, Count = 20 }, Tr, Glyphs.Unicode));
        Assert.Equal("roll", RowText.Count(new RowView { State = RowState.Roll }, Tr, Glyphs.Unicode));
        Assert.Equal("always", RowText.Count(new RowView { State = RowState.Always }, Tr, Glyphs.Unicode));
        Assert.Equal("-", RowText.Count(new RowView { State = RowState.NoSource }, Tr, Glyphs.Unicode));
        Assert.Equal("✓ collected", RowText.Count(new RowView { State = RowState.Collected }, Tr, Glyphs.Unicode));
    }

    [Fact]
    public void AsciiGlyphsWhenTheFontLacksTheSymbols()
    {
        Assert.Equal("3 *1", RowText.Count(new RowView { State = RowState.Exact, Count = 3, Stars = 1 }, Tr, Glyphs.Ascii));
        Assert.Equal("?  <=40", RowText.Count(new RowView { State = RowState.Unknown, Count = 40 }, Tr, Glyphs.Ascii));
    }

    [Theory]
    [InlineData(10f, "10%")]
    [InlineData(50f, "50%")]
    [InlineData(2.5f, "2.5%")]
    [InlineData(0.5f, "0.5%")]
    [InlineData(-1f, "")]
    public void FormatsChance(float percent, string expected)
    {
        Assert.Equal(expected, RowText.Chance(percent));
    }

    [Fact]
    public void FormatsMore()
    {
        Assert.Equal("+3 more", RowText.More(3, Tr));
    }

    [Fact]
    public void FallsBackToEnglishWordsWithoutTranslations()
    {
        Assert.Equal("roll", RowText.Count(new RowView { State = RowState.Roll }, new Translations(), Glyphs.Unicode));
    }
}
```

`tests/TrophyTracker.Tests/TranslationsTests.cs`:

```csharp
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TrophyTracker.Core.Model;
using Xunit;

public class TranslationsTests
{
    private static string Root([CallerFilePath] string here = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", ".."));
    }

    [Fact]
    public void ParsesKeysValuesAndComments()
    {
        Translations t = Translations.Parse("# comment\na = one\n b=two \nbad line\n");
        Assert.Equal("one", t.Get("a"));
        Assert.Equal("two", t.Get("B"));
        Assert.Null(t.Get("bad line"));
    }

    [Fact]
    public void FallsBack()
    {
        Translations t = Translations.Parse("a = eins").WithFallback(Translations.Parse("a = one\nb = two"));
        Assert.Equal("eins", t.Get("a"));
        Assert.Equal("two", t.Get("b"));
    }

    [Fact]
    public void EnglishFileHasEveryKeyTheCodeUses()
    {
        Translations en = Translations.Parse(File.ReadAllText(Path.Combine(Root(), "src", "TrophyTracker", "translations", "English.txt")));
        string[] used = { "count_roll", "count_always", "count_none", "collected", "more", "uncollected", "uncollected_nochance", "pinned" };
        Assert.Empty(used.Where(k => en.Get(k) == null));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/TrophyTracker.Tests --nologo -v minimal`
Expected: compile errors for `BarTracker`, `RowInputs`, `RowBuilder`, `RowText`, `Translations`, `Glyphs`.

- [ ] **Step 3: Implement**

`src/TrophyTracker/Core/Model/BarTracker.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>
    /// PLAN.md §3.2: the progress bar. Remembers the kills-left value each counter started from after its
    /// last (re)seed; empty at the seed, full when the next kill drops.
    /// </summary>
    public sealed class BarTracker
    {
        private readonly Dictionary<string, int> _start = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>A kill of a creature that drops this trophy went through GenerateDropList.</summary>
        public void Observe(string trophy, CounterEntry? before, CounterEntry? after)
        {
            if (!after.HasValue)
                return;
            // Seeded now (no entry, or a different tier), or the previous count was at 1 so this kill dropped and reseeded.
            bool reseeded = !before.HasValue || before.Value.Chance != after.Value.Chance || before.Value.Remaining <= 1;
            int left = PityMath.KillsLeft(after.Value.Remaining);
            int start;
            if (reseeded || !_start.TryGetValue(trophy, out start) || left > start)
                _start[trophy] = left;
        }

        public float Fill(string trophy, CounterEntry current)
        {
            int left = PityMath.KillsLeft(current.Remaining);
            int start;
            if (!_start.TryGetValue(trophy, out start) || left > start)
                return 0f;
            if (start <= 1)
                return 1f;
            return Math.Max(0f, Math.Min(1f, (start - left) / (float)(start - 1)));
        }

        public void Clear()
        {
            _start.Clear();
        }
    }
}
```

`src/TrophyTracker/Core/Model/RowView.cs`:

```csharp
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>PLAN.md §3.2.</summary>
    public enum RowState
    {
        Exact,
        Unknown,
        Roll,
        Always,
        NoSource,
        Collected,
    }

    /// <summary>One HUD row, already decided; the UI only binds it.</summary>
    public struct RowView
    {
        public string TrophyId;
        /// <summary>Creature name token of the primary source; null when the trophy has no source.</summary>
        public string NameToken;
        public RowState State;
        /// <summary>Exact: kills left. Unknown: the worst case.</summary>
        public int Count;
        /// <summary>Star tier the counter tracks, or -1 for no label.</summary>
        public int Stars;
        /// <summary>Base 0★ chance × 100, or -1 when there is no source.</summary>
        public float ChancePercent;
        /// <summary>0..1, or -1 when the bar is hidden.</summary>
        public float BarFill;
        public bool Dim;
        public bool Recent;
    }

    public sealed class RowList
    {
        public readonly List<RowView> Rows = new List<RowView>();
        public int More;
    }

    /// <summary>Everything RowBuilder reads, as plain values.</summary>
    public sealed class RowInputs
    {
        public TrophyCatalog Catalog;
        public IList<string> Pins;
        public DiscoveredSet Discovered;
        public ICollection<string> Collected;
        /// <summary>CharacterDrop.s_pseudoCounter, keyed by trophy prefab name.</summary>
        public IDictionary<string, CounterEntry> Counters;
        /// <summary>m_playerStats[0].m_enemyStats[0], keyed by creature name token.</summary>
        public IDictionary<string, float> Kills;
        public BarTracker Bars;
        /// <summary>Trophy → time its "collected" flash ends.</summary>
        public IDictionary<string, float> FlashUntil;
        public string RecentToken;
        public float RecentTime = float.NegativeInfinity;
        public float RecentSeconds;
        public bool ShowRecent;
        public float Now;
        public bool NoPseudoDrops;
        /// <summary>Multiplayer and DimInMultiplayer: exact counts may belong to another client (PLAN.md §1.4).</summary>
        public bool Dim;
        public int MaxRows;
    }
}
```

`src/TrophyTracker/Core/Model/RowBuilder.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>PLAN.md §3: which rows, in which order, in which state.</summary>
    public static class RowBuilder
    {
        public static RowList Build(RowInputs i)
        {
            var result = new RowList();
            var ordered = new List<RowView>();
            var pinned = new HashSet<string>(i.Pins ?? new List<string>(), StringComparer.Ordinal);

            string recent = RecentTrophy(i, pinned);
            if (recent != null)
                ordered.Add(Row(i, i.Catalog.Get(recent), true));

            if (i.Pins != null)
            {
                foreach (string id in i.Pins)
                {
                    TrophyInfo t = i.Catalog.Get(id);
                    if (t == null || !i.Discovered.IsTrophyDiscovered(t, i.Collected))
                        continue;
                    ordered.Add(Row(i, t, false));
                }
            }

            int max = Math.Max(1, i.MaxRows);
            for (int k = 0; k < ordered.Count && k < max; k++)
                result.Rows.Add(ordered[k]);
            result.More = Math.Max(0, ordered.Count - max);
            return result;
        }

        /// <summary>The source whose name a row shows: most kills; on a tie a discovered one; then catalog order.</summary>
        public static DropSource PrimarySource(TrophyInfo t, IDictionary<string, float> kills, DiscoveredSet discovered)
        {
            DropSource best = null;
            float bestKills = -1f;
            foreach (DropSource s in t.Sources)
            {
                float k = 0f;
                if (kills != null && s.NameToken != null)
                    kills.TryGetValue(s.NameToken, out k);
                bool better = k > bestKills ||
                    (k == bestKills && discovered != null && discovered.Contains(s.CreaturePrefab) && !discovered.Contains(best.CreaturePrefab));
                if (better)
                {
                    best = s;
                    bestKills = k;
                }
            }
            return best;
        }

        private static string RecentTrophy(RowInputs i, HashSet<string> pinned)
        {
            if (!i.ShowRecent || i.RecentToken == null || i.Now - i.RecentTime > i.RecentSeconds)
                return null;
            foreach (string id in i.Catalog.TrophiesForToken(i.RecentToken))
            {
                if (pinned.Contains(id))
                    continue;
                if (i.Collected.Contains(id) && !Flashing(i, id))
                    continue;
                if (!i.Discovered.IsTrophyDiscovered(i.Catalog.Get(id), i.Collected))
                    continue;
                return id;
            }
            return null;
        }

        private static bool Flashing(RowInputs i, string id)
        {
            float until;
            return i.FlashUntil != null && i.FlashUntil.TryGetValue(id, out until) && i.Now < until;
        }

        private static RowView Row(RowInputs i, TrophyInfo t, bool recent)
        {
            var v = new RowView { TrophyId = t.Id, Stars = -1, ChancePercent = -1f, BarFill = -1f, Recent = recent };
            DropSource s = PrimarySource(t, i.Kills, i.Discovered);
            if (s != null)
            {
                v.NameToken = s.NameToken;
                v.ChancePercent = s.Chance * 100f;
            }

            if (Flashing(i, t.Id))
            {
                v.State = RowState.Collected;
                return v;
            }
            if (s == null)
            {
                v.State = RowState.NoSource;
                return v;
            }
            if (s.Chance >= 1f)
            {
                v.State = RowState.Always;
                return v;
            }
            if (!PityMath.UsesPity(s.Chance, i.NoPseudoDrops))
            {
                v.State = RowState.Roll;
                return v;
            }

            CounterEntry c;
            if (i.Counters != null && i.Counters.TryGetValue(t.Id, out c))
            {
                v.State = RowState.Exact;
                v.Count = PityMath.KillsLeft(c.Remaining);
                v.Stars = s.LevelMultiplier ? PityMath.MatchTier(c.Chance, s.Chance, true) : -1;
                v.BarFill = i.Bars != null ? i.Bars.Fill(t.Id, c) : -1f;
                v.Dim = i.Dim;
                return v;
            }

            v.State = RowState.Unknown;
            v.Count = PityMath.WorstCaseUnseeded(s.Chance);
            return v;
        }
    }
}
```

`src/TrophyTracker/Core/Model/Translations.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>TrophyTracker's own strings: "key = text" lines, '#' comments. Missing keys fall back to English.</summary>
    public sealed class Translations
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Translations _fallback;

        public IEnumerable<string> Keys { get { return _map.Keys; } }

        public static Translations Parse(string text)
        {
            var t = new Translations();
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Length > 0 && value.Length > 0)
                    t._map[key] = value;
            }
            return t;
        }

        public Translations WithFallback(Translations fallback)
        {
            _fallback = fallback;
            return this;
        }

        public string Get(string key)
        {
            if (key == null)
                return null;
            string value;
            if (_map.TryGetValue(key, out value))
                return value;
            return _fallback != null ? _fallback.Get(key) : null;
        }
    }
}
```

`src/TrophyTracker/Core/Model/RowText.cs`:

```csharp
using System.Globalization;

namespace TrophyTracker.Core.Model
{
    /// <summary>Symbols used in row text; Ascii when the HUD font lacks them (PLAN.md §3.1).</summary>
    public struct Glyphs
    {
        public string Star;
        public string AtMost;
        public string Check;

        public static readonly Glyphs Unicode = new Glyphs { Star = "★", AtMost = "≤", Check = "✓" };
        public static readonly Glyphs Ascii = new Glyphs { Star = "*", AtMost = "<=", Check = "+" };
    }

    /// <summary>PLAN.md §3.2: the text of a row's count and chance columns.</summary>
    public static class RowText
    {
        public static string Count(RowView v, Translations tr, Glyphs g)
        {
            switch (v.State)
            {
                case RowState.Exact:
                    string n = v.Count.ToString(CultureInfo.InvariantCulture);
                    return v.Stars >= 0 ? n + " " + g.Star + v.Stars.ToString(CultureInfo.InvariantCulture) : n;
                case RowState.Unknown:
                    return "?  " + g.AtMost + v.Count.ToString(CultureInfo.InvariantCulture);
                case RowState.Roll:
                    return Word(tr, "count_roll", "roll");
                case RowState.Always:
                    return Word(tr, "count_always", "always");
                case RowState.Collected:
                    return g.Check + " " + Word(tr, "collected", "collected");
                default:
                    return Word(tr, "count_none", "-");
            }
        }

        public static string Chance(float percent)
        {
            if (percent < 0f)
                return "";
            string format = percent >= 10f ? "0" : percent >= 1f ? "0.#" : "0.##";
            return percent.ToString(format, CultureInfo.InvariantCulture) + "%";
        }

        public static string More(int more, Translations tr)
        {
            return Word(tr, "more", "+{0} more").Replace("{0}", more.ToString(CultureInfo.InvariantCulture));
        }

        private static string Word(Translations tr, string key, string english)
        {
            return (tr != null ? tr.Get(key) : null) ?? english;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/TrophyTracker.Tests --nologo -v minimal`
Expected: all pass.

- [ ] **Step 5: Commit and push**

```bash
git add -A && git commit -m "Model: row states, ordering, recent kill, collected flash, progress bar, row text" && git push
```

---

### Task 5: Panel layout and visibility

**Files:**
- Create: `src/TrophyTracker/Core/Model/PanelLayout.cs`, `Visibility.cs`
- Test: `tests/TrophyTracker.Tests/PanelLayoutTests.cs`, `VisibilityTests.cs`

**Interfaces:**
- Produces:
  - `enum PanelAnchor { TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight }`
  - `struct Vec2 { float X; float Y; Vec2(float x, float y) }`
  - `static class PanelLayout { Vec2 Pivot(PanelAnchor a); Vec2 AnchoredPosition(PanelAnchor a, float offsetX, float offsetY); Vec2 OffsetAfterDrag(PanelAnchor a, float offsetX, float offsetY, float dx, float dy) }`
  - `struct VisibilityInputs { bool Enabled, ToggledOn, HasPlayer, MapOpen, HideOnLargeMap, BuildMode, HideInBuildMode, InventoryOpen, HideWithInventory, DragMode, HideWhenEmpty; int RowCount; }` and `static class Visibility { bool Show(VisibilityInputs v) }`

- [ ] **Step 1: Write the failing tests**

`tests/TrophyTracker.Tests/PanelLayoutTests.cs`:

```csharp
using TrophyTracker.Core.Model;
using Xunit;

public class PanelLayoutTests
{
    [Theory]
    [InlineData(PanelAnchor.TopLeft, 0f, 1f)]
    [InlineData(PanelAnchor.TopCenter, 0.5f, 1f)]
    [InlineData(PanelAnchor.TopRight, 1f, 1f)]
    [InlineData(PanelAnchor.MiddleLeft, 0f, 0.5f)]
    [InlineData(PanelAnchor.MiddleCenter, 0.5f, 0.5f)]
    [InlineData(PanelAnchor.MiddleRight, 1f, 0.5f)]
    [InlineData(PanelAnchor.BottomLeft, 0f, 0f)]
    [InlineData(PanelAnchor.BottomCenter, 0.5f, 0f)]
    [InlineData(PanelAnchor.BottomRight, 1f, 0f)]
    public void PivotSitsInTheAnchoredCorner(PanelAnchor a, float x, float y)
    {
        Vec2 p = PanelLayout.Pivot(a);
        Assert.Equal(x, p.X);
        Assert.Equal(y, p.Y);
    }

    [Theory]
    [InlineData(PanelAnchor.TopLeft, 20f, -10f)]
    [InlineData(PanelAnchor.TopRight, -20f, -10f)]
    [InlineData(PanelAnchor.BottomRight, -20f, 10f)]
    [InlineData(PanelAnchor.BottomLeft, 20f, 10f)]
    [InlineData(PanelAnchor.MiddleLeft, 20f, 10f)]
    [InlineData(PanelAnchor.MiddleCenter, 20f, 10f)]
    public void PositiveOffsetPointsInwardFromTheAnchoredEdge(PanelAnchor a, float x, float y)
    {
        Vec2 p = PanelLayout.AnchoredPosition(a, 20f, 10f);
        Assert.Equal(x, p.X);
        Assert.Equal(y, p.Y);
    }

    [Theory]
    [InlineData(PanelAnchor.TopLeft)]
    [InlineData(PanelAnchor.TopRight)]
    [InlineData(PanelAnchor.MiddleCenter)]
    [InlineData(PanelAnchor.BottomRight)]
    public void DraggingMovesThePanelExactlyByTheDragDelta(PanelAnchor a)
    {
        Vec2 before = PanelLayout.AnchoredPosition(a, 20f, 10f);
        Vec2 o = PanelLayout.OffsetAfterDrag(a, 20f, 10f, 35f, -12f);
        Vec2 after = PanelLayout.AnchoredPosition(a, o.X, o.Y);
        Assert.Equal(before.X + 35f, after.X, 3);
        Assert.Equal(before.Y - 12f, after.Y, 3);
    }
}
```

`tests/TrophyTracker.Tests/VisibilityTests.cs`:

```csharp
using TrophyTracker.Core.Model;
using Xunit;

public class VisibilityTests
{
    private static VisibilityInputs Shown()
    {
        return new VisibilityInputs
        {
            Enabled = true, ToggledOn = true, HasPlayer = true,
            HideOnLargeMap = true, HideInBuildMode = true, HideWithInventory = true, HideWhenEmpty = true,
            RowCount = 2,
        };
    }

    [Fact]
    public void ShownByDefault()
    {
        Assert.True(Visibility.Show(Shown()));
    }

    [Fact]
    public void EachHideConditionHidesOnlyWhenItsToggleIsOn()
    {
        VisibilityInputs v = Shown(); v.MapOpen = true; Assert.False(Visibility.Show(v));
        v.HideOnLargeMap = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.BuildMode = true; Assert.False(Visibility.Show(v));
        v.HideInBuildMode = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.InventoryOpen = true; Assert.False(Visibility.Show(v));
        v.HideWithInventory = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.RowCount = 0; Assert.False(Visibility.Show(v));
        v.HideWhenEmpty = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.ToggledOn = false; Assert.False(Visibility.Show(v));
    }

    [Fact]
    public void DragModeShowsThePanelOverInventoryToggleAndEmpty()
    {
        VisibilityInputs v = Shown();
        v.InventoryOpen = true; v.DragMode = true; v.ToggledOn = false; v.RowCount = 0;
        Assert.True(Visibility.Show(v));
    }

    [Fact]
    public void NeverWithoutAPlayerOrWhenDisabled()
    {
        VisibilityInputs v = Shown(); v.HasPlayer = false; v.DragMode = true; Assert.False(Visibility.Show(v));
        v = Shown(); v.Enabled = false; v.DragMode = true; Assert.False(Visibility.Show(v));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/TrophyTracker.Tests --nologo -v minimal`
Expected: compile errors for `PanelAnchor`, `PanelLayout`, `Visibility`.

- [ ] **Step 3: Implement**

`src/TrophyTracker/Core/Model/PanelLayout.cs`:

```csharp
namespace TrophyTracker.Core.Model
{
    public enum PanelAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// PLAN.md §6.1-6.2. Anchor and pivot share the preset corner, so the panel grows away from it. Offsets
    /// point inward: X away from the left/right edge (right for centre columns), Y away from the top/bottom
    /// edge (up for the middle row). Unity UI space: y up.
    /// </summary>
    public static class PanelLayout
    {
        public static Vec2 Pivot(PanelAnchor a)
        {
            int col = (int)a % 3;
            int row = (int)a / 3;
            return new Vec2(col * 0.5f, 1f - row * 0.5f);
        }

        public static Vec2 AnchoredPosition(PanelAnchor a, float offsetX, float offsetY)
        {
            return new Vec2(SignX(a) * offsetX, SignY(a) * offsetY);
        }

        /// <summary>dx, dy: the drag in the parent's local units.</summary>
        public static Vec2 OffsetAfterDrag(PanelAnchor a, float offsetX, float offsetY, float dx, float dy)
        {
            return new Vec2(offsetX + SignX(a) * dx, offsetY + SignY(a) * dy);
        }

        private static float SignX(PanelAnchor a)
        {
            return (int)a % 3 == 2 ? -1f : 1f;
        }

        private static float SignY(PanelAnchor a)
        {
            return (int)a / 3 == 0 ? -1f : 1f;
        }
    }
}
```

`src/TrophyTracker/Core/Model/Visibility.cs`:

```csharp
namespace TrophyTracker.Core.Model
{
    public struct VisibilityInputs
    {
        public bool Enabled;
        public bool ToggledOn;
        public bool HasPlayer;
        public bool MapOpen;
        public bool HideOnLargeMap;
        public bool BuildMode;
        public bool HideInBuildMode;
        public bool InventoryOpen;
        public bool HideWithInventory;
        /// <summary>Inventory open and the drag modifier held (PLAN.md §6.2).</summary>
        public bool DragMode;
        public bool HideWhenEmpty;
        public int RowCount;
    }

    /// <summary>PLAN.md §6.4. Hiding with the vanilla HUD is done by parenting, not here.</summary>
    public static class Visibility
    {
        public static bool Show(VisibilityInputs v)
        {
            if (!v.Enabled || !v.HasPlayer)
                return false;
            if (v.DragMode)
                return true;
            if (!v.ToggledOn)
                return false;
            if (v.HideOnLargeMap && v.MapOpen)
                return false;
            if (v.HideInBuildMode && v.BuildMode)
                return false;
            if (v.HideWithInventory && v.InventoryOpen)
                return false;
            if (v.HideWhenEmpty && v.RowCount == 0)
                return false;
            return true;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/TrophyTracker.Tests --nologo -v minimal`
Expected: all pass.

- [ ] **Step 5: Commit and push**

```bash
git add -A && git commit -m "Model: panel anchors, inward offsets, drag maths, visibility rules" && git push
```

---

### Task 6: Game layer: config, catalog, counters, discovery, hooks, console

**Files:**
- Create: `src/TrophyTracker/PluginConfig.cs`, `src/TrophyTracker/Core/Runtime.cs`, `Core/CatalogBuilder.cs`, `Core/CounterReader.cs`, `Core/Discovery.cs`, `Core/Keys.cs`, `Core/TrophyConsole.cs`, `src/TrophyTracker/Patches/EnemyHudPatch.cs`, `Patches/DropRollPatch.cs`, `Patches/KillPatch.cs`, `Patches/TrophyPatch.cs`
- Modify: `src/TrophyTracker/Plugin.cs` (bind config, register console, tick Runtime)

**Interfaces:**
- Consumes: all model types from Tasks 2-5.
- Produces:
  - `PluginConfig` static `ConfigEntry`s: `Enabled`(bool), `ToggleKey`(KeyboardShortcut), `Pinned`(string), `UnpinWhenCollected`(bool), `ShowRecentKill`(bool), `RecentKillSeconds`(float), `Anchor`(PanelAnchor), `OffsetX`(float), `OffsetY`(float), `Scale`(float), `MaxRows`(int), `DragModifier`(KeyCode), `ShowIcon`(bool), `ShowChance`(bool), `ShowProgressBar`(bool), `DimInMultiplayer`(bool), `HideWithVanillaHud`(bool), `HideOnLargeMap`(bool), `HideInBuildMode`(bool), `HideWithInventory`(bool), `HideWhenEmpty`(bool); `static event Action Changed` raised on any SettingChanged.
  - `Runtime` statics: `TrophyCatalog Catalog`, `DiscoveredSet Discovered`, `BarTracker Bars`, `Dictionary<string,float> FlashUntil`, `Translations Tr`, `bool ToggledOn`, `bool Dirty`, `void Tick()`, `void OnKill(string nameToken)`, `void OnTrophyCollected(string id)`, `RowList BuildRows()`, `const float FlashSeconds = 5f`.
  - `CounterReader.Get(string trophy) → CounterEntry?`, `CounterReader.Snapshot() → Dictionary<string, CounterEntry>`, `CounterReader.NoPseudoDrops() → bool`
  - `Discovery.Load(Player)`, `Discovery.See(Character)`, `Discovery.Backfill()`, `Discovery.Kills() → IDictionary<string,float>`, `Discovery.Collected() → HashSet<string>`
  - `Keys.Down(KeyboardShortcut)`, `Keys.InputBlocked()`

- [ ] **Step 1: PluginConfig.cs**

```csharp
using System;
using BepInEx.Configuration;
using TrophyTracker.Core.Model;
using UnityEngine;

namespace TrophyTracker
{
    /// <summary>PLAN.md §4. Everything applies live; any change raises Changed.</summary>
    public static class PluginConfig
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> ToggleKey;

        public static ConfigEntry<string> Pinned;
        public static ConfigEntry<bool> UnpinWhenCollected;
        public static ConfigEntry<bool> ShowRecentKill;
        public static ConfigEntry<float> RecentKillSeconds;

        public static ConfigEntry<PanelAnchor> Anchor;
        public static ConfigEntry<float> OffsetX;
        public static ConfigEntry<float> OffsetY;
        public static ConfigEntry<float> Scale;
        public static ConfigEntry<int> MaxRows;
        public static ConfigEntry<KeyCode> DragModifier;

        public static ConfigEntry<bool> ShowIcon;
        public static ConfigEntry<bool> ShowChance;
        public static ConfigEntry<bool> ShowProgressBar;
        public static ConfigEntry<bool> DimInMultiplayer;

        public static ConfigEntry<bool> HideWithVanillaHud;
        public static ConfigEntry<bool> HideOnLargeMap;
        public static ConfigEntry<bool> HideInBuildMode;
        public static ConfigEntry<bool> HideWithInventory;
        public static ConfigEntry<bool> HideWhenEmpty;

        public static event Action Changed;

        private static ConfigurationManagerAttributes Attr(int order)
        {
            return new ConfigurationManagerAttributes { Order = order };
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("General", "Enabled", true, new ConfigDescription("Master switch.", null, Attr(100)));
            ToggleKey = cfg.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.F7),
                new ConfigDescription("Show or hide the panel for this session.", null, Attr(99)));

            Pinned = cfg.Bind("Pins", "Pinned", "",
                new ConfigDescription("Pinned trophies, comma-separated prefab names (TrophyBoar,TrophyNeck). Click a trophy in the Compendium to pin or unpin it.", null, Attr(90)));
            UnpinWhenCollected = cfg.Bind("Pins", "UnpinWhenCollected", true,
                new ConfigDescription("Unpin a trophy a few seconds after you collect it for the first time.", null, Attr(89)));
            ShowRecentKill = cfg.Bind("Pins", "ShowRecentKill", true,
                new ConfigDescription("Show the trophy of the creature you last killed for a while, if it isn't pinned or collected.", null, Attr(88)));
            RecentKillSeconds = cfg.Bind("Pins", "RecentKillSeconds", 60f,
                new ConfigDescription("How long the recent-kill row stays.", new AcceptableValueRange<float>(5f, 600f), Attr(87)));

            Anchor = cfg.Bind("Layout", "Anchor", PanelAnchor.MiddleLeft,
                new ConfigDescription("Screen corner or edge the panel is attached to.", null, Attr(80)));
            OffsetX = cfg.Bind("Layout", "OffsetX", 20f,
                new ConfigDescription("Distance from the anchored edge, in HUD pixels (positive is inward; right for centre anchors). Set by dragging too.", new AcceptableValueRange<float>(-3000f, 3000f), Attr(79)));
            OffsetY = cfg.Bind("Layout", "OffsetY", 0f,
                new ConfigDescription("Distance from the anchored edge, in HUD pixels (positive is inward; up for middle anchors). Set by dragging too.", new AcceptableValueRange<float>(-2000f, 2000f), Attr(78)));
            Scale = cfg.Bind("Layout", "Scale", 1f,
                new ConfigDescription("Size of the panel and its text.", new AcceptableValueRange<float>(0.5f, 2f), Attr(77)));
            MaxRows = cfg.Bind("Layout", "MaxRows", 8,
                new ConfigDescription("Rows shown at once; the rest collapse into '+N more'.", new AcceptableValueRange<int>(1, 12), Attr(76)));
            DragModifier = cfg.Bind("Layout", "DragModifier", KeyCode.LeftAlt,
                new ConfigDescription("With the inventory open, hold this and drag the panel to move it.", null, Attr(75)));

            ShowIcon = cfg.Bind("Rows", "ShowIcon", true, new ConfigDescription("Trophy icon.", null, Attr(70)));
            ShowChance = cfg.Bind("Rows", "ShowChance", true, new ConfigDescription("Base drop chance.", null, Attr(69)));
            ShowProgressBar = cfg.Bind("Rows", "ShowProgressBar", true,
                new ConfigDescription("A thin bar that fills as the guaranteed drop gets closer (exact counts only).", null, Attr(68)));
            DimInMultiplayer = cfg.Bind("Rows", "DimInMultiplayer", true,
                new ConfigDescription("Dim exact counts while other players are online: the counter belongs to whichever client controls the creature.", null, Attr(67)));

            HideWithVanillaHud = cfg.Bind("Visibility", "HideWithVanillaHud", true,
                new ConfigDescription("Hide when the game hides its HUD (Ctrl+F3, cutscenes).", null, Attr(60)));
            HideOnLargeMap = cfg.Bind("Visibility", "HideOnLargeMap", true, new ConfigDescription("Hide while the large map is open.", null, Attr(59)));
            HideInBuildMode = cfg.Bind("Visibility", "HideInBuildMode", true, new ConfigDescription("Hide while placing buildings.", null, Attr(58)));
            HideWithInventory = cfg.Bind("Visibility", "HideWithInventory", true,
                new ConfigDescription("Hide while the inventory is open (it still shows while you hold the drag key).", null, Attr(57)));
            HideWhenEmpty = cfg.Bind("Visibility", "HideWhenEmpty", true, new ConfigDescription("Hide when there is nothing to show.", null, Attr(56)));

            cfg.SettingChanged += (sender, args) =>
            {
                Action handler = Changed;
                if (handler != null)
                    handler();
            };
        }
    }
}
```

- [ ] **Step 2: Keys.cs (template idiom from GearSets/Trove)**

`src/TrophyTracker/Core/Keys.cs`:

```csharp
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace TrophyTracker.Core
{
    /// <summary>
    /// KeyboardShortcut.IsDown() fails while any other key is held (W while running); these only check the
    /// shortcut's own keys. InputBlocked: chat, console, menus and text fields have focus.
    /// </summary>
    internal static class Keys
    {
        public static bool Down(KeyboardShortcut s)
        {
            if (s.MainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(s.MainKey))
                return false;
            foreach (KeyCode m in s.Modifiers)
            {
                if (!UnityInput.Current.GetKey(m))
                    return false;
            }
            return true;
        }

        public static bool Held(KeyCode key)
        {
            return key != KeyCode.None && UnityInput.Current.GetKey(key);
        }

        public static bool InputBlocked()
        {
            if (Console.IsVisible()) return true;
            if (Menu.IsVisible()) return true;
            if (TextInput.IsVisible()) return true;
            if (Minimap.InTextInput()) return true;
            if (Chat.instance != null && Chat.instance.HasFocus()) return true;
            return false;
        }
    }
}
```

- [ ] **Step 3: CatalogBuilder.cs and CounterReader.cs**

`src/TrophyTracker/Core/CatalogBuilder.cs`:

```csharp
using System;
using TrophyTracker.Core.Model;
using UnityEngine;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §5.1: every ObjectDB trophy, and every ZNetScene creature that drops one.</summary>
    internal static class CatalogBuilder
    {
        public static TrophyCatalog Build(ObjectDB db, ZNetScene scene)
        {
            var catalog = new TrophyCatalog();
            foreach (GameObject go in db.m_items)
            {
                if (go == null)
                    continue;
                ItemDrop item = go.GetComponent<ItemDrop>();
                if (item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                    catalog.AddTrophy(go.name);
            }

            foreach (GameObject go in scene.m_prefabs)
            {
                if (go == null)
                    continue;
                try
                {
                    Character character = go.GetComponent<Character>();
                    CharacterDrop drops = go.GetComponent<CharacterDrop>();
                    if (character == null || drops == null || character is Player)
                        continue;
                    foreach (CharacterDrop.Drop drop in drops.m_drops)
                    {
                        if (drop == null || drop.m_prefab == null)
                            continue;
                        ItemDrop item = drop.m_prefab.GetComponent<ItemDrop>();
                        if (item == null || item.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                            continue;
                        catalog.AddSource(drop.m_prefab.name, new DropSource
                        {
                            CreaturePrefab = go.name,
                            NameToken = character.m_name,
                            Chance = drop.m_chance,
                            LevelMultiplier = drop.m_levelMultiplier,
                        });
                    }
                }
                catch (Exception e)
                {
                    catalog.SkippedEntries++;
                    TrophyTrackerPlugin.WarnOnce("catalog " + go.name, e);
                }
            }
            return catalog;
        }
    }
}
```

`src/TrophyTracker/Core/CounterReader.cs`:

```csharp
using System;
using System.Collections.Generic;
using TrophyTracker.Core.Model;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §1.1: CharacterDrop.s_pseudoCounter, read into plain values. If the game changes the
    /// field, reads fail once (logged) and every row falls back to Unknown/Roll.</summary>
    internal static class CounterReader
    {
        public static CounterEntry? Get(string trophy)
        {
            try
            {
                Tuple<float, int> value;
                if (CharacterDrop.s_pseudoCounter != null && CharacterDrop.s_pseudoCounter.TryGetValue(trophy, out value) && value != null)
                    return new CounterEntry(value.Item1, value.Item2);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("CounterReader.Get", e);
            }
            return null;
        }

        public static Dictionary<string, CounterEntry> Snapshot()
        {
            var result = new Dictionary<string, CounterEntry>(StringComparer.Ordinal);
            try
            {
                if (CharacterDrop.s_pseudoCounter != null)
                {
                    foreach (KeyValuePair<string, Tuple<float, int>> kv in CharacterDrop.s_pseudoCounter)
                    {
                        if (kv.Value != null)
                            result[kv.Key] = new CounterEntry(kv.Value.Item1, kv.Value.Item2);
                    }
                }
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("CounterReader.Snapshot", e);
            }
            return result;
        }

        public static bool NoPseudoDrops()
        {
            return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPseudoDrops);
        }
    }
}
```

- [ ] **Step 4: Discovery.cs**

`src/TrophyTracker/Core/Discovery.cs`:

```csharp
using System;
using System.Collections.Generic;
using TrophyTracker.Core.Model;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §5.2-5.3: the discovered set, kept in Player.m_customData so the game saves it with the character.</summary>
    internal static class Discovery
    {
        private static readonly Dictionary<string, float> NoKills = new Dictionary<string, float>();
        /// <summary>Character instances already checked, so EnemyHud's per-frame calls cost one HashSet lookup.</summary>
        private static readonly HashSet<int> Checked = new HashSet<int>();

        public static void Load(Player p)
        {
            Checked.Clear();
            string raw;
            p.m_customData.TryGetValue(DiscoveredSet.CustomDataKey, out raw);
            Runtime.Discovered = DiscoveredSet.Parse(raw);
            Backfill();
        }

        public static void Backfill()
        {
            Player p = Player.m_localPlayer;
            if (p == null || Runtime.Catalog == null)
                return;
            if (Runtime.Discovered.Backfill(Runtime.Catalog, Kills(), Collected()))
                Save(p);
            Runtime.Dirty = true;
        }

        public static void See(Character c)
        {
            if (c == null || !Checked.Add(c.GetInstanceID()) || c.IsPlayer() || Runtime.Catalog == null)
                return;
            string prefab = Utils.GetPrefabName(c.gameObject);
            if (!Runtime.Catalog.IsCreature(prefab) || !Runtime.Discovered.Add(prefab))
                return;
            Save(Player.m_localPlayer);
            Runtime.Dirty = true;
        }

        public static IDictionary<string, float> Kills()
        {
            Game game = Game.instance;
            PlayerProfile profile = game != null ? game.GetPlayerProfile() : null;
            if (profile == null || profile.m_playerStats == null || profile.m_playerStats[0] == null)
                return NoKills;
            return profile.m_playerStats[0].m_enemyStats[(int)KillModifiers.MixedAndTotal] ?? NoKills;
        }

        public static HashSet<string> Collected()
        {
            Player p = Player.m_localPlayer;
            return p != null ? new HashSet<string>(p.GetTrophies(), StringComparer.Ordinal) : new HashSet<string>();
        }

        private static void Save(Player p)
        {
            if (p != null)
                p.m_customData[DiscoveredSet.CustomDataKey] = Runtime.Discovered.Serialize();
        }
    }
}
```

- [ ] **Step 5: Runtime.cs**

`src/TrophyTracker/Core/Runtime.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using TrophyTracker.Core.Model;
using UnityEngine;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §5: shared state, the 1 s tick, catalog and player lifecycle, collected flashes.</summary>
    internal static class Runtime
    {
        public const float FlashSeconds = 5f;

        public static TrophyCatalog Catalog;
        public static DiscoveredSet Discovered = new DiscoveredSet();
        public static readonly BarTracker Bars = new BarTracker();
        public static readonly Dictionary<string, float> FlashUntil = new Dictionary<string, float>(StringComparer.Ordinal);
        public static Translations Tr = new Translations();
        public static bool ToggledOn = true;
        public static bool Dirty = true;

        private static string _recentToken;
        private static float _recentTime = float.NegativeInfinity;
        private static ZNetScene _catalogScene;
        private static Player _player;
        private static float _nextTick;
        private static bool _initialised;
        private static readonly List<string> Expired = new List<string>();

        public static void Tick()
        {
            if (!_initialised)
                TryInit();
            EnsureCatalog();
            EnsurePlayer();

            if (PluginConfig.Enabled.Value && Keys.Down(PluginConfig.ToggleKey.Value) && !Keys.InputBlocked())
            {
                ToggledOn = !ToggledOn;
                Dirty = true;
            }

            ExpireFlashes();

            if (Time.time >= _nextTick)
            {
                _nextTick = Time.time + 1f;
                Dirty = true;
            }
        }

        public static void OnKill(string nameToken)
        {
            _recentToken = nameToken;
            _recentTime = Time.time;
            Dirty = true;
        }

        public static void OnTrophyCollected(string id)
        {
            FlashUntil[id] = Time.time + FlashSeconds;
            Dirty = true;
        }

        public static RowList BuildRows()
        {
            var inputs = new RowInputs
            {
                Catalog = Catalog,
                Pins = PinList.Parse(PluginConfig.Pinned.Value),
                Discovered = Discovered,
                Collected = Discovery.Collected(),
                Counters = CounterReader.Snapshot(),
                Kills = Discovery.Kills(),
                Bars = Bars,
                FlashUntil = FlashUntil,
                RecentToken = _recentToken,
                RecentTime = _recentTime,
                RecentSeconds = PluginConfig.RecentKillSeconds.Value,
                ShowRecent = PluginConfig.ShowRecentKill.Value,
                Now = Time.time,
                NoPseudoDrops = CounterReader.NoPseudoDrops(),
                Dim = PluginConfig.DimInMultiplayer.Value && ZNet.instance != null && ZNet.instance.GetNrOfPlayers() > 1,
                MaxRows = PluginConfig.MaxRows.Value,
            };
            return RowBuilder.Build(inputs);
        }

        private static void EnsureCatalog()
        {
            ZNetScene scene = ZNetScene.instance;
            ObjectDB db = ObjectDB.instance;
            if (scene == null || db == null || db.m_items.Count == 0 || scene == _catalogScene)
                return;
            _catalogScene = scene;
            Catalog = CatalogBuilder.Build(db, scene);
            TrophyTrackerPlugin.Log.LogInfo("Catalog: " + Catalog.All.Count + " trophies" +
                (Catalog.SkippedEntries > 0 ? ", " + Catalog.SkippedEntries + " broken prefabs skipped" : ""));
            Discovery.Backfill();
        }

        private static void EnsurePlayer()
        {
            Player p = Player.m_localPlayer;
            if (p == _player)
                return;
            _player = p;
            FlashUntil.Clear();
            _recentToken = null;
            if (p != null)
                Discovery.Load(p);
            Dirty = true;
        }

        private static void ExpireFlashes()
        {
            if (FlashUntil.Count == 0)
                return;
            Expired.Clear();
            foreach (KeyValuePair<string, float> kv in FlashUntil)
            {
                if (Time.time >= kv.Value)
                    Expired.Add(kv.Key);
            }
            foreach (string id in Expired)
            {
                FlashUntil.Remove(id);
                if (PluginConfig.UnpinWhenCollected.Value && PinList.Contains(PluginConfig.Pinned.Value, id))
                    PluginConfig.Pinned.Value = PinList.Remove(PluginConfig.Pinned.Value, id);
                Dirty = true;
            }
        }

        /// <summary>Localization initialises on first access; the language is fixed for the session.</summary>
        private static void TryInit()
        {
            if (Localization.instance == null)
                return;
            _initialised = true;
            Translations english = Translations.Parse(ReadResource("TrophyTracker.English.txt"));
            string language = Localization.instance.GetSelectedLanguage();
            string custom = Path.Combine(Path.Combine(Path.Combine(Paths.ConfigPath, "TrophyTracker"), "translations"), language + ".txt");
            Tr = File.Exists(custom) ? Translations.Parse(File.ReadAllText(custom)).WithFallback(english) : english;
        }

        private static string ReadResource(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null)
                {
                    TrophyTrackerPlugin.Log.LogError("Missing embedded resource " + name);
                    return "";
                }
                using (var r = new StreamReader(s))
                    return r.ReadToEnd();
            }
        }
    }
}
```

- [ ] **Step 6: The four patches**

`src/TrophyTracker/Patches/EnemyHudPatch.cs`:

```csharp
using System;
using HarmonyLib;
using TrophyTracker.Core;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §1.7, §5.3: a creature's health bar appearing near you discovers it.</summary>
    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
    internal static class EnemyHudPatch
    {
        private static void Postfix(Character c)
        {
            try
            {
                Discovery.See(c);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("EnemyHud.ShowHud", e);
            }
        }
    }
}
```

`src/TrophyTracker/Patches/DropRollPatch.cs`:

```csharp
using System;
using System.Collections.Generic;
using HarmonyLib;
using TrophyTracker.Core;
using TrophyTracker.Core.Model;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §5.4: snapshot this creature's trophy counters around the roll, to see reseeds for the bar.</summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class DropRollPatch
    {
        private static void Prefix(CharacterDrop __instance, out Dictionary<string, CounterEntry?> __state)
        {
            __state = null;
            try
            {
                foreach (CharacterDrop.Drop drop in __instance.m_drops)
                {
                    if (drop == null || drop.m_prefab == null)
                        continue;
                    ItemDrop item = drop.m_prefab.GetComponent<ItemDrop>();
                    if (item == null || item.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                        continue;
                    if (__state == null)
                        __state = new Dictionary<string, CounterEntry?>();
                    __state[drop.m_prefab.name] = CounterReader.Get(drop.m_prefab.name);
                }
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("GenerateDropList prefix", e);
            }
        }

        private static void Postfix(Dictionary<string, CounterEntry?> __state)
        {
            if (__state == null)
                return;
            try
            {
                foreach (KeyValuePair<string, CounterEntry?> kv in __state)
                    Runtime.Bars.Observe(kv.Key, kv.Value, CounterReader.Get(kv.Key));
                Runtime.Dirty = true;
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("GenerateDropList postfix", e);
            }
        }
    }
}
```

`src/TrophyTracker/Patches/KillPatch.cs`:

```csharp
using System;
using HarmonyLib;
using TrophyTracker.Core;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §1.4, §5.5: runs on your client for every creature you damaged, owner or not.</summary>
    [HarmonyPatch(typeof(Game), nameof(Game.RPC_RegisterKill))]
    internal static class KillPatch
    {
        private static void Postfix(string enemyName)
        {
            try
            {
                Runtime.OnKill(enemyName);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("Game.RPC_RegisterKill", e);
            }
        }
    }
}
```

`src/TrophyTracker/Patches/TrophyPatch.cs`:

```csharp
using System;
using HarmonyLib;
using TrophyTracker.Core;
using UnityEngine;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §1.5, §5.6: a trophy entering the Compendium for the first time starts the collected flash.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AddTrophy))]
    internal static class TrophyPatch
    {
        private static void Prefix(Player __instance, out int __state)
        {
            __state = __instance.m_trophies.Count;
        }

        private static void Postfix(Player __instance, ItemDrop.ItemData item, int __state)
        {
            try
            {
                if (__instance != Player.m_localPlayer || __instance.m_trophies.Count == __state)
                    return;
                GameObject prefab = item.m_dropPrefab != null ? item.m_dropPrefab : ObjectDB.instance.GetItemPrefab(item.m_shared);
                if (prefab != null)
                    Runtime.OnTrophyCollected(prefab.name);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("Player.AddTrophy", e);
            }
        }
    }
}
```

- [ ] **Step 7: Console command**

`src/TrophyTracker/Core/TrophyConsole.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrophyTracker.Core.Model;

namespace TrophyTracker.Core
{
    /// <summary>"trophytracker": the catalog with every source, live counters with decoded tier, the discovered
    /// set and unknown pins (PLAN.md §7). Mirrored to the BepInEx log.</summary>
    internal static class TrophyConsole
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("trophytracker", "TrophyTracker: trophies, their sources, live counters, discovered creatures",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    try
                    {
                        Dump(args.Context);
                    }
                    catch (Exception e)
                    {
                        TrophyTrackerPlugin.WarnOnce("trophytracker console", e);
                    }
                });
        }

        private static void Say(Terminal ctx, string line)
        {
            if (ctx != null)
                ctx.AddString(line);
            TrophyTrackerPlugin.Log.LogInfo(line);
        }

        private static string Pct(float chance)
        {
            return (chance * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        private static void Dump(Terminal ctx)
        {
            TrophyCatalog cat = Runtime.Catalog;
            if (cat == null)
            {
                Say(ctx, "TrophyTracker: no catalog yet (load a world).");
                return;
            }
            Dictionary<string, CounterEntry> counters = CounterReader.Snapshot();
            HashSet<string> collected = Discovery.Collected();
            Say(ctx, "TrophyTracker: " + cat.All.Count + " trophies, " + Runtime.Discovered.Count + " creatures discovered, " +
                cat.SkippedEntries + " broken prefabs skipped, NoPseudoDrops=" + CounterReader.NoPseudoDrops());
            foreach (TrophyInfo t in cat.All)
            {
                string sources = t.Sources.Count == 0 ? "(no creature drops it)" :
                    string.Join(", ", t.Sources.Select(s => s.CreaturePrefab + " " + s.NameToken + " " + Pct(s.Chance) +
                        (Runtime.Discovered.Contains(s.CreaturePrefab) ? " seen" : "")));
                string counter = "";
                CounterEntry c;
                if (counters.TryGetValue(t.Id, out c))
                {
                    int tier = t.Sources.Count > 0 ? PityMath.MatchTier(c.Chance, t.Sources[0].Chance, t.Sources[0].LevelMultiplier) : -1;
                    counter = "  counter " + Pct(c.Chance) + " n=" + c.Remaining + (tier >= 0 ? " tier " + tier : " tier ?");
                }
                Say(ctx, "  " + t.Id + (collected.Contains(t.Id) ? " [owned]" : "") + ": " + sources + counter);
            }
            List<string> unknown = PinList.Parse(PluginConfig.Pinned.Value).Where(id => cat.Get(id) == null).ToList();
            if (unknown.Count > 0)
                Say(ctx, "Unknown pins (kept in config, not shown): " + string.Join(", ", unknown));
        }
    }
}
```

- [ ] **Step 8: Wire Plugin.cs**

In `src/TrophyTracker/Plugin.cs` add `using TrophyTracker.Core;`, and change `Awake` and add `Update`:

```csharp
        private void Awake()
        {
            Log = Logger;
            PluginConfig.Bind(Config);
            TrophyConsole.Register();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(TrophyTrackerPlugin).Assembly);

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            try
            {
                Runtime.Tick();
            }
            catch (Exception e)
            {
                WarnOnce("Runtime.Tick", e);
            }
        }
```

- [ ] **Step 9: Build, test, deploy**

```bash
cd /workspace/gamemods/Valheim/TrophyTracker && $ENV
dotnet test tests/TrophyTracker.Tests --nologo -v minimal && ./build/deploy.sh
```
Expected: tests pass, build has 0 errors (if `Player.AddTrophy` or `m_trophies` is reported inaccessible, the publicizer is not applied: check `Publicize="true"` on `assembly_valheim`).

- [ ] **Step 10: In-game check of catalog and counters (ask the user to drive)**

Ask the user to launch the game on the Default profile, load a world, open the console (F5) and run `devcommands` then `trophytracker`. Then:

```bash
./build/logs.sh
```
Expected: `Catalog: N trophies` with N ≥ 60; lines for `TrophyBjorn` and `TrophyBjornUndead` listing their sources (this answers PLAN.md §1.6 — record which case applies in PLAN.md §1.6); no warnings from TrophyTracker. Then have them `spawn Boar` near them and kill it, and run `trophytracker` again: `TrophyBoar ... counter 15% n=<k> tier 0`. Record the observed Bear/Vile cause in PLAN.md §1.6.

- [ ] **Step 11: Commit and push**

```bash
git add -A && git commit -m "Game layer: config, catalog, counters, discovery, kill and trophy hooks, console command" && git push
```

---

### Task 7: The HUD panel

**Files:**
- Create: `src/TrophyTracker/UI/UiUtil.cs`, `src/TrophyTracker/UI/Sprites.cs`, `src/TrophyTracker/UI/TrophyPanel.cs`
- Modify: `src/TrophyTracker/Core/Runtime.cs` (`Tick` calls `UI.TrophyPanel.Ensure()` last)

**Interfaces:**
- Consumes: `Runtime.BuildRows()`, `Runtime.Dirty`, `Runtime.ToggledOn`, `Runtime.Tr`, `Runtime.Catalog`, `RowText`, `Glyphs`, `PanelLayout`, `Visibility`, `PluginConfig.*`, `PluginConfig.Changed`.
- Produces: `TrophyPanel.Ensure()`; `TrophyPanel.Instance`; `Sprites.Plate`, `Sprites.White`, `Sprites.Dot`; `UiUtil.Rect`, `UiUtil.Text`, `UiUtil.Font`.

- [ ] **Step 1: UiUtil.cs and Sprites.cs**

Copy Earshot's helpers and rename the namespace:

```bash
cd /workspace/gamemods/Valheim/TrophyTracker
sed 's/namespace Earshot.UI/namespace TrophyTracker.UI/' /workspace/gamemods/Valheim/Earshot/src/Earshot/UI/UiUtil.cs > src/TrophyTracker/UI/UiUtil.cs
```

`src/TrophyTracker/UI/Sprites.cs` (Earshot's `Make` and plate, plus a solid square for bars and a dot for the pin badge):

```csharp
using System;
using UnityEngine;

namespace TrophyTracker.UI
{
    /// <summary>White sprites drawn in code and tinted: a rounded plate for 9-slicing, a solid square for bars,
    /// a round dot for the Compendium pin badge.</summary>
    internal static class Sprites
    {
        private static Sprite _plate;
        private static Sprite _white;
        private static Sprite _dot;

        public static Sprite Plate => _plate ?? (_plate = Make(32, PlatePixel, new Vector4(12f, 12f, 12f, 12f)));
        public static Sprite White => _white ?? (_white = Make(4, (x, y) => true, Vector4.zero));
        public static Sprite Dot => _dot ?? (_dot = Make(32, DotPixel, Vector4.zero));

        private static bool PlatePixel(float x, float y)
        {
            const float r = 10f;
            float dx = Math.Max(Math.Max(r - x, 0f), x - (32f - r));
            float dy = Math.Max(Math.Max(r - y, 0f), y - (32f - r));
            return dx * dx + dy * dy <= r * r;
        }

        private static bool DotPixel(float x, float y)
        {
            float dx = x - 16f, dy = y - 16f;
            return dx * dx + dy * dy <= 14f * 14f;
        }

        private static Sprite Make(int size, Func<float, float, bool> inside, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            if (inside(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss))
                                hits++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
```

- [ ] **Step 2: TrophyPanel.cs**

Fixed-column layout so nothing jitters: icon 24, name 150 (ellipsis), count 72 right-aligned, chance 46 right-aligned; row 28 high with a 3 px bar at the bottom; plate padding 8. Rows are created once (12 + one "more" line) and re-bound in place.

`src/TrophyTracker/UI/TrophyPanel.cs`:

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using TrophyTracker.Core;
using TrophyTracker.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyTracker.UI
{
    /// <summary>
    /// PLAN.md §3, §6. The HUD panel: pooled rows under Hud.m_rootObject (so the game's own HUD hiding applies),
    /// rebuilt from RowBuilder only when Runtime.Dirty is set. Drag mode: PLAN.md §6.2 (Task 8).
    /// </summary>
    internal sealed class TrophyPanel : MonoBehaviour
    {
        private const int PoolSize = 12;
        private const float RowHeight = 28f;
        private const float Pad = 8f;
        private const float IconSize = 24f;
        private const float NameWidth = 150f;
        private const float CountWidth = 72f;
        private const float ChanceWidth = 46f;
        private const float Gap = 6f;
        private const float BarHeight = 3f;

        private static readonly Color NameColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color RecentColor = new Color(0.65f, 0.85f, 1f, 1f);
        private static readonly Color ExactColor = new Color(1f, 0.82f, 0.36f, 1f);
        private static readonly Color MutedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color CollectedColor = new Color(0.55f, 0.95f, 0.55f, 1f);
        private static readonly Color BarBack = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color BarFront = new Color(1f, 0.82f, 0.36f, 0.9f);
        private const float DimAlpha = 0.5f;

        public static TrophyPanel Instance;

        private sealed class Row
        {
            public RectTransform Rect;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Count;
            public TextMeshProUGUI Chance;
            public Image BarBack;
            public Image BarFront;
            public string BoundTrophy;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private RectTransform _root;
        private Image _plate;
        private CanvasGroup _group;
        private TextMeshProUGUI _more;
        private Glyphs _glyphs = Glyphs.Unicode;
        private int _rowCount;
        private bool _layoutDirty = true;

        public static void Ensure()
        {
            if (Instance != null)
            {
                Instance.Tick();
                return;
            }
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null || UiUtil.Font == null)
                return;
            RectTransform holder = UiUtil.Rect("TrophyTracker", ParentFor(hud));
            Instance = holder.gameObject.AddComponent<TrophyPanel>();
            Instance.Build(holder);
        }

        private static Transform ParentFor(Hud hud)
        {
            Transform root = hud.m_rootObject.transform;
            return PluginConfig.HideWithVanillaHud.Value || root.parent == null ? root : root.parent;
        }

        private void Build(RectTransform root)
        {
            _root = root;
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _group.alpha = 0f;
            _plate = root.gameObject.AddComponent<Image>();
            _plate.sprite = Sprites.Plate;
            _plate.type = Image.Type.Sliced;
            _plate.color = new Color(0f, 0f, 0f, 0.55f);
            _plate.raycastTarget = false;
            for (int i = 0; i < PoolSize; i++)
                _rows.Add(MakeRow(i));
            _more = UiUtil.Text(root, "More", 16f);
            _more.color = MutedColor;
            Place(_more.rectTransform, 0f, NameWidth, RowHeight);

            TMP_FontAsset font = UiUtil.Font;
            bool unicode = font != null && font.HasCharacter('★', true) && font.HasCharacter('≤', true) && font.HasCharacter('✓', true);
            _glyphs = unicode ? Glyphs.Unicode : Glyphs.Ascii;

            PluginConfig.Changed += OnConfigChanged;
        }

        private void OnDestroy()
        {
            PluginConfig.Changed -= OnConfigChanged;
            if (Instance == this)
                Instance = null;
        }

        private void OnConfigChanged()
        {
            _layoutDirty = true;
            Runtime.Dirty = true;
        }

        private Row MakeRow(int i)
        {
            RectTransform rt = UiUtil.Rect("Row" + i, _root);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            var row = new Row { Rect = rt };

            RectTransform iconRt = UiUtil.Rect("Icon", rt);
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0f, 1f);
            iconRt.sizeDelta = new Vector2(IconSize, IconSize);
            row.Icon = iconRt.gameObject.AddComponent<Image>();
            row.Icon.raycastTarget = false;
            row.Icon.preserveAspect = true;

            row.Name = UiUtil.Text(rt, "Name", 18f);
            row.Name.overflowMode = TextOverflowModes.Ellipsis;
            row.Count = UiUtil.Text(rt, "Count", 18f);
            row.Count.alignment = TextAlignmentOptions.MidlineRight;
            row.Chance = UiUtil.Text(rt, "Chance", 15f);
            row.Chance.alignment = TextAlignmentOptions.MidlineRight;
            row.Chance.color = MutedColor;

            row.BarBack = MakeBar(rt, "BarBack", BarBack);
            row.BarFront = MakeBar(rt, "BarFront", BarFront);
            rt.gameObject.SetActive(false);
            return row;
        }

        private static Image MakeBar(RectTransform parent, string name, Color color)
        {
            RectTransform rt = UiUtil.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Sprites.White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Top-left anchored child at x, with a fixed width, vertically in the text band of the row.</summary>
        private static void Place(RectTransform rt, float x, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, height);
        }

        private float RowWidth()
        {
            float w = NameWidth + Gap + CountWidth;
            if (PluginConfig.ShowIcon.Value)
                w += IconSize + Gap;
            if (PluginConfig.ShowChance.Value)
                w += Gap + ChanceWidth;
            return w;
        }

        private void ApplyLayout()
        {
            _layoutDirty = false;
            Hud hud = Hud.instance;
            if (hud != null && _root.parent != ParentFor(hud))
                _root.SetParent(ParentFor(hud), false);

            PanelAnchor anchor = PluginConfig.Anchor.Value;
            Vec2 pivot = PanelLayout.Pivot(anchor);
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(pivot.X, pivot.Y);
            ApplyPosition(PluginConfig.OffsetX.Value, PluginConfig.OffsetY.Value);
            float s = PluginConfig.Scale.Value;
            _root.localScale = new Vector3(s, s, 1f);

            bool icon = PluginConfig.ShowIcon.Value;
            float x = 0f;
            foreach (Row row in _rows)
            {
                x = 0f;
                row.Icon.gameObject.SetActive(icon);
                if (icon)
                {
                    row.Icon.rectTransform.anchoredPosition = new Vector2(0f, -(RowHeight - BarHeight - IconSize) / 2f);
                    x += IconSize + Gap;
                }
                Place(row.Name.rectTransform, x, NameWidth, RowHeight - BarHeight);
                x += NameWidth + Gap;
                Place(row.Count.rectTransform, x, CountWidth, RowHeight - BarHeight);
                x += CountWidth;
                row.Chance.gameObject.SetActive(PluginConfig.ShowChance.Value);
                if (PluginConfig.ShowChance.Value)
                {
                    Place(row.Chance.rectTransform, x + Gap, ChanceWidth, RowHeight - BarHeight);
                }
                float barX = icon ? IconSize + Gap : 0f;
                row.BarBack.rectTransform.anchoredPosition = new Vector2(barX, 0f);
                row.BarBack.rectTransform.sizeDelta = new Vector2(RowWidth() - barX, BarHeight);
                row.BarFront.rectTransform.anchoredPosition = new Vector2(barX, 0f);
            }
        }

        /// <summary>Also used live while dragging (Task 8), without writing config.</summary>
        internal void ApplyPosition(float offsetX, float offsetY)
        {
            Vec2 pos = PanelLayout.AnchoredPosition(PluginConfig.Anchor.Value, offsetX, offsetY);
            _root.anchoredPosition = new Vector2(pos.X, pos.Y);
        }

        private void Tick()
        {
            try
            {
                if (_layoutDirty)
                    ApplyLayout();
                if (Runtime.Dirty)
                {
                    Runtime.Dirty = false;
                    Bind(Runtime.Catalog != null ? Runtime.BuildRows() : new RowList());
                }
                _group.alpha = Visibility.Show(VisibilityNow()) ? 1f : 0f;
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("TrophyPanel", e);
                _group.alpha = 0f;
            }
        }

        private VisibilityInputs VisibilityNow()
        {
            Player p = Player.m_localPlayer;
            return new VisibilityInputs
            {
                Enabled = PluginConfig.Enabled.Value,
                ToggledOn = Runtime.ToggledOn,
                HasPlayer = p != null,
                MapOpen = Minimap.IsOpen(),
                HideOnLargeMap = PluginConfig.HideOnLargeMap.Value,
                BuildMode = p != null && p.InPlaceMode(),
                HideInBuildMode = PluginConfig.HideInBuildMode.Value,
                InventoryOpen = InventoryGui.IsVisible(),
                HideWithInventory = PluginConfig.HideWithInventory.Value,
                DragMode = DragModeActive(),
                HideWhenEmpty = PluginConfig.HideWhenEmpty.Value,
                RowCount = _rowCount,
            };
        }

        /// <summary>Replaced in Task 8.</summary>
        private bool DragModeActive()
        {
            return false;
        }

        private void Bind(RowList list)
        {
            _rowCount = list.Rows.Count;
            float width = RowWidth();
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool on = i < list.Rows.Count;
                if (row.Rect.gameObject.activeSelf != on)
                    row.Rect.gameObject.SetActive(on);
                if (!on)
                    continue;
                RowView v = list.Rows[i];
                row.Rect.anchoredPosition = new Vector2(Pad, -Pad - i * RowHeight);
                row.Rect.sizeDelta = new Vector2(width, RowHeight);
                if (row.BoundTrophy != v.TrophyId)
                {
                    row.BoundTrophy = v.TrophyId;
                    row.Icon.sprite = IconFor(v.TrophyId);
                }
                SetText(row.Name, v.NameToken != null ? Localization.instance.Localize(v.NameToken) : TrophyName(v.TrophyId));
                row.Name.color = v.Recent ? RecentColor : NameColor;
                SetText(row.Count, RowText.Count(v, Runtime.Tr, _glyphs));
                Color count = v.State == RowState.Exact ? ExactColor : v.State == RowState.Collected ? CollectedColor : MutedColor;
                if (v.Dim)
                    count.a = DimAlpha;
                row.Count.color = count;
                SetText(row.Chance, RowText.Chance(v.ChancePercent));

                bool bar = PluginConfig.ShowProgressBar.Value && v.BarFill >= 0f;
                row.BarBack.gameObject.SetActive(bar);
                row.BarFront.gameObject.SetActive(bar && v.BarFill > 0f);
                if (bar)
                {
                    float full = row.BarBack.rectTransform.sizeDelta.x;
                    row.BarFront.rectTransform.sizeDelta = new Vector2(full * v.BarFill, BarHeight);
                    Color front = BarFront;
                    if (v.Dim)
                        front.a *= DimAlpha;
                    row.BarFront.color = front;
                }
            }

            int lines = list.Rows.Count;
            _more.gameObject.SetActive(list.More > 0);
            if (list.More > 0)
            {
                SetText(_more, RowText.More(list.More, Runtime.Tr));
                _more.rectTransform.anchoredPosition = new Vector2(Pad, -Pad - lines * RowHeight);
                lines++;
            }
            _root.sizeDelta = new Vector2(width + 2f * Pad, Math.Max(1, lines) * RowHeight + 2f * Pad);
        }

        private static void SetText(TextMeshProUGUI t, string text)
        {
            if (t.text != text)
                t.text = text;
        }

        private Sprite IconFor(string trophy)
        {
            Sprite s;
            if (_icons.TryGetValue(trophy, out s))
                return s;
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(trophy) : null;
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            s = item != null ? item.m_itemData.GetIcon() : null;
            _icons[trophy] = s;
            return s;
        }

        /// <summary>The trophy's own name minus " trophy", as the vanilla Compendium does.</summary>
        internal static string TrophyName(string trophy)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(trophy) : null;
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null)
                return trophy;
            string name = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
            return name.EndsWith(" trophy", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 7) : name;
        }
    }
}
```

- [ ] **Step 3: Hook the panel into the tick**

In `Runtime.Tick()`, add as the last line: `UI.TrophyPanel.Ensure();` and add `using TrophyTracker.UI;` if preferred over the qualified name.

- [ ] **Step 4: Build, test, deploy**

```bash
$ENV && dotnet test tests/TrophyTracker.Tests --nologo -v minimal && ./build/deploy.sh
```
Expected: tests pass, build has 0 errors and 0 new warnings.

- [ ] **Step 5: In-game check (user drives; take screenshots)**

Set pins on the rig without the Compendium (it comes in Task 9): ask the user to close the game, then:

```bash
cd /workspace/gamemods/Valheim/TrophyTracker
eval "$(grep -E '^(SSH_TARGET|PROFILE)=' build/deploy.sh)"   # the rig target lives only in the gitignored script
ssh "$SSH_TARGET" "sed -i 's/^Pinned = .*/Pinned = TrophyBoar,TrophyDeer,TrophyEikthyr,TrophyNeck/' '$PROFILE/BepInEx/config/com.jumpingmushroom.trophytracker.cfg' && grep '^Pinned' '$PROFILE/BepInEx/config/com.jumpingmushroom.trophytracker.cfg'"
```
Ask the user to launch and check, then `./build/shot.sh probe-panel`:
- Panel at the middle-left; Boar `?  ≤13  15%` (if discovered), Deer `roll  50%`, Eikthyr `always 100%` (if met).
- Kill a Boar (`spawn Boar`): row turns gold `n ★0` with an empty bar; kill more: the bar fills; a 1★ Boar (`spawn Boar 1 2`) shows `★1` with a new count.
- The killed creature (if not pinned) appears at the top in light blue for 60 s.
- F7 hides/shows; large map, build mode, inventory hide it; Ctrl+F3 hides it.
- F1: change Anchor, OffsetX/Y, Scale, MaxRows, ShowIcon/ShowChance/ShowProgressBar: each applies live.
Fix anything off, redeploy, recheck.

- [ ] **Step 6: Commit and push**

```bash
git add -A && git commit -m "HUD panel: pooled rows, live layout, visibility, toggle key" && git push
```

---

### Task 8: Drag mode

**Files:**
- Modify: `src/TrophyTracker/UI/TrophyPanel.cs`

**Interfaces:**
- Consumes: `PanelLayout.OffsetAfterDrag`, `Keys.Held`, `PluginConfig.DragModifier/OffsetX/OffsetY`, `TrophyPanel.ApplyPosition`.
- Produces: `TrophyPanel.DragModeActive()` real implementation.

- [ ] **Step 1: Add drag state and the per-frame drag handler**

Add fields to `TrophyPanel`:

```csharp
        private Canvas _sortCanvas;
        private bool _dragging;
        private Vector2 _dragStartLocal;
        private float _dragStartX;
        private float _dragStartY;
        private float _liveX;
        private float _liveY;
        private bool _outline;
```

At the end of `Build(...)` (after the pool is created):

```csharp
            // Drawn above the inventory while dragging (PLAN.md §6.2); off otherwise.
            _sortCanvas = root.gameObject.AddComponent<Canvas>();
            _sortCanvas.overrideSorting = false;
```

Replace `DragModeActive()`:

```csharp
        private bool DragModeActive()
        {
            return PluginConfig.Enabled.Value && InventoryGui.IsVisible() && Keys.Held(PluginConfig.DragModifier.Value);
        }
```

Add a call `Drag();` at the start of `Tick()`'s try block, and the method:

```csharp
        /// <summary>PLAN.md §6.2. Manual hit test and mouse tracking, so no raycaster on the HUD canvas is needed.</summary>
        private void Drag()
        {
            bool mode = DragModeActive();
            if (mode != _outline)
            {
                _outline = mode;
                _sortCanvas.overrideSorting = mode;
                _sortCanvas.sortingOrder = mode ? 30000 : 0;
                _plate.color = mode ? new Color(0.25f, 0.2f, 0.05f, 0.85f) : new Color(0f, 0f, 0f, 0.55f);
            }
            if (!mode)
            {
                if (_dragging)
                    EndDrag();
                return;
            }

            Vector2 mouse = BepInEx.UnityInput.Current.mousePosition;
            Camera cam = EventCamera();
            if (!_dragging && BepInEx.UnityInput.Current.GetMouseButtonDown(0) &&
                RectTransformUtility.RectangleContainsScreenPoint(_root, mouse, cam))
            {
                _dragging = true;
                _dragStartLocal = ToParentLocal(mouse, cam);
                _dragStartX = _liveX = PluginConfig.OffsetX.Value;
                _dragStartY = _liveY = PluginConfig.OffsetY.Value;
            }
            if (!_dragging)
                return;
            if (!BepInEx.UnityInput.Current.GetMouseButton(0))
            {
                EndDrag();
                return;
            }
            Vector2 now = ToParentLocal(mouse, cam);
            Vec2 o = PanelLayout.OffsetAfterDrag(PluginConfig.Anchor.Value, _dragStartX, _dragStartY, now.x - _dragStartLocal.x, now.y - _dragStartLocal.y);
            _liveX = o.X;
            _liveY = o.Y;
            ApplyPosition(_liveX, _liveY);
        }

        /// <summary>Writes the offset once, on release (or when the modifier is let go mid-drag).</summary>
        private void EndDrag()
        {
            _dragging = false;
            PluginConfig.OffsetX.Value = Mathf.Round(_liveX);
            PluginConfig.OffsetY.Value = Mathf.Round(_liveY);
        }

        private Camera EventCamera()
        {
            Canvas canvas = _root.GetComponentInParent<Canvas>();
            canvas = canvas != null ? canvas.rootCanvas : null;
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private Vector2 ToParentLocal(Vector2 screen, Camera cam)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_root.parent, screen, cam, out local);
            return local;
        }
```

Note `Canvas` on the panel root makes it a nested canvas; `UiUtil.Rect` children are unaffected. `overrideSorting` must only be set after the object is under a canvas (it is: built under the HUD).

- [ ] **Step 2: Build, test, deploy**

```bash
$ENV && dotnet test tests/TrophyTracker.Tests --nologo -v minimal && ./build/deploy.sh
```

- [ ] **Step 3: In-game check (user drives)**

- Open the inventory: panel hidden. Hold Left Alt: panel appears above the inventory with a warm plate.
- Drag it with the left mouse: it follows the cursor 1:1 (also at Scale 1.5 and with a different `Anchor`). Release: F1 shows the new OffsetX/OffsetY; reopening the game keeps it.
- Release Alt mid-drag: drag ends, position saved.
- Check that a drag starting over an inventory slot does not pick up the item; if it does, record it as a known limitation in README (move the panel off the grid first) — do not add input-blocking code without asking the user.
- `./build/shot.sh probe-drag`.

- [ ] **Step 4: Commit and push**

```bash
git add -A && git commit -m "Drag mode: hold the modifier with the inventory open to move the panel" && git push
```

---

### Task 9: Compendium: silhouettes, pin badges, click to pin

**Files:**
- Create: `src/TrophyTracker/UI/CompendiumDecorator.cs`, `src/TrophyTracker/UI/PinToggle.cs`, `src/TrophyTracker/Patches/CompendiumPatch.cs`

**Interfaces:**
- Consumes: `Runtime.Catalog`, `Runtime.Discovered`, `Runtime.Tr`, `Discovery.Kills()`, `RowBuilder.PrimarySource`, `RowText.Chance`, `PinList.Toggle/Contains`, `PluginConfig.Pinned/DragModifier`, `Keys.Held`, `Sprites.Dot`, `TrophyPanel.TrophyName`.
- Produces: `CompendiumDecorator.Decorate(InventoryGui gui)`.

- [ ] **Step 1: PinToggle.cs**

```csharp
using TrophyTracker.Core;
using TrophyTracker.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyTracker.UI
{
    /// <summary>PLAN.md §7.3: click a Compendium trophy to pin or unpin it; a dot on the icon marks pinned ones.</summary>
    internal sealed class PinToggle : MonoBehaviour
    {
        private static readonly Color BadgeColor = new Color(1f, 0.62f, 0.18f, 1f);

        private string _id;
        private Image _badge;
        private TMPro.TMP_Text _description;
        private string _baseDescription;
        private bool _collected;

        public void Init(string id, bool collected)
        {
            _id = id;
            _collected = collected;
            Transform iconBkg = transform.Find("icon_bkg");
            Image icon = transform.Find("icon_bkg/icon").GetComponent<Image>();
            icon.raycastTarget = true;
            Transform description = transform.Find("description");
            _description = description != null ? description.GetComponent<TMPro.TMP_Text>() : null;
            _baseDescription = _description != null ? _description.text : "";

            RectTransform badge = UiUtil.Rect("TrophyTrackerPin", iconBkg != null ? iconBkg : icon.transform);
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1f, 1f);
            badge.anchoredPosition = new Vector2(-2f, -2f);
            badge.sizeDelta = new Vector2(12f, 12f);
            _badge = badge.gameObject.AddComponent<Image>();
            _badge.sprite = Sprites.Dot;
            _badge.color = BadgeColor;
            _badge.raycastTarget = false;

            // Not ??: Unity overloads == for destroyed objects, and ?? bypasses it.
            Button button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = icon;
            button.onClick.AddListener(Toggle);
            Refresh();
        }

        private void Toggle()
        {
            if (Keys.Held(PluginConfig.DragModifier.Value))
                return;
            PluginConfig.Pinned.Value = PinList.Toggle(PluginConfig.Pinned.Value, _id);
            Runtime.Dirty = true;
            Refresh();
        }

        private void Refresh()
        {
            bool pinned = PinList.Contains(PluginConfig.Pinned.Value, _id);
            _badge.gameObject.SetActive(pinned);
            // Collected trophies keep vanilla's lore text; only silhouettes carry our hint line.
            if (_description != null && !_collected)
                _description.text = pinned ? Runtime.Tr.Get("pinned") ?? _baseDescription : _baseDescription;
        }
    }
}
```

- [ ] **Step 2: CompendiumDecorator.cs**

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using TrophyTracker.Core;
using TrophyTracker.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyTracker.UI
{
    /// <summary>PLAN.md §1.5, §7: after vanilla rebuilds the trophy page, tag its elements for pinning and add
    /// silhouettes for discovered but uncollected trophies at their own grid slots.</summary>
    internal static class CompendiumDecorator
    {
        private static readonly Color Silhouette = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        public static void Decorate(InventoryGui gui)
        {
            Player p = Player.m_localPlayer;
            TrophyCatalog catalog = Runtime.Catalog;
            if (p == null || catalog == null || ObjectDB.instance == null)
                return;

            // 1. Vanilla's elements, matched by repeating its own filter and order (InventoryGui.UpdateTrophyList).
            List<string> owned = p.GetTrophies();
            int k = 0;
            foreach (string id in owned)
            {
                if (ObjectDB.instance.GetItemPrefab(id) == null)
                    continue;
                if (k >= gui.m_trophyList.Count)
                    break;
                gui.m_trophyList[k++].AddComponent<PinToggle>().Init(id, true);
            }

            // 2. Silhouettes.
            var ownedSet = new HashSet<string>(owned, StringComparer.Ordinal);
            IDictionary<string, float> kills = Discovery.Kills();
            foreach (TrophyInfo t in catalog.All)
            {
                if (ownedSet.Contains(t.Id) || !Runtime.Discovered.IsTrophyDiscovered(t, ownedSet))
                    continue;
                GameObject prefab = ObjectDB.instance.GetItemPrefab(t.Id);
                ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (item == null)
                    continue;

                GameObject el = UnityEngine.Object.Instantiate(gui.m_trophieElementPrefab, gui.m_trophieListRoot);
                el.SetActive(true);
                var rt = (RectTransform)el.transform;
                Vector2Int pos = item.m_itemData.m_shared.m_trophyPos;
                rt.anchoredPosition = new Vector2(pos.x * gui.m_trophieListSpace, pos.y * -gui.m_trophieListSpace);

                Image icon = rt.Find("icon_bkg/icon").GetComponent<Image>();
                icon.sprite = item.m_itemData.GetIcon();
                icon.color = Silhouette;

                DropSource s = RowBuilder.PrimarySource(t, kills, Runtime.Discovered);
                rt.Find("name").GetComponent<TMP_Text>().text = s != null ? Localization.instance.Localize(s.NameToken) : TrophyPanel.TrophyName(t.Id);
                string hint = s != null
                    ? (Runtime.Tr.Get("uncollected") ?? "Not yet collected. {0} drop. Click to pin.").Replace("{0}", RowText.Chance(s.Chance * 100f))
                    : Runtime.Tr.Get("uncollected_nochance") ?? "Not yet collected. Click to pin.";
                rt.Find("description").GetComponent<TMP_Text>().text = hint;

                gui.m_trophyList.Add(el);
                el.AddComponent<PinToggle>().Init(t.Id, false);
            }

            // 3. Scroll height, with vanilla's formula over every element.
            float lowest = 0f;
            foreach (GameObject el in gui.m_trophyList)
                lowest = Mathf.Min(lowest, ((RectTransform)el.transform).anchoredPosition.y - gui.m_trophieListSpace);
            gui.m_trophieListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(gui.m_trophieListBaseSize, -lowest));
        }
    }
}
```

- [ ] **Step 3: CompendiumPatch.cs**

```csharp
using System;
using HarmonyLib;
using TrophyTracker.UI;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §7. Vanilla destroys everything in m_trophyList on each rebuild, including what we add.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateTrophyList))]
    internal static class CompendiumPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                CompendiumDecorator.Decorate(__instance);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("InventoryGui.UpdateTrophyList", e);
            }
        }
    }
}
```

- [ ] **Step 4: Build, test, deploy**

```bash
$ENV && dotnet test tests/TrophyTracker.Tests --nologo -v minimal && ./build/deploy.sh
```

- [ ] **Step 5: In-game check (user drives)**

- Compendium → Trophies: collected trophies look vanilla; discovered-but-uncollected ones appear as dark silhouettes at their slots with the creature name and "Not yet collected. 10% drop. Click to pin."; undiscovered ones are absent; the page scrolls to the lowest one.
- Click a silhouette: orange dot appears, description changes to "Pinned to the HUD. Click to unpin.", F1 shows it in `Pinned`, the HUD row appears. Click again: removed.
- Click a collected trophy: pinned (dot), lore text unchanged.
- Holding Left Alt, clicks do nothing.
- Close and reopen the Compendium several times: no duplicate silhouettes, no errors in `./build/logs.sh`.
- `./build/shot.sh probe-compendium`.

- [ ] **Step 6: Commit and push**

```bash
git add -A && git commit -m "Compendium: discovered-trophy silhouettes, click to pin, pin badge" && git push
```

---

### Task 10: Full in-game test pass (PLAN.md §9)

**Files:**
- Modify: whatever the pass shows is wrong; `PLAN.md` (record results in a new "§11 Test results (date)" section).

**Interfaces:** none new.

- [ ] **Step 1: Run the checklist with the user**, one item at a time, with `devcommands` on a test world (`god`, `ghost`, `spawn <prefab> <count> <level>`, `killall`):
  1. `trophytracker`: every vanilla trophy listed and mapped; Bear (`TrophyBjorn`) and Vile (`TrophyBjornUndead`) present with sources.
  2. Unknown → Exact on the first kill; count reaches 1; the drop lands exactly on the kill that showed 1 (repeat for one 10% and one 5% trophy).
  3. A ★1 kill: `★1` label and a new count; then a 0★ kill: back to `★0` with a fresh count.
  4. A Roll trophy (Deer) and a boss (`always`).
  5. Discovery: a new creature's health bar adds it (row and silhouette appear); relog keeps it; an older character with kills is back-filled on first load.
  6. Recent-kill row: appears on kill, expires after 60 s, not shown for pinned or collected trophies.
  7. Collected: picking up a first trophy of a pinned creature shows `✓ collected` (or `+ collected` if the font lacks ✓) for 5 s, then unpins.
  8. Multiplayer (only if the user can join a friend): exact rows dim while another player is online.
  9. Each anchor at Scale 0.75 / 1 / 1.5; drag at each; every hide toggle; F7; HideWithVanillaHud off keeps it visible on Ctrl+F3.
  10. No overlap with MyLittleUI, RecentXpBars, Enhuddlement, ExtraSlots at the default spot (screenshot `probe-default`).
- [ ] **Step 2: Fix each failure** with a test first where the failure is in the model (add a failing test in the matching test file, then fix), redeploy, recheck that item.
- [ ] **Step 3: Record results** in `PLAN.md` §11 and the Bear/Vile cause in §1.6.
- [ ] **Step 4: Commit and push** (`git commit -m "In-game test pass: <summary>"`) after each fix and after recording results.

---

### Task 11: README, icon, package (no release yet)

**Files:**
- Create: `build/make_icon.py`, `thunderstore/icon.png`, `thunderstore/README.md`, `docs/images/panel.jpg`, `docs/images/compendium.jpg`
- Modify: `README.md`, `CHANGELOG.md`

- [ ] **Step 1: Release screenshots** with the user in game: `./build/shot.sh probe-release-panel` and `./build/shot.sh probe-release-compendium`, then `./build/crop.sh probe-release-panel panel <WxH+X+Y>` and `./build/crop.sh probe-release-compendium compendium <WxH+X+Y>` (geometry chosen by looking at the PNG with the Read tool).

- [ ] **Step 2: Icon.** Copy `Forewarned/build/make_icon.py`, keep its pixel helpers (`blend`, `rounded_rect`, `polygon`, PNG writer) and the family plate (dark plate, golden border), and replace the drawing with: a stylised trophy head silhouette (polygon) in off-white, centred, and under it a thin progress bar (dark track, gold fill at about two-thirds) — the panel's own visual language. Run `python3 build/make_icon.py`, view `thunderstore/icon.png` with the Read tool, iterate until it reads clearly at 64 px.

- [ ] **Step 3: README.md**, in Earshot's style: one-paragraph intro; screenshot; **How it works** (the pity counter in plain words: exact once you've killed one this session, `?  ≤N` before, star tiers reset progress, >30% is a plain roll, the counter is per game session and per creature owner in multiplayer); **Features**; **Pinning** (Compendium click; gamepad: config only); **Moving the panel** (Left Alt + drag with the inventory open, or F1); **Configuration** table generated from PluginConfig (section, key, default, description — copy descriptions verbatim); **Compatibility** (client-side; vanilla servers; works alongside MyLittleUI etc.; replaces Combifightet-TrophyPercent, do not run both); **Translating** (the English.txt path); **Changelog** link. `thunderstore/README.md` = the same with image URLs as `https://raw.githubusercontent.com/jumpingmushroom/TrophyTracker/v0.1.0/docs/images/<name>.jpg`.

- [ ] **Step 4: CHANGELOG** 0.1.0 entry: bullet the user-visible features (exact counts from the game's counter, star tier, recent-kill row, every trophy pinnable from the Compendium, only discovered creatures shown, movable panel with drag, hide options, multiplayer dimming).

- [ ] **Step 5: Package to check it** — `./build/package.sh`. Expected: `ok: TrophyTracker 0.1.0, 1 dependencies` and a zip containing `manifest.json`, `README.md`, `icon.png`, `CHANGELOG.md`, `LICENSE`, `plugins/TrophyTracker/TrophyTracker.dll`.

- [ ] **Step 6: Commit and push**

```bash
git add -A && git commit -m "README, icon, changelog for 0.1.0" && git push
```

---

### Task 12: Release 0.1.0 and clean up TrophyPercent (only after the user says they're happy)

- [ ] **Step 1: Confirm with the user** that 0.1.0 is ready to release. Stop here until they say yes.
- [ ] **Step 2: Release**

```bash
cd /workspace/gamemods/Valheim/TrophyTracker && ./build/package.sh
git tag v0.1.0 && git push origin v0.1.0
gh release create v0.1.0 dist/TrophyTracker-0.1.0.zip --title "TrophyTracker 0.1.0" --notes-file <(sed -n '/^## 0.1.0/,/^## /p' CHANGELOG.md | sed '$d')
eval "$(grep -E '^SSH_TARGET=' build/deploy.sh)"
scp dist/TrophyTracker-0.1.0.zip "$SSH_TARGET":Downloads/
```
Expected: release page with the zip; zip in the rig's `~/Downloads`.

- [ ] **Step 3: TrophyPercent.** It is already absent from the Default profile's `plugins/` and `mods.yml`; only `BepInEx/config/Combifightet.TrophyPercent.cfg` remains. Ask the user whether to delete that cfg. If r2modman still lists the mod, the user removes it through r2modman; if `mods.yml` must be edited by hand, the user closes r2modman first.
- [ ] **Step 4: Remove the manual dev-deploy DLL if the user installs the Thunderstore build** — ask first; the dev deploy lives in `plugins/TrophyTracker/` and would duplicate a Thunderstore install in `plugins/Jumpingmushroom-TrophyTracker/`.
