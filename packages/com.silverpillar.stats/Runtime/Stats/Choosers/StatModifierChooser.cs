using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class SimpleStatOperatorChooser 
    {
        [Title("Operator Number")]
        [SerializeField]
        private IntCachedScore m_NumberOfStatOperatorToChoose;

        [Title("Stat Operation")]
        [SerializeField]
        private ChoosingList<StatType> StatTypeToOperateModification;
        [SerializeField]
        private ChoosingList<StatOperation> StatOperation;
        [SerializeField]
        private ChoosingList<StatVariable> StatVariable;

        [Title("Operation on Stat Type To Operate")]

        [SerializeField]
        private ChoosingList<bool> ApplyOperationToStatTypeToOperate;
        [SerializeField]
        public ChoosingList<StatOperation> StatOperationToApplyOnStatTypeToOperate;
        [SerializeField]
        public ChoosingList<StatVariable> StatVariableToModifyOnStatTypeToOperate;
        [SerializeField]
        public ChoosingList<WhenToModifyStatTypeToOperate> WhenToModifyStatTypeToOperate;

        public bool SetGameObject(GameObject gameObj)
        {
            return m_NumberOfStatOperatorToChoose.SetGameObject(gameObj);
        }

        public StatOperationGroup Choose()
        {
            StatOperationGroup next = new();

            next.StatOperatorsThatModifyIncoming ??= new();

            int amount = m_NumberOfStatOperatorToChoose.CalculateScoreAsInt();

            for (int i = 0; i < amount; i++)
            {
                var statOperation = new SimpleStatOperation();
                statOperation.StatTypeToOperateModification = StatTypeToOperateModification.ChooseNext();
                statOperation.StatOperation = StatOperation.ChooseNext();
                statOperation.StatVariable = StatVariable.ChooseNext();
                statOperation.ApplyOperationToStatTypeToOperate = ApplyOperationToStatTypeToOperate.ChooseNext();

                if (statOperation.ApplyOperationToStatTypeToOperate)
                {
                    statOperation.StatOperationToApplyOnStatTypeToOperate = StatOperationToApplyOnStatTypeToOperate.ChooseNext();
                    statOperation.StatVariableToModifyOnStatTypeToOperate = StatVariableToModifyOnStatTypeToOperate.ChooseNext();
                    statOperation.WhenToModifyStatTypeToOperate = WhenToModifyStatTypeToOperate.ChooseNext();
                }

                next.StatOperatorsThatModifyIncoming.Add(statOperation);
            }

            return next;

        }
    }

    [Serializable]
    public class HowToGetModificationValueChooser
    {
        [SerializeField]
        private ChoosingList<StatTarget> m_FromWhoToGetModifierStat;
        [SerializeField, Tooltip("This is the normal behaviour")]
        private ChoosingList<bool> m_InterpretModificationValueAsPercentageOfWorldMax;
        [SerializeField]
        private ChoosingList<StatVariable> m_InterpretModificationValueAsAPercentageOf;

        public HowToGetModificationValue ChooseNext()
        {
            HowToGetModificationValue next = new();
            next.FromWhoToGetModifierStat = m_FromWhoToGetModifierStat.ChooseNext();
            next.InterpretModificationValueAsPercentageOfWorldMax = m_InterpretModificationValueAsPercentageOfWorldMax.ChooseNext();
            next.InterpretModificationValueAsAPercentageOf = m_InterpretModificationValueAsAPercentageOf.ChooseNext();

            return next;
        }
    }

    [Serializable]
    public class IncomingModificationDataChooser
    {
        [SerializeField]
        private ChoosingList<StatOperation> m_StatOperationsOnSelfStat;
        [SerializeField]
        private ChoosingList<StatVariable> m_StatVariablesToChangeOnSelfStat;
        [SerializeField]
        private SimpleStatOperatorChooser m_OperationsOnSelfModification;


        public bool SetGameObject(GameObject gameObj)
        {
            return m_OperationsOnSelfModification.SetGameObject(gameObj);
        }

        public IncomingModificationData ChooseNext()
        {

            IncomingModificationData current = new();
            if (m_StatOperationsOnSelfStat.Count > 0)
            {
                current.StatOperation = m_StatOperationsOnSelfStat.ChooseNext();
            }
            if (m_StatVariablesToChangeOnSelfStat.Count > 0)
            {
                current.StatVariable = m_StatVariablesToChangeOnSelfStat.ChooseNext();
            }

            current.ModifyIncoming = m_OperationsOnSelfModification.Choose();

            return current;
        }
    }
    [Serializable]
    public class StatModifierChooser : IInteraction, IChooseData<IInteraction>
    {
        [Title("Number")]
        [SerializeField]
        private IntCachedScore m_NumberOfStatModifiersToChoose;

        [Title("Settings")]
        [SerializeField]
        private SelfType m_WhereToGetStatControllerFrom;

        [SerializeField, ShowIf(nameof(m_WhereToGetStatControllerFrom), SelfType.CustomGameObject)]
        private StatController m_Controller;

        private GameObject m_Self;

        [SerializeField]
        private HowToGetModificationValueChooser m_HowToGetModificationValueChooser;

        [Title("Self Stat")]
        [SerializeField]
        private ChoosingList<StatType> m_SelfStatTypes;

        [Title("Modifications On Self Stat")]
        [SerializeField]
        private ChoosingList<bool> m_CanModifySelfStat;

        [SerializeField]
        private IncomingModificationDataChooser m_OperationModificationsOnSelfStat;

        [Title("Target Stat")]
        [SerializeField]
        private ChoosingList<StatType> m_TargetStatTypes;

        [Title("Modifications On Target Stat")]
        [SerializeField]
        private IncomingModificationDataChooser m_OperationModificationsOnTargetStat;

        private List<IInteraction> m_ChosenInteractions;
        private List<IStatModifier> m_Modifiers;

        public List<IInteraction> ChooseData()
        {
            m_ChosenInteractions ??= new();
            m_Modifiers ??= new();

            if (m_NumberOfStatModifiersToChoose == null)
            {
                Debug.LogError($"{nameof(StatModifierChooser)} has no number assigned.", m_Self);
                return m_ChosenInteractions;
            }

            if (m_Controller == null)
            {
                Debug.LogError($"{nameof(StatModifierChooser)} has no StatController.", m_Self);
                return m_ChosenInteractions;
            }

            if (m_ChosenInteractions.Count < 1)
            {
                StatModify_Interaction interaction = new StatModify_Interaction();
                interaction.SetSelf(m_Controller.gameObject);
                m_ChosenInteractions.Add(interaction);
            }

            int amount = Mathf.Max(1, m_NumberOfStatModifiersToChoose.CalculateScoreAsInt());

            while (m_Modifiers.Count < amount)
            {
                OmniStatModifier modifier = new OmniStatModifier();
                m_Modifiers.Add(modifier);
            }

            while (m_Modifiers.Count > amount)
            {
                m_Modifiers.RemoveAt(m_Modifiers.Count - 1);
            }

            for (int i = 0; i < m_Modifiers.Count; i++)
            {
                OmniStatModifier current = m_Modifiers[i] as OmniStatModifier;

                if (current == null)
                {
                    current = new OmniStatModifier();
                    m_Modifiers[i] = current;
                }

                if (m_HowToGetModificationValueChooser != null)
                    current.HowToGetModificationValue = m_HowToGetModificationValueChooser.ChooseNext();

                if (m_SelfStatTypes != null && m_SelfStatTypes.Count > 0)
                    current.SelfStatType = m_SelfStatTypes.ChooseNext();

                current.CanModifySelfStat = m_CanModifySelfStat != null &&
                                            m_CanModifySelfStat.Count > 0 &&
                                            m_CanModifySelfStat.ChooseNext();

                if (current.CanModifySelfStat && m_OperationModificationsOnSelfStat != null)
                    current.SelfStatData = m_OperationModificationsOnSelfStat.ChooseNext();

                if (m_TargetStatTypes != null && m_TargetStatTypes.Count > 0)
                    current.TargetStatType = m_TargetStatTypes.ChooseNext();

                if (m_OperationModificationsOnTargetStat != null)
                    current.TargetStatData = m_OperationModificationsOnTargetStat.ChooseNext();

            }

            StatModify_Interaction statModifyInteraction = m_ChosenInteractions[0] as StatModify_Interaction;

            if (statModifyInteraction != null)
            {
                statModifyInteraction.SetSelf(m_Controller.gameObject);
                statModifyInteraction.SetData(
                    StatModify_Interaction.WhichStatModifiersToUse.FromCustom,
                    m_Modifiers
                );
            }

            return m_ChosenInteractions;
        }

        public IInteraction Clone()
        {
            return CloneInternal();
        }

        IChooseData<IInteraction> IChooseData<IInteraction>.Clone()
        {
            return CloneInternal();
        }

        private StatModifierChooser CloneInternal()
        {
            StatModifierChooser clone = new StatModifierChooser();

            clone.m_NumberOfStatModifiersToChoose = m_NumberOfStatModifiersToChoose;

            clone.m_WhereToGetStatControllerFrom = m_WhereToGetStatControllerFrom;
            clone.m_Controller = m_Controller;
            clone.m_Self = m_Self;

            clone.m_HowToGetModificationValueChooser = m_HowToGetModificationValueChooser;
            clone.m_SelfStatTypes = m_SelfStatTypes != null ? m_SelfStatTypes.Clone() : null;
            clone.m_CanModifySelfStat = m_CanModifySelfStat != null ? m_CanModifySelfStat.Clone() : null;
            clone.m_OperationModificationsOnSelfStat = m_OperationModificationsOnSelfStat;

            clone.m_TargetStatTypes = m_TargetStatTypes != null ? m_TargetStatTypes.Clone() : null;
            clone.m_OperationModificationsOnTargetStat = m_OperationModificationsOnTargetStat;

            if (m_ChosenInteractions != null)
            {
                clone.m_ChosenInteractions = new();

                foreach (IInteraction interaction in m_ChosenInteractions)
                {
                    if (interaction != null)
                        clone.m_ChosenInteractions.Add(interaction.Clone());
                }
            }

            if (m_Modifiers != null)
            {
                clone.m_Modifiers = new();

                foreach (IStatModifier modifier in m_Modifiers)
                {
                    if (modifier != null)
                        clone.m_Modifiers.Add(modifier.Clone());
                }
            }

            return clone;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public void Interact(GameObject target)
        {
            ChooseData();

            if (m_ChosenInteractions == null)
                return;

            foreach (IInteraction interaction in m_ChosenInteractions)
            {
                interaction?.Interact(target);
            }
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return SetSelf(gameObj);
        }

        public bool SetSelf(GameObject self)
        {
            m_Self = self;

            if (m_WhereToGetStatControllerFrom == SelfType.ThisGameObject)
            {
                if (self != null)
                    self.TryGetComponent(out m_Controller);
            }

            bool allGood = true;

            allGood &= m_Self != null;
            allGood &= m_Controller != null;

            if (m_NumberOfStatModifiersToChoose != null)
                allGood &= m_NumberOfStatModifiersToChoose.SetGameObject(self);
            else
                allGood = false;

            if (m_OperationModificationsOnSelfStat != null)
                allGood &= m_OperationModificationsOnSelfStat.SetGameObject(self);

            if (m_OperationModificationsOnTargetStat != null)
                allGood &= m_OperationModificationsOnTargetStat.SetGameObject(self);

            m_ChosenInteractions ??= new();

            for (int i = 0; i < m_ChosenInteractions.Count; i++)
            {
                if (m_ChosenInteractions[i] != null)
                    allGood &= m_ChosenInteractions[i].SetSelf(self);
            }

            m_Modifiers ??= new();

            return allGood;
        }
    }
}
