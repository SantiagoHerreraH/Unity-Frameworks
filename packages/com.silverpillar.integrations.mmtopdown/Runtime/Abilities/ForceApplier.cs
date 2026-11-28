using MoreMountains.TopDownEngine;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public enum TopDownControllerForceTypes
    {
        AddForce,
        Impact
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

        public bool SetGameObject(GameObject gameObj)
        {
            m_TopDownController = null;
            m_Rigidbody = null;

            if (gameObj == null)
            {
                return false;
            }

            // Prioritize TopDownController.
            if (gameObj.TryGetComponent(out m_TopDownController))
            {
                return true;
            }

            return gameObj.TryGetComponent(out m_Rigidbody);
        }

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

                        float magnitude = force.magnitude;

                        if (magnitude > 0f)
                        {
                            m_TopDownController.Impact(
                                force / magnitude,
                                magnitude);
                        }

                        break;
                }
            }
            else if (m_Rigidbody != null)
            {
                switch (RigidbodyForceType)
                {
                    case RigidbodyForceTypes.AddForce:

                        m_Rigidbody.AddForce(
                            force,
                            ForceModeForRigidbodyForceType);

                        break;

                    case RigidbodyForceTypes.AddRelativeForce:

                        m_Rigidbody.AddRelativeForce(
                            force,
                            ForceModeForRigidbodyForceType);

                        break;

                    case RigidbodyForceTypes.AddForceAtPosition:

                        Transform rbTransform = m_Rigidbody.transform;

                        m_Rigidbody.AddForceAtPosition(
                            force,
                            rbTransform.position + rbTransform.forward,
                            ForceModeForRigidbodyForceType);

                        break;
                }
            }
        }
    }
}