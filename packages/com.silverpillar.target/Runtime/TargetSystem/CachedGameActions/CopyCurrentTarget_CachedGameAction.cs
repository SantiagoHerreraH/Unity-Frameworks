using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    public enum CopyType
    {
        SelfToCustom,
        CustomToSelf
    }

    [Serializable]
    public class CopyCurrentTarget_CachedGameAction : ICachedGameAction
    {
        [Title("Data")]
        [SerializeField]
        private CopyType m_CopyType;

        [SerializeField]
        private TargetSystem m_CustomTargetSystem;

        [Tooltip(
            "If true, a null source target will also be copied, " +
            "causing the destination TargetSystem to null its current target.")]
        [SerializeField]
        private bool m_CopyTargetIfNull;


        [Title("Debug")]

        [SerializeField]
        private bool m_PrintMessageIfSelfHasNoTargetSystem = true;

        [SerializeField]
        private bool m_PrintMessageIfCopiedTargetSystemHasNullTarget = false;


        private GameObject m_GameObject;
        private TargetSystem m_SelfTargetSystem;


        public ICachedGameAction Clone()
        {
            CopyCurrentTarget_CachedGameAction clone =
                new CopyCurrentTarget_CachedGameAction
                {
                    m_CopyType = m_CopyType,
                    m_CustomTargetSystem = m_CustomTargetSystem,
                    m_CopyTargetIfNull = m_CopyTargetIfNull,

                    m_PrintMessageIfSelfHasNoTargetSystem =
                        m_PrintMessageIfSelfHasNoTargetSystem,

                    m_PrintMessageIfCopiedTargetSystemHasNullTarget =
                        m_PrintMessageIfCopiedTargetSystemHasNullTarget
                };

            if (m_GameObject != null)
            {
                clone.SetGameObject(m_GameObject);
            }

            return clone;
        }


        public void Execute()
        {
            if (m_SelfTargetSystem == null)
            {
                if (m_PrintMessageIfSelfHasNoTargetSystem)
                {
                    Debug.LogWarning(
                        $"{nameof(CopyCurrentTarget_CachedGameAction)}: " +
                        $"{m_GameObject?.name ?? "NULL"} has no TargetSystem.");
                }

                return;
            }

            if (m_CustomTargetSystem == null)
            {
                Debug.LogWarning(
                        $"{nameof(CopyCurrentTarget_CachedGameAction)}: " +
                        $"{m_GameObject?.name ?? "NULL"} has no Custom TargetSystem.");
                return;
            }

            switch (m_CopyType)
            {
                case CopyType.SelfToCustom:
                    CopySelfToCustom();
                    break;

                case CopyType.CustomToSelf:
                    CopyCustomToSelf();
                    break;
            }
        }


        private void CopySelfToCustom()
        {
            GameObject target = m_SelfTargetSystem.CurrentTarget;

            if (target == null)
            {
                if (m_PrintMessageIfCopiedTargetSystemHasNullTarget)
                {
                    Debug.LogWarning(
                        $"{nameof(CopyCurrentTarget_CachedGameAction)}: " +
                        $"{m_GameObject.name}'s TargetSystem has no current target.");
                }

                if (m_CopyTargetIfNull)
                {
                    m_CustomTargetSystem.NullCurrentTarget();
                }

                return;
            }

            m_CustomTargetSystem.ChangeCurrentTarget(target);
        }


        private void CopyCustomToSelf()
        {
            GameObject target = m_CustomTargetSystem.CurrentTarget;

            if (target == null)
            {
                if (m_PrintMessageIfCopiedTargetSystemHasNullTarget)
                {
                    Debug.LogWarning(
                        $"{nameof(CopyCurrentTarget_CachedGameAction)}: " +
                        $"{m_CustomTargetSystem.gameObject.name}'s TargetSystem has no current target.");
                }

                if (m_CopyTargetIfNull)
                {
                    m_SelfTargetSystem.NullCurrentTarget();
                }

                return;
            }

            m_SelfTargetSystem.ChangeCurrentTarget(target);
        }


        public GameObject GetGameObject()
        {
            return m_GameObject;
        }


        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            m_SelfTargetSystem = null;

            if (gameObj == null)
            {
                return false;
            }

            if (!gameObj.TryGetComponent(out m_SelfTargetSystem))
            {
                if (m_PrintMessageIfSelfHasNoTargetSystem)
                {
                    Debug.LogWarning(
                        $"{nameof(CopyCurrentTarget_CachedGameAction)}: " +
                        $"{gameObj.name} has no TargetSystem.");
                }

                return false;
            }

            return true;
        }
    }
}