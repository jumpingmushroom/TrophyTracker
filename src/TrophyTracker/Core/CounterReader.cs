using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §1.1: CharacterDrop.s_pseudoCounter (1.0.20: Dictionary&lt;int, int&gt;, key = PityMath.CounterKey),
    /// read into plain values. The field is touched only inside a NoInlining helper, so if the game changes it the
    /// MissingFieldException is thrown inside the try, logged once, and every row falls back to Unknown/Roll.</summary>
    internal static class CounterReader
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int? Get(int key)
        {
            try
            {
                Dictionary<int, int> counters = Counters();
                int value;
                if (counters != null && counters.TryGetValue(key, out value))
                    return value;
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("CounterReader.Get", e);
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Dictionary<int, int> Snapshot()
        {
            try
            {
                Dictionary<int, int> counters = Counters();
                if (counters != null)
                    return new Dictionary<int, int>(counters);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("CounterReader.Snapshot", e);
            }
            return new Dictionary<int, int>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Dictionary<int, int> Counters()
        {
            return CharacterDrop.s_pseudoCounter;
        }

        public static bool NoPseudoDrops()
        {
            return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPseudoDrops);
        }
    }
}
