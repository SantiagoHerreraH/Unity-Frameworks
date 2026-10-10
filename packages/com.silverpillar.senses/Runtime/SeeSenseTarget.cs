using System;
using UnityEngine;

namespace SilverPillar.Senses
{
    [Serializable]
    public class SeeSenseTargetQuality : SenseTargetQuality
    {

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
