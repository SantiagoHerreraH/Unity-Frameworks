using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    [Serializable]
    public class Timer
    {
        public enum StartOptions
        {
            DontAutoStart,
            Awake,
            Start,
            OnEnable
        }


        [Title("Auto Calling")]
        [SerializeField]
        private StartOptions m_WhenToAutoStart;

        public StartOptions WhenToAutoStart
        {
            get { return m_WhenToAutoStart; }
        }


        [Title("Timer Data")]
        [OdinSerialize, ShowInInspector]
        private ITimerType m_TimerType;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_MaxTime;


        [Title("Events")]
        [SerializeField]
        private UnityEvent m_OnStart;

        [SerializeField]
        private UnityEvent m_OnEnd;

        [SerializeField]
        private UnityEvent m_OnPause;

        [SerializeField]
        private UnityEvent m_OnPlay;


        [Title("Debug")]
        [ReadOnly]
        private float m_MaxTimeValue = 0;

        [ReadOnly]
        private float m_CurrentTime = 0;

        [ReadOnly]
        private bool m_HasStarted = false;


        private GameObject m_GameObject;


        public float CurrentTime => m_CurrentTime;
        public float MaxTime => m_MaxTimeValue;
        public bool HasStarted => m_HasStarted;


        public Timer()
        {
        }

        public Timer(Timer other)
        {
            m_WhenToAutoStart = other.m_WhenToAutoStart;

            m_CurrentTime = other.m_CurrentTime;
            m_MaxTimeValue = other.m_MaxTimeValue;

            m_MaxTime = other.m_MaxTime?.Clone();
            m_TimerType = other.m_TimerType;

            // UnityEvents are kept as the same serialized event objects.
            m_OnStart = other.m_OnStart;
            m_OnEnd = other.m_OnEnd;
            m_OnPause = other.m_OnPause;
            m_OnPlay = other.m_OnPlay;

            m_HasStarted = other.m_HasStarted;
        }

        public Timer Clone()
        {
            return new Timer(this);
        }


        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError(
                    $"{nameof(Timer)} received a NULL {nameof(GameObject)}.");
                return false;
            }

            if (m_MaxTime == null)
            {
                Debug.LogError(
                    $"{nameof(m_MaxTime)} is NULL in {nameof(Timer)} " +
                    $"in {gameObj.name} gameObj.");

                return false;
            }

            if (m_TimerType == null)
            {
                Debug.LogError(
                    $"{nameof(m_TimerType)} is NULL in {nameof(Timer)} " +
                    $"in {gameObj.name} gameObj.");

                return false;
            }

            m_GameObject = gameObj;

            if (!m_MaxTime.SetGameObject(gameObj))
            {
                Debug.LogError(
                    $"{nameof(m_MaxTime)} could not be set in {nameof(Timer)} " +
                    $"in {gameObj.name} gameObj.");

                return false;
            }

            m_MaxTimeValue = m_MaxTime.CalculateScore();

            return true;
        }


        public bool IsFinished()
        {
            return m_CurrentTime >= m_MaxTimeValue;
        }


        public void ResetTimer()
        {
            m_CurrentTime = 0;
        }


        public bool SetMaxTime(float max)
        {
            if (max <= 0)
            {
                return false;
            }

            m_MaxTimeValue = max;

            return true;
        }


        public void RecalculateMaxTime(bool reset)
        {
            if (m_GameObject == null || m_MaxTime == null)
            {
                return;
            }

            if (m_MaxTime.SetGameObject(m_GameObject))
            {
                m_MaxTimeValue = m_MaxTime.CalculateScore();

                if (reset)
                {
                    ResetTimer();
                }
            }
        }


        /// <summary>
        /// Begins the timer from zero.
        /// </summary>
        public void StartTimer()
        {
            ResetTimer();

            m_HasStarted = true;

            m_OnStart?.Invoke();
        }


        /// <summary>
        /// Marks the timer as paused.
        /// The TimerController is responsible for stopping its updates.
        /// </summary>
        public void Pause()
        {
            if (!m_HasStarted)
            {
                return;
            }

            m_OnPause?.Invoke();
        }


        /// <summary>
        /// Marks a paused timer as playing again.
        /// </summary>
        public void Play()
        {
            if (!m_HasStarted)
            {
                return;
            }

            m_OnPlay?.Invoke();
        }


        /// <summary>
        /// Ends the current timer run.
        /// </summary>
        public void End()
        {
            if (!m_HasStarted)
            {
                return;
            }

            m_HasStarted = false;

            m_OnEnd?.Invoke();
        }


        public void Update()
        {
            if (!m_HasStarted || m_TimerType == null)
            {
                return;
            }

            m_CurrentTime = m_TimerType.IncreaseTime(m_CurrentTime);

            // Prevent CurrentTime from continuing beyond the maximum.
            if (m_CurrentTime > m_MaxTimeValue)
            {
                m_CurrentTime = m_MaxTimeValue;
            }
        }
    }


    public class TimerController : SerializedMonoBehaviour
    {
        [OdinSerialize, ShowInInspector, DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.Foldout, KeyLabel = "Timer Name", ValueLabel = "Timer Data")]
        private Dictionary<TimerName, Timer> m_Timers = null;


        /// <summary>
        /// Active timers always occupy:
        ///
        /// [0, m_ActivatedTimers)
        ///
        /// Inactive timers occupy:
        ///
        /// [m_ActivatedTimers, Count)
        ///
        /// This means Update only needs to iterate through active timers.
        /// </summary>
        private List<Timer> m_TimersList = null;

        private int m_ActivatedTimers;

        private bool m_IsInitialized = false;


        private void Awake()
        {
            Initialize();

            AutoStartTimers(Timer.StartOptions.Awake);
        }


        private void OnEnable()
        {
            Initialize();

            AutoStartTimers(Timer.StartOptions.OnEnable);
        }


        private void Start()
        {
            Initialize();

            AutoStartTimers(Timer.StartOptions.Start);
        }


        private void Initialize()
        {
            if (m_IsInitialized)
            {
                return;
            }

            m_TimersList = new List<Timer>();
            m_ActivatedTimers = 0;

            if (m_Timers != null)
            {
                foreach (KeyValuePair<TimerName, Timer> item in m_Timers)
                {
                    if (item.Value == null)
                    {
                        Debug.LogError(
                            $"{nameof(TimerController)} in {gameObject.name}: " +
                            $"Timer '{item.Key}' is NULL.");

                        continue;
                    }

                    if (!item.Value.SetGameObject(gameObject))
                    {
                        Debug.LogError(
                            $"{nameof(TimerController)} in {gameObject.name}: " +
                            $"Timer '{item.Key}' could not be initialized.");
                    }

                    m_TimersList.Add(item.Value);
                }
            }

            m_IsInitialized = true;
        }


        private void Update()
        {
            if (!m_IsInitialized)
            {
                Initialize();
            }

            int index = 0;

            /*
             * Don't use a normal for loop here.
             *
             * When a timer finishes, DeactivateTimer swaps another active
             * timer into the current index. We therefore need to process
             * that timer before advancing the index.
             */
            while (index < m_ActivatedTimers)
            {
                Timer timer = m_TimersList[index];

                timer.Update();

                if (timer.IsFinished())
                {
                    FinishTimer(timer);

                    // Don't increment index because another active timer
                    // may now occupy this position.
                    continue;
                }

                index++;
            }
        }


        // -------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------

        /// <summary>
        /// Starts/restarts a timer from zero.
        /// </summary>
        public void StartTimer(TimerName timerName)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return;
            }

            timer.StartTimer();
            ActivateTimer(timer);
        }

        public bool IsFinished(TimerName timerName)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return false;
            }

            return timer.IsFinished();
        }


        public void ResetTimer(TimerName timerName)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return;
            }

            timer.ResetTimer();
        }


        public bool SetMaxTime(TimerName timerName, float max)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return false;
            }

            return timer.SetMaxTime(max);
        }


        public void RecalculateMaxTime(TimerName timerName, bool reset)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return;
            }

            timer.RecalculateMaxTime(reset);
        }


        public void Pause(TimerName timerName)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return;
            }

            // Already paused/inactive.
            if (!IsTimerActive(timer))
            {
                return;
            }

            DeactivateTimer(timer);
            timer.Pause();
        }


        public void Play(TimerName timerName)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return;
            }

            // Already playing.
            if (IsTimerActive(timer))
            {
                return;
            }

            /*
             * If this timer has never been started, or it previously
             * completed, Play behaves like StartTimer.
             *
             * Otherwise it is simply resuming from Pause.
             */
            if (!timer.HasStarted || timer.IsFinished())
            {
                timer.StartTimer();
            }
            else
            {
                timer.Play();
            }

            ActivateTimer(timer);
        }


        // -------------------------------------------------------------------
        // Auto Start
        // -------------------------------------------------------------------

        private void AutoStartTimers(Timer.StartOptions startOption)
        {
            if (m_TimersList == null)
            {
                return;
            }

            foreach (Timer timer in m_TimersList)
            {
                if (timer == null)
                {
                    continue;
                }

                if (timer.WhenToAutoStart != startOption)
                {
                    continue;
                }

                timer.StartTimer();
                ActivateTimer(timer);
            }
        }


        // -------------------------------------------------------------------
        // Internal Timer Management
        // -------------------------------------------------------------------

        private void FinishTimer(Timer timer)
        {
            DeactivateTimer(timer);

            timer.End();
        }


        /// <summary>
        /// Moves the timer into the active portion of m_TimersList.
        /// </summary>
        private void ActivateTimer(Timer timer)
        {
            if (timer == null)
            {
                return;
            }

            int index = m_TimersList.IndexOf(timer);

            if (index < 0)
            {
                return;
            }

            // Timer is already active.
            if (index < m_ActivatedTimers)
            {
                return;
            }

            SwapTimers(index, m_ActivatedTimers);

            m_ActivatedTimers++;
        }


        /// <summary>
        /// Removes the timer from the active portion of m_TimersList.
        /// </summary>
        private void DeactivateTimer(Timer timer)
        {
            if (timer == null)
            {
                return;
            }

            int index = m_TimersList.IndexOf(timer);

            if (index < 0)
            {
                return;
            }

            // Timer is already inactive.
            if (index >= m_ActivatedTimers)
            {
                return;
            }

            m_ActivatedTimers--;

            SwapTimers(index, m_ActivatedTimers);
        }
        public bool IsTimerActive(TimerName timerName)
        {
            Initialize();

            if (!TryGetTimer(timerName, out Timer timer))
            {
                return false;
            }

            return IsTimerActive(timer);
        }

        private bool IsTimerActive(Timer timer)
        {
            if (timer == null)
            {
                return false;
            }

            int index = m_TimersList.IndexOf(timer);

            return index >= 0 && index < m_ActivatedTimers;
        }


        private void SwapTimers(int firstIndex, int secondIndex)
        {
            if (firstIndex == secondIndex)
            {
                return;
            }

            Timer temp = m_TimersList[firstIndex];

            m_TimersList[firstIndex] = m_TimersList[secondIndex];
            m_TimersList[secondIndex] = temp;
        }


        // -------------------------------------------------------------------
        // Lookup
        // -------------------------------------------------------------------

        private bool TryGetTimer(TimerName timerName, out Timer timer)
        {
            timer = null;

            if (timerName == null)
            {
                Debug.LogError(
                    $"{nameof(TimerController)} in {gameObject.name}: " +
                    $"{nameof(timerName)} is NULL.");

                return false;
            }

            if (m_Timers == null)
            {
                Debug.LogError(
                    $"{nameof(TimerController)} in {gameObject.name}: " +
                    $"{nameof(m_Timers)} is NULL.");

                return false;
            }

            if (!m_Timers.TryGetValue(timerName, out timer) || timer == null)
            {
                Debug.LogError(
                    $"{nameof(TimerController)} in {gameObject.name}: " +
                    $"Timer '{timerName.name}' was not found.");

                return false;
            }

            return true;
        }
    }
}