using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public enum SelfType
    {
        ThisGameObject,
        CustomGameObject
    }

    public class CollisionMachine : SerializedMonoBehaviour
    {
        public enum WhenToCallEvents
        {
            AfterActions,
            BeforeActions
        }

        [FoldoutGroup("Ignore If")]
        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_IgnoreIf = null;
        [FoldoutGroup("Ignore If")]
        [SerializeField]
        private SelfType m_WhoIsSelfInCondition;
        [FoldoutGroup("Ignore If")]
        [SerializeField, ShowIf(nameof(m_WhoIsSelfInCondition), SelfType.CustomGameObject)]
        private GameObject m_CustomGameObject;


        [FoldoutGroup("Collisions")]
        [Title("Collision Actions")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision> m_OnEnterCollisions = new();
        [FoldoutGroup("Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision> m_OnStayCollisions = new();
        [FoldoutGroup("Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ICollision> m_OnExitCollisions = new();
        [FoldoutGroup("Collisions")]
        [Title("Events")]
        [SerializeField]
        private WhenToCallEvents m_WhenToCallCollisionEvents; 
        [FoldoutGroup("Collisions")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnEnterCollisionEvent = null;
        [FoldoutGroup("Collisions")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnStayCollisionEvent = null;
        [SerializeField]
        [FoldoutGroup("Collisions")]
        private UnityEvent<GameObject> m_OnExitCollisionEvent = null;

        [FoldoutGroup("Trigger Collisions")]
        [Title("Trigger Actions")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision> m_OnEnterTriggerCollisions = new();
        [FoldoutGroup("Trigger Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision> m_OnStayTriggerCollisions = new();
        [FoldoutGroup("Trigger Collisions")]
        [OdinSerialize, ShowInInspector]
        private List<ITriggerCollision> m_OnExitTriggerCollisions = new();

        [FoldoutGroup("Trigger Collisions")]
        [Title("Events")]
        [SerializeField]
        private WhenToCallEvents m_WhenToCallTriggerEvents;
        [FoldoutGroup("Trigger Collisions")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnEnterTriggerEvent = null;
        [FoldoutGroup("Trigger Collisions")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnStayTriggerEvent = null;
        [SerializeField]
        [FoldoutGroup("Trigger Collisions")]
        private UnityEvent<GameObject> m_OnExitTriggerEvent = null;

        [FoldoutGroup("Debug")]
        [SerializeField]
        private bool m_PrintCollisions;

        private void Awake()
        {
            Initialize(m_OnEnterCollisions);
            Initialize(m_OnStayCollisions);
            Initialize(m_OnExitCollisions);

            Initialize(m_OnEnterTriggerCollisions);
            Initialize(m_OnStayTriggerCollisions);
            Initialize(m_OnExitTriggerCollisions);

            if (m_IgnoreIf != null)
            {
                switch (m_WhoIsSelfInCondition)
                {
                    case SelfType.ThisGameObject:
                        m_IgnoreIf.SetGameObject(gameObject);
                        break;
                    case SelfType.CustomGameObject:
                        m_IgnoreIf.SetGameObject(m_CustomGameObject);
                        break;
                    default:
                        break;
                }
            }
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

        // --- 3D Collisions ---

        private void OnCollisionEnter(Collision collision)
        {
            if (IsIgnored(collision.gameObject)) return;
            if (m_PrintCollisions) LogCollision("OnCollisionEnter", collision.gameObject.name);

            if (m_WhenToCallCollisionEvents == WhenToCallEvents.BeforeActions) m_OnEnterCollisionEvent?.Invoke(collision.gameObject);
            foreach (var c in m_OnEnterCollisions) c.Collide(collision);
            if (m_WhenToCallCollisionEvents == WhenToCallEvents.AfterActions) m_OnEnterCollisionEvent?.Invoke(collision.gameObject);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (IsIgnored(collision.gameObject)) return;
            if (m_PrintCollisions) LogCollision("OnCollisionStay", collision.gameObject.name);

            if (m_WhenToCallCollisionEvents == WhenToCallEvents.BeforeActions) m_OnStayCollisionEvent?.Invoke(collision.gameObject);
            foreach (var c in m_OnStayCollisions) c.Collide(collision);
            if (m_WhenToCallCollisionEvents == WhenToCallEvents.AfterActions) m_OnStayCollisionEvent?.Invoke(collision.gameObject);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (IsIgnored(collision.gameObject)) return;
            if (m_PrintCollisions) LogCollision("OnCollisionExit", collision.gameObject.name);

            if (m_WhenToCallCollisionEvents == WhenToCallEvents.BeforeActions) m_OnExitCollisionEvent?.Invoke(collision.gameObject);
            foreach (var c in m_OnExitCollisions) c.Collide(collision);
            if (m_WhenToCallCollisionEvents == WhenToCallEvents.AfterActions) m_OnExitCollisionEvent?.Invoke(collision.gameObject);
        }

        // --- 3D Triggers ---

        private void OnTriggerEnter(Collider other)
        {
            if (IsIgnored(other.gameObject)) return;
            if (m_PrintCollisions) LogCollision("OnTriggerEnter", other.gameObject.name);

            if (m_WhenToCallTriggerEvents == WhenToCallEvents.BeforeActions) m_OnEnterTriggerEvent?.Invoke(other.gameObject);
            foreach (var tc in m_OnEnterTriggerCollisions) tc.Collide(other);
            if (m_WhenToCallTriggerEvents == WhenToCallEvents.AfterActions) m_OnEnterTriggerEvent?.Invoke(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            if (IsIgnored(other.gameObject)) return;
            if (m_PrintCollisions) LogCollision("OnTriggerStay", other.gameObject.name);

            if (m_WhenToCallTriggerEvents == WhenToCallEvents.BeforeActions) m_OnStayTriggerEvent?.Invoke(other.gameObject);
            foreach (var tc in m_OnStayTriggerCollisions) tc.Collide(other);
            if (m_WhenToCallTriggerEvents == WhenToCallEvents.AfterActions) m_OnStayTriggerEvent?.Invoke(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsIgnored(other.gameObject)) return;
            if (m_PrintCollisions) LogCollision("OnTriggerExit", other.gameObject.name);

            if (m_WhenToCallTriggerEvents == WhenToCallEvents.BeforeActions) m_OnExitTriggerEvent?.Invoke(other.gameObject);
            foreach (var tc in m_OnExitTriggerCollisions) tc.Collide(other);
            if (m_WhenToCallTriggerEvents == WhenToCallEvents.AfterActions) m_OnExitTriggerEvent?.Invoke(other.gameObject);
        }
        private bool IsIgnored(GameObject gameObject)
        {
            if (m_IgnoreIf != null)
            {
                return m_IgnoreIf.IsFulfilled(gameObject);
            }

            return false;
        }

        // --- Debug Helper ---

        private void LogCollision(string eventName, string otherName)
        {
            Debug.Log($"<color=#4FC3F7>[CollisionMachine]</color> <b>{eventName}</b> on {gameObject.name} with <b>{otherName}</b>");
        }
    }
}
