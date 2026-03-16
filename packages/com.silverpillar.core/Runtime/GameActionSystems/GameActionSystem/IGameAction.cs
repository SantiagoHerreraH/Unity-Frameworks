using System.Collections;
using UnityEngine;

namespace SilverPillar.Core
{
    public interface IGameAction
    {
        bool CanExecute(GameObject gameObj);
        public IEnumerator Execute(GameObject gameObj);
    }

    public abstract class GameActionScriptableObject : SaveableScriptableObject, IGameAction
    {
        public abstract bool CanExecute(GameObject gameObj);
        public abstract IEnumerator Execute(GameObject gameObj);
    }

    
}
