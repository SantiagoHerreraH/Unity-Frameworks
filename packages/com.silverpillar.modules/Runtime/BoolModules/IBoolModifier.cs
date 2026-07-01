using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Modules
{
    [Serializable]
    public struct BoolModuleIdentity
    {
        public BoolModuleType ModuleType;
        public TargetType TargetType;
    }

    public interface IBoolModifier
    {
        public void Modify(BoolModuleController self, BoolModuleController target);
        public IBoolModifier Clone();
    }

    public enum BoolModifierOperation
    {
        Toggle,
        SetTrue,
        SetFalse,
        CustomCondition
    }

    [Serializable]
    public class DualOperationModifier : IBoolModifier
    {
        private bool m_ShowSelfCustomCondition => m_OperateOnSelf && m_OperationOnSelf == BoolModifierOperation.CustomCondition;
        private bool m_ShowTargetCustomCondition => m_OperateOnTarget && m_OperationOnTarget == BoolModifierOperation.CustomCondition;

        [Title("Self")]
        [SerializeField] private bool m_OperateOnSelf = true;
        [SerializeField, ShowIf(nameof(m_OperateOnSelf))] private BoolModuleType m_SelfModule;
        [SerializeField, ShowIf(nameof(m_OperateOnSelf))] private BoolModifierOperation m_OperationOnSelf;
        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_ShowSelfCustomCondition))]
        private ICachedCondition m_SelfCustomCondition;

        [Title("Target")]
        [SerializeField] private bool m_OperateOnTarget = true;
        [SerializeField, ShowIf(nameof(m_OperateOnTarget))] private BoolModuleType m_TargetModule;
        [SerializeField, ShowIf(nameof(m_OperateOnTarget))] private BoolModifierOperation m_OperationOnTarget;
        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_ShowTargetCustomCondition))]
        private ICachedCondition m_TargetCustomCondition;
        public void Modify(BoolModuleController self, BoolModuleController target)
        {
            if (m_OperateOnSelf && self != null)
                ExecuteOperation(self, m_SelfModule, m_OperationOnSelf, true);

            if (m_OperateOnTarget && target != null)
                ExecuteOperation(target, m_TargetModule, m_OperationOnTarget, false);
        }

        private void ExecuteOperation(BoolModuleController controller, BoolModuleType type, BoolModifierOperation op, bool self)
        {
            switch (op)
            {
                case BoolModifierOperation.Toggle:
                    controller.SetState(type, !controller.GetState(type));
                    break;
                case BoolModifierOperation.SetTrue:
                    controller.SetState(type, true);
                    break;
                case BoolModifierOperation.SetFalse:
                    controller.SetState(type, false);
                    break;
                case BoolModifierOperation.CustomCondition:

                    if (self)
                    {
                        m_SelfCustomCondition.SetGameObject(controller.gameObject);
                        controller.SetState(type, m_SelfCustomCondition.IsFulfilled());
                    }
                    else
                    {
                        m_TargetCustomCondition.SetGameObject(controller.gameObject);
                        controller.SetState(type, m_TargetCustomCondition.IsFulfilled());
                    }

                    break;
            }
        }

        public IBoolModifier Clone() => (IBoolModifier)this.MemberwiseClone();
    }

    [Serializable]
    public class StateCopierModifier : IBoolModifier
    {
        [Title("Copy Operation")]
        [SerializeField] private BoolModuleIdentity m_CopyThis;
        [SerializeField] private BoolModuleIdentity m_ToThis;

        [Title("Operations On Copied")]
        [SerializeField] private bool m_OperateBeforeCopied = true;
        [SerializeField, ShowIf(nameof(m_OperateBeforeCopied))] private BoolModifierOperation m_OperationBeforeCopied;
        [SerializeField] private bool m_OperateAfterCopied = true;
        [SerializeField, ShowIf(nameof(m_OperateAfterCopied))] private BoolModifierOperation m_OperationAfterCopied;

        [Title("Operations On Pasted")]
        [SerializeField] private bool m_OperateAfterPasted = true;
        [SerializeField, ShowIf(nameof(m_OperateAfterPasted))] private BoolModifierOperation m_OperationAfterPasted;

        public void Modify(BoolModuleController self, BoolModuleController target)
        {
            var source = GetController(m_CopyThis.TargetType, self, target);
            var destination = GetController(m_ToThis.TargetType, self, target);

            if (source == null || destination == null) return;

            if (m_OperateBeforeCopied)
                ExecuteOperation(source, m_CopyThis.ModuleType, m_OperationBeforeCopied);

            bool stateToCopy = source.GetState(m_CopyThis.ModuleType);
            destination.SetState(m_ToThis.ModuleType, stateToCopy);

            if (m_OperateAfterCopied)
                ExecuteOperation(source, m_CopyThis.ModuleType, m_OperationAfterCopied);

            if (m_OperateAfterPasted)
                ExecuteOperation(destination, m_ToThis.ModuleType, m_OperationAfterPasted);
        }

#nullable enable
        private BoolModuleController? GetController(TargetType targetType, BoolModuleController self, BoolModuleController target)
        {
            return targetType == TargetType.Self ? self : target;
        }

        private void ExecuteOperation(BoolModuleController controller, BoolModuleType type, BoolModifierOperation op)
        {
            switch (op)
            {
                case BoolModifierOperation.Toggle:
                    controller.SetState(type, !controller.GetState(type));
                    break;
                case BoolModifierOperation.SetTrue:
                    controller.SetState(type, true);
                    break;
                case BoolModifierOperation.SetFalse:
                    controller.SetState(type, false);
                    break;
            }
        }

        public IBoolModifier Clone() => (IBoolModifier)this.MemberwiseClone();
    }
}
