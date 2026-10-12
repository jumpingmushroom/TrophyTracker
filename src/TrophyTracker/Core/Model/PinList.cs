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
