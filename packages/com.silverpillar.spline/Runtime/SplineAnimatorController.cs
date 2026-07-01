using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

namespace SilverPillar.Spline
{
    public class SplineAnimatorController : SerializedMonoBehaviour
    {
        private const float NormalizedTimeEpsilon = 0.0001f;

        [Title("References")]
        [SerializeField]
        private SplineAnimate m_SplineAnimate;

        [Title("Animation Parts")]
        [OdinSerialize, ShowInInspector]
        private IntCachedScore m_InHowManyPartsToDivideAnimation;


        [Title("Events")]
        [SerializeField]
        private UnityEvent m_OnStartAnimationPart;

        [SerializeField]
        private UnityEvent m_OnStartAnimation;

        [SerializeField]
        private UnityEvent m_OnEndAnimation;

        [Title("Settings")]
        [SerializeField, Tooltip("If true, the SplineAnimate will be paused on enable, so it only advances when PlayNextPart is called.")]
        private bool m_PauseOnEnable = true;

        [SerializeField, Tooltip("If true, PlayNextPart can restart the animation after it has fully ended.")]
        private bool m_CanRestartAfterEnd;

        [SerializeField, Tooltip("If true, when the spline reaches the end of the currently allowed part, NormalizedTime is clamped exactly to that part end.")]
        private bool m_ClampToPartEnd = true;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        private int m_CurrentPartCount = 1;

        [ShowInInspector, ReadOnly]
        private int m_CurrentAllowedPartIndex = -1;

        [ShowInInspector, ReadOnly]
        private int m_CurrentSplinePartIndex = -1;

        [ShowInInspector, ReadOnly]
        private int m_LastStartedAnimationPartIndex = -1;

        [ShowInInspector, ReadOnly]
        private float m_CurrentAllowedNormalizedEnd;

        [ShowInInspector, ReadOnly]
        private bool m_HasStartedAnimation;

        [ShowInInspector, ReadOnly]
        private bool m_HasEndedAnimation;

        public int CurrentPartCount => m_CurrentPartCount;
        public int CurrentAllowedPartIndex => m_CurrentAllowedPartIndex;
        public int CurrentSplinePartIndex => m_CurrentSplinePartIndex;
        public int LastStartedAnimationPartIndex => m_LastStartedAnimationPartIndex;
        public float CurrentAllowedNormalizedEnd => m_CurrentAllowedNormalizedEnd;
        public bool HasStartedAnimation => m_HasStartedAnimation;
        public bool HasEndedAnimation => m_HasEndedAnimation;

        private void Reset()
        {
            TryGetComponent(out m_SplineAnimate);
        }

        private void Awake()
        {
            ResolveReferences();
            RefreshPartData();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshPartData();

            if (m_PauseOnEnable && m_SplineAnimate != null)
            {
                m_SplineAnimate.Pause();
            }
        }

        private void Update()
        {
            if (m_SplineAnimate == null)
            {
                return;
            }

            RefreshPartData();
            UpdateCurrentSplinePartIndex();

            if (m_CurrentAllowedPartIndex < 0)
            {
                return;
            }

            TryInvokeReachedAnimationPartEvents();

            float normalizedTime = Mathf.Clamp01(m_SplineAnimate.NormalizedTime);

            if (normalizedTime >= m_CurrentAllowedNormalizedEnd - NormalizedTimeEpsilon)
            {
                ReachCurrentAllowedPartEnd();
            }
        }

