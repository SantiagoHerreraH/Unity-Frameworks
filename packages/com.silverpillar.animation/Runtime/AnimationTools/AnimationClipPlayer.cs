using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace SilverPillar.Animation
{
    public enum AnimationSequenceType
    {
        Stop,
        Loop,
        ForwardThenBackward,
        Random
    }

    [Serializable]
    public class AnimationClipData
    {
        public enum TimeMode { Scaled, Unscaled, Fixed, UnscaledFixed }

        [BoxGroup("Clip Settings")]
        public AnimationClip Clip;

        [BoxGroup("Clip Settings"), Min(0)]
        public float TransitionTime = 0.2f;

        [BoxGroup("Clip Settings"), Min(0)]
        public float ClipSpeed = 1f;

        [BoxGroup("Clip Settings"), Min(0)]
        public float Delay = 0f;

        [BoxGroup("Clip Settings")]
        public TimeMode UpdateMode = TimeMode.Scaled;

        [BoxGroup("Clip Settings"), Tooltip("Negative number is infinite. 1 == played once")]
        public int LoopCount = 1;

        [BoxGroup("Clip Settings")]
        public bool PauseOnLastFrame = false;

        [BoxGroup("Clip Settings")]
        [Tooltip("If another clip tries to play on a non interruptable clip, it will be queued. Non interruptables will always take the queue space of an interruptable.")]
        public bool IsInterruptable = true;

        [BoxGroup("Actions"), OdinSerialize, ShowInInspector]
        public IAction Action;

        [BoxGroup("Events")]
        public UnityEvent OnAnimationStart;

        [BoxGroup("Events")]
        public UnityEvent OnAnimationEnd;

        [BoxGroup("Debug")]
        public bool PrintOnStart;

        [BoxGroup("Debug")]
        public bool PrintOnEnd;

        public void PrintStart(GameObject owner)
        {
            if (!PrintOnStart) return;

            Debug.Log(
                $"[Animation START] {(Clip != null ? Clip.name : "NULL CLIP")} | " +
                $"GO: {(owner != null ? owner.name : "NULL")} | " +
                $"Speed: {ClipSpeed} | LoopCount: {LoopCount}",
                owner
            );
        }

        public void PrintEnd(GameObject owner)
        {
            if (!PrintOnEnd) return;

            Debug.Log(
                $"[Animation END] {(Clip != null ? Clip.name : "NULL CLIP")} | " +
                $"GO: {(owner != null ? owner.name : "NULL")}",
                owner
            );
        }
    }

    [RequireComponent(typeof(Animator))]
    public class AnimationClipPlayer : MonoBehaviour
    {
        private const int AnimatorInput = 0;
        private const int ClipInputA = 1;
        private const int ClipInputB = 2;

        private PlayableGraph m_Graph;
        private AnimationLayerMixerPlayable m_Mixer;
        private AnimatorControllerPlayable m_AnimatorControllerPlayable;
        private Animator m_Animator;
        private bool m_Initialized;

        private Coroutine m_CurrentAnimationRoutine;

        private readonly AnimationClipPlayable[] m_ClipPlayables = new AnimationClipPlayable[2];
        private int m_ActiveClipSlot = -1;

        private AnimationClipPlayable m_CurrentClipPlayable;
        private WaitForFixedUpdate m_WaitForFixedUpdate;

        private AnimationClipData m_CurrentAnimationData;
        private AnimationClipData m_QueuedAnimationData;
        private IAction m_CurrentAction;

        private void Awake()
        {
            m_WaitForFixedUpdate = new WaitForFixedUpdate();
            Initialize();
        }

        public void Pause()
        {
            if (m_Graph.IsValid())
                m_Graph.Stop();
        }

        public void Resume()
        {
            if (m_Graph.IsValid())
                m_Graph.Play();
        }

        public bool IsPaused()
        {
            return m_Graph.IsValid() && !m_Graph.IsPlaying();
        }

        public void SetCurrentClipSpeed(float speed)
        {
            if (m_CurrentClipPlayable.IsValid())
                m_CurrentClipPlayable.SetSpeed(speed);
        }

        public float GetCurrentClipSpeed()
        {
            return m_CurrentClipPlayable.IsValid()
                ? (float)m_CurrentClipPlayable.GetSpeed()
                : 1f;
        }

        public void PlayClip(AnimationClip clip, float transitionTime, int loopCount)
        {
            PlayClip(new AnimationClipData
            {
                Clip = clip,
                TransitionTime = transitionTime,
                LoopCount = loopCount
            });
        }

        public void PlayClip(AnimationClipData data)
        {
            if (data == null || data.Clip == null)
                return;

            if (m_CurrentAnimationRoutine != null &&
                m_CurrentAnimationData != null &&
                !m_CurrentAnimationData.IsInterruptable)
            {
                QueueAnimation(data);
                return;
            }

            bool isPausedOnLastFrame =
                m_CurrentAnimationRoutine == null &&
                m_CurrentAnimationData != null &&
                m_CurrentAnimationData.PauseOnLastFrame &&
                m_ActiveClipSlot >= 0 &&
                IsSlotValid(m_ActiveClipSlot);

            bool canBlendFromCurrent =
                (m_CurrentAnimationRoutine != null || isPausedOnLastFrame) &&
                m_ActiveClipSlot >= 0 &&
                IsSlotValid(m_ActiveClipSlot);

            if (canBlendFromCurrent)
            {
                Coroutine routineToStop = m_CurrentAnimationRoutine;
                m_CurrentAnimationRoutine = StartCoroutine(BlendFromCurrentClipToNewClip(data, routineToStop));
            }
            else
            {
                StopCurrentAnimationRoutine();
                DestroyAllClipInputs();
                PlayAnimationImmediately(data);
            }
        }

        public void CancelCurrentClip(float transitionTime, bool cancelQueued)
        {

            if (cancelQueued)
            {
                CancelCurrentQueuedClip();
            }

            if (m_CurrentAnimationData == null)
                return;

            if (m_ActiveClipSlot < 0 || !IsSlotValid(m_ActiveClipSlot))
                return;

            Coroutine routineToStop = m_CurrentAnimationRoutine;
            m_CurrentAnimationRoutine = StartCoroutine(FadeCurrentClipToAnimator(transitionTime, routineToStop));
        }

        public void CancelCurrentQueuedClip()
        {
            if (m_QueuedAnimationData == null)
                return;

            m_QueuedAnimationData = null;
        }


        public void CancelClip(AnimationClip clip, float transitionTime, bool cancelQueued)
        {
            if (clip == null)
                return;

            if (cancelQueued)
            {
                CancelQueuedClip(clip);
            }

            if (m_CurrentAnimationData == null || m_CurrentAnimationData.Clip != clip)
                return;

            if (m_ActiveClipSlot < 0 || !IsSlotValid(m_ActiveClipSlot))
                return;

            Coroutine routineToStop = m_CurrentAnimationRoutine;
            m_CurrentAnimationRoutine = StartCoroutine(FadeCurrentClipToAnimator(transitionTime, routineToStop));
        }

        public void CancelQueuedClip(AnimationClip clip)
        {
            if (clip == null || m_QueuedAnimationData == null)
                return;

            if (m_QueuedAnimationData.Clip == clip)
                m_QueuedAnimationData = null;
        }

        private void QueueAnimation(AnimationClipData data)
        {
            if (m_QueuedAnimationData == null)
            {
                m_QueuedAnimationData = data;
                return;
            }

            if (!data.IsInterruptable || m_QueuedAnimationData.IsInterruptable)
                m_QueuedAnimationData = data;
        }

        private void PlayAnimationImmediately(AnimationClipData data)
        {
            m_CurrentAnimationData = data;
            m_CurrentAnimationRoutine = StartCoroutine(ExecuteAnimationData(data));
        }

        public void PlaySequence(List<AnimationClipData> sequence, AnimationSequenceType type, int loopCount)
        {
            if (sequence == null || sequence.Count == 0)
                return;

            StopCurrentAnimationRoutine();
            DestroyAllClipInputs();

            m_CurrentAnimationData = null;
            m_CurrentAction = null;
            m_QueuedAnimationData = null;

            m_CurrentAnimationRoutine = StartCoroutine(ExecuteSequenceRoutine(sequence, type, loopCount));
        }

        private void StopCurrentAnimationRoutine()
        {
            if (m_CurrentAnimationRoutine != null)
            {
                StopCoroutine(m_CurrentAnimationRoutine);
                m_CurrentAnimationRoutine = null;
            }
        }

        private IEnumerator ExecuteAnimationData(AnimationClipData data)
        {
            if (data == null || data.Clip == null)
                yield break;

            Initialize();

            yield return WaitDelay(data);

            IAction action = StartClipOnSlot(data, 0, 0f);

            int currentLoop = 0;
            float transitionTime = Mathf.Max(0f, data.TransitionTime);
            float clipDuration = Mathf.Max(0.001f, data.Clip.length);

            while (data.LoopCount < 0 || currentLoop < data.LoopCount)
            {
                float elapsed = 0f;

                if (currentLoop > 0 && m_CurrentClipPlayable.IsValid())
                    m_CurrentClipPlayable.SetTime(0);

                while (elapsed < clipDuration)
                {
                    if (!m_CurrentClipPlayable.IsValid())
                        yield break;

                    float dt = GetDeltaTime(data.UpdateMode);
                    float logicDelta = dt * Mathf.Max(0f, data.ClipSpeed);

                    float weight = 1f;

                    if (transitionTime > 0f && currentLoop == 0 && elapsed < transitionTime)
                    {
                        weight = elapsed / transitionTime;
                    }
                    else if (transitionTime > 0f &&
                             !data.PauseOnLastFrame &&
                             data.LoopCount > 0 &&
                             currentLoop == data.LoopCount - 1 &&
                             elapsed > clipDuration - transitionTime)
                    {
                        weight = (clipDuration - elapsed) / transitionTime;
                    }

                    SetAnimatorToClipWeights(0, Mathf.Clamp01(weight));

                    action?.UpdateAction();

                    elapsed += logicDelta;

                    yield return ShouldUseFixedUpdate(data.UpdateMode)
                        ? m_WaitForFixedUpdate
                        : null;
                }

                currentLoop++;
            }

            if (data.PauseOnLastFrame)
            {
                SetAnimatorToClipWeights(0, 1f);

                if (m_CurrentClipPlayable.IsValid())
                {
                    float freezeTime = Mathf.Max(0f, data.Clip.length - 0.001f);
                    m_CurrentClipPlayable.SetTime(freezeTime);
                    m_CurrentClipPlayable.SetSpeed(0f);
                }

                action?.EndAction();
                data.OnAnimationEnd?.Invoke();
                data.PrintEnd(gameObject);

                if (m_QueuedAnimationData != null)
                {
                    CompleteAnimationAndPlayQueued();
                }
            }
            else
            {
                FinishAnimation(action, data);
            }
        }

        private IEnumerator ExecuteSequenceRoutine(List<AnimationClipData> sequence, AnimationSequenceType type, int loopCount)
        {
            int totalSequenceLoops = 0;
            bool isInfiniteSequence = loopCount < 0;

            AnimationClipData currentData = null;
            IAction currentAction = null;
            int currentSlot = 0;
            float currentElapsed = 0f;
            int currentClipCompletedLoops = 0;
            int lastRandomIndex = -1;

            while (isInfiniteSequence || totalSequenceLoops < loopCount)
            {
                List<int> order = BuildSequenceOrder(sequence, type, ref lastRandomIndex);

                for (int orderIndex = 0; orderIndex < order.Count; orderIndex++)
                {
                    bool isLastClipInPass = orderIndex == order.Count - 1;
                    bool isLastPass = !isInfiniteSequence && totalSequenceLoops >= loopCount - 1;
                    bool isFinalClipInSequence = isLastClipInPass && isLastPass;

                    if (type == AnimationSequenceType.Stop)
                        isFinalClipInSequence = isLastClipInPass;

                    if (currentData == null)
                    {
                        currentData = sequence[order[orderIndex]];
                        yield return WaitDelay(currentData);

                        currentAction = StartClipOnSlot(currentData, currentSlot, 0f);
                        currentElapsed = 0f;
                        currentClipCompletedLoops = 0;

                        yield return FadeFromAnimatorIntoClip(
                            currentData,
                            currentAction,
                            currentSlot,
                            value => currentElapsed = value
                        );
                    }

                    float currentDuration = Mathf.Max(0.001f, currentData.Clip.length);
                    float currentTransition = Mathf.Max(0f, currentData.TransitionTime);

                    while (ShouldRepeatCurrentClip(currentData, currentClipCompletedLoops))
                    {
                        yield return PlayClipLoopUntilEnd(currentData, currentAction, currentSlot, currentElapsed);

                        if (!IsSlotValid(currentSlot))
                            yield break;

                        currentClipCompletedLoops++;
                        currentElapsed = 0f;

                        m_ClipPlayables[currentSlot].SetTime(0);
                        m_ClipPlayables[currentSlot].SetSpeed(currentData.ClipSpeed);
                        SetOnlyClipWeight(currentSlot, 1f);
                    }

                    if (isFinalClipInSequence)
                    {
                        yield return PlayClipUntilFinalFade(currentData, currentAction, currentSlot, currentElapsed);

                        currentAction?.EndAction();
                        currentData.OnAnimationEnd?.Invoke();
                        currentData.PrintEnd(gameObject);

                        if (currentData.PauseOnLastFrame)
                        {
                            SetAnimatorToClipWeights(currentSlot, 1f);

                            if (IsSlotValid(currentSlot))
                            {
                                float freezeTime = Mathf.Max(0f, currentData.Clip.length - 0.001f);
                                m_ClipPlayables[currentSlot].SetTime(freezeTime);
                                m_ClipPlayables[currentSlot].SetSpeed(0f);
                            }

                            m_CurrentAnimationRoutine = null;
                            m_CurrentAnimationData = currentData;
                            m_CurrentAction = currentAction;
                            yield break;
                        }

                        DestroySlot(currentSlot);
                        SetAnimatorOnly();

                        currentData = null;
                        currentAction = null;
                        currentElapsed = 0f;
                        currentClipCompletedLoops = 0;
                        break;
                    }
                    if (currentData.PauseOnLastFrame)
                    {
                        yield return PlayClipUntilFinalFade(currentData, currentAction, currentSlot, currentElapsed);

                        SetOnlyClipWeight(currentSlot, 1f);

                        if (IsSlotValid(currentSlot))
                        {
                            float freezeTime = Mathf.Max(0f, currentData.Clip.length - 0.001f);
                            m_ClipPlayables[currentSlot].SetTime(freezeTime);
                            m_ClipPlayables[currentSlot].SetSpeed(0f);
                        }

                        currentAction?.EndAction();
                        currentData.OnAnimationEnd?.Invoke();
                        currentData.PrintEnd(gameObject);

                        m_CurrentAnimationRoutine = null;
                        m_CurrentAnimationData = currentData;
                        m_CurrentAction = currentAction;
                        yield break;
                    }

                    int nextOrderIndex = orderIndex + 1;

                    if (nextOrderIndex >= order.Count)
                        nextOrderIndex = 0;

                    AnimationClipData nextData = sequence[order[nextOrderIndex]];

                    float blendStartTime = currentTransition <= 0f
                        ? currentDuration
                        : Mathf.Max(0f, currentDuration - currentTransition);

                    while (currentElapsed < blendStartTime)
                    {
                        if (!IsSlotValid(currentSlot))
                            yield break;

                        float dt = GetDeltaTime(currentData.UpdateMode);
                        float logicDelta = dt * Mathf.Max(0f, currentData.ClipSpeed);

                        SetOnlyClipWeight(currentSlot, 1f);
                        currentAction?.UpdateAction();

                        currentElapsed += logicDelta;

                        yield return ShouldUseFixedUpdate(currentData.UpdateMode)
                            ? m_WaitForFixedUpdate
                            : null;
                    }

                    SetOnlyClipWeight(currentSlot, 1f);

                    yield return WaitDelay(nextData);

                    if (!IsSlotValid(currentSlot))
                        yield break;

                    int nextSlot = currentSlot == 0 ? 1 : 0;
                    IAction nextAction = StartClipOnSlot(nextData, nextSlot, 0f);
                    float nextElapsed = 0f;

                    float blendDuration = Mathf.Min(currentTransition, currentDuration);

                    if (blendDuration <= 0f)
                    {
                        SetClipToClipWeights(currentSlot, nextSlot, 1f);

                        if (IsSlotValid(nextSlot))
                        {
                            m_ClipPlayables[nextSlot].SetTime(0f);
                            m_ClipPlayables[nextSlot].SetSpeed(nextData.ClipSpeed);
                        }

                        if (m_Graph.IsValid())
                            m_Graph.Evaluate(0f);

                        currentAction?.EndAction();
                        currentData.OnAnimationEnd?.Invoke();
                        currentData.PrintEnd(gameObject);

                        DestroySlot(currentSlot);

                        currentData = nextData;
                        currentAction = nextAction;
                        currentSlot = nextSlot;
                        currentElapsed = nextElapsed;
                        currentClipCompletedLoops = 0;

                        m_ActiveClipSlot = currentSlot;
                        m_CurrentClipPlayable = m_ClipPlayables[currentSlot];
                        m_CurrentAnimationData = currentData;
                        m_CurrentAction = currentAction;

                        if (nextOrderIndex == 0)
                            break;

                        continue;
                    }

                    float blendElapsed = 0f;
                    bool frozeOutgoingClip = false;

                    while (blendElapsed < blendDuration)
                    {
                        if (!IsSlotValid(currentSlot) || !IsSlotValid(nextSlot))
                            yield break;

                        if (!frozeOutgoingClip)
                        {
                            m_ClipPlayables[currentSlot].SetSpeed(0f);
                            frozeOutgoingClip = true;
                        }

                        float nextDt = GetDeltaTime(nextData.UpdateMode);
                        float nextLogicDelta = nextDt * Mathf.Max(0f, nextData.ClipSpeed);

                        float t = Mathf.Clamp01(blendElapsed / blendDuration);

                        SetClipToClipWeights(currentSlot, nextSlot, t);

                        currentAction?.UpdateAction();
                        nextAction?.UpdateAction();

                        nextElapsed += nextLogicDelta;
                        blendElapsed += nextDt;

                        yield return ShouldUseFixedUpdate(nextData.UpdateMode)
                            ? m_WaitForFixedUpdate
                            : null;
                    }

                    SetClipToClipWeights(currentSlot, nextSlot, 1f);

                    currentAction?.EndAction();
                    currentData.OnAnimationEnd?.Invoke();
                    currentData.PrintEnd(gameObject);

                    DestroySlot(currentSlot);

                    currentData = nextData;
                    currentAction = nextAction;
                    currentSlot = nextSlot;
                    currentElapsed = nextElapsed;
                    currentClipCompletedLoops = 0;

                    m_ActiveClipSlot = currentSlot;
                    m_CurrentClipPlayable = m_ClipPlayables[currentSlot];
                    m_CurrentAnimationData = currentData;
                    m_CurrentAction = currentAction;

                    if (nextOrderIndex == 0)
                        break;
                }

                totalSequenceLoops++;

                if (type == AnimationSequenceType.Stop)
                    break;
            }

            m_CurrentAnimationRoutine = null;
            m_CurrentAnimationData = null;
            m_CurrentAction = null;

            CompleteAnimationAndPlayQueued();
        }

        private bool ShouldRepeatCurrentClip(AnimationClipData data, int completedLoops)
        {
            if (data == null)
                return false;

            if (data.LoopCount < 0)
                return true;

            int targetLoops = Mathf.Max(1, data.LoopCount);

            return completedLoops < targetLoops - 1;
        }

        private IEnumerator PlayClipLoopUntilEnd(AnimationClipData data, IAction action, int slot, float elapsed)
        {
            float duration = Mathf.Max(0.001f, data.Clip.length);

            while (elapsed < duration)
            {
                if (!IsSlotValid(slot))
                    yield break;

                float dt = GetDeltaTime(data.UpdateMode);
                float logicDelta = dt * Mathf.Max(0f, data.ClipSpeed);

                SetOnlyClipWeight(slot, 1f);
                action?.UpdateAction();

                elapsed += logicDelta;

                yield return ShouldUseFixedUpdate(data.UpdateMode)
                    ? m_WaitForFixedUpdate
                    : null;
            }
        }

        private IEnumerator BlendFromCurrentClipToNewClip(AnimationClipData newData, Coroutine routineToStop)
        {
            if (newData == null || newData.Clip == null)
                yield break;

            Initialize();

            int fromSlot = m_ActiveClipSlot;
            int toSlot = fromSlot == 0 ? 1 : 0;

            IAction previousAction = m_CurrentAction;
            AnimationClipData previousData = m_CurrentAnimationData;

            if (!IsSlotValid(fromSlot))
            {
                if (routineToStop != null)
                    StopCoroutine(routineToStop);

                DestroyAllClipInputs();
                PlayAnimationImmediately(newData);
                yield break;
            }

            SetOnlyClipWeight(fromSlot, 1f);

            if (routineToStop != null)
                StopCoroutine(routineToStop);

            yield return WaitDelay(newData);

            if (!IsSlotValid(fromSlot))
                yield break;

            SetOnlyClipWeight(fromSlot, 1f);

            IAction newAction = StartClipOnSlot(newData, toSlot, 0f);

            float blendDuration = Mathf.Max(0f, newData.TransitionTime);

            if (blendDuration <= 0f)
            {
                SetClipToClipWeights(fromSlot, toSlot, 1f);

                if (m_Graph.IsValid())
                    m_Graph.Evaluate(0f);

                previousAction?.EndAction();
                previousData?.OnAnimationEnd?.Invoke();
                previousData?.PrintEnd(gameObject);

                DestroySlot(fromSlot);

                m_ActiveClipSlot = toSlot;
                m_CurrentClipPlayable = m_ClipPlayables[toSlot];
                m_CurrentAnimationData = newData;
                m_CurrentAction = newAction;

                yield return ContinueSingleClipAfterInterrupt(newData, newAction, toSlot);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < blendDuration)
            {
                if (!IsSlotValid(fromSlot) || !IsSlotValid(toSlot))
                    yield break;

                float dt = GetDeltaTime(newData.UpdateMode);
                float t = Mathf.Clamp01(elapsed / blendDuration);

                SetClipToClipWeights(fromSlot, toSlot, t);

                previousAction?.UpdateAction();
                newAction?.UpdateAction();

                elapsed += dt;

                yield return ShouldUseFixedUpdate(newData.UpdateMode)
                    ? m_WaitForFixedUpdate
                    : null;
            }

            SetClipToClipWeights(fromSlot, toSlot, 1f);

            previousAction?.EndAction();
            previousData?.OnAnimationEnd?.Invoke();
            previousData?.PrintEnd(gameObject);

            DestroySlot(fromSlot);

            m_ActiveClipSlot = toSlot;
            m_CurrentClipPlayable = m_ClipPlayables[toSlot];
            m_CurrentAnimationData = newData;
            m_CurrentAction = newAction;

            yield return ContinueSingleClipAfterInterrupt(newData, newAction, toSlot);

            m_CurrentAnimationRoutine = null;
        }

        private IEnumerator ContinueSingleClipAfterInterrupt(AnimationClipData data, IAction action, int slot)
        {
            float clipDuration = Mathf.Max(0.001f, data.Clip.length);
            float transitionTime = Mathf.Max(0f, data.TransitionTime);

            int currentLoop = 0;

            while (data.LoopCount < 0 || currentLoop < data.LoopCount)
            {
                if (!IsSlotValid(slot))
                    yield break;

                float elapsed = 0f;

                if (currentLoop > 0)
                    m_ClipPlayables[slot].SetTime(0);

                while (elapsed < clipDuration)
                {
                    if (!IsSlotValid(slot))
                        yield break;

                    float dt = GetDeltaTime(data.UpdateMode);
                    float logicDelta = dt * Mathf.Max(0f, data.ClipSpeed);

                    float weight = 1f;

                    if (transitionTime > 0f &&
                        !data.PauseOnLastFrame &&
                        data.LoopCount > 0 &&
                        currentLoop == data.LoopCount - 1 &&
                        elapsed > clipDuration - transitionTime)
                    {
                        weight = (clipDuration - elapsed) / transitionTime;
                    }

                    SetAnimatorToClipWeights(slot, Mathf.Clamp01(weight));

                    action?.UpdateAction();

                    elapsed += logicDelta;

                    yield return ShouldUseFixedUpdate(data.UpdateMode)
                        ? m_WaitForFixedUpdate
                        : null;
                }

                currentLoop++;
            }

            if (data.PauseOnLastFrame)
            {
                SetAnimatorToClipWeights(slot, 1f);

                if (IsSlotValid(slot))
                {
                    float freezeTime = Mathf.Max(0f, data.Clip.length - 0.001f);
                    m_ClipPlayables[slot].SetTime(freezeTime);
                    m_ClipPlayables[slot].SetSpeed(0f);
                }

                action?.EndAction();
                data.OnAnimationEnd?.Invoke();
                data.PrintEnd(gameObject);

                if (m_QueuedAnimationData != null)
                {
                    CompleteAnimationAndPlayQueued();
                }
            }
            else
            {
                FinishAnimation(action, data);
            }
        }

        private IEnumerator FadeCurrentClipToAnimator(float transitionTime, Coroutine routineToStop)
        {
            int slot = m_ActiveClipSlot;

            IAction action = m_CurrentAction;
            AnimationClipData data = m_CurrentAnimationData;

            if (!IsSlotValid(slot))
                yield break;

            SetOnlyClipWeight(slot, 1f);

            if (routineToStop != null)
                StopCoroutine(routineToStop);

            float duration = Mathf.Max(0f, transitionTime);

            if (duration <= 0f)
            {
                SetAnimatorOnly();

                action?.EndAction();
                data?.OnAnimationEnd?.Invoke();
                data?.PrintEnd(gameObject);

                DestroySlot(slot);

                m_CurrentAnimationRoutine = null;
                m_CurrentAnimationData = null;
                m_CurrentAction = null;

                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!IsSlotValid(slot))
                    yield break;

                float dt = Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                SetAnimatorToClipWeights(slot, 1f - t);

                action?.UpdateAction();

                elapsed += dt;
                yield return null;
            }

            SetAnimatorOnly();

            action?.EndAction();
            data?.OnAnimationEnd?.Invoke();
            data?.PrintEnd(gameObject);

            DestroySlot(slot);

            m_CurrentAnimationRoutine = null;
            m_CurrentAnimationData = null;
            m_CurrentAction = null;
        }

        private IEnumerator FadeFromAnimatorIntoClip(
            AnimationClipData data,
            IAction action,
            int slot,
            Action<float> setElapsed)
        {
            float elapsed = 0f;
            float transitionTime = Mathf.Max(0f, data.TransitionTime);

            if (transitionTime <= 0f)
            {
                SetAnimatorToClipWeights(slot, 1f);
                yield break;
            }

            while (elapsed < transitionTime)
            {
                if (!IsSlotValid(slot))
                    yield break;

                float dt = GetDeltaTime(data.UpdateMode);
                float logicDelta = dt * Mathf.Max(0f, data.ClipSpeed);

                float weight = Mathf.Clamp01(elapsed / transitionTime);
                SetAnimatorToClipWeights(slot, weight);

                action?.UpdateAction();

                elapsed += logicDelta;
                setElapsed?.Invoke(elapsed);

                yield return ShouldUseFixedUpdate(data.UpdateMode)
                    ? m_WaitForFixedUpdate
                    : null;
            }

            SetAnimatorToClipWeights(slot, 1f);
        }

        private IEnumerator PlayClipUntilFinalFade(AnimationClipData data, IAction action, int slot, float elapsed)
        {
            float duration = Mathf.Max(0.001f, data.Clip.length);
            float transition = Mathf.Max(0f, data.TransitionTime);

            while (elapsed < duration)
            {
                if (!IsSlotValid(slot))
                    yield break;

                float dt = GetDeltaTime(data.UpdateMode);
                float logicDelta = dt * Mathf.Max(0f, data.ClipSpeed);

                float weight = 1f;

                if (transition > 0f &&
                    !data.PauseOnLastFrame &&
                    elapsed > duration - transition)
                {
                    weight = (duration - elapsed) / transition;
                }

                SetAnimatorToClipWeights(slot, Mathf.Clamp01(weight));

                action?.UpdateAction();

                elapsed += logicDelta;

                yield return ShouldUseFixedUpdate(data.UpdateMode)
                    ? m_WaitForFixedUpdate
                    : null;
            }
        }

        private IAction StartClipOnSlot(AnimationClipData data, int slot, float startTime)
        {
            Initialize();

            m_Graph.SetTimeUpdateMode(
                data.UpdateMode is AnimationClipData.TimeMode.Unscaled or AnimationClipData.TimeMode.UnscaledFixed
                    ? DirectorUpdateMode.UnscaledGameTime
                    : DirectorUpdateMode.GameTime
            );

            DestroySlot(slot);

            m_ClipPlayables[slot] = AnimationClipPlayable.Create(m_Graph, data.Clip);
            m_ClipPlayables[slot].SetSpeed(data.ClipSpeed);
            m_ClipPlayables[slot].SetTime(startTime);

            int input = GetClipInput(slot);
            m_Graph.Connect(m_ClipPlayables[slot], 0, m_Mixer, input);
            m_Mixer.SetInputWeight(input, 0f);

            m_ActiveClipSlot = slot;
            m_CurrentClipPlayable = m_ClipPlayables[slot];
            m_CurrentAnimationData = data;

            IAction action = data.Action?.Clone();
            action?.SetGameObject(gameObject);

            data.OnAnimationStart?.Invoke();
            action?.StartAction();
            data.PrintStart(gameObject);

            m_CurrentAction = action;

            return action;
        }

        private List<int> BuildSequenceOrder(List<AnimationClipData> sequence, AnimationSequenceType type, ref int lastRandomIndex)
        {
            List<int> order = new();

            switch (type)
            {
                case AnimationSequenceType.Random:
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        int randomIndex = lastRandomIndex;

                        if (sequence.Count > 1)
                        {
                            while (randomIndex == lastRandomIndex)
                                randomIndex = UnityEngine.Random.Range(0, sequence.Count);
                        }
                        else
                        {
                            randomIndex = 0;
                        }

                        lastRandomIndex = randomIndex;
                        order.Add(randomIndex);
                    }

                    break;

                case AnimationSequenceType.ForwardThenBackward:
                    for (int i = 0; i < sequence.Count; i++)
                        order.Add(i);

                    for (int i = sequence.Count - 2; i >= 1; i--)
                        order.Add(i);

                    break;

                case AnimationSequenceType.Loop:
                case AnimationSequenceType.Stop:
                    for (int i = 0; i < sequence.Count; i++)
                        order.Add(i);

                    break;
            }

            return order;
        }

        private IEnumerator WaitDelay(AnimationClipData data)
        {
            float delayRemaining = data.Delay;

            while (delayRemaining > 0f)
            {
                delayRemaining -= GetDeltaTime(data.UpdateMode);

                yield return ShouldUseFixedUpdate(data.UpdateMode)
                    ? m_WaitForFixedUpdate
                    : null;
            }
        }

        private void FinishAnimation(IAction action, AnimationClipData data)
        {
            SetAnimatorOnly();

            action?.EndAction();
            data.OnAnimationEnd?.Invoke();
            data.PrintEnd(gameObject);

            DestroyAllClipInputs();

            CompleteAnimationAndPlayQueued();
        }

        private void CompleteAnimationAndPlayQueued()
        {
            m_CurrentAnimationRoutine = null;
            m_CurrentAnimationData = null;
            m_CurrentAction = null;

            if (m_QueuedAnimationData == null)
                return;

            AnimationClipData queuedData = m_QueuedAnimationData;
            m_QueuedAnimationData = null;

            DestroyAllClipInputs();
            PlayAnimationImmediately(queuedData);
        }

        private void SetAnimatorOnly()
        {
            if (!m_Mixer.IsValid())
                return;

            m_Mixer.SetInputWeight(AnimatorInput, m_AnimatorControllerPlayable.IsValid() ? 1f : 0f);
            m_Mixer.SetInputWeight(ClipInputA, 0f);
            m_Mixer.SetInputWeight(ClipInputB, 0f);
        }

        private void SetAnimatorToClipWeights(int slot, float clipWeight)
        {
            if (!m_Mixer.IsValid())
                return;

            int input = GetClipInput(slot);
            float animatorWeight = m_AnimatorControllerPlayable.IsValid() ? 1f - clipWeight : 0f;

            m_Mixer.SetInputWeight(AnimatorInput, animatorWeight);
            m_Mixer.SetInputWeight(input, clipWeight);
            m_Mixer.SetInputWeight(GetOtherClipInput(slot), 0f);
        }

        private void SetOnlyClipWeight(int slot, float weight)
        {
            if (!m_Mixer.IsValid())
                return;

            int input = GetClipInput(slot);

            m_Mixer.SetInputWeight(AnimatorInput, 0f);
            m_Mixer.SetInputWeight(input, weight);
            m_Mixer.SetInputWeight(GetOtherClipInput(slot), 0f);
        }

        private void SetClipToClipWeights(int fromSlot, int toSlot, float t)
        {
            if (!m_Mixer.IsValid())
                return;

            m_Mixer.SetInputWeight(AnimatorInput, 0f);
            m_Mixer.SetInputWeight(GetClipInput(fromSlot), 1f - t);
            m_Mixer.SetInputWeight(GetClipInput(toSlot), t);
        }

        private int GetClipInput(int slot)
        {
            return slot == 0 ? ClipInputA : ClipInputB;
        }

        private int GetOtherClipInput(int slot)
        {
            return slot == 0 ? ClipInputB : ClipInputA;
        }

        private bool IsSlotValid(int slot)
        {
            return slot >= 0 &&
                   slot < m_ClipPlayables.Length &&
                   m_ClipPlayables[slot].IsValid();
        }

        private void DestroySlot(int slot)
        {
            if (slot < 0 || slot >= m_ClipPlayables.Length)
                return;

            int input = GetClipInput(slot);

            if (m_Mixer.IsValid())
            {
                m_Mixer.SetInputWeight(input, 0f);

                if (m_Mixer.GetInput(input).IsValid())
                    m_Graph.Disconnect(m_Mixer, input);
            }

            if (m_ClipPlayables[slot].IsValid())
                m_ClipPlayables[slot].Destroy();

            if (m_ActiveClipSlot == slot)
            {
                m_ActiveClipSlot = -1;
                m_CurrentClipPlayable = default;
            }
        }

        private void DestroyAllClipInputs()
        {
            DestroySlot(0);
            DestroySlot(1);
            SetAnimatorOnly();
        }

        private float GetDeltaTime(AnimationClipData.TimeMode mode)
        {
            return mode switch
            {
                AnimationClipData.TimeMode.Unscaled => Time.unscaledDeltaTime,
                AnimationClipData.TimeMode.Fixed => Time.fixedDeltaTime,
                AnimationClipData.TimeMode.UnscaledFixed => Time.fixedUnscaledDeltaTime,
                _ => Time.deltaTime
            };
        }

        private bool ShouldUseFixedUpdate(AnimationClipData.TimeMode mode)
        {
            return mode is AnimationClipData.TimeMode.Fixed or AnimationClipData.TimeMode.UnscaledFixed;
        }

        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_Animator = GetComponent<Animator>();

            m_Graph = PlayableGraph.Create("AnimationClipPlayerGraph");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            m_Mixer = AnimationLayerMixerPlayable.Create(m_Graph, 3);

            if (m_Animator.runtimeAnimatorController != null)
            {
                m_AnimatorControllerPlayable = AnimatorControllerPlayable.Create(
                    m_Graph,
                    m_Animator.runtimeAnimatorController
                );

                m_Graph.Connect(m_AnimatorControllerPlayable, 0, m_Mixer, AnimatorInput);
            }

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(m_Graph, "AnimationOutput", m_Animator);
            output.SetSourcePlayable(m_Mixer);

            m_Mixer.SetInputWeight(AnimatorInput, m_AnimatorControllerPlayable.IsValid() ? 1f : 0f);
            m_Mixer.SetInputWeight(ClipInputA, 0f);
            m_Mixer.SetInputWeight(ClipInputB, 0f);

            m_Graph.Play();

            m_Initialized = true;
        }

        private void OnDestroy()
        {
            StopCurrentAnimationRoutine();
            DestroyAllClipInputs();

            if (m_Graph.IsValid())
                m_Graph.Destroy();
        }
    }
}