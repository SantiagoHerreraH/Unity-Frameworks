using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class CollisionMachine : SerializedMonoBehaviour
    {
        [FoldoutGroup("Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision> m_OnEnterCollisions = new();
        [FoldoutGroup("Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision> m_OnStayCollisions = new();
        [FoldoutGroup("Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision> m_OnExitCollisions = new();

        [FoldoutGroup("Trigger Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision> m_OnEnterTriggerCollisions = new();
        [FoldoutGroup("Trigger Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision> m_OnStayTriggerCollisions = new();
        [FoldoutGroup("Trigger Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision> m_OnExitTriggerCollisions = new();

        private void Awake()
        {
            Initialize(m_OnEnterCollisions);
            Initialize(m_OnStayCollisions);
            Initialize(m_OnExitCollisions);

            Initialize(m_OnEnterTriggerCollisions);
            Initialize(m_OnStayTriggerCollisions);
            Initialize(m_OnExitTriggerCollisions);

        }

        private void Initialize<T>(List<T> list)
        {
            foreach (var item in list)
            {
                if (item is ICollision c) c.SetSelf(gameObject);
                else if (item is ITriggerCollision tc) tc.SetSelf(gameObject);
                else if (item is ICollision2D c2) c2.SetSelf(gameObject);
                else if (item is ITriggerCollision2D tc2) tc2.SetSelf(gameObject);
            }
        }

        // --- 3D Physics ---
        private void OnCollisionEnter(Collision collision) => m_OnEnterCollisions.ForEach(c => c.Collide(collision));
        private void OnCollisionStay(Collision collision) => m_OnStayCollisions.ForEach(c => c.Collide(collision));
        private void OnCollisionExit(Collision collision) => m_OnExitCollisions.ForEach(c => c.Collide(collision));

        private void OnTriggerEnter(Collider other) => m_OnEnterTriggerCollisions.ForEach(c => c.Collide(other));
        private void OnTriggerStay(Collider other) => m_OnStayTriggerCollisions.ForEach(c => c.Collide(other));
        private void OnTriggerExit(Collider other) => m_OnExitTriggerCollisions.ForEach(c => c.Collide(other));
    }
}