        [Title("Actions")]
        [Button(ButtonSizes.Medium)]
        public void PlayNextPart()
        {
            ResolveReferences();

            if (m_SplineAnimate == null)
            {
                Debug.LogError($"{nameof(SplineAnimatorController)} on {name} cannot play because no {nameof(SplineAnimate)} is assigned.", this);
                return;
            }

            RefreshPartData();

            if (m_HasEndedAnimation)
            {
                if (!m_CanRestartAfterEnd)
                {
                    return;
                }

                RestartAnimation(false);
            }

            if (!m_HasStartedAnimation)
            {
                StartAnimation();
            }

            if (m_CurrentAllowedPartIndex < m_CurrentPartCount - 1)
            {
                m_CurrentAllowedPartIndex++;
            }

            m_CurrentAllowedNormalizedEnd = GetPartEndNormalizedTime(
                m_CurrentAllowedPartIndex,
                m_CurrentPartCount);

            // Important:
            // This only invokes parts whose start time has actually already been reached.
            // Calling PlayNextPart several times does not invoke future part events early.
            TryInvokeReachedAnimationPartEvents();

            m_SplineAnimate.Play();
        }

        [Button(ButtonSizes.Medium)]
        public void PlayUntilEnd()
        {
            ResolveReferences();

            if (m_SplineAnimate == null)
            {
                return;
            }

            RefreshPartData();

            if (m_HasEndedAnimation)
            {
                if (!m_CanRestartAfterEnd)
                {
                    return;
                }

                RestartAnimation(false);
            }

            if (!m_HasStartedAnimation)
            {
                StartAnimation();
            }

            m_CurrentAllowedPartIndex = m_CurrentPartCount - 1;
            m_CurrentAllowedNormalizedEnd = 1f;

            TryInvokeReachedAnimationPartEvents();

            m_SplineAnimate.Play();
        }

        [Button(ButtonSizes.Medium)]
        public void Pause()
        {
            if (m_SplineAnimate == null)
            {
                return;
            }

            m_SplineAnimate.Pause();
        }

        [Button(ButtonSizes.Medium)]
        public void RestartAnimation()
        {
            RestartAnimation(false);
        }

        public void RestartAnimation(bool autoplayFirstPart)
        {
            ResolveReferences();

            if (m_SplineAnimate == null)
            {
                return;
            }

            RefreshPartData();

            m_CurrentAllowedPartIndex = -1;
            m_CurrentAllowedNormalizedEnd = 0f;
            m_CurrentSplinePartIndex = 0;
            m_LastStartedAnimationPartIndex = -1;

            m_HasStartedAnimation = false;
            m_HasEndedAnimation = false;

            m_SplineAnimate.Restart(false);
            m_SplineAnimate.NormalizedTime = 0f;
            m_SplineAnimate.Pause();

            if (autoplayFirstPart)
            {
                PlayNextPart();
            }
        }

        [Button(ButtonSizes.Medium)]
        public void ResetControllerStateToCurrentSplineTime()
        {
            ResolveReferences();

            if (m_SplineAnimate == null)
            {
                return;
            }

            RefreshPartData();

            float normalizedTime = Mathf.Clamp01(m_SplineAnimate.NormalizedTime);
            int currentPart = GetPartIndexFromNormalizedTime(normalizedTime, m_CurrentPartCount);

            m_CurrentSplinePartIndex = currentPart;
            m_CurrentAllowedPartIndex = currentPart;
            m_CurrentAllowedNormalizedEnd = GetPartEndNormalizedTime(currentPart, m_CurrentPartCount);

            // Set it to currentPart - 1 so the current part event can be invoked
            // if the current time is already inside that part.
            m_LastStartedAnimationPartIndex = currentPart - 1;

            m_HasStartedAnimation = normalizedTime > NormalizedTimeEpsilon;
            m_HasEndedAnimation = normalizedTime >= 1f - NormalizedTimeEpsilon;

            TryInvokeReachedAnimationPartEvents();

            if (m_HasEndedAnimation)
            {
                EndAnimation();
            }
        }

        private void StartAnimation()
        {
            if (m_HasStartedAnimation)
            {
                return;
            }

            m_HasStartedAnimation = true;
            m_HasEndedAnimation = false;

            m_OnStartAnimation?.Invoke();
        }

