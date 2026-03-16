using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.GOAP
{
    [CreateAssetMenu(fileName = "ActionList", menuName = "GOAP/ActionList")]
    public class ActionList : SaveableScriptableObject
    {
        [OdinSerialize]
        public SortedSet<Action> PossibleActions;
    }
}
