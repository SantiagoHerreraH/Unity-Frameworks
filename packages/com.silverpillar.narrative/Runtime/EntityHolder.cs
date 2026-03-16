using UnityEngine;

namespace SilverPillar.Narrative
{
    public class EntityHolder : MonoBehaviour
    {
        public Entity Entity { get; private set; }

        public void InitializeEntity(Entity entity)
        {
            if (Entity == null)
            {
                Entity = entity;
            }
        }
    }
}

