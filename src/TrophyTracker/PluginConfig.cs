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
