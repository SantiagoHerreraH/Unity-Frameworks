using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Core
{
    public class TimeScaleSetter : MonoBehaviour
    {
        [Button]
        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
        }
    }
}
