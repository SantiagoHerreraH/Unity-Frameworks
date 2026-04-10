namespace SilverPillar.Stats
{
    public interface IStatModifier
    {
        public void Modify(StatController self, StatController target);
        public IStatModifier Clone();
    }

    public enum StatTarget
    {
        Self,
        Target
    }

    public enum StatTargets
    {
        Self,
        Target,
        Both
    }
}