        private void ReachCurrentAllowedPartEnd()
        {
            if (m_ClampToPartEnd)
            {
                m_SplineAnimate.NormalizedTime = m_CurrentAllowedNormalizedEnd;
            }

            m_SplineAnimate.Pause();

            UpdateCurrentSplinePartIndex();

            if (m_CurrentAllowedPartIndex >= m_CurrentPartCount - 1)
            {
                EndAnimation();
            }
        }

        private void EndAnimation()
        {
            if (m_HasEndedAnimation)
            {
                return;
            }

            m_HasEndedAnimation = true;
            m_OnEndAnimation?.Invoke();
        }

        private void TryInvokeReachedAnimationPartEvents()
        {
            if (m_SplineAnimate == null)
            {
                return;
            }

            if (m_CurrentAllowedPartIndex < 0)
            {
                return;
            }

            float normalizedTime = Mathf.Clamp01(m_SplineAnimate.NormalizedTime);

            int reachedPartIndex = GetPartIndexFromNormalizedTime(
                normalizedTime,
                m_CurrentPartCount);

            int highestPartThatCanBeInvoked = Mathf.Min(
                reachedPartIndex,
                m_CurrentAllowedPartIndex);

            if (highestPartThatCanBeInvoked <= m_LastStartedAnimationPartIndex)
            {
                return;
            }

            for (int partIndex = m_LastStartedAnimationPartIndex + 1;
                 partIndex <= highestPartThatCanBeInvoked;
                 partIndex++)
            {
                InvokeStartAnimationPart(partIndex);
            }
        }

        private void InvokeStartAnimationPart(int partIndex)
        {
            m_LastStartedAnimationPartIndex = partIndex;
            m_CurrentSplinePartIndex = partIndex;

            m_OnStartAnimationPart?.Invoke();
        }

        private void ResolveReferences()
        {
            if (m_SplineAnimate == null)
            {
                TryGetComponent(out m_SplineAnimate);
            }
        }

        private void RefreshPartData()
        {
            m_CurrentPartCount = GetAnimationPartCount();

            if (m_CurrentAllowedPartIndex >= m_CurrentPartCount)
            {
                m_CurrentAllowedPartIndex = m_CurrentPartCount - 1;
            }

            if (m_CurrentAllowedPartIndex >= 0)
            {
                m_CurrentAllowedNormalizedEnd = GetPartEndNormalizedTime(
                    m_CurrentAllowedPartIndex,
                    m_CurrentPartCount);
            }
        }

        private int GetAnimationPartCount()
        {
            if (m_InHowManyPartsToDivideAnimation == null)
            {
                return 1;
            }

            if (m_InHowManyPartsToDivideAnimation.GetGameObject() == null)
            {
                m_InHowManyPartsToDivideAnimation.SetGameObject(gameObject);
            }

            return Mathf.Max(1, m_InHowManyPartsToDivideAnimation.CalculateScoreAsInt());
        }

        private void UpdateCurrentSplinePartIndex()
        {
            if (m_SplineAnimate == null)
            {
                m_CurrentSplinePartIndex = -1;
                return;
            }

            float normalizedTime = Mathf.Clamp01(m_SplineAnimate.NormalizedTime);

            m_CurrentSplinePartIndex = GetPartIndexFromNormalizedTime(
                normalizedTime,
                m_CurrentPartCount);
        }

        private static float GetPartEndNormalizedTime(int partIndex, int partCount)
        {
            partCount = Mathf.Max(1, partCount);
            partIndex = Mathf.Clamp(partIndex, 0, partCount - 1);

            return (partIndex + 1) / (float)partCount;
        }

        private static int GetPartIndexFromNormalizedTime(float normalizedTime, int partCount)
        {
            partCount = Mathf.Max(1, partCount);
            normalizedTime = Mathf.Clamp01(normalizedTime);

            if (normalizedTime >= 1f - NormalizedTimeEpsilon)
            {
                return partCount - 1;
            }

            return Mathf.Clamp(
                Mathf.FloorToInt(normalizedTime * partCount),
                0,
                partCount - 1);
        }
    }
}