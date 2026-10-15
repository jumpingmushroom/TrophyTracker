using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>
    /// PLAN.md §3.2: the progress bar. Remembers, per counter key (one per trophy and star tier), the kills-left
    /// value the counter started from after its last (re)seed; empty at the seed, full when the next kill drops.
    /// </summary>
    public sealed class BarTracker
    {
        private readonly Dictionary<int, int> _start = new Dictionary<int, int>();

        /// <summary>A kill went through GenerateDropList for this counter key: its stored count before and after.</summary>
        public void Observe(int key, int? before, int? after)
        {
            if (!after.HasValue)
                return;
            // Seeded now (no entry), or the previous count was at 1 or below so this kill dropped and reseeded.
            bool reseeded = !before.HasValue || before.Value <= 1;
            int left = PityMath.KillsLeft(after.Value);
            int start;
            if (reseeded || !_start.TryGetValue(key, out start) || left > start)
                _start[key] = left;
        }

        public float Fill(int key, int remaining)
        {
            int left = PityMath.KillsLeft(remaining);
            int start;
            if (!_start.TryGetValue(key, out start) || left > start)
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
