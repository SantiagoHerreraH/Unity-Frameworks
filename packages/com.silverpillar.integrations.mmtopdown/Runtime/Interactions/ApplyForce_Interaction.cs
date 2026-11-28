using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class ApplyForce_Interaction : IInteraction
    {
        [Title("Force Vector")]
        [SerializeField]
        private AxisFilter m_ConstraintAxisVector = AxisFilter.xyz;

        [Title("Self Forces")]
        [SerializeField]
        private bool m_ApplyForcesToSelf = false;

        [SerializeField, ShowIf(nameof(m_ApplyForcesToSelf))]
        [Tooltip("Positive values push self away from the target. " +
                 "Negative values pull self towards the target.")]
        private float m_SelfForceMagnitude = 10f;

        [SerializeField, ShowIf(nameof(m_ApplyForcesToSelf))]
        private ForceApplier m_SelfForceApplier;

        [Title("Target Forces")]
        [SerializeField]
        private bool m_ApplyForcesToTarget = true;

        [SerializeField, ShowIf(nameof(m_ApplyForcesToTarget))]
        [Tooltip("Positive values push the target away from self. " +
                 "Negative values pull the target towards self.")]
        private float m_TargetForceMagnitude = 10f;

        [SerializeField, ShowIf(nameof(m_ApplyForcesToTarget))]
        private ForceApplier m_TargetForceApplier;

        private GameObject m_Self;
        private Transform m_SelfTransform;
        private bool m_SelfForceApplierInitialized;

        public IInteraction Clone()
        {
            var clone = new ApplyForce_Interaction
            {
                m_ConstraintAxisVector = m_ConstraintAxisVector,

                m_ApplyForcesToSelf = m_ApplyForcesToSelf,
                m_SelfForceMagnitude = m_SelfForceMagnitude,
                m_SelfForceApplier = m_SelfForceApplier,

                m_ApplyForcesToTarget = m_ApplyForcesToTarget,
                m_TargetForceMagnitude = m_TargetForceMagnitude,
                m_TargetForceApplier = m_TargetForceApplier,

                m_Self = m_Self,
                m_SelfTransform = m_SelfTransform,
                m_SelfForceApplierInitialized =
                    m_SelfForceApplierInitialized
            };

            // Clear cached target components.
            clone.m_TargetForceApplier.SetGameObject(null);

            return clone;
        }

        public bool SetSelf(GameObject self)
        {
            m_Self = self;
            m_SelfTransform = self != null ? self.transform : null;
            m_SelfForceApplierInitialized = false;

            // Clear any previously cached components.
            m_SelfForceApplier.SetGameObject(null);

            if (self == null)
            {
                Debug.LogError(
                    $"{nameof(ApplyForce_Interaction)} could not set self " +
                    "because the provided GameObject is null.");

                return false;
            }

            if (m_ApplyForcesToSelf)
            {
                m_SelfForceApplierInitialized =
                    m_SelfForceApplier.SetGameObject(self);

                if (!m_SelfForceApplierInitialized)
                {
                    Debug.LogError(
                        $"{nameof(ApplyForce_Interaction)} on '{self.name}' " +
                        $"could not initialize its self force applier because " +
                        "the GameObject has neither a TopDownController " +
                        "nor a Rigidbody.",
                        self);

                    return false;
                }
            }

            return true;
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public void Interact(GameObject target)
        {
            if (!m_ApplyForcesToSelf && !m_ApplyForcesToTarget)
            {
                return;
            }

            if (m_Self == null)
            {
                Debug.LogError(
                    $"{nameof(ApplyForce_Interaction)} could not execute " +
                    "because self has not been assigned or has been destroyed.");

                return;
            }

            if (target == null)
            {
                Debug.LogWarning(
                    $"{nameof(ApplyForce_Interaction)} on '{m_Self.name}' " +
                    "could not execute because the target is null.",
                    m_Self);

                return;
            }

            bool applyToSelf =
                m_ApplyForcesToSelf &&
                m_SelfForceMagnitude != 0f;

            bool applyToTarget =
                m_ApplyForcesToTarget &&
                m_TargetForceMagnitude != 0f;

            if (!applyToSelf && !applyToTarget)
            {
                return;
            }

            // Direction pointing from self towards the target.
            Vector3 direction =
                target.transform.position - m_SelfTransform.position;

            // Remove unwanted axes before normalizing.
            direction = AxisFilterTools.GetFilteredVector(
                direction,
                m_ConstraintAxisVector);

            direction.Normalize();

            // There is no valid direction if the filtered positions coincide.
            if (direction.sqrMagnitude == 0f)
            {
                return;
            }

            // Resolve the target before applying either force.
            if (applyToTarget)
            {
                if (!m_TargetForceApplier.SetGameObject(target))
                {
                    Debug.LogWarning(
                        $"{nameof(ApplyForce_Interaction)} on '{m_Self.name}' " +
                        $"could not apply force to '{target.name}' because " +
                        "the target has neither a TopDownController " +
                        "nor a Rigidbody.",
                        target);

                    applyToTarget = false;
                }
            }

            if (applyToSelf && m_SelfForceApplierInitialized)
            {
                m_SelfForceApplier.ApplyForce(
                    -direction * m_SelfForceMagnitude);
            }

            if (applyToTarget)
            {
                m_TargetForceApplier.ApplyForce(
                    direction * m_TargetForceMagnitude);
            }
        }
    }
}