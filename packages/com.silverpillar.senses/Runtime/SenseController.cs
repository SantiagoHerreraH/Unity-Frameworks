using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine.Events;
using SilverPillar.Core;

namespace SilverPillar.Senses
{
    public class SenseController : SerializedMonoBehaviour
    {
        private static SharedSenses m_SharedSenses = new();
        private static List<SenseController> m_RegisteredControllers = new();
        public static List<SenseController> RegisteredControllers
        {
            get { return m_RegisteredControllers; }
        }

        private bool m_Registered;
        private bool m_Initialized;

        private HashSet<SenseTarget> m_RegisteredTargets = new();

        [Title("Forget Configuration")]
        [SerializeField]
        private bool m_CanForget;

        [SerializeField, ShowIf(nameof(m_CanForget)), Min(0)]
        private float m_TimeToForget;

        // Parallel arrays: timer at index i belongs to target at index i.
        private readonly List<float> m_ForgetTimers = new();
        private readonly List<SenseTarget> m_ForgetTimerTargets = new();
        private readonly Dictionary<SenseTarget, int> m_ForgetTimerIndices = new();

        [Title("Sense Configuration")]
        [SerializeField, Min(0), Tooltip("Time between each sensing")]
        private float m_HowOftenToSense;

        private float m_CurrentTimeSinceLastSensed;

        private enum WhenToUnregisterInSenseGroup
        {
            OnStopSensing,
            OnForget
        }

        [Title("Sense Group Configuration")]
        [SerializeField]
        private bool m_RegisterInSenseGroupUponSensing;

        private bool m_GroupSensingAndCanForget => m_RegisterInSenseGroupUponSensing && m_CanForget;

        [SerializeField, ShowIf(nameof(m_GroupSensingAndCanForget))]
        private WhenToUnregisterInSenseGroup m_WhenToUnregisterInSenseGroup = WhenToUnregisterInSenseGroup.OnStopSensing;

        [OdinSerialize, ShowIf(nameof(m_RegisterInSenseGroupUponSensing))]
        private IString m_SenseGroupName;
        private string m_SenseGroupNameValue;

        [Title("Senses")]
        [OdinSerialize, ShowInInspector]
        private List<ISense> m_Senses;

        private SensedInformation m_CurrentSenseInformation = new();
        private SensedInformation m_NewSenseInformation = new();
        private SensedInformation m_ChangesSenseInformation = new();

        [Title("Events")]
        [SerializeField]
        private UnityEvent<GameObject> m_OnSenseNew;

        [SerializeField]
        private UnityEvent<GameObject> m_OnStopSensing;

        [SerializeField, ShowIf(nameof(m_CanForget)),
            Tooltip("OnForget is called when TimeToForget time is met after stop sensing." +
            " If object is sensed again, time is reset.")]
        private UnityEvent<GameObject> m_OnForget;

        [SerializeField, ShowIf(nameof(m_RegisterInSenseGroupUponSensing)),
            Tooltip("This event is called when this SenseController is the first in the" +
            " sense group to sense this object.")]
        private UnityEvent<GameObject> m_OnFirstInGroupToSense;

        [SerializeField, ShowIf(nameof(m_RegisterInSenseGroupUponSensing)),
           Tooltip("This event is called when this SenseController is the last in the sense group" +
            " to stop sensing or forget (whichever unregister mode you chose) this object.")]
        private UnityEvent<GameObject> m_OnLastInGroupToStopSensingOrForget;

        public bool HasTargetsInMemory()
        {
            return m_CurrentSenseInformation.Sensed.Count > 0
                || m_ForgetTimerIndices.Count > 0;
        }

        //aka hasn't forgotten
        public bool IsInMemory(GameObject gameObject)
        {
            if (gameObject != null &&
                gameObject.TryGetComponent(out SenseTarget target))
            {
                return IsInMemory(target);
            }

            return false;
        }

        public bool IsInMemory(SenseTarget senseTarget)
        {
            if (senseTarget == null)
            {
                return false;
            }

            return m_CurrentSenseInformation.IsSensing(senseTarget)
                || m_ForgetTimerIndices.ContainsKey(senseTarget);
        }

        public bool IsSensing(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out SenseTarget target))
            {
                return IsSensing(target);
            }

