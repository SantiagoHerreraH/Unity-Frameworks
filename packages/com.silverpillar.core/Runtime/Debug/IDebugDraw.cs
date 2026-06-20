using UnityEngine;

namespace SilverPillar.Core
{
    public enum WhereToDraw
    {
        Runtime,
        OnDrawGizmos
    }

    public interface IDebugDraw
    {
        public void DebugDraw(WhereToDraw whereToDraw);
    }
}
