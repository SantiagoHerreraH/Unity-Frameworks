using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

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

}
