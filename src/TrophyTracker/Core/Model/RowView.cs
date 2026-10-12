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
