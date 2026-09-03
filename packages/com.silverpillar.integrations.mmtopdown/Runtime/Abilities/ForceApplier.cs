using log4net.Util;
using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public enum TopDownControllerForceTypes
    {
        AddForce,
        Impact,
    }

    public enum RigidbodyForceTypes
    {
        AddForce,
        AddRelativeForce,
        AddForceAtPosition
    }

    [Serializable]
    public struct ForceApplier
    {
        public TopDownControllerForceTypes TopDownControllerForceType;
        public RigidbodyForceTypes RigidbodyForceType;
        public ForceMode ForceModeForRigidbodyForceType;

        private TopDownController m_TopDownController;
        private Rigidbody m_Rigidbody;
        private CharacterController m_CharacterController;
        
        public void ApplyForce(Vector3 force)
        {
            if (m_TopDownController != null)
            {
                switch (TopDownControllerForceType)
                {
                    case TopDownControllerForceTypes.AddForce:

                        m_TopDownController.AddForce(force);

                        break;
                    case TopDownControllerForceTypes.Impact:

                        m_TopDownController.Impact(force.normalized, force.magnitude);

                        break;
                    default:
                        break;
                }
            }
            else if (m_Rigidbody != null)
            {
                switch (RigidbodyForceType)
                {
                    case RigidbodyForceTypes.AddForce:

                        m_Rigidbody.AddForce(force, ForceModeForRigidbodyForceType);

                        break;
                    case RigidbodyForceTypes.AddRelativeForce:

                        m_Rigidbody.AddRelativeForce(force, ForceModeForRigidbodyForceType);

                        break;
                    case RigidbodyForceTypes.AddForceAtPosition:

                        m_Rigidbody.AddForceAtPosition(force, m_Rigidbody.transform.position + m_Rigidbody.transform.forward, ForceModeForRigidbodyForceType);

                        break;
                    default:
                        break;
                }
            }
        }

        public bool SetGameObject(GameObject gameObj)
        {
            bool allGood = gameObj.TryGetComponent(out m_TopDownController);
            allGood &= gameObj.TryGetComponent(out m_Rigidbody);

            return allGood;
        }
    }
}
