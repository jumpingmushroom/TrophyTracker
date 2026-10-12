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
