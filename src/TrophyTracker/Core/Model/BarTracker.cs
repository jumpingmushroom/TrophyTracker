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
