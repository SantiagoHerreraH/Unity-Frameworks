using UnityEngine;
using System.Collections.Generic;
using System;

namespace SilverPillar.Senses
{
    public class SensedInformation
    {
        public HashSet<GameObject> Sensed;
        public HashSet<GameObject> NotSensed;

        public void EnsureCapacity(int capacity)
        {
            Sensed ??= new();
            NotSensed ??= new();

            Sensed.EnsureCapacity(capacity);
            NotSensed.EnsureCapacity(capacity);
        }

        public void UnionWith(SensedInformation senseInfo)
        {
            if (senseInfo == null)
            {
                return;
            }

            Sensed ??= new();
            NotSensed ??= new();

            if (senseInfo.Sensed != null)
            {
                Sensed.UnionWith(senseInfo.Sensed);
            }

            if (senseInfo.NotSensed != null)
            {
                NotSensed.UnionWith(senseInfo.NotSensed);
            }

            // A target sensed by any sense wins over a negative result from another sense.
            NotSensed.ExceptWith(Sensed);
        }

        public void KeepNew(SensedInformation old)
        {
            Sensed ??= new();
            NotSensed ??= new();

            if (old == null)
            {
                return;
            }

            if (old.Sensed != null)
            {
                // Only targets that were not sensed before are newly sensed.
                Sensed.ExceptWith(old.Sensed);
            }

            if (old.NotSensed != null)
            {
                // Only targets that were not already in the not-sensed state are new negatives.
                NotSensed.ExceptWith(old.NotSensed);
            }
        }

        public void Clear()
        {
            Sensed ??= new();
            NotSensed ??= new();
            Sensed.Clear();
            NotSensed.Clear();
        }
    }

    public enum SensingPermissions
    {
        DontSenseIfSenseTargetHiddenOrSenseTargetQualityHidden,
        DontSenseIfSenseTargetHidden,
        SenseEvenIfSenseTargetHidden,
    }

    public abstract class ISense
    {
        protected bool m_CanSense = true;

        public abstract void SetSenseController(SenseController senseController);

        public abstract bool RegisterSenseTarget(SenseTarget senseTarget);
        public abstract void UnregisterSenseTarget(SenseTarget senseTarget);
        public abstract void UnregisterAll();

        public void SetCanSense(bool canSense)
        {
            m_CanSense = canSense;
        }

        public bool CanSense()
        {
            return m_CanSense;
        }

        public abstract SensedInformation CalculateSensed();

        public abstract Type SenseTargetQualityNeededToSense();

        public abstract void Draw();
    }
}