            return false;
        }

        public bool IsSensing(SenseTarget senseTarget)
        {
            return m_CurrentSenseInformation.IsSensing(senseTarget);
        }

        public bool IsInSensingGroup(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out SenseTarget target))
            {
                return IsInSensingGroup(target);
            }

            return false;
        }

        public bool IsInSensingGroup(SenseTarget senseTarget)
        {
            return m_SharedSenses.IsTargetSensed(m_SenseGroupNameValue, senseTarget);
        }
        private void OnEnable()
        {
            RegisterSenseController();
        }

        private void OnDisable()
        {
            UnregisterSenseController();
        }

        private void Update()
        {
            Initialize();

            if (m_CurrentTimeSinceLastSensed >= m_HowOftenToSense)
            {
                m_CurrentTimeSinceLastSensed = 0f;
                CalculateSenses();
            }

            m_CurrentTimeSinceLastSensed += Time.deltaTime;

            UpdateForgetTimers(Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            DrawSenses();
        }

        private void DrawSenses()
        {
            m_Senses ??= new();

            for (int i = 0; i < m_Senses.Count; i++)
            {
                ISense sense = m_Senses[i];

                if (sense == null)
                {
                    continue;
                }

                sense.SetSenseController(this);
                sense.Draw();
            }
        }

        private void CalculateSenses()
        {
            m_NewSenseInformation.Clear();
            m_NewSenseInformation.EnsureCapacity(m_RegisteredTargets.Count);

            for (int i = 0; i < m_Senses.Count; i++)
            {
                if (m_Senses[i] != null && m_Senses[i].CanSense())
                {
                    m_NewSenseInformation.UnionWith(
                        m_Senses[i].CalculateSensed());
                }
            }

            // Determine what changed compared with the previous sensing pass.
            m_ChangesSenseInformation.Clear();
            m_ChangesSenseInformation.UnionWith(m_NewSenseInformation);
            m_ChangesSenseInformation.KeepNew(m_CurrentSenseInformation);

            // Newly sensed.
            foreach (SenseTarget item in m_ChangesSenseInformation.Sensed)
            {
                if (item == null)
                {
                    continue;
                }

                // The object became visible/sensed again.
                // Cancel and reset any pending forget timer.
                ResetForgetTimer(item);

                m_OnSenseNew?.Invoke(item.gameObject);

                if (m_RegisterInSenseGroupUponSensing)
                {
                    if (m_SharedSenses.RegisterSensedTarget(m_SenseGroupNameValue, item) == 1)
                    {
                        m_OnFirstInGroupToSense?.Invoke(item.gameObject);
                    }
                }
            }

            // No longer sensed.
            foreach (SenseTarget item in m_ChangesSenseInformation.NotSensed)
            {
                if (item == null)
                {
                    continue;
                }

                m_OnStopSensing?.Invoke(item.gameObject);

                if (m_RegisterInSenseGroupUponSensing && 
                    m_WhenToUnregisterInSenseGroup == WhenToUnregisterInSenseGroup.OnStopSensing)
                {
                    if (m_SharedSenses.UnregisterSensedTarget(m_SenseGroupNameValue, item) == 0)
                    {
                        m_OnLastInGroupToStopSensingOrForget?.Invoke(item.gameObject);
                    }
                }

                StartForgetTimer(item);
            }

            m_CurrentSenseInformation.Clear();
            m_CurrentSenseInformation.UnionWith(m_NewSenseInformation);
        }

        private void InvokeOnForget(SenseTarget senseTarget)
        {
            if (m_RegisterInSenseGroupUponSensing &&
                   m_WhenToUnregisterInSenseGroup == WhenToUnregisterInSenseGroup.OnForget)
            {
                if (m_SharedSenses.UnregisterSensedTarget(m_SenseGroupNameValue, senseTarget) == 0)
                {
                    m_OnLastInGroupToStopSensingOrForget?.Invoke(senseTarget.gameObject);
                }
            }
            m_OnForget?.Invoke(senseTarget.gameObject);
        }

        /// <summary>Starts a timer only if one is not already pending.</summary>
        private void StartForgetTimer(SenseTarget senseTarget)
        {
            if (!m_CanForget || senseTarget == null || senseTarget.gameObject == null)
                return;

            if (m_TimeToForget <= 0f)
            {
                InvokeOnForget(senseTarget);
                return;
            }

            if (m_ForgetTimerIndices.ContainsKey(senseTarget))
                return;

            int index = m_ForgetTimers.Count;
            m_ForgetTimerIndices.Add(senseTarget, index);
            m_ForgetTimerTargets.Add(senseTarget);
            m_ForgetTimers.Add(0f);
        }

        /// <summary>Cancels the pending timer, if any.</summary>
        private void ResetForgetTimer(SenseTarget senseTarget)
        {
            // ReferenceEquals permits cleanup of Unity objects that have been destroyed.
            if (ReferenceEquals(senseTarget, null))
                return;

            if (m_ForgetTimerIndices.TryGetValue(senseTarget, out int index))
                RemoveForgetTimerAt(index);
        }

        /// <summary>Removes an entry in O(1) average time by moving the last entry into its slot.</summary>
        private void RemoveForgetTimerAt(int index)
        {
            int lastIndex = m_ForgetTimers.Count - 1;
            SenseTarget removedTarget = m_ForgetTimerTargets[index];

            if (index != lastIndex)
            {
                SenseTarget movedTarget = m_ForgetTimerTargets[lastIndex];
                m_ForgetTimers[index] = m_ForgetTimers[lastIndex];
                m_ForgetTimerTargets[index] = movedTarget;
                m_ForgetTimerIndices[movedTarget] = index;
            }

            m_ForgetTimers.RemoveAt(lastIndex);
            m_ForgetTimerTargets.RemoveAt(lastIndex);
            m_ForgetTimerIndices.Remove(removedTarget);
        }

        private void ClearForgetTimers()
        {
            m_ForgetTimers.Clear();
            m_ForgetTimerTargets.Clear();
            m_ForgetTimerIndices.Clear();
        }

        /// <summary>Advances pending timers and forgets expired targets.</summary>
        private void UpdateForgetTimers(float deltaTime)
        {
            if (!m_CanForget)
            {
                ClearForgetTimers();
                return;
            }

            // Iterate backwards so swap-back removal never skips an unprocessed entry.
            // Remove before invoking callbacks, which may add or cancel other timers.
            for (int i = m_ForgetTimers.Count - 1; i >= 0; i--)
            {
                SenseTarget target = m_ForgetTimerTargets[i];
                if (target == null)
                {
                    RemoveForgetTimerAt(i);
                    continue;
                }

                float elapsed = m_ForgetTimers[i] + deltaTime;
                if (elapsed < m_TimeToForget)
                {
                    m_ForgetTimers[i] = elapsed;
                    continue;
                }

                RemoveForgetTimerAt(i);
                InvokeOnForget(target);
            }
        }

        public void RegisterSenseController()
        {
            m_RegisteredControllers ??= new();

            if (!m_Registered)
            {
                m_Registered = true;
                m_RegisteredControllers.Add(this);

                Initialize();

                for (int i = 0; i < m_Senses.Count; i++)
                {
                    m_Senses[i]?.UnregisterAll();
                }

                for (int i = 0; i < SenseTarget.RegisteredTargets.Count; i++)
                {
                    Register(SenseTarget.RegisteredTargets[i]);
                }
            }
        }

        public void UnregisterSenseController()
        {
            if (!m_Registered)
            {
                return;
            }

            m_Registered = false;
            m_RegisteredControllers.Remove(this);

            Initialize();

            for (int i = 0; i < m_Senses.Count; i++)
            {
                m_Senses[i]?.UnregisterAll();
            }

            ClearForgetTimers();
        }

        public void Register(SenseTarget target)
        {
            if (target == null || m_RegisteredTargets.Contains(target))
            {
                return;
            }

            Initialize();

            if (m_RegisterInSenseGroupUponSensing)
            {
                m_SharedSenses.RegisterWithoutSensed(m_SenseGroupNameValue, target);
            }

            m_RegisteredTargets.Add(target);

            for (int i = 0; i < m_Senses.Count; i++)
            {
                m_Senses[i]?.RegisterSenseTarget(target);
            }
        }

        public void Unregister(SenseTarget target)
        {
            if (target == null)
            {
                return;
            }

            Initialize();

            m_RegisteredTargets.Remove(target);

            for (int i = 0; i < m_Senses.Count; i++)
            {
                m_Senses[i]?.UnregisterSenseTarget(target);
            }

            // The target is no longer part of this controller at all,
            // so it shouldn't have a pending forget timer.
            ResetForgetTimer(target);
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            if (m_RegisterInSenseGroupUponSensing)
            {
                if (m_SenseGroupName != null)
                {
                    m_SenseGroupName.SetGameObject(gameObject);
                    m_SenseGroupNameValue = m_SenseGroupName.CalculateString();
                }
                else
                {
                    m_SenseGroupNameValue = "Default Sense Group";
                }

                m_SharedSenses.CreateSenseGroup(m_SenseGroupNameValue);
            }

            m_Senses ??= new();

            for (int i = 0; i < m_Senses.Count; i++)
            {
                m_Senses[i]?.SetSenseController(this);
            }

            m_Initialized = true;
        }
    }
}