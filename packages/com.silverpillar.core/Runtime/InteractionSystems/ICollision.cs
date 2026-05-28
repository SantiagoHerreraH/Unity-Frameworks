using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{

#nullable enable
    public interface ICollision
    {
        public bool SetSelf(GameObject gameObject);
        public GameObject? GetSelf();
        public void Collide(Collision collision);
    }
    public interface ICollision2D
    {
        public bool SetSelf(GameObject gameObject);
        public GameObject? GetSelf();
        public void Collide(Collision2D collision);
    }
    public interface ITriggerCollision
    {
        public bool SetSelf(GameObject gameObject);
        public GameObject? GetSelf();
        public void Collide(Collider collider);
    }
    public interface ITriggerCollision2D
    {
        public bool SetSelf(GameObject gameObject);
        public GameObject? GetSelf();
        public void Collide(Collider2D collider);
    }

    [Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {

    }

    [Serializable]
    public class CollisionEvent : UnityEvent<Collision>
    {

    }

    [Serializable]
    public class InvokeTriggerEvent : ITriggerCollision
    {
        [SerializeField] private TriggerEvent m_OnTrigger = new TriggerEvent();
        private GameObject? m_Self;

        public bool SetSelf(GameObject gameObject)
        {
            m_Self = gameObject;
            return true;
        }

        public GameObject? GetSelf() => m_Self;

        public void Collide(Collider collider)
        {
            m_OnTrigger?.Invoke(collider);
        }
    }

    [Serializable]
    public class InvokeCollisionEvent : ICollision
    {
        
        [SerializeField] private CollisionEvent m_OnCollide = new CollisionEvent();
        private GameObject? m_Self;

        public bool SetSelf(GameObject gameObject)
        {
            m_Self = gameObject;
            return true;
        }

        public GameObject? GetSelf() => m_Self;

        public void Collide(Collision collision)
        {
            m_OnCollide?.Invoke(collision);
        }
    }


}
