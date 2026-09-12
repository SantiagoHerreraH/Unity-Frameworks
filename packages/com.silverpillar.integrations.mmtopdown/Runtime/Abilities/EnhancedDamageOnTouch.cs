using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using System.Collections.Generic;
using UnityEngine;
using SilverPillar.Core;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedDamageOnTouch : DamageOnTouch
    {
        [Header("Knockback Settings")]
        public ForceApplier ForceApplier;
        private Rigidbody _colliderRigidbody;

        [Header("Debug")]
        [SerializeField]
        private bool m_PrintOnHitDamageable;
        [SerializeField]
        private bool m_PrintOnHitNonDamageable;
        [SerializeField]
        private Color m_PrintColor = Color.coral;

        /// <summary>
		/// When colliding, we apply the appropriate damage
		/// </summary>
		/// <param name="collider"></param>
		protected override void Colliding(GameObject collider)
        {
            if (!EvaluateAvailability(collider))
            {
                return;
            }

            // cache reset 
            ForceApplier.SetGameObject(collider.gameObject);
            _colliderTopDownController = null;
            _colliderHealth = collider.gameObject.MMGetComponentNoAlloc<Health>();
            _colliderRigidbody = collider.gameObject.MMGetComponentNoAlloc<Rigidbody>();

            // if what we're colliding with is damageable
            if (_colliderHealth != null)
            {
                if (_colliderHealth.CurrentHealth > 0)
                {
                    OnCollideWithDamageable(_colliderHealth);
                }
            }
            else // if what we're colliding with can't be damaged
            {
                OnCollideWithNonDamageable();
                HitNonDamageableEvent?.Invoke(collider); 
                if (m_PrintOnHitNonDamageable)
                {
                    Debug.Log($"{nameof(EnhancedDamageOnTouch)} hit {collider.gameObject}: NON DAMAGEABLE".Color(m_PrintColor));
                }
            }

            OnAnyCollision(collider);
            HitAnythingEvent?.Invoke(collider);
            HitAnythingFeedback?.PlayFeedbacks(transform.position);
        }

        /// <summary>
		/// Describes what happens when colliding with a damageable object
		/// </summary>
		/// <param name="health">Health.</param>
		protected override void OnCollideWithDamageable(Health health)
        {
            _collidingHealth = health;

            if (health.CanTakeDamageThisFrame())
            {
                // if what we're colliding with is a TopDownController, we apply a knockback force
                _colliderTopDownController = health.gameObject.MMGetComponentNoAlloc<TopDownController>();
                if (_colliderTopDownController == null)
                {
                    _colliderTopDownController = health.gameObject.GetComponentInParent<TopDownController>();
                }

                HitDamageableFeedback?.PlayFeedbacks(this.transform.position);
                HitDamageableEvent?.Invoke(_colliderHealth);
                if (m_PrintOnHitDamageable)
                {
                    Debug.Log($"{nameof(EnhancedDamageOnTouch)} hit {health.gameObject}: DAMAGEABLE".Color(m_PrintColor));
                }

                // we apply the damage to the thing we've collided with
                float randomDamage =
                    UnityEngine.Random.Range(MinDamageCaused, Mathf.Max(MaxDamageCaused, MinDamageCaused));

                ApplyKnockback(randomDamage, TypedDamages);

                DetermineDamageDirection();

                if (RepeatDamageOverTime)
                {
                    _colliderHealth.DamageOverTime(randomDamage, gameObject, InvincibilityDuration,
                        InvincibilityDuration, _damageDirection, TypedDamages, AmountOfRepeats, DurationBetweenRepeats,
                        DamageOverTimeInterruptible, RepeatedDamageType);
                }
                else
                {
                    _colliderHealth.Damage(randomDamage, gameObject, InvincibilityDuration, InvincibilityDuration,
                        _damageDirection, TypedDamages);
                }
            }

            // we apply self damage
            if (DamageTakenEveryTime + DamageTakenDamageable > 0 && !_colliderHealth.PreventTakeSelfDamage)
            {
                SelfDamage(DamageTakenEveryTime + DamageTakenDamageable);
            }
        }

        /// <summary>
		/// Applies knockback if we're in a 3D context
		/// </summary>
		protected override void ApplyKnockback3D()
        {
            Vector3 collidedSpeed = Vector3.zero;

            if (_colliderTopDownController != null)
            {
                collidedSpeed = _colliderTopDownController.Speed;
            }
            else if (_colliderRigidbody != null)
            {
                collidedSpeed = _colliderRigidbody.linearVelocity;
            }


            switch (DamageCausedKnockbackDirection)
            {
                case KnockbackDirections.BasedOnSpeed:
                    var totalVelocity = collidedSpeed + _velocity;
                    _knockbackForce = _knockbackForce * totalVelocity.magnitude;
                    break;
                case KnockbackDirections.BasedOnOwnerPosition:
                    if (Owner == null)
                    {
                        Owner = gameObject;
                    }
                    _relativePosition = (_colliderHealth.transform.position - Owner.transform.position).normalized;
                    _knockbackForce = Quaternion.LookRotation(_relativePosition) * _knockbackForce;
                    break;
                case KnockbackDirections.BasedOnDirection:
                    var direction = transform.position - _positionLastFrame;
                    _knockbackForce = direction.normalized * _knockbackForce.magnitude;
                    break;
                case KnockbackDirections.BasedOnScriptDirection:
                    _knockbackForce = _knockbackScriptDirection.normalized * _knockbackForce.magnitude;
                    break;
            }
        }

        /// <summary>
		/// Determines whether or not knockback should be applied
		/// </summary>
		/// <returns></returns>
		protected override bool ShouldApplyKnockback(float damage, List<TypedDamage> typedDamages)
        {
            if (_colliderHealth.ImmuneToKnockbackIfZeroDamage)
            {
                if (_colliderHealth.ComputeDamageOutput(damage, typedDamages, false) == 0)
                {
                    return false;
                }
            }

            return (DamageCausedKnockbackForce != Vector3.zero)
                   && !_colliderHealth.Invulnerable
                   && _colliderHealth.CanGetKnockback(typedDamages);
        }

        /// <summary>
		/// Applies knockback if needed
		/// </summary>
		protected override void ApplyKnockback(float damage, List<TypedDamage> typedDamages)
        {
            if (ShouldApplyKnockback(damage, typedDamages))
            {
                _knockbackForce = DamageCausedKnockbackForce * _colliderHealth.KnockbackForceMultiplier;
                _knockbackForce = _colliderHealth.ComputeKnockbackForce(_knockbackForce, typedDamages);

                if (_twoD) // if we're in 2D
                {
                    ApplyKnockback2D();
                }
                else // if we're in 3D
                {
                    ApplyKnockback3D();
                }

                if (DamageCausedKnockbackType == KnockbackStyles.AddForce)
                {
                    ForceApplier.ApplyForce(_knockbackForce);
                }
            }
        }
    }
}
