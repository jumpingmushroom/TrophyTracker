using System;
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
        /// <summary>The 0★ tier. Exact: kills left. Unknown: the worst case before the first kill.</summary>
        public int Count;
        /// <summary>Exact and Unknown only: every starred tier that still uses pity and has a counter, in star
        /// order (PLAN.md §3.2). Null or empty when none.</summary>
        public List<TierCount> Extras;
        /// <summary>Base 0★ chance × 100, or -1 when there is no source.</summary>
        public float ChancePercent;
        /// <summary>0..1, or -1 when the bar is hidden.</summary>
        public float BarFill;
        public bool Dim;
        public bool Recent;
    }

    /// <summary>One starred tier's own countdown: kills left at that star level.</summary>
    public struct TierCount
    {
        public int Stars;
        public int KillsLeft;
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
        /// <summary>A snapshot of CharacterDrop.s_pseudoCounter: PityMath.CounterKey → stored count (PLAN.md §1.1).</summary>
        public IDictionary<int, int> Counters;
        /// <summary>Trophy prefab name → the game's GetStableHashCode (the first half of a counter key).</summary>
        public Func<string, int> Hash;
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
