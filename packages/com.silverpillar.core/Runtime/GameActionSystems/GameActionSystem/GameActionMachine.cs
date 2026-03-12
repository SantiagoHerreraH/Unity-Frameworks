using Pillar;
using UnityEngine;

namespace Pillar
{
    public class GameActionMachine : MonoBehaviour
    {
        public Coroutine StartAction(IGameAction gameAction)
        {
            return StartCoroutine(gameAction.Execute(gameObject));
        }

        public void StopAction(Coroutine gameActionCoroutine)
        {
            StopCoroutine(gameActionCoroutine);
        }
    }
}

