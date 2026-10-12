using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Senses
{
    public class SharedSenses
    {
        public class SenseGroupData
        {
            private Dictionary<SenseTarget, int> m_SharedTargets_To_NumberOfSenseControllersSensing = new();

            public void RegisterWithoutSensed(SenseTarget senseTarget)
            {
                if (!m_SharedTargets_To_NumberOfSenseControllersSensing.ContainsKey(senseTarget))
                {
                    m_SharedTargets_To_NumberOfSenseControllersSensing.Add(senseTarget, 0);
                }
            }

            public int RegisterSensedTarget(SenseTarget senseTarget)
            {
                m_SharedTargets_To_NumberOfSenseControllersSensing.TryGetValue(senseTarget, out int currentCount);

                int newCount = currentCount + 1;
                m_SharedTargets_To_NumberOfSenseControllersSensing[senseTarget] = newCount;

                return newCount;
            }

            public int UnregisterSensedTarget(SenseTarget senseTarget)
            {
                if (!m_SharedTargets_To_NumberOfSenseControllersSensing.TryGetValue(senseTarget, out int currentCount))
                {
                    return -1;
                }

                int newCount = currentCount <= 0 ? 0 : currentCount - 1;

                m_SharedTargets_To_NumberOfSenseControllersSensing[senseTarget] = newCount;
                return newCount;
            }

            public bool IsTargetSensed(SenseTarget senseTarget)
            {
                if (!m_SharedTargets_To_NumberOfSenseControllersSensing.ContainsKey(senseTarget))
                {
                    return false;
                }

                return m_SharedTargets_To_NumberOfSenseControllersSensing[senseTarget] > 0;
            }
        }

        private Dictionary<string, SenseGroupData> m_SenseGroups = new();

        public void CreateSenseGroup(string senseGroupName)
        {
            if (!m_SenseGroups.ContainsKey(senseGroupName))
            {
                m_SenseGroups.Add(senseGroupName, new());
            }
        }

        public void RegisterWithoutSensed(string senseGroupName, SenseTarget senseTarget)
        {
            CreateSenseGroup(senseGroupName);
            m_SenseGroups[senseGroupName].RegisterWithoutSensed(senseTarget);
        }

        public int RegisterSensedTarget(string senseGroupName, SenseTarget senseTarget)
        {
            CreateSenseGroup(senseGroupName);
            return m_SenseGroups[senseGroupName].RegisterSensedTarget(senseTarget);
        }

        public int UnregisterSensedTarget(string senseGroupName, SenseTarget senseTarget)
        {
            CreateSenseGroup(senseGroupName);
            return m_SenseGroups[senseGroupName].UnregisterSensedTarget(senseTarget);
        }

        public bool IsTargetSensed(string senseGroupName, SenseTarget senseTarget)
        {
            if (!m_SenseGroups.ContainsKey(senseGroupName))
            {
                return false;
            }

            return m_SenseGroups[senseGroupName].IsTargetSensed(senseTarget);
        }
    }
}
