using UnityEngine;

namespace SilverPillar.Core
{
    public class GameObjectTools : MonoBehaviour
    {
        public void Activate(GameObject go)
        {
            go.SetActive(true);
        }

        public void Deactivate(GameObject go)
        {
            go.SetActive(false);
        }
    }
}
