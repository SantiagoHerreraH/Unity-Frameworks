using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface IChooseData<TOption>
    {
        public List<TOption> ChooseData();

        public bool SetGameObject(GameObject gameObj);
        public GameObject GetGameObject();

        public IChooseData<TOption> Clone();
    }
}