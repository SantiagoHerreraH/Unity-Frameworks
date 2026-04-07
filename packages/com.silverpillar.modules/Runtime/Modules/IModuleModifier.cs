using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Modules
{
    [Serializable]
    public struct ModuleIdentity
    {
        public ModuleType ModuleType;
        public TargetType TargetType;
    }

    public interface IModuleModifier
    {
        public void Modify(ModuleController self, ModuleController target);
        public IModuleModifier Clone();
    }

    public enum ModuleModifierOperation
    {
        SetToFirst,
        SetToLast,
        NextIndexClamp,     
        NextIndexLoop,      
        PreviousIndexClamp, 
        PreviousIndexLoop,
        RandomIndex,
        SetToIndex
    }

    [Serializable]
    public class DualModuleOperationModifier : IModuleModifier
    {
        [Title("Self")]
        [SerializeField] private bool m_OperateOnSelf = true;
        [SerializeField, ShowIf(nameof(m_OperateOnSelf))] private ModuleType m_SelfModule;
        [SerializeField, ShowIf(nameof(m_OperateOnSelf))] private ModuleModifierOperation m_OpOnSelf;
        [SerializeField, ShowIf(nameof(m_OpOnSelf), ModuleModifierOperation.SetToIndex)]
        private int m_IndexSelf;

        [Title("Target")]
        [SerializeField] private bool m_OperateOnTarget = true;
        [SerializeField, ShowIf(nameof(m_OperateOnTarget))] private ModuleType m_TargetModule;
        [SerializeField, ShowIf(nameof(m_OperateOnTarget))] private ModuleModifierOperation m_OpOnTarget;
        [SerializeField, ShowIf(nameof(m_OpOnTarget), ModuleModifierOperation.SetToIndex)]
        private int m_IndexTarget;

        public void Modify(ModuleController self, ModuleController target)
        {
            if (m_OperateOnSelf && self != null)
                ExecuteOperation(self, m_SelfModule, m_OpOnSelf, m_IndexSelf);

            if (m_OperateOnTarget && target != null)
                ExecuteOperation(target, m_TargetModule, m_OpOnTarget, m_IndexTarget);
        }

        private void ExecuteOperation(ModuleController controller, ModuleType type, ModuleModifierOperation op, int value)
        {
            int current = controller.GetCurrent(type);
            int max = controller.GetCount(type);

            if (max <= 0) return;

            switch (op)
            {
                case ModuleModifierOperation.SetToFirst:
                    controller.SetCurrent(type, 0);
                    break;

                case ModuleModifierOperation.SetToLast:
                    controller.SetCurrent(type, max - 1);
                    break;

                case ModuleModifierOperation.NextIndexClamp:
                    controller.SetCurrent(type, current + 1);
                    break;

                case ModuleModifierOperation.NextIndexLoop:
                    int next = (current + 1 >= max) ? 0 : current + 1;
                    controller.SetCurrent(type, next);
                    break;

                case ModuleModifierOperation.PreviousIndexClamp:
                    controller.SetCurrent(type, current - 1);
                    break;

                case ModuleModifierOperation.PreviousIndexLoop:
                    int prev = (current - 1 < 0) ? max - 1 : current - 1;
                    controller.SetCurrent(type, prev);
                    break;
                case ModuleModifierOperation.RandomIndex:
                    int randomIndex = UnityEngine.Random.Range(0, max);
                    controller.SetCurrent(type, randomIndex);
                    break;
                case ModuleModifierOperation.SetToIndex:
                    controller.SetCurrent(type, value);
                    break;
            }
        }
        public IModuleModifier Clone() => (IModuleModifier)this.MemberwiseClone();
    }

    [Serializable]
    public class ModuleStateCopierModifier : IModuleModifier
    {
        [Title("Copy Operation")]
        [SerializeField] private ModuleIdentity m_CopyThis;
        [SerializeField] private ModuleIdentity m_ToThis;

        [Title("Settings")]
        [Tooltip("If true, the destination index will be offset by this value.")]
        [SerializeField] private int m_IndexOffset = 0;

        public void Modify(ModuleController self, ModuleController target)
        {
            var sourceController = GetController(m_CopyThis.TargetType, self, target);
            var destController = GetController(m_ToThis.TargetType, self, target);

            if (sourceController == null || destController == null) return;

            int sourceIndex = sourceController.GetCurrent(m_CopyThis.ModuleType);
            destController.SetCurrent(m_ToThis.ModuleType, sourceIndex + m_IndexOffset);
        }

#nullable enable
        private ModuleController? GetController(TargetType targetType, ModuleController self, ModuleController target)
        {
            return targetType == TargetType.Self ? self : target;
        }

        public IModuleModifier Clone() => (IModuleModifier)this.MemberwiseClone();
    }
}
