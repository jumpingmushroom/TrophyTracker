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
