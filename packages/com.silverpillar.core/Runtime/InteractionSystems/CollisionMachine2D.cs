using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public class CollisionMachine2D : MonoBehaviour
    {
        [FoldoutGroup("Collisions 2D")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision2D> m_OnEnterCollisions2D = new();
        [FoldoutGroup("Collisions 2D")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision2D> m_OnStayCollisions2D = new();
        [FoldoutGroup("Collisions 2D")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision2D> m_OnExitCollisions2D = new();

        [FoldoutGroup("Trigger Collisions 2D")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision2D> m_OnEnterTriggerCollisions2D = new();
        [FoldoutGroup("Trigger Collisions 2D")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision2D> m_OnStayTriggerCollisions2D = new();
        [FoldoutGroup("Trigger Collisions 2D")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision2D> m_OnExitTriggerCollisions2D = new();

        private void Awake()
        {

            Initialize(m_OnEnterCollisions2D);
            Initialize(m_OnStayCollisions2D);
            Initialize(m_OnExitCollisions2D);

            Initialize(m_OnEnterTriggerCollisions2D);
            Initialize(m_OnStayTriggerCollisions2D);
            Initialize(m_OnExitTriggerCollisions2D);
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

        // --- 2D Physics ---
        private void OnCollisionEnter2D(Collision2D collision) => m_OnEnterCollisions2D.ForEach(c => c.Collide(collision));
        private void OnCollisionStay2D(Collision2D collision) => m_OnStayCollisions2D.ForEach(c => c.Collide(collision));
        private void OnCollisionExit2D(Collision2D collision) => m_OnExitCollisions2D.ForEach(c => c.Collide(collision));

        private void OnTriggerEnter2D(Collider2D other) => m_OnEnterTriggerCollisions2D.ForEach(c => c.Collide(other));
        private void OnTriggerStay2D(Collider2D other) => m_OnStayTriggerCollisions2D.ForEach(c => c.Collide(other));
        private void OnTriggerExit2D(Collider2D other) => m_OnExitTriggerCollisions2D.ForEach(c => c.Collide(other));
    }
}
