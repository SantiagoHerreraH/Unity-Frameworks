using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedWeaponAim3D : WeaponAim3D
    {
        [MMInspectorGroup("Reticle", true, 11)]
        [MMEnumCondition("ReticleType", (int)ReticleTypes.Scene, (int)ReticleTypes.UI)]
        [SerializeField, Range(0.1f, 1f)]
        private float m_ReticleFollowSpeed = 0.7f;

        protected override void MoveReticle()
        {
            if (ReticleType == ReticleTypes.None) { return; }
            if (_reticle == null) { return; }
            if (_weapon.Owner.ConditionState.CurrentState == CharacterStates.CharacterConditions.Paused) { return; }

            if (ReticleType == ReticleTypes.Scene)
            {
                // if we're not supposed to rotate the reticle, we force its rotation, otherwise we apply the current look rotation
                if (!RotateReticle)
                {
                    _reticle.transform.rotation = Quaternion.identity;
                }
                else
                {
                    if (ReticleAtMousePosition)
                    {
                        _reticle.transform.rotation = _lookRotation;
                    }
                }

                // if we're in follow mouse mode and the current control scheme is mouse, we move the reticle to the mouse's position
                if (ReticleAtMousePosition && AimControl == AimControls.Mouse)
                {
                    _reticle.transform.position = MMMaths.Lerp(_reticle.transform.position, _reticlePosition, m_ReticleFollowSpeed, Time.deltaTime);
                }
            }
            _reticlePosition = _reticle.transform.position;

            if (ReticleMovesWithSlopes)
            {
                RaycastHit groundCheck = new RaycastHit();
                if (ScreenPointToRay)
                {
                    Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.MousePosition);
                    groundCheck = MMDebug.Raycast3D(ray.origin, ray.direction, MaximumSlopeElevation, ReticleObstacleMask, Color.cyan, true);
                }
                else
                {
                    // we cast a ray from above
                    groundCheck = MMDebug.Raycast3D(_reticlePosition + Vector3.up * MaximumSlopeElevation / 2f, Vector3.down, MaximumSlopeElevation, ReticleObstacleMask, Color.cyan, true);
                }

                if (groundCheck.collider != null)
                {
                    _reticlePosition.y = groundCheck.point.y + ReticleHeight;
                    _reticle.transform.position = _reticlePosition;

                    _slopeTargetPosition = groundCheck.point + Vector3.up * ReticleHeight;
                }
                else
                {
                    _slopeTargetPosition = _reticle.transform.position;
                }
            }
        }
    }
}
