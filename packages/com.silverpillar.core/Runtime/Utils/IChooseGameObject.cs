using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface IChooseGameObject //change this to just querier without enumerable
    {
        public GameObject Choose(GameObject querier, IEnumerable<GameObject> enumerable);
    }

    [Serializable]
    public class ChooseNearest : IChooseGameObject
    {
        public GameObject Choose(GameObject querier, IEnumerable<GameObject> enumerable)
        {
            return enumerable?
                .OrderBy(obj => (obj.transform.position - querier.transform.position).sqrMagnitude)
                .FirstOrDefault();
        }
    }

    [Serializable]
    public class ChooseFurthest : IChooseGameObject
    {
        public GameObject Choose(GameObject querier, IEnumerable<GameObject> enumerable)
        {
            return enumerable?
                .OrderByDescending(obj => (obj.transform.position - querier.transform.position).sqrMagnitude)
                .FirstOrDefault();
        }
    }
}
