using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Graphs;
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
        public float Delay = 0f; // New Delay field
        [BoxGroup("Clip Settings")]
        public TimeMode UpdateMode = TimeMode.Scaled;
        [BoxGroup("Clip Settings"), Tooltip("Negative number is infinite. 1 == played once")]
        public int LoopCount = 1;
        [BoxGroup("Clip Settings")]
        public bool PauseOnLastFrame = false;

        [BoxGroup("Actions"), OdinSerialize, ShowInInspector]
        public IAction Action;
        [BoxGroup("Events")]
        public UnityEvent OnAnimationStart;
        [BoxGroup("Events")]
        public UnityEvent OnAnimationEnd;
    }



    [RequireComponent(typeof(Animator))]
    public class AnimationClipPlayer : MonoBehaviour
    {
        private PlayableGraph m_Graph;
        private AnimationLayerMixerPlayable m_Mixer;
        private Animator m_Animator;
        private bool m_Initialized = false;

        private Coroutine m_CurrentAnimationRoutine;
        private AnimationClipPlayable m_CurrentClipPlayable;

        void Awake() => Initialize();

        public void Pause()
        {
            if (m_Graph.IsValid())
            {
                m_Graph.Stop(); 
            }
        }

        public void Resume()
        {
            if (m_Graph.IsValid())
            {
                m_Graph.Play(); 
            }
        }

        public bool IsPaused()
        {
            return m_Graph.IsValid() && !m_Graph.IsPlaying();
        }


        public void SetCurrentClipSpeed(float speed)
        {
            if (m_CurrentClipPlayable.IsValid())
            {
                m_CurrentClipPlayable.SetSpeed(speed);
            }
        }

        public float GetCurrentClipSpeed()
        {
            if (m_CurrentClipPlayable.IsValid())
            {
                return (float)m_CurrentClipPlayable.GetSpeed();
            }
            return 1f;
        }

        public void PlayClip(AnimationClip clip, float transitionTime, int loopCount)
        {
            AnimationClipData data = new();
            data.Clip = clip;
            data.TransitionTime = transitionTime;
            data.LoopCount = loopCount;

            PlayClip(data);
        }

        public void PlayClip(AnimationClipData data)
        {
            if (data == null || data.Clip == null) return;

            // Stop existing routine to allow the new one to take control of weights
            if (m_CurrentAnimationRoutine != null)
            {
                StopCoroutine(m_CurrentAnimationRoutine);
            }

            m_CurrentAnimationRoutine = StartCoroutine(ExecuteAnimationData(data));
        }
        private IEnumerator ExecuteAnimationData(AnimationClipData data)
        {
            Initialize();

            // 1. Delay logic
            float delayRemaining = data.Delay;
            while (delayRemaining > 0)
            {
                delayRemaining -= GetDeltaTime(data.UpdateMode);
                yield return data.UpdateMode is AnimationClipData.TimeMode.Fixed or AnimationClipData.TimeMode.UnscaledFixed
                    ? new WaitForFixedUpdate()
                    : null;
            }

            // 2. Setup Graph and Playable
            m_Graph.SetTimeUpdateMode(
                (data.UpdateMode is AnimationClipData.TimeMode.Unscaled or AnimationClipData.TimeMode.UnscaledFixed)
                ? DirectorUpdateMode.UnscaledGameTime
                : DirectorUpdateMode.GameTime
            );

            IAction action = data.Action?.Clone();
            action?.SetGameObject(gameObject);

            // Cleanup before starting new clip
            if (m_Mixer.GetInput(1).IsValid()) m_Graph.Disconnect(m_Mixer, 1);
            if (m_CurrentClipPlayable.IsValid()) m_CurrentClipPlayable.Destroy();

            m_CurrentClipPlayable = AnimationClipPlayable.Create(m_Graph, data.Clip);
            m_CurrentClipPlayable.SetSpeed(data.ClipSpeed);
            m_CurrentClipPlayable.SetTime(0);
            m_Graph.Connect(m_CurrentClipPlayable, 0, m_Mixer, 1);

            data.OnAnimationStart?.Invoke();
            action?.StartAction();

            int currentLoop = 0;
            float transitionTime = Mathf.Max(0.001f, data.TransitionTime);

            // 3. Playback Loop
            while (data.LoopCount < 0 || currentLoop < data.LoopCount)
            {
                float duration = data.Clip.length;
                float elapsed = 0f;

                if (currentLoop > 0) m_CurrentClipPlayable.SetTime(0);

                while (elapsed < duration)
                {
                    float dt = GetDeltaTime(data.UpdateMode);
                    float logicDelta = dt * data.ClipSpeed;

                    float weight = 1f;
                    if (currentLoop == 0 && elapsed < transitionTime)
                        weight = elapsed / transitionTime;
                    // Only fade out if NOT pausing on last frame OR if it's not the final loop
                    else if (!data.PauseOnLastFrame && data.LoopCount > 0 && currentLoop == data.LoopCount - 1 && elapsed > duration - transitionTime)
                        weight = (duration - elapsed) / transitionTime;

                    m_Mixer.SetInputWeight(1, weight);
                    m_Mixer.SetInputWeight(0, 1f - weight);

                    action?.UpdateAction();
                    elapsed += logicDelta;

                    yield return data.UpdateMode is AnimationClipData.TimeMode.Fixed or AnimationClipData.TimeMode.UnscaledFixed
                        ? new WaitForFixedUpdate()
                        : null;
                }
                currentLoop++;
            }

            // 4. Finalization
            if (data.PauseOnLastFrame)
            {
                // Keep weight at 1 and freeze time at the very end
                m_Mixer.SetInputWeight(1, 1f);
                m_Mixer.SetInputWeight(0, 0f);
                m_CurrentClipPlayable.SetTime(data.Clip.length);
                m_CurrentClipPlayable.SetSpeed(0); // Freeze the clip

                action?.EndAction();
                data.OnAnimationEnd?.Invoke();

                // We keep the routine reference null so new animations can take over,
                // but we DON'T destroy the playable yet.
                m_CurrentAnimationRoutine = null;
            }
            else
            {
                FinishAnimation(action, data);
            }
        }

        private void FinishAnimation(IAction action, AnimationClipData data)
        {
            m_Mixer.SetInputWeight(1, 0f);
            m_Mixer.SetInputWeight(0, 1f);

            action?.EndAction();
            data.OnAnimationEnd?.Invoke();

            if (m_CurrentClipPlayable.IsValid()) m_CurrentClipPlayable.Destroy();
            m_CurrentAnimationRoutine = null;
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

        public void PlaySequence(List<AnimationClipData> sequence, AnimationSequenceType type, int loopCount)
        {
            if (m_CurrentAnimationRoutine != null) StopCoroutine(m_CurrentAnimationRoutine);
            m_CurrentAnimationRoutine = StartCoroutine(ExecuteSequenceRoutine(sequence, type, loopCount));
        }
        private IEnumerator ExecuteSequenceRoutine(List<AnimationClipData> sequence, AnimationSequenceType type, int loopCount)
        {
            int totalSequenceLoops = 0;
            bool isInfinite = loopCount < 0;
            int lastRandomIndex = -1; // Tracking to avoid consecutive repetitions

            while (isInfinite || totalSequenceLoops < loopCount)
            {
                switch (type)
                {
                    case AnimationSequenceType.Random:
                        for (int i = 0; i < sequence.Count; i++)
                        {
                            int randomIndex = lastRandomIndex;

                            // Only try to find a different index if there's more than one clip
                            if (sequence.Count > 1)
                            {
                                while (randomIndex == lastRandomIndex)
                                {
                                    randomIndex = UnityEngine.Random.Range(0, sequence.Count);
                                }
                            }
                            else
                            {
                                randomIndex = 0;
                            }

                            lastRandomIndex = randomIndex;
                            yield return StartCoroutine(ExecuteAnimationData(sequence[randomIndex]));
                        }
                        break;

                    case AnimationSequenceType.ForwardThenBackward:
                        // Forward
                        for (int i = 0; i < sequence.Count; i++)
                        {
                            yield return StartCoroutine(ExecuteAnimationData(sequence[i]));
                        }
                        // Backward (avoiding double-playing first/last)
                        for (int i = sequence.Count - 2; i >= 1; i--)
                        {
                            yield return StartCoroutine(ExecuteAnimationData(sequence[i]));
                        }
                        break;

                    case AnimationSequenceType.Loop:
                    case AnimationSequenceType.Stop:
                        for (int i = 0; i < sequence.Count; i++)
                        {
                            yield return StartCoroutine(ExecuteAnimationData(sequence[i]));
                        }
                        break;
                }

                totalSequenceLoops++;

                if (type == AnimationSequenceType.Stop)
                    break;
            }

            m_CurrentAnimationRoutine = null;
        }

        private void Initialize()
        {
            if (m_Initialized) return;

            m_Animator = GetComponent<Animator>();
            m_Graph = PlayableGraph.Create("AnimationClipPlayerGraph");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // Mixer with 2 inputs: 0 for Animator Controller, 1 for Custom Clips
            m_Mixer = AnimationLayerMixerPlayable.Create(m_Graph, 2);

            var output = AnimationPlayableOutput.Create(m_Graph, "AnimationOutput", m_Animator);
            output.SetSourcePlayable(m_Mixer);

            m_Mixer.SetInputWeight(0, 1f);
            m_Mixer.SetInputWeight(1, 0f);

            m_Graph.Play();
            m_Initialized = true;
        }

        void OnDestroy()
        {
            if (m_Graph.IsValid()) m_Graph.Destroy();
        }
    }


}
