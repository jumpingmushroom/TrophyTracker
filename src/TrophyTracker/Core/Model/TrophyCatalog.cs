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
        /// <summary>1.0.20 m_levelMultiplierNotAmount: stars raise the chance but not the amount. Informational
        /// (console); the chance and counter maths are the same either way.</summary>
        public bool LevelMultiplierNotAmount;
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
