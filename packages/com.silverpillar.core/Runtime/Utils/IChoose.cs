using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface IChoose //change this to just querier without enumerable
    {
        public GameObject Choose(GameObject querier, IEnumerable<GameObject> enumerable);
    }

    [Serializable]
    public class ChooseNearest : IChoose
    {
        public GameObject Choose(GameObject querier, IEnumerable<GameObject> enumerable)
        {
            return enumerable?
                .OrderBy(obj => (obj.transform.position - querier.transform.position).sqrMagnitude)
                .FirstOrDefault();
        }
    }

    [Serializable]
    public class ChooseFurthest : IChoose
    {
        public GameObject Choose(GameObject querier, IEnumerable<GameObject> enumerable)
        {
            return enumerable?
                .OrderByDescending(obj => (obj.transform.position - querier.transform.position).sqrMagnitude)
                .FirstOrDefault();
        }
    }
}
