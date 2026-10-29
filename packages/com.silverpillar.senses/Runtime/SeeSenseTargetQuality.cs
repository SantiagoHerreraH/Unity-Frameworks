using System;
using UnityEngine;

namespace SilverPillar.Senses
{
    [Serializable]
    public class SeeSenseTargetQuality : SenseTargetQuality
    {
        public enum SeeSenseTargetType
        {
            CanBeSeenAndCanBlockSight,
            CanBeSeenButCantBlockSight,
            CantBeSeenButCanBlockSight
        }

        [SerializeField, Tooltip("If a target can't be seen, it will never be able to be sensed.")]
        private SeeSenseTargetType m_SenseTargetType = SeeSenseTargetType.CanBeSeenAndCanBlockSight;

        //different from can be sensed. If can't be seen, it can never be sensed
        public bool CanBeSeen()
        {
            return m_SenseTargetType != SeeSenseTargetType.CantBeSeenButCanBlockSight;
        }

        public bool CanBlockSight()
        {
            return m_SenseTargetType != SeeSenseTargetType.CanBeSeenButCantBlockSight;
        }

        public override void CheckSelfValidity(GameObject self)
        {
            Collider[] colliders = self.GetComponentsInChildren<Collider>(true);
            bool valid = colliders.Length > 0;
            if (!valid)
            {
                Debug.LogError($"{nameof(SeeSenseTargetQuality)} needs a collider to be seen in gameobject {self.name}");
            }
        }
    }
}
