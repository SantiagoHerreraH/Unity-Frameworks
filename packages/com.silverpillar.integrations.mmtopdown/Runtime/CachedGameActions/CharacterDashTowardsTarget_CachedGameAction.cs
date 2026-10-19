using MoreMountains.TopDownEngine;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class CharacterDashTowardsTarget_CachedGameAction : ICachedGameAction
    {
        private enum DashDistanceOptions
        {
            KeepDashDistance,
            MakeDashDistanceEqualToDistanceFromTarget
        }

        [Title("References")]
        [SerializeField]
        private SelfType m_WhichCharacterDash;

        [SerializeField, ShowIf(nameof(m_WhichCharacterDash), SelfType.CustomGameObject)]
        private CharacterDash3D m_CharacterDash;


        [Title("Dash")]
        [SerializeField]
        private DashOperation m_DashOperation;

        [SerializeField, ShowIf(nameof(m_DashOperation), DashOperation.DashStart)]
        private DashStartDirectionParams m_DashDirectionParameters;

        [SerializeField, Tooltip("Dash length will always be set back to the previous value after changing it.")]
        private DashDistanceOptions m_DashDistanceOptions;

        [OdinSerialize, Tooltip("Will apply offset even if keep dash distance.")]
        private ICachedInteractionScore m_DashDistanceOffset;

        [Title("Target")]
        [OdinSerialize]
        private GameObjectCalculatorData m_Target;

        private GameObject m_Self;
        private float m_PreviousDashDistance = 0;
        private Vector3 m_PreviousDashDirection;

        public CharacterDashTowardsTarget_CachedGameAction() { }

        public CharacterDashTowardsTarget_CachedGameAction(CharacterDashTowardsTarget_CachedGameAction other)
        {
            m_WhichCharacterDash = other.m_WhichCharacterDash;
            m_CharacterDash = other.m_CharacterDash;
            m_DashOperation = other.m_DashOperation;
            m_Self = other.m_Self;
            m_DashDirectionParameters = other.m_DashDirectionParameters;
            m_Target = other.m_Target.CloneData();
        }

        public ICachedGameAction Clone()
        {
            return new CharacterDashTowardsTarget_CachedGameAction(this);
        }

        public void Execute()
        {
            GameObject target = m_Target.CalculateGameObject();

            if (m_CharacterDash == null || target == null) return;

            Vector3 delta = target.transform.position - m_CharacterDash.gameObject.transform.position;
            
            if (m_DashOperation == DashOperation.DashStart)
            {
                SetDistanceBefore(target, delta);
                SetDirectionBefore(target, delta);

                m_CharacterDash.DashStart();

                SetDistanceAfter();
                SetDirectionAfter();
            }
            else if (m_DashOperation == DashOperation.DashStop)
            {
                m_CharacterDash.DashStop();
            }
        }

        private void SetDistanceBefore(GameObject target, Vector3 delta)
        {
            float lengthOffset = 0;

            if (m_DashDistanceOffset != null)
            {
                lengthOffset = m_DashDistanceOffset.CalculateScore(target);
            }

            m_PreviousDashDistance = m_CharacterDash.DashDistance;

            switch (m_DashDistanceOptions)
            {
                case DashDistanceOptions.KeepDashDistance:
                    m_CharacterDash.DashDistance = lengthOffset + m_CharacterDash.DashDistance;
                    break;
                case DashDistanceOptions.MakeDashDistanceEqualToDistanceFromTarget:
                    m_CharacterDash.DashDistance = lengthOffset + delta.magnitude;
                    break;
                default:
                    break;
            }

        }

        private void SetDistanceAfter()
        {
            m_CharacterDash.DashDistance = m_PreviousDashDistance;
        }

        private void SetDirectionBefore(GameObject target, Vector3 delta)
        {
            m_PreviousDashDirection = m_CharacterDash.DashDirection;

            if (m_DashDirectionParameters.DashDirectionOperation ==
                DashDirectionOperation.DontSet)
            {
                return;
            }

            if (delta.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector3 directionToTarget = delta.normalized;

            Quaternion targetRotation = Quaternion.FromToRotation(
                Vector3.forward,
                directionToTarget
            );

            switch (m_DashDirectionParameters.DashDirectionOperation)
            {
                case DashDirectionOperation.ResetToForwardVector:
                    m_CharacterDash.DashDirection = directionToTarget;
                    break;

                case DashDirectionOperation.SetDirection:
                    m_CharacterDash.DashDirection =
                        targetRotation * m_DashDirectionParameters.DashDirection;
                    break;
            }
        }

        private void SetDirectionAfter()
        {
            m_CharacterDash.DashDirection = m_PreviousDashDirection;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            m_Target.SetGameObject(gameObj);

            if (m_WhichCharacterDash == SelfType.ThisGameObject && gameObj != null)
            {
                m_CharacterDash = gameObj.GetComponent<CharacterDash3D>();
            }

            m_DashDistanceOffset?.SetGameObject(gameObj);

            return m_Self != null && m_CharacterDash != null && m_Target.IsValid();
        }
    }
}
