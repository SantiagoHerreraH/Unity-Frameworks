using UnityEngine;

namespace SilverPillar.Core
{
    public class QueueCaller : MonoBehaviour
    {
        public void ExecuteNextInQueue(Queue queuedActionChannel)
        {
            QueueManager.Instance.ExecuteAndPop(queuedActionChannel, gameObject);
        }
    }
}
