using MoreMountains.CorgiEngine;
using SilverPillar.Modules;
using SilverPillar.Stats;
using UnityEngine;

namespace SilverPillar.Integrations.Corgi
{
    public class JumpVariables : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("If null will try to get from self")]
        private CharacterJump m_CharacterJump;
        [SerializeField, Tooltip("If null will try to get from self")]
        private StatController m_StatController;
        [SerializeField, Tooltip("If null will try to get from self")]
        private BoolModuleController m_BoolModuleController;

        [Header("Stat Variables")]
        [SerializeField]
        private StatValueRange m_NumberOfJumps = new();
        [Header("Bool Module Variables")]
        [SerializeField]
        private BoolModuleType m_CanJumpIfNotTouchingGround;

        private void Awake()
        {
            if (ReferencesAreCorrect())
            {
                CreateVariables();
                SubscribeToEvents();
            }
        }

        private bool ReferencesAreCorrect()
        {
            bool allGood = true;

            if (m_CharacterJump == null)
            {
                if (!gameObject.TryGetComponent(out m_CharacterJump))
                {
                    Debug.LogError($"{nameof(m_CharacterJump)} is NULL in {nameof(JumpVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_StatController == null)
            {
                if (!gameObject.TryGetComponent(out m_StatController))
                {
                    Debug.LogError($"{nameof(m_StatController)} is NULL in {nameof(JumpVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            if (m_BoolModuleController == null)
            {
                if (!gameObject.TryGetComponent(out m_BoolModuleController))
                {
                    Debug.LogError($"{nameof(m_BoolModuleController)} is NULL in {nameof(JumpVariables)} on gameobject {gameObject.name}.");
                    allGood = false;
                }
            }

            allGood &= IsStatTypeValid(m_NumberOfJumps.StatTypeToGetValueFrom.Get(), nameof(m_NumberOfJumps));

            allGood &= IsBoolModuleValid(m_CanJumpIfNotTouchingGround, nameof(m_CanJumpIfNotTouchingGround));

            return allGood;
        }

        private void CreateVariables()
        {
            m_StatController.CreateStatType(m_NumberOfJumps.StatTypeToGetValueFrom.Get());

            m_BoolModuleController.CreateBoolModule(m_CanJumpIfNotTouchingGround, false);
        }

        private void SubscribeToEvents()
        {
            m_StatController.SubscribeOnCurrentStatChange(m_NumberOfJumps.StatTypeToGetValueFrom.Get(), UpdateNumberOfJumps);

            m_BoolModuleController.SubscribeOnSetState(m_CanJumpIfNotTouchingGround, UpdateCanJumpIfNotTochingGround);
        }

        private void UpdateNumberOfJumps(float pastStat, float currentStat)
        {
            m_CharacterJump.NumberOfJumps = (int)m_NumberOfJumps.GetValue(currentStat);
        }

        private void UpdateCanJumpIfNotTochingGround(bool state)
        {
            if (state)
            {
                m_CharacterJump.JumpRestrictions = CharacterJump.JumpBehavior.CanJumpAnywhere;
            }
            else
            {
                m_CharacterJump.JumpRestrictions = CharacterJump.JumpBehavior.CanJumpOnGroundAndFromLadders;
            }

        }

        private bool IsStatTypeValid(StatType statType, string variableName)
        {
            if (statType == null)
            {
                Debug.LogError($"StatType is NULL in {variableName} in {nameof(JumpVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }

        private bool IsBoolModuleValid(BoolModuleType type, string variableName)
        {
            if (type == null)
            {
                Debug.LogError($"BoolModule is NULL in {variableName} in {nameof(JumpVariables)} in gameobject {gameObject.name}.");
                return false;
            }

            return true;
        }
    }
}
