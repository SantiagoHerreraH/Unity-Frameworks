using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace Pillar
{
    [CreateAssetMenu(fileName = "ActionList", menuName = "GOAP/ActionList")]
    public class ActionList : SaveableScriptableObject
    {
        [OdinSerialize]
        public SortedSet<Action> PossibleActions;
    }
}
