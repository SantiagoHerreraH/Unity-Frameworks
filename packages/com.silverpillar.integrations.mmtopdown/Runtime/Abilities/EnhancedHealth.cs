
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedHealth : Health
    {
        [MMInspectorGroup("Events", true, 34)]
        [SerializeField]
        private UnityEvent<GameObject> m_OnTakeDamage = new();

        [SerializeField]
        private UnityEvent m_OnZeroHealth = new();


        public override void Damage(
            float damage,
            GameObject instigator,
            float flickerDuration,
            float invincibilityDuration,
            Vector3 damageDirection,
            List<TypedDamage> typedDamages = null)
        {
            // Preserve the original damage eligibility checks.
            if (!CanTakeDamageThisFrame())
            {
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

            // Notify listeners that damage was processed.
            m_OnTakeDamage?.Invoke(instigator);
        }

        public override void Kill()
        {
            // Preserve the original immunity behavior.
            if (ImmuneToDamage)
            {
                return;
            }

            // Invoke before the base implementation, which may
            // disable the GameObject immediately.
            m_OnZeroHealth?.Invoke();

            // Execute all original TopDown Engine death logic.
            base.Kill();
        }
    }
}
