using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class ContactActionMachine : SerializedMonoBehaviour
    {
        [Flags]
        private enum WhenToRegister
        {
            None = 0,
            OnTriggerEnter = 1 << 0,
            OnTriggerEnter2D = 1 << 1,
            OnCollisionEnter = 1 << 2,
            OnCollisionEnter2D = 1 << 3,

            All = OnTriggerEnter
                | OnTriggerEnter2D
                | OnCollisionEnter
                | OnCollisionEnter2D
        }

        [Flags]
        private enum WhenToDeRegister
        {
            None = 0,
            OnTriggerExit = 1 << 0,
            OnTriggerExit2D = 1 << 1,
            OnCollisionExit = 1 << 2,
            OnCollisionExit2D = 1 << 3,

            All = OnTriggerExit
                | OnTriggerExit2D
                | OnCollisionExit
                | OnCollisionExit2D
        }

        private enum WhenToActOnRegistered
        {
            DontActAutomatically,
            OnRegister,
            OnDeregister
        }


        [Header("Registration")]

        [SerializeField]
        private LayerMask m_CollisionLayerMask;

        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_ConditionToRegister;

        [SerializeField, Min(1)]
        private int m_MaxGameObjectsToRegister = 1;

        [SerializeField]
        private WhenToRegister m_WhenToRegister = WhenToRegister.All;

        [SerializeField]
        private WhenToDeRegister m_WhenToDeRegister = WhenToDeRegister.All;


        [Header("Action")]
        [SerializeField]
        private WhenToActOnRegistered m_WhenToActOnRegistered;

        [SerializeField]
        private UnityEvent<GameObject> m_ActOnRegistered;


        private readonly List<GameObject> m_RegisteredGameObjects = new();


        public IReadOnlyList<GameObject> RegisteredGameObjects
        {
            get { return m_RegisteredGameObjects; }
        }

        public int RegisteredGameObjectCount
        {
            get { return m_RegisteredGameObjects.Count; }
        }


        public bool RegisterGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            if (m_RegisteredGameObjects.Contains(gameObject))
            {
                return false;
            }

            if (m_ConditionToRegister != null && 
                m_ConditionToRegister.SetGameObject(gameObject) &&
                !m_ConditionToRegister.IsFulfilled())
            {
                return false;
            }

            if (!m_CollisionLayerMask.Contains(gameObject.layer))
            {
                return false;
            }

            m_RegisteredGameObjects.Add(gameObject);

            if (m_WhenToActOnRegistered == WhenToActOnRegistered.OnRegister)
            {
                ActOnRegistered(gameObject);
            }

            return true;
        }

        public bool DeRegisterGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            if (!m_RegisteredGameObjects.Remove(gameObject))
            {
                return false;
            }

            if (m_WhenToActOnRegistered == WhenToActOnRegistered.OnDeregister)
            {
                ActOnRegistered(gameObject);
            }

            return true;
        }

        public void ActOnRegistered(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            m_ActOnRegistered?.Invoke(gameObject);
        }

        public void ActOnAllRegistered()
        {
            for (int i = 0; i < m_RegisteredGameObjects.Count; i++)
            {
                GameObject registeredGameObject = m_RegisteredGameObjects[i];

                if (registeredGameObject != null)
                {
                    ActOnRegistered(registeredGameObject);
                }
            }
        }

        public void ClearRegisteredGameObjects()
        {
            m_RegisteredGameObjects.Clear();
        }

        public bool IsRegistered(GameObject gameObject)
        {
            return gameObject != null
                && m_RegisteredGameObjects.Contains(gameObject);
        }


        private bool ShouldRegister(WhenToRegister registrationType)
        {
            return (m_WhenToRegister & registrationType) != 0;
        }

        private bool ShouldDeRegister(WhenToDeRegister deRegistrationType)
        {
            return (m_WhenToDeRegister & deRegistrationType) != 0;
        }


        private void OnTriggerEnter(Collider other)
        {
            if (!ShouldRegister(WhenToRegister.OnTriggerEnter))
            {
                return;
            }

            RegisterGameObject(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!ShouldRegister(WhenToRegister.OnTriggerEnter2D))
            {
                return;
            }

            RegisterGameObject(collision.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!ShouldRegister(WhenToRegister.OnCollisionEnter))
            {
                return;
            }

            RegisterGameObject(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!ShouldRegister(WhenToRegister.OnCollisionEnter2D))
            {
                return;
            }

            RegisterGameObject(collision.gameObject);
        }


        private void OnCollisionExit(Collision collision)
        {
            if (!ShouldDeRegister(WhenToDeRegister.OnCollisionExit))
            {
                return;
            }

            DeRegisterGameObject(collision.gameObject);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!ShouldDeRegister(WhenToDeRegister.OnCollisionExit2D))
            {
                return;
            }

            DeRegisterGameObject(collision.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!ShouldDeRegister(WhenToDeRegister.OnTriggerExit))
            {
                return;
            }

            DeRegisterGameObject(other.gameObject);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!ShouldDeRegister(WhenToDeRegister.OnTriggerExit2D))
            {
                return;
            }

            DeRegisterGameObject(collision.gameObject);
        }
    }
}