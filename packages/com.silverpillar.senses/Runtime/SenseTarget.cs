using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

namespace SilverPillar.Senses
{

   public class SenseTarget : SerializedMonoBehaviour
    {
        private static List<SenseTarget> m_RegisteredTargets = new();
        public static List<SenseTarget> RegisteredTargets {  get { return m_RegisteredTargets; } }
        private bool m_Registered;

        [SerializeField]
        private bool m_CanBeSensed = true;
        [OdinSerialize, ShowInInspector]
        private List<SenseTargetQuality> m_SenseTargetQualities;
        public List<SenseTargetQuality> SenseTargetQualities { get { return m_SenseTargetQualities; } }

        public void ActivateSensing()
        {
            m_CanBeSensed = true;
        }

        public void DeactivateSensing()
        {
            m_CanBeSensed = false;
        }

        public bool CanBeSensed()
        {
            return m_CanBeSensed;
        }

        public bool HasSenseQualityType(Type byWhatSenseTargetQuality)
        {
            for (int i = 0; i < m_SenseTargetQualities.Count; i++)
            {
                if (m_SenseTargetQualities[i].GetType() == byWhatSenseTargetQuality)
                {
                    return true;
                }
            }

            return false;
        }

        public void ActivateSensing(Type byWhatSenseTargetQuality)
        {
            for (int i = 0; i < m_SenseTargetQualities.Count; i++)
            {
                if (m_SenseTargetQualities[i].GetType() == byWhatSenseTargetQuality)
                {
                    m_SenseTargetQualities[i].SetCanBeSensed(true);
                    return;
                }
            }
        }

        public void DeactivateSensing(Type byWhatSenseTargetQuality)
        {
            for (int i = 0; i < m_SenseTargetQualities.Count; i++)
            {
                if (m_SenseTargetQualities[i].GetType() == byWhatSenseTargetQuality)
                {
                    m_SenseTargetQualities[i].SetCanBeSensed(false);
                    return;
                }
            }
        }

        public bool CanBeSensed(Type byWhatSenseTargetQuality)
        {
            if (!m_CanBeSensed)
            {
                return false;
            }

            for (int i = 0; i < m_SenseTargetQualities.Count; i++)
            {
                if (m_SenseTargetQualities[i].GetType() == byWhatSenseTargetQuality)
                {
                    return m_SenseTargetQualities[i].CanBeSensed();
                }
            }

            return false;
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        public void Register()
        {
            m_RegisteredTargets ??= new();
            if (!m_Registered)
            {
                m_Registered = true;
                m_RegisteredTargets.Add(this);

                for (int i = 0; i < SenseController.RegisteredControllers.Count; i++)
                {
                    SenseController.RegisteredControllers[i].Register(this);
                }
            }
        }

        public void Unregister()
        {
            if (!m_Registered)
            {
                return;
            }
            m_Registered = false;
            m_RegisteredTargets.Remove(this);

            for (int i = 0; i < SenseController.RegisteredControllers.Count; i++)
            {
                SenseController.RegisteredControllers[i].Unregister(this);
            }

        }
    }
}