using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedHealth : Health
    {
        [Serializable]
        private struct HealthConditions
        {
            [SerializeField]
            private ConditionType m_ConditionType;

            [SerializeField]
            private List<InteractionCondition> m_InteractionConditions;

            private List<IInteractionCondition> m_ConditionInstances;
            private bool m_InitializedSuccessfully;

            public void Initialize(GameObject self)
            {
                m_InitializedSuccessfully = true;

                m_ConditionInstances?.Clear();

                if (m_InteractionConditions == null ||
                    m_InteractionConditions.Count == 0)
                {
                    return;
                }

                m_ConditionInstances ??=
                    new List<IInteractionCondition>(
                        m_InteractionConditions.Count);

                for (int i = 0; i < m_InteractionConditions.Count; i++)
                {
                    var item = m_InteractionConditions[i];

                    if (item == null)
                    {
                        Debug.LogError(
                            $"{nameof(EnhancedHealth)} on '{self.name}' " +
                            $"contains a null damage condition at index {i}.",
                            self);

                        m_InitializedSuccessfully = false;
                        continue;
                    }

                    var clone = item.Clone();

                    if (clone == null)
                    {
                        Debug.LogError(
                            $"{nameof(EnhancedHealth)} on '{self.name}' " +
                            $"could not clone the damage condition at index {i}.",
                            self);

                        m_InitializedSuccessfully = false;
                        continue;
                    }

                    if (!clone.SetGameObject(self))
                    {
                        Debug.LogError(
                            $"{nameof(EnhancedHealth)} on '{self.name}' " +
                            $"could not initialize damage condition " +
                            $"'{clone.GetType().Name}' at index {i}.",
                            self);

                        m_InitializedSuccessfully = false;
                        continue;
                    }

                    m_ConditionInstances.Add(clone);
                }
            }

            public bool IsFulfilled(GameObject target)
            {
                if (!m_InitializedSuccessfully)
                {
                    return false;
                }

                if (m_ConditionInstances == null ||
                    m_ConditionInstances.Count == 0)
                {
                    return true;
                }

                return InteractionConditions.IsFulfilled(
                    m_ConditionType,
                    m_ConditionInstances,
                    target);
            }
        }

        [MMInspectorGroup("Damage Conditions", true, 34)]
        [SerializeField]
        private HealthConditions m_CanDamageIf;

        [MMInspectorGroup("On Damaged Events", true, 36)]
        [SerializeField]
        private UnityEvent<GameObject> m_OnDamaged = new();

        [MMInspectorGroup("On Not Damaged Events", true, 37)]
        [SerializeField]
        private UnityEvent<GameObject> m_OnNotDamaged = new();

        [MMInspectorGroup("Death Events", true, 35)]
        [SerializeField]
        private UnityEvent m_OnZeroHealth = new();

        private bool m_DeathProcessed;

        public override void Initialization()
        {
            m_CanDamageIf.Initialize(gameObject);

            // Avoid duplicate subscriptions when reinitializing.
            OnDeath -= HandleDeath;
            OnDeath += HandleDeath;

            base.Initialization();
        }

        public override void InitializeCurrentHealth()
        {
            m_DeathProcessed = false;

            base.InitializeCurrentHealth();
        }

        public virtual bool CanTakeDamageFrom(GameObject instigator)
        {
            if (m_DeathProcessed || !CanTakeDamageThisFrame())
            {
                return false;
            }

            return m_CanDamageIf.IsFulfilled(instigator);
        }

        public override void Damage(
            float damage,
            GameObject instigator,
            float flickerDuration,
            float invincibilityDuration,
            Vector3 damageDirection,
            List<TypedDamage> typedDamages = null)
        {
            if (!CanTakeDamageFrom(instigator))
            {
                m_OnNotDamaged?.Invoke(instigator);
                return;
            }

            // Execute all original TopDown Engine damage logic.
            base.Damage(
                damage,
                instigator,
                flickerDuration,
                invincibilityDuration,
                damageDirection,
                typedDamages);

            // LastDamage contains the final damage after
            // processing damage resistances and typed damage.
            if (LastDamage > 0f)
            {
                m_OnDamaged?.Invoke(instigator);
            }
            else
            {
                m_OnNotDamaged?.Invoke(instigator);
            }
        }

        public override void Kill()
        {
            // Preserve the original immunity behaviour and
            // prevent death from being processed multiple times.
            if (ImmuneToDamage || m_DeathProcessed)
            {
                return;
            }

            // Set before calling the base function to prevent
            // recursive Kill calls originating from death events.
            m_DeathProcessed = true;

            base.Kill();
        }

        private void HandleDeath()
        {
            m_OnZeroHealth?.Invoke();
        }
    }
}