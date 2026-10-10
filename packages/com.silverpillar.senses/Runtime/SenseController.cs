using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine.Events;

namespace SilverPillar.Senses
{
    public class SenseController : SerializedMonoBehaviour
    {
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

        /// <summary>
        /// Objects that are currently waiting to be forgotten.
        /// Value = amount of time since they stopped being sensed.
        /// </summary>
        private readonly Dictionary<GameObject, float> m_ForgetTimers = new();

        /// <summary>
        /// Temporary list used so we can safely remove entries from
        /// m_ForgetTimers after iterating through it.
        /// </summary>
        private readonly List<GameObject> m_ObjectsToForget = new();

        [Title("Sense Configuration")]
        [SerializeField, Min(0), Tooltip("Time between each sensing")]
        private float m_HowOftenToSense;

        private float m_CurrentTimeSinceLastSensed;

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
            Tooltip("OnForget is called when TimeToForget time is met after stop sensing. If object is sensed again, time is reset.")]
        private UnityEvent<GameObject> m_OnForget;


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
            foreach (GameObject item in m_ChangesSenseInformation.Sensed)
            {
                if (item == null)
                {
                    continue;
                }

                // The object became visible/sensed again.
                // Cancel and reset any pending forget timer.
                ResetForgetTimer(item);

                m_OnSenseNew?.Invoke(item);
            }

            // No longer sensed.
            foreach (GameObject item in m_ChangesSenseInformation.NotSensed)
            {
                if (item == null)
                {
                    continue;
                }

                m_OnStopSensing?.Invoke(item);

                StartForgetTimer(item);
            }

            m_CurrentSenseInformation.Clear();
            m_CurrentSenseInformation.UnionWith(m_NewSenseInformation);
        }

        /// <summary>
        /// Starts counting the time until an object is forgotten.
        ///
        /// Calling this multiple times for the same object does not restart
        /// the timer. The timer is only reset when the object is sensed again.
        /// </summary>
        private void StartForgetTimer(GameObject gameObject)
        {
            if (!m_CanForget || gameObject == null)
            {
                return;
            }

            if (m_TimeToForget <= 0f)
            {
                m_OnForget?.Invoke(gameObject);
                return;
            }

            if (!m_ForgetTimers.ContainsKey(gameObject))
            {
                m_ForgetTimers.Add(gameObject, 0f);
            }
        }

        /// <summary>
        /// Cancels the forget timer when the object becomes sensed again.
        /// </summary>
        private void ResetForgetTimer(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            m_ForgetTimers.Remove(gameObject);
        }

        /// <summary>
        /// Updates every pending forget timer.
        /// </summary>
        private void UpdateForgetTimers(float deltaTime)
        {
            if (!m_CanForget)
            {
                // If forgetting is disabled while timers exist,
                // discard all pending forget operations.
                if (m_ForgetTimers.Count > 0)
                {
                    m_ForgetTimers.Clear();
                }

                return;
            }

            if (m_ForgetTimers.Count == 0)
            {
                return;
            }

            m_ObjectsToForget.Clear();

            foreach (KeyValuePair<GameObject, float> pair in m_ForgetTimers)
            {
                GameObject target = pair.Key;

                // The GameObject may have been destroyed while waiting.
                if (target == null)
                {
                    m_ObjectsToForget.Add(target);
                    continue;
                }

                float newTime = pair.Value + deltaTime;

                if (newTime >= m_TimeToForget)
                {
                    m_ObjectsToForget.Add(target);
                }
                else
                {
                    // Dictionary values cannot be changed while enumerating,
                    // so we'll update it after this loop.
                }
            }

            // Update elapsed times separately.
            //
            // Copying the keys is avoided by using the registered timer keys
            // through the temporary buffer below.
            if (m_ForgetTimers.Count > 0)
            {
                UpdateForgetTimerValues(deltaTime);
            }

            // Invoke after updating/removing so callbacks are free to alter
            // sensing state without corrupting the iteration.
            for (int i = 0; i < m_ObjectsToForget.Count; i++)
            {
                GameObject target = m_ObjectsToForget[i];

                m_ForgetTimers.Remove(target);

                if (target != null)
                {
                    m_OnForget?.Invoke(target);
                }
            }

            m_ObjectsToForget.Clear();
        }

        private void UpdateForgetTimerValues(float deltaTime)
        {
            // We need a temporary copy of the keys because assigning a
            // dictionary value also modifies the dictionary version.
            m_ObjectsToForget.Clear();

            foreach (GameObject target in m_ForgetTimers.Keys)
            {
                if (target != null)
                {
                    m_ObjectsToForget.Add(target);
                }
            }

            for (int i = 0; i < m_ObjectsToForget.Count; i++)
            {
                GameObject target = m_ObjectsToForget[i];

                if (m_ForgetTimers.TryGetValue(target, out float elapsed))
                {
                    m_ForgetTimers[target] = elapsed + deltaTime;
                }
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

            m_ForgetTimers.Clear();
        }

        public void Register(SenseTarget target)
        {
            if (target == null || m_RegisteredTargets.Contains(target))
            {
                return;
            }

            Initialize();

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
            ResetForgetTimer(target.gameObject);
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
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