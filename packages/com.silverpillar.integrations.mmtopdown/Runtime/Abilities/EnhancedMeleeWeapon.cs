using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using static SilverPillar.Integrations.MMTopDown.EnhancedDamageOnTouch;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedMeleeWeapon : MeleeWeapon
    {
        [MMInspectorGroup("Knockback Settings", true, 23), MMEnumCondition("MeleeDamageAreaMode", (int)MeleeDamageAreaModes.Generated)]
        public ForceApplier ForceApplier;
        /// <summary>
		/// Creates the damage area.
		/// </summary>
		protected override void CreateDamageArea()
        {
            if ((MeleeDamageAreaMode == MeleeDamageAreaModes.Existing) && (ExistingDamageArea != null))
            {
                _damageArea = ExistingDamageArea.gameObject;
                _damageAreaCollider = _damageArea.gameObject.GetComponent<Collider>();
                _damageAreaCollider2D = _damageArea.gameObject.GetComponent<Collider2D>();
                _damageOnTouch = ExistingDamageArea;
                return;
            }

            _damageArea = new GameObject();
            _damageArea.name = this.name + "DamageArea";
            _damageArea.transform.position = this.transform.position;
            _damageArea.transform.rotation = this.transform.rotation;
            _damageArea.transform.SetParent(this.transform);
            _damageArea.transform.localScale = Vector3.one;
            _damageArea.layer = this.gameObject.layer;

            if (DamageAreaShape == MeleeDamageAreaShapes.Rectangle)
            {
                _boxCollider2D = _damageArea.AddComponent<BoxCollider2D>();
                _boxCollider2D.offset = AreaOffset;
                _boxCollider2D.size = AreaSize;
                _damageAreaCollider2D = _boxCollider2D;
                _damageAreaCollider2D.isTrigger = true;
            }
            if (DamageAreaShape == MeleeDamageAreaShapes.Circle)
            {
                _circleCollider2D = _damageArea.AddComponent<CircleCollider2D>();
                _circleCollider2D.transform.position = this.transform.position;
                _circleCollider2D.offset = AreaOffset;
                _circleCollider2D.radius = AreaSize.x / 2;
                _damageAreaCollider2D = _circleCollider2D;
                _damageAreaCollider2D.isTrigger = true;
            }

            if ((DamageAreaShape == MeleeDamageAreaShapes.Rectangle) || (DamageAreaShape == MeleeDamageAreaShapes.Circle))
            {
                Rigidbody2D rigidBody = _damageArea.AddComponent<Rigidbody2D>();
                rigidBody.bodyType = RigidbodyType2D.Kinematic;
                rigidBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            }

            if (DamageAreaShape == MeleeDamageAreaShapes.Box)
            {
                _boxCollider = _damageArea.AddComponent<BoxCollider>();
                _boxCollider.center = AreaOffset;
                _boxCollider.size = AreaSize;
                _damageAreaCollider = _boxCollider;
                _damageAreaCollider.isTrigger = true;
            }
            if (DamageAreaShape == MeleeDamageAreaShapes.Sphere)
            {
                _sphereCollider = _damageArea.AddComponent<SphereCollider>();
                _sphereCollider.transform.position = this.transform.position + this.transform.rotation * AreaOffset;
                _sphereCollider.radius = AreaSize.x / 2;
                _damageAreaCollider = _sphereCollider;
                _damageAreaCollider.isTrigger = true;
            }

            if ((DamageAreaShape == MeleeDamageAreaShapes.Box) || (DamageAreaShape == MeleeDamageAreaShapes.Sphere))
            {
                Rigidbody rigidBody = _damageArea.AddComponent<Rigidbody>();
                rigidBody.isKinematic = true;

                rigidBody.gameObject.AddComponent<MMRagdollerIgnore>();
            }

            EnhancedDamageOnTouch enhancedDamageOnTouch = _damageArea.AddComponent<EnhancedDamageOnTouch>();
            enhancedDamageOnTouch.ForceApplier = ForceApplier;

            enhancedDamageOnTouch.SetGizmoSize(AreaSize);
            enhancedDamageOnTouch.SetGizmoOffset(AreaOffset);
            enhancedDamageOnTouch.TargetLayerMask = TargetLayerMask;
            enhancedDamageOnTouch.MinDamageCaused = MinDamageCaused;
            enhancedDamageOnTouch.MaxDamageCaused = MaxDamageCaused;
            enhancedDamageOnTouch.DamageDirectionMode = DamageOnTouch.DamageDirections.BasedOnOwnerPosition;
            enhancedDamageOnTouch.DamageCausedKnockbackType = Knockback;
            enhancedDamageOnTouch.DamageCausedKnockbackForce = KnockbackForce;
            enhancedDamageOnTouch.DamageCausedKnockbackDirection = KnockbackDirection;
            enhancedDamageOnTouch.InvincibilityDuration = InvincibilityDuration;
            enhancedDamageOnTouch.HitDamageableFeedback = HitDamageableFeedback;
            enhancedDamageOnTouch.HitNonDamageableFeedback = HitNonDamageableFeedback;
            enhancedDamageOnTouch.TriggerFilter = TriggerFilter;
            _damageOnTouch = enhancedDamageOnTouch;

            if (!CanDamageOwner && (Owner != null))
            {
                _damageOnTouch.IgnoreGameObject(Owner.gameObject);
            }
        }

        /// <summary>
		/// On disable we set our flag to false
		/// </summary>
		protected override void OnDisable()
        {
            _attackInProgress = false;
            DisableDamageArea();
        }
    }
}
