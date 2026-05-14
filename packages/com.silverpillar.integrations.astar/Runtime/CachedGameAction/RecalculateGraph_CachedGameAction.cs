using Pathfinding;
using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.AStar
{
    /// <summary>
    /// Triggers a full rescan of all A* Pathfinding Project graphs when executed.
    /// Equivalent to pressing the "Scan" button in the AstarPath inspector at runtime.
    /// </summary>
    [Serializable]
    public class RecalculateGraph_CachedGameAction : ICachedGameAction
    {
        private GameObject m_GameObj;

        /// <inheritdoc/>
        public ICachedGameAction Clone()
        {
            // Stateless — a memberwise clone is a perfect copy.
            return (RecalculateGraph_CachedGameAction)MemberwiseClone();
        }

        /// <inheritdoc/>
        public void Execute()
        {
            if (AstarPath.active == null)
            {
                Debug.LogWarning("[RecalculateGraph] AstarPath.active is null – no graph to scan.");
                return;
            }

            // AstarPath.active.Scan() rescans every graph registered with the pathfinder.
            // It is safe to call at runtime and blocks until the scan is complete.
            AstarPath.active.Scan();
        }

        /// <inheritdoc/>
        public GameObject GetGameObject()
        {
            return m_GameObj;
        }

        /// <inheritdoc/>
        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null)
            {
                m_GameObj = gameObj;
                return true;    
            }

            return false;
        }
    }
}
