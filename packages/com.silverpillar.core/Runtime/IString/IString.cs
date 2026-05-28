using System;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable

    public interface IString
    {
        public IString Clone();
        public bool SetGameObject(GameObject gameObject);
        public GameObject? GetGameObject();
        public string CalculateString();
    }

    [Serializable]
    public class Constant_String : IString
    {
        [SerializeField]
        private string m_String = string.Empty;

        private GameObject? m_GameObject;

        public string CalculateString()
        {
            return m_String;
        }

        public IString Clone()
        {
            return new Constant_String
            {
                m_String = m_String,
                m_GameObject = m_GameObject
            };
        }

        public GameObject? GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObject)
        {
            m_GameObject = gameObject;
            return true;
        }
    }
}