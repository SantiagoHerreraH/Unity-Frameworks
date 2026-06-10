using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class InteractWithFilteredPossibleTargets_CachedGameAction : ICachedGameAction
    {
        public enum HowToChooseInteractionTargets
        {
            FirstToMeetCondition,
            OrderThroughScore
        }

        private bool m_CanShowScoreData =>
            m_LimitToMaxInteractionNumber &&
            m_HowToChooseInteractionTargets == HowToChooseInteractionTargets.OrderThroughScore;

        [Title("Interaction Number")]
        [SerializeField]
        private bool m_LimitToMaxInteractionNumber;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_LimitToMaxInteractionNumber))]
        private ICachedInteractionScore m_MaxInteractionNumber;

        [SerializeField, ShowIf(nameof(m_LimitToMaxInteractionNumber))]
        private HowToChooseInteractionTargets m_HowToChooseInteractionTargets =
            HowToChooseInteractionTargets.FirstToMeetCondition;

        [SerializeField, ShowIf(nameof(m_CanShowScoreData))]
        public HowToOrderScores m_HowToOrderScores;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_CanShowScoreData))]
        public ICachedInteractionScore m_ScoreGenerator;

        [Title("Interaction Condition")]
        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_ConditionToInteract;

        [OdinSerialize, ShowInInspector]
        private List<IInteraction> m_Interactions = new();

        private TargetSystem m_TargetSystem;

        public InteractWithFilteredPossibleTargets_CachedGameAction() { }

        public InteractWithFilteredPossibleTargets_CachedGameAction(
            InteractWithFilteredPossibleTargets_CachedGameAction other)
        {
            m_LimitToMaxInteractionNumber = other.m_LimitToMaxInteractionNumber;
            m_HowToChooseInteractionTargets = other.m_HowToChooseInteractionTargets;
            m_HowToOrderScores = other.m_HowToOrderScores;

            m_MaxInteractionNumber = other.m_MaxInteractionNumber != null
                ? (ICachedInteractionScore)other.m_MaxInteractionNumber.Clone()
                : null;

            m_ScoreGenerator = other.m_ScoreGenerator != null
                ? (ICachedInteractionScore)other.m_ScoreGenerator.Clone()
                : null;

            m_ConditionToInteract = other.m_ConditionToInteract != null
                ? (IInteractionCondition)other.m_ConditionToInteract.Clone()
                : null;

            m_Interactions = other.m_Interactions != null
                ? other.m_Interactions
                    .Where(i => i != null)
                    .Select(i => i.Clone())
                    .ToList()
                : new List<IInteraction>();

            m_TargetSystem = other.m_TargetSystem;
        }

        public ICachedGameAction Clone()
        {
            return new InteractWithFilteredPossibleTargets_CachedGameAction(this);
        }

        public void Execute()
        {
            if (m_TargetSystem == null)
                return;

            if (m_Interactions == null || m_Interactions.Count == 0)
                return;

            List<GameObject> targetsToInteractWith = GetTargetsToInteractWith();

            if (targetsToInteractWith == null)
            {
                return;
            }

            foreach (GameObject target in targetsToInteractWith)
            {
                if (target == null)
                    continue;

                foreach (IInteraction interaction in m_Interactions)
                {
                    interaction?.Interact(target);
                }
            }
        }

        private List<GameObject> GetTargetsToInteractWith()
        {
            if (m_TargetSystem == null || m_TargetSystem.PossibleTargets == null)
                return null;

            IEnumerable<GameObject> filteredTargets = m_TargetSystem.PossibleTargets
                .Where(target => target != null)
                .Where(MeetsInteractionCondition);

            if (!m_LimitToMaxInteractionNumber)
                return filteredTargets.ToList();

            int maxInteractionNumber = GetMaxInteractionNumber();

            if (maxInteractionNumber <= 0)
                return new List<GameObject>();

            switch (m_HowToChooseInteractionTargets)
            {
                case HowToChooseInteractionTargets.FirstToMeetCondition:
                    return filteredTargets
                        .Take(maxInteractionNumber)
                        .ToList();

                case HowToChooseInteractionTargets.OrderThroughScore:
                    return OrderTargetsByScore(filteredTargets)
                        .Take(maxInteractionNumber)
                        .ToList();

                default:
                    return filteredTargets
                        .Take(maxInteractionNumber)
                        .ToList();
            }
        }

        private bool MeetsInteractionCondition(GameObject target)
        {
            if (target == null)
                return false;

            if (m_ConditionToInteract == null)
                return true;

            return m_ConditionToInteract.IsFulfilled(target);
        }

        private int GetMaxInteractionNumber()
        {
            if (!m_LimitToMaxInteractionNumber)
                return int.MaxValue;

            if (m_MaxInteractionNumber == null)
                return 0;

            // There is no specific interaction target for the max count,
            // so the TargetSystem's own GameObject is used as the reference.
            float rawValue = Convert.ToSingle(
                m_MaxInteractionNumber.CalculateScore(m_TargetSystem.gameObject)
            );

            return Mathf.Max(0, Mathf.FloorToInt(rawValue));
        }

        private IEnumerable<GameObject> OrderTargetsByScore(IEnumerable<GameObject> targets)
        {
            if (m_ScoreGenerator == null)
                return targets;

            bool lowestToHighest = m_HowToOrderScores == HowToOrderScores.LowestToHighest;

            return lowestToHighest
                ? targets.OrderBy(GetTargetScore)
                : targets.OrderByDescending(GetTargetScore);
        }

        private float GetTargetScore(GameObject target)
        {
            if (target == null || m_ScoreGenerator == null)
                return 0f;

            return Convert.ToSingle(m_ScoreGenerator.CalculateScore(target));
        }

        public GameObject? GetGameObject()
        {
            return m_TargetSystem != null ? m_TargetSystem.gameObject : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                m_TargetSystem = null;
                return false;
            }

            if (!gameObj.TryGetComponent(out m_TargetSystem))
                return false;

            bool allGood = true;

            if (m_Interactions != null)
            {
                foreach (IInteraction interaction in m_Interactions)
                {
                    allGood &= interaction != null && interaction.SetSelf(gameObj);
                }
            }

            allGood &= m_ConditionToInteract != null
                ? m_ConditionToInteract.SetGameObject(gameObj)
                : true;

            allGood &= m_MaxInteractionNumber != null
                ? m_MaxInteractionNumber.SetGameObject(gameObj)
                : !m_LimitToMaxInteractionNumber;

            allGood &= m_ScoreGenerator != null
                ? m_ScoreGenerator.SetGameObject(gameObj)
                : !m_CanShowScoreData;

            return allGood;
        }
    }
}