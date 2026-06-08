using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Stats
{

    [Serializable]
    public class StatComparisonChooser : ICachedCondition, IChooseData<ICachedCondition>
    {
        public enum StatTypesToCompare
        {
            All,
            OnlySelected,
            AllExceptSelected
        }

        public enum ProtocolWhenCallingIsFulfilled
        {
            ChooseIfNotChosen,
            ReturnTrueIfNotChosen,
            ReturnFalseIfNotChosen
        }

        [Title("Comparison Operations")]
        [Tooltip("If empty, will use left < right.")]
        [SerializeField]
        private ChoosingList<FloatComparison.OperationType> m_PossibleOperations = new();

        [Title("Stat Controller")]
        [SerializeField]
        private SelfType m_WhoToGetStatControllerFrom;

        [SerializeField, ShowIf(nameof(m_WhoToGetStatControllerFrom), SelfType.CustomGameObject)]
        private StatController m_StatController;

        private GameObject m_Self;

        [Title("Stat Variables")]
        [SerializeField, Tooltip("If empty, will compare current stat.")]
        private ChoosingList<StatVariable> m_PossibleStatVariables = new();

        [Title("Stat Types")]
        [SerializeField, Tooltip("If you compare the same stat type, they won't compare the same stat variables.")]
        private bool m_AllowToCompareTheSameStatTypes;

        [SerializeField]
        private StatTypesToCompare m_LeftStatTypesToCompare;

        [SerializeField, HideIf(nameof(m_LeftStatTypesToCompare), StatTypesToCompare.All)]
        private ChoosingList<StatType> m_LeftStatTypes = new();

        [SerializeField]
        private StatTypesToCompare m_RightStatTypesToCompare;

        [SerializeField, HideIf(nameof(m_RightStatTypesToCompare), StatTypesToCompare.All)]
        private ChoosingList<StatType> m_RightStatTypes = new();

        [Button(ButtonSizes.Medium)]
        private void PopulateWithAllStatTypes()
        {
            m_LeftStatTypes ??= new();
            m_RightStatTypes ??= new();

            m_LeftStatTypes.List.Clear();
            m_RightStatTypes.List.Clear();

            var allStatTypes = ScriptableObjectRegistry.Instance.GetAllOfType<StatType>();

            foreach (var statType in allStatTypes)
            {
                if (statType == null)
                    continue;

                m_LeftStatTypes.List.Add(statType);
                m_RightStatTypes.List.Add(statType);
            }
        }

        private void GetAllStatTypesExceptSelected(ChoosingList<StatType> statTypes)
        {
            if (statTypes == null || statTypes.List == null)
                return;

            List<StatType> selected = new(statTypes.List);
            statTypes.List.Clear();

            var allStatTypes = ScriptableObjectRegistry.Instance.GetAllOfType<StatType>();

            foreach (var statType in allStatTypes)
            {
                if (statType != null && !selected.Contains(statType))
                    statTypes.List.Add(statType);
            }
        }

        [Title("Choosing Settings")]
        [SerializeField]
        private IntCachedScore m_NumberOfStatComparisonConditionsToChoose;

        [Title("Condition Settings")]
        [SerializeField]
        private ConditionType m_ConditionType;

        [SerializeField]
        private ProtocolWhenCallingIsFulfilled m_ProtocolWhenCallingIsFulfilled;

        private List<ICachedCondition> m_Chosen;
        private bool m_Initialized;

        public List<ICachedCondition> ChooseData()
        {
            Initialize();

            m_Chosen ??= new();

            if (m_NumberOfStatComparisonConditionsToChoose == null)
            {
                Debug.LogError($"{nameof(StatComparisonChooser)} has no number assigned.", m_Self);
                return m_Chosen;
            }

            if (m_StatController == null)
            {
                Debug.LogError($"{nameof(StatComparisonChooser)} has no StatController.", m_Self);
                return m_Chosen;
            }

            if (m_LeftStatTypes == null || m_LeftStatTypes.Count == 0)
                return m_Chosen;

            if (m_RightStatTypes == null || m_RightStatTypes.Count == 0)
                return m_Chosen;

            int amount = Mathf.Max(0, m_NumberOfStatComparisonConditionsToChoose.CalculateScoreAsInt());

            while (m_Chosen.Count < amount)
            {
                StatComparison_CachedCondition condition = new StatComparison_CachedCondition();
                condition.SetGameObject(m_StatController.gameObject);
                m_Chosen.Add(condition);
            }

            while (m_Chosen.Count > amount)
            {
                m_Chosen.RemoveAt(m_Chosen.Count - 1);
            }

            for (int i = 0; i < m_Chosen.Count; i++)
            {
                StatType leftStatType = m_LeftStatTypes.ChooseNext();
                StatType rightStatType = m_RightStatTypes.ChooseNext();

                StatVariable leftVariable = m_PossibleStatVariables.ChooseNext();
                StatVariable rightVariable = m_PossibleStatVariables.ChooseNext();

                FloatComparison.OperationType operation = m_PossibleOperations.ChooseNext();

                if (!m_AllowToCompareTheSameStatTypes &&
                    leftStatType == rightStatType &&
                    leftVariable == rightVariable)
                {
                    rightStatType = m_RightStatTypes.ChooseNext();
                }

                StatComparison_CachedCondition condition = m_Chosen[i] as StatComparison_CachedCondition;

                if (condition == null)
                {
                    condition = new StatComparison_CachedCondition();
                    m_Chosen[i] = condition;
                }

                condition.StatType = leftStatType;
                condition.StatVariable = leftVariable;
                condition.ConditionOperation = operation;
                condition.OtherStatType = rightStatType;
                condition.OtherStatVariable = rightVariable;

                condition.SetGameObject(m_StatController.gameObject);
            }

            return m_Chosen;
        }

        public bool IsFulfilled()
        {
            if (m_Chosen == null || m_Chosen.Count == 0)
            {
                switch (m_ProtocolWhenCallingIsFulfilled)
                {
                    case ProtocolWhenCallingIsFulfilled.ChooseIfNotChosen:
                        ChooseData();
                        break;

                    case ProtocolWhenCallingIsFulfilled.ReturnTrueIfNotChosen:
                        return true;

                    case ProtocolWhenCallingIsFulfilled.ReturnFalseIfNotChosen:
                        return false;
                }
            }

            if (m_Chosen == null || m_Chosen.Count == 0)
                return false;

            return CachedConditions.IsFulfilled(m_ConditionType, m_Chosen);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_WhoToGetStatControllerFrom == SelfType.ThisGameObject)
            {
                if (gameObj == null)
                    return false;

                return gameObj.TryGetComponent(out m_StatController);
            }

            return m_StatController != null;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public ICachedCondition Clone()
        {
            return CloneInternal();
        }

        IChooseData<ICachedCondition> IChooseData<ICachedCondition>.Clone()
        {
            return CloneInternal();
        }

        private StatComparisonChooser CloneInternal()
        {
            StatComparisonChooser clone = new StatComparisonChooser();

            clone.m_PossibleOperations = m_PossibleOperations != null ? m_PossibleOperations.Clone() : new();
            clone.m_PossibleStatVariables = m_PossibleStatVariables != null ? m_PossibleStatVariables.Clone() : new();

            clone.m_WhoToGetStatControllerFrom = m_WhoToGetStatControllerFrom;
            clone.m_StatController = m_StatController;
            clone.m_Self = m_Self;

            clone.m_AllowToCompareTheSameStatTypes = m_AllowToCompareTheSameStatTypes;

            clone.m_LeftStatTypesToCompare = m_LeftStatTypesToCompare;
            clone.m_RightStatTypesToCompare = m_RightStatTypesToCompare;

            clone.m_LeftStatTypes = m_LeftStatTypes != null ? m_LeftStatTypes.Clone() : new();
            clone.m_RightStatTypes = m_RightStatTypes != null ? m_RightStatTypes.Clone() : new();

            clone.m_NumberOfStatComparisonConditionsToChoose = m_NumberOfStatComparisonConditionsToChoose;

            clone.m_ConditionType = m_ConditionType;
            clone.m_ProtocolWhenCallingIsFulfilled = m_ProtocolWhenCallingIsFulfilled;

            if (m_Chosen != null)
            {
                clone.m_Chosen = new();

                foreach (var condition in m_Chosen)
                {
                    if (condition != null)
                        clone.m_Chosen.Add(condition.Clone());
                }
            }

            clone.m_Initialized = false;

            return clone;
        }

        private void Initialize()
        {
            InitializeStatTypes();

            m_PossibleOperations ??= new();
            m_PossibleOperations.EnsureDefault(FloatComparison.OperationType.Less);

            m_PossibleStatVariables ??= new();
            m_PossibleStatVariables.EnsureDefault(StatVariable.Current);
        }

        private void InitializeStatTypes()
        {
            if (m_Initialized)
                return;

            m_LeftStatTypes ??= new();
            m_RightStatTypes ??= new();

            switch (m_LeftStatTypesToCompare)
            {
                case StatTypesToCompare.All:
                    PopulateStatTypesWithAll(m_LeftStatTypes);
                    break;

                case StatTypesToCompare.OnlySelected:
                    break;

                case StatTypesToCompare.AllExceptSelected:
                    GetAllStatTypesExceptSelected(m_LeftStatTypes);
                    break;
            }

            switch (m_RightStatTypesToCompare)
            {
                case StatTypesToCompare.All:
                    PopulateStatTypesWithAll(m_RightStatTypes);
                    break;

                case StatTypesToCompare.OnlySelected:
                    break;

                case StatTypesToCompare.AllExceptSelected:
                    GetAllStatTypesExceptSelected(m_RightStatTypes);
                    break;
            }

            m_Initialized = true;
        }

        private void PopulateStatTypesWithAll(ChoosingList<StatType> statTypes)
        {
            if (statTypes == null || statTypes.List == null)
                return;

            statTypes.List.Clear();

            var allStatTypes = ScriptableObjectRegistry.Instance.GetAllOfType<StatType>();

            foreach (var statType in allStatTypes)
            {
                if (statType != null)
                    statTypes.List.Add(statType);
            }
        }
    }
}