using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace SilverPillar.Animation
{
    [Serializable]
    public class AnimationClipData
    {
        [BoxGroup("Clip Settings")]
        public AnimationClip Clip;
        [BoxGroup("Clip Settings"), Min(0)]
        public float TransitionTime = 0.2f;
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
        void Awake()
        {
            Initialize();
        }

        public void PlayOneShot(AnimationClip clip)
        {
            Initialize();

            // 4. Creamos el clip "jugable"
            var clipPlayable = AnimationClipPlayable.Create(m_Graph, clip);

            // 5. Reemplazamos la conexión en la entrada 0
            m_Graph.Disconnect(m_Mixer, 0);
            m_Graph.Connect(clipPlayable, 0, m_Mixer, 0);

            // Reiniciamos el tiempo del clip
            clipPlayable.SetTime(0);
            m_Mixer.SetInputWeight(0, 1f);
        }

        public void PlayOneShot(AnimationClipData data)
        {
            if (data == null || data.Clip == null) return;

            // Cancelar animación previa para evitar solapamiento de IActions
            if (m_CurrentAnimationRoutine != null)
            {
                StopCoroutine(m_CurrentAnimationRoutine);
            }

            m_CurrentAnimationRoutine = StartCoroutine(ExecuteAnimationData(data));
        }
        private IEnumerator ExecuteAnimationData(AnimationClipData data)
        {
            Initialize();

            IAction action = null;

            if (data.Action != null)
            {
                action = data.Action.Clone();
                action.SetGameObject(gameObject);
            }

            // Clean previous playable connected to input 1
            if (m_Mixer.GetInput(1).IsValid())
            {
                m_Graph.Disconnect(m_Mixer, 1);
            }

            if (m_CurrentClipPlayable.IsValid())
            {
                m_CurrentClipPlayable.Destroy();
            }

            m_CurrentClipPlayable = AnimationClipPlayable.Create(m_Graph, data.Clip);
            m_CurrentClipPlayable.SetTime(0);

            m_Graph.Connect(m_CurrentClipPlayable, 0, m_Mixer, 1);

            data.OnAnimationStart?.Invoke();
            action?.StartAction();

            float duration = data.Clip.length;
            float elapsed = 0f;
            float transitionTime = Mathf.Max(0.001f, data.TransitionTime);

            while (elapsed < duration)
            {
                float currentWeight = 1f;

                if (elapsed < transitionTime)
                {
                    currentWeight = elapsed / transitionTime;
                }
                else if (elapsed > duration - transitionTime)
                {
                    currentWeight = (duration - elapsed) / transitionTime;
                }

                currentWeight = Mathf.Clamp01(currentWeight);

                m_Mixer.SetInputWeight(1, currentWeight);
                m_Mixer.SetInputWeight(0, 1f - currentWeight);

                action?.UpdateAction();

                elapsed += Time.deltaTime;
                yield return null;
            }

            m_Mixer.SetInputWeight(1, 0f);
            m_Mixer.SetInputWeight(0, 1f);

            action?.EndAction();
            data.OnAnimationEnd?.Invoke();

            if (m_Mixer.GetInput(1).IsValid())
            {
                m_Graph.Disconnect(m_Mixer, 1);
            }

            if (m_CurrentClipPlayable.IsValid())
            {
                m_CurrentClipPlayable.Destroy();
            }

            m_CurrentAnimationRoutine = null;
        }

        void OnDestroy()
        {
            // 6. Limpieza vital para evitar fugas de memoria
            if (m_Graph.IsValid()) m_Graph.Destroy();
        }

        private void Initialize()
        {
            if (m_Initialized) return;

            m_Animator = GetComponent<Animator>();

            m_Graph = PlayableGraph.Create("DinoGraph");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // Needs 2 inputs because you use input 0 and input 1
            m_Mixer = AnimationLayerMixerPlayable.Create(m_Graph, 2);

            var output = AnimationPlayableOutput.Create(m_Graph, "AnimationOutput", m_Animator);
            output.SetSourcePlayable(m_Mixer);

            m_Mixer.SetInputWeight(0, 1f);
            m_Mixer.SetInputWeight(1, 0f);

            m_Graph.Play();

            m_Initialized = true;
        }
    }
}
